using System.Net.Http.Headers;
using System.Text.Json;
using DevExpress.ExpressApp;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects.Email;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Mail;

/// <summary>
/// Microsoft identity platform token exchange plus encrypted refresh-token storage in
/// <see cref="OfficerMailConnection"/>. Access tokens are cached in memory only.
/// </summary>
public sealed class OfficerMailTokenService
{
    public const string HttpClientName = "VisaOfficerMail";

    private const string RefreshTokenPurpose = "Visa2026.OfficerMail.RefreshToken.v1";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly INonSecuredObjectSpaceFactory objectSpaceFactory;
    private readonly IDataProtector refreshTokenProtector;
    private readonly IMemoryCache cache;
    private readonly MicrosoftGraphMailOptions options;
    private readonly ILogger<OfficerMailTokenService> logger;

    public OfficerMailTokenService(
        IHttpClientFactory httpClientFactory,
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IDataProtectionProvider dataProtectionProvider,
        IMemoryCache cache,
        IOptions<MicrosoftGraphMailOptions> options,
        ILogger<OfficerMailTokenService> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.objectSpaceFactory = objectSpaceFactory;
        refreshTokenProtector = dataProtectionProvider.CreateProtector(RefreshTokenPurpose);
        this.cache = cache;
        this.options = options.Value;
        this.logger = logger;
    }

    public sealed record TokenResponse(string AccessToken, string? RefreshToken, int ExpiresInSeconds);

    public async Task<TokenResponse> RedeemCodeAsync(string code, string redirectUri, string codeVerifier, CancellationToken ct)
    {
        return await PostTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier,
            ["scope"] = MicrosoftGraphMailOptions.Scopes,
        }, ct);
    }

    public async Task SaveConnectionAsync(Guid userId, TokenResponse token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
            throw new OfficerMailException("Microsoft did not return a refresh token (offline_access was not granted).");

        var (email, displayName) = await GetMailboxAsync(token.AccessToken, ct);

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace<OfficerMailConnection>();
        var connection = objectSpace.GetObjectsQuery<OfficerMailConnection>().FirstOrDefault(c => c.UserID == userId)
            ?? objectSpace.CreateObject<OfficerMailConnection>();
        connection.UserID = userId;
        connection.AccountEmail = email;
        connection.AccountDisplayName = displayName;
        connection.ProtectedRefreshToken = refreshTokenProtector.Protect(token.RefreshToken);
        connection.ConnectedOnUtc = DateTime.UtcNow;
        objectSpace.CommitChanges();

        CacheAccessToken(userId, token);
    }

    public OfficerMailConnectionInfo? GetConnection(Guid userId)
    {
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace<OfficerMailConnection>();
        var connection = objectSpace.GetObjectsQuery<OfficerMailConnection>().FirstOrDefault(c => c.UserID == userId);
        return connection == null || string.IsNullOrWhiteSpace(connection.ProtectedRefreshToken)
            ? null
            : new OfficerMailConnectionInfo { AccountEmail = connection.AccountEmail, AccountDisplayName = connection.AccountDisplayName };
    }

    public void Disconnect(Guid userId)
    {
        cache.Remove(CacheKey(userId));
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace<OfficerMailConnection>();
        foreach (var connection in objectSpace.GetObjectsQuery<OfficerMailConnection>().Where(c => c.UserID == userId).ToList())
            objectSpace.Delete(connection);
        objectSpace.CommitChanges();
    }

    /// <summary>Cached access token, or a fresh one from the stored refresh token.</summary>
    public async Task<string> GetAccessTokenAsync(Guid userId, CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey(userId), out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace<OfficerMailConnection>();
        var connection = objectSpace.GetObjectsQuery<OfficerMailConnection>().FirstOrDefault(c => c.UserID == userId);
        if (connection == null || string.IsNullOrWhiteSpace(connection.ProtectedRefreshToken))
            throw new OfficerMailException("Outlook is not connected.", connectionLost: true);

        string refreshToken;
        try
        {
            refreshToken = refreshTokenProtector.Unprotect(connection.ProtectedRefreshToken);
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            logger.LogWarning(ex, "Officer mail: stored refresh token for {UserId} cannot be decrypted (data protection keys changed).", userId);
            throw new OfficerMailException("Your Outlook connection expired. Connect again.", connectionLost: true, ex);
        }

        TokenResponse token;
        try
        {
            token = await PostTokenAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["scope"] = MicrosoftGraphMailOptions.Scopes,
            }, ct);
        }
        catch (OfficerMailException ex) when (ex.ConnectionLost)
        {
            objectSpace.Delete(connection);
            objectSpace.CommitChanges();
            throw;
        }

        if (!string.IsNullOrWhiteSpace(token.RefreshToken))
            connection.ProtectedRefreshToken = refreshTokenProtector.Protect(token.RefreshToken);
        connection.LastUsedOnUtc = DateTime.UtcNow;
        objectSpace.CommitChanges();

        CacheAccessToken(userId, token);
        return token.AccessToken;
    }

    private async Task<TokenResponse> PostTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        if (!options.IsConfigured)
            throw new OfficerMailException("Email sending is not configured on this server.");

        form["client_id"] = options.ClientId!;
        form["client_secret"] = options.ClientSecret!;

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.PostAsync($"{options.AuthorityBase}/token", new FormUrlEncodedContent(form), ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        var root = document.RootElement;

        if (!response.IsSuccessStatusCode)
        {
            var error = root.TryGetProperty("error", out var e) ? e.GetString() : null;
            var description = root.TryGetProperty("error_description", out var d) ? d.GetString() : null;
            logger.LogWarning("Officer mail: token endpoint returned {Status} {Error}: {Description}", (int)response.StatusCode, error, description);
            bool lost = string.Equals(error, "invalid_grant", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(error, "interaction_required", StringComparison.OrdinalIgnoreCase);
            throw new OfficerMailException(
                lost ? "Your Outlook connection expired. Connect again." : $"Microsoft sign-in failed ({error}).",
                connectionLost: lost);
        }

        return new TokenResponse(
            root.GetProperty("access_token").GetString()!,
            root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            root.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var seconds) ? seconds : 3600);
    }

    private async Task<(string Email, string? DisplayName)> GetMailboxAsync(string accessToken, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me?$select=displayName,mail,userPrincipalName");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        string? mail = root.TryGetProperty("mail", out var m) ? m.GetString() : null;
        string? upn = root.TryGetProperty("userPrincipalName", out var u) ? u.GetString() : null;
        string? name = root.TryGetProperty("displayName", out var n) ? n.GetString() : null;
        return (string.IsNullOrWhiteSpace(mail) ? upn ?? string.Empty : mail, name);
    }

    private void CacheAccessToken(Guid userId, TokenResponse token) =>
        cache.Set(CacheKey(userId), token.AccessToken, TimeSpan.FromSeconds(Math.Max(60, token.ExpiresInSeconds - 120)));

    private static string CacheKey(Guid userId) => $"visa-officer-mail-token:{userId:N}";
}
