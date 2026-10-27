using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Mail;

/// <summary>
/// <c>/visa-mail/connect</c> and <c>/visa-mail/callback</c>: Microsoft authorization-code flow with PKCE
/// that stores the officer's delegated Graph consent. The connect link is minted inside the signed-in
/// Blazor circuit (time-limited, data-protected user id); a browser-bound cookie carries the PKCE
/// verifier and state so a callback cannot be replayed from another browser.
/// </summary>
public static class OfficerMailEndpoints
{
    private const string FlowCookieName = "visa_mail_oauth";
    private const string FlowPurpose = "Visa2026.OfficerMail.Flow.v1";

    private sealed record FlowState(string UserId, string Verifier, string State);

    public static IEndpointRouteBuilder MapVisaOfficerMail(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(MicrosoftGraphMailOptions.ConnectPath, Connect);
        endpoints.MapGet(MicrosoftGraphMailOptions.CallbackPath, CallbackAsync);
        return endpoints;
    }

    private static IResult Connect(HttpContext context, IOptions<MicrosoftGraphMailOptions> optionsAccessor, IDataProtectionProvider dataProtection)
    {
        var options = optionsAccessor.Value;
        if (!options.IsConfigured)
            return ResultPage(false, VisaUiMessages.Get("EmailDocumentCopies.Connect.NotConfigured"));

        string? userId;
        try
        {
            var linkProtector = dataProtection.CreateProtector(GraphOfficerMailSender.ConnectLinkPurpose).ToTimeLimitedDataProtector();
            userId = linkProtector.Unprotect(context.Request.Query["t"].ToString());
        }
        catch (CryptographicException)
        {
            userId = null;
        }

        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParseExact(userId, "N", out _))
            return ResultPage(false, VisaUiMessages.Get("EmailDocumentCopies.Connect.LinkExpired"));

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        var flowProtector = dataProtection.CreateProtector(FlowPurpose).ToTimeLimitedDataProtector();
        context.Response.Cookies.Append(
            FlowCookieName,
            flowProtector.Protect(JsonSerializer.Serialize(new FlowState(userId, verifier, state)), TimeSpan.FromMinutes(15)),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/visa-mail",
                MaxAge = TimeSpan.FromMinutes(15),
            });

        var authorizeUrl = QueryHelpers.AddQueryString($"{options.AuthorityBase}/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = BuildRedirectUri(context, options),
            ["response_mode"] = "query",
            ["scope"] = MicrosoftGraphMailOptions.Scopes,
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account",
        });
        return Results.Redirect(authorizeUrl);
    }

    private static async Task<IResult> CallbackAsync(
        HttpContext context,
        IOptions<MicrosoftGraphMailOptions> optionsAccessor,
        IDataProtectionProvider dataProtection,
        OfficerMailTokenService tokens,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(OfficerMailEndpoints));
        var options = optionsAccessor.Value;
        var query = context.Request.Query;

        FlowState? flow = null;
        if (context.Request.Cookies.TryGetValue(FlowCookieName, out var cookie))
        {
            try
            {
                var flowProtector = dataProtection.CreateProtector(FlowPurpose).ToTimeLimitedDataProtector();
                flow = JsonSerializer.Deserialize<FlowState>(flowProtector.Unprotect(cookie));
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException)
            {
                flow = null;
            }
        }
        context.Response.Cookies.Delete(FlowCookieName, new CookieOptions { Path = "/visa-mail" });

        var error = query["error"].ToString();
        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("Officer mail connect: Microsoft returned {Error}: {Description}", error, query["error_description"].ToString());
            var description = query["error_description"].ToString();
            bool needsAdmin = description.Contains("AADSTS65001", StringComparison.Ordinal)
                              || description.Contains("AADSTS90094", StringComparison.Ordinal)
                              || description.Contains("admin", StringComparison.OrdinalIgnoreCase);
            return ResultPage(false, VisaUiMessages.Get(needsAdmin
                ? "EmailDocumentCopies.Connect.NeedsAdmin"
                : "EmailDocumentCopies.Connect.Cancelled"));
        }

        if (flow == null
            || !Guid.TryParseExact(flow.UserId, "N", out var userId)
            || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(flow.State), Encoding.ASCII.GetBytes(query["state"].ToString())))
        {
            return ResultPage(false, VisaUiMessages.Get("EmailDocumentCopies.Connect.LinkExpired"));
        }

        try
        {
            var token = await tokens.RedeemCodeAsync(query["code"].ToString(), BuildRedirectUri(context, options), flow.Verifier, context.RequestAborted);
            await tokens.SaveConnectionAsync(userId, token, context.RequestAborted);
            var connection = tokens.GetConnection(userId);
            return ResultPage(true, VisaUiMessages.Format("EmailDocumentCopies.Connect.Done", connection?.AccountEmail ?? string.Empty));
        }
        catch (Exception ex) when (ex is OfficerMailException or HttpRequestException)
        {
            logger.LogWarning(ex, "Officer mail connect failed for {UserId}.", userId);
            return ResultPage(false, VisaUiMessages.Format("EmailDocumentCopies.Connect.Failed", ex.Message));
        }
    }

    private static string BuildRedirectUri(HttpContext context, MicrosoftGraphMailOptions options)
    {
        var baseUrl = string.IsNullOrWhiteSpace(options.PublicBaseUrl)
            ? $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}"
            : options.PublicBaseUrl.TrimEnd('/');
        return baseUrl + MicrosoftGraphMailOptions.CallbackPath;
    }

    private static IResult ResultPage(bool success, string message)
    {
        var title = WebUtility.HtmlEncode(VisaUiMessages.Get(success ? "EmailDocumentCopies.Connect.TitleDone" : "EmailDocumentCopies.Connect.TitleFailed"));
        var body = WebUtility.HtmlEncode(message);
        var close = WebUtility.HtmlEncode(VisaUiMessages.Get("EmailDocumentCopies.Connect.CloseTab"));
        var color = success ? "#1b7f3b" : "#c62828";
        var html = $$"""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8"><title>{{title}}</title>
            <style>body{font-family:Segoe UI,Arial,sans-serif;background:#f5f6f8;display:flex;align-items:center;justify-content:center;height:100vh;margin:0}
            .card{background:#fff;border:1px solid #dde1e6;border-radius:10px;padding:28px 32px;max-width:460px;box-shadow:0 2px 10px rgba(0,0,0,.06)}
            h1{font-size:1.15rem;margin:0 0 10px;color:{{color}}}p{margin:0 0 16px;color:#333;line-height:1.45}button{padding:8px 16px;border-radius:6px;border:1px solid #0b5cd5;background:#0b5cd5;color:#fff;cursor:pointer}</style>
            </head><body><div class="card"><h1>{{title}}</h1><p>{{body}}</p><button type="button" onclick="window.close()">{{close}}</button></div>
            </body></html>
            """;
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
