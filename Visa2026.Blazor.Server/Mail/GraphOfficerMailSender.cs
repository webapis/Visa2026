using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Mail;

/// <summary>
/// Sends from the officer's own mailbox through Microsoft Graph. Small messages use
/// <c>/me/sendMail</c>; when total attachments exceed the inline limit the message is built as a
/// draft and large files go through an attachment upload session, then the draft is sent.
/// Both paths keep a copy in the officer's Sent Items.
/// </summary>
public sealed class GraphOfficerMailSender : IOfficerMailSender
{
    public const string ConnectLinkPurpose = "Visa2026.OfficerMail.ConnectLink.v1";

    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private const int InlineRequestLimitBytes = 3 * 1024 * 1024;
    private const int UploadChunkBytes = 10 * 327680;

    private readonly OfficerMailTokenService tokens;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ITimeLimitedDataProtector connectLinkProtector;
    private readonly MicrosoftGraphMailOptions options;
    private readonly ILogger<GraphOfficerMailSender> logger;

    public GraphOfficerMailSender(
        OfficerMailTokenService tokens,
        IHttpClientFactory httpClientFactory,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<MicrosoftGraphMailOptions> options,
        ILogger<GraphOfficerMailSender> logger)
    {
        this.tokens = tokens;
        this.httpClientFactory = httpClientFactory;
        connectLinkProtector = dataProtectionProvider.CreateProtector(ConnectLinkPurpose).ToTimeLimitedDataProtector();
        this.options = options.Value;
        this.logger = logger;
    }

    public bool IsConfigured => options.IsConfigured;

    public bool RequiresUserConnection => true;

    public long MaxTotalAttachmentBytes => options.MaxTotalAttachmentBytes > 0 ? options.MaxTotalAttachmentBytes : 20L * 1024 * 1024;

    public string CreateConnectUrl(Guid userId) =>
        $"{MicrosoftGraphMailOptions.ConnectPath}?t={Uri.EscapeDataString(connectLinkProtector.Protect(userId.ToString("N"), TimeSpan.FromMinutes(10)))}";

    public Task<OfficerMailConnectionInfo?> GetConnectionAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(tokens.GetConnection(userId));

    public Task DisconnectAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        tokens.Disconnect(userId);
        return Task.CompletedTask;
    }

    public async Task SendAsync(Guid userId, OfficerMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var accessToken = await tokens.GetAccessTokenAsync(userId, cancellationToken);
        var client = httpClientFactory.CreateClient(OfficerMailTokenService.HttpClientName);

        long attachmentBytes = message.Attachments.Sum(a => (long)a.Content.Length);
        // Base64 grows content by a third; keep the whole sendMail request under Graph's 4 MB limit.
        if (attachmentBytes * 4 / 3 < InlineRequestLimitBytes)
        {
            var payload = new
            {
                message = BuildMessage(message, includeAttachments: true),
                saveToSentItems = true,
            };
            await SendGraphAsync(client, HttpMethod.Post, $"{GraphBase}/me/sendMail", accessToken, payload, cancellationToken);
            return;
        }

        var draft = await SendGraphAsync(client, HttpMethod.Post, $"{GraphBase}/me/messages", accessToken,
            BuildMessage(message, includeAttachments: false), cancellationToken);
        var draftId = draft.GetProperty("id").GetString()!;
        try
        {
            foreach (var attachment in message.Attachments)
            {
                if (attachment.Content.Length * 4L / 3 < InlineRequestLimitBytes)
                {
                    await SendGraphAsync(client, HttpMethod.Post, $"{GraphBase}/me/messages/{draftId}/attachments", accessToken,
                        FileAttachment(attachment), cancellationToken);
                }
                else
                {
                    await UploadLargeAttachmentAsync(client, accessToken, draftId, attachment, cancellationToken);
                }
            }

            await SendGraphAsync(client, HttpMethod.Post, $"{GraphBase}/me/messages/{draftId}/send", accessToken, null, cancellationToken);
        }
        catch
        {
            await TryDeleteDraftAsync(client, accessToken, draftId);
            throw;
        }
    }

    private async Task UploadLargeAttachmentAsync(HttpClient client, string accessToken, string draftId, OfficerMailAttachment attachment, CancellationToken ct)
    {
        var session = await SendGraphAsync(client, HttpMethod.Post,
            $"{GraphBase}/me/messages/{draftId}/attachments/createUploadSession", accessToken,
            new
            {
                AttachmentItem = new
                {
                    attachmentType = "file",
                    name = attachment.FileName,
                    size = attachment.Content.Length,
                    contentType = attachment.ContentType,
                },
            }, ct);
        var uploadUrl = session.GetProperty("uploadUrl").GetString()!;

        int total = attachment.Content.Length;
        for (int offset = 0; offset < total; offset += UploadChunkBytes)
        {
            int length = Math.Min(UploadChunkBytes, total - offset);
            using var chunk = new ByteArrayContent(attachment.Content, offset, length);
            chunk.Headers.ContentLength = length;
            chunk.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + length - 1, total);
            // The upload URL carries its own auth token; Graph rejects an Authorization header here.
            using var response = await client.PutAsync(uploadUrl, chunk, ct);
            if (!response.IsSuccessStatusCode)
                throw await ToExceptionAsync(response, ct);
        }
    }

    private async Task<JsonElement> SendGraphAsync(HttpClient client, HttpMethod method, string url, string accessToken, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = body == null
            ? new StringContent(string.Empty)
            : JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await ToExceptionAsync(response, ct);

        var text = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    private async Task<OfficerMailException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        string? graphMessage = null;
        string? code = null;
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                code = error.TryGetProperty("code", out var c) ? c.GetString() : null;
                graphMessage = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            }
        }
        catch (JsonException)
        {
        }

        logger.LogWarning("Officer mail: Graph returned {Status} {Code}: {Message}", (int)response.StatusCode, code, graphMessage);
        bool lost = response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
        return new OfficerMailException(
            string.IsNullOrWhiteSpace(graphMessage) ? $"Microsoft Graph returned {(int)response.StatusCode}." : graphMessage!,
            connectionLost: lost);
    }

    private static async Task TryDeleteDraftAsync(HttpClient client, string accessToken, string draftId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{GraphBase}/me/messages/{draftId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var _ = await client.SendAsync(request);
        }
        catch (HttpRequestException)
        {
        }
    }

    private static Dictionary<string, object?> BuildMessage(OfficerMailMessage message, bool includeAttachments)
    {
        var result = new Dictionary<string, object?>
        {
            ["subject"] = message.Subject,
            ["body"] = new { contentType = "Text", content = message.Body },
            ["toRecipients"] = message.To.Select(Recipient).ToList(),
            ["ccRecipients"] = message.Cc.Select(Recipient).ToList(),
        };
        if (includeAttachments && message.Attachments.Count > 0)
            result["attachments"] = message.Attachments.Select(FileAttachment).ToList();
        return result;
    }

    private static object Recipient(EmailRecipient recipient) => new
    {
        emailAddress = new { address = recipient.Email, name = string.IsNullOrWhiteSpace(recipient.Name) ? recipient.Email : recipient.Name },
    };

    private static Dictionary<string, object> FileAttachment(OfficerMailAttachment attachment) => new()
    {
        ["@odata.type"] = "#microsoft.graph.fileAttachment",
        ["name"] = attachment.FileName,
        ["contentType"] = attachment.ContentType,
        ["contentBytes"] = Convert.ToBase64String(attachment.Content),
    };
}
