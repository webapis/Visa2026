namespace Visa2026.Blazor.Server.Mail;

/// <summary>
/// Microsoft Entra app registration used to send document-copies email from each officer's own
/// Office 365 mailbox (delegated permissions, no tenant admin consent required by default).
/// Bound from <c>MicrosoftGraphMail</c>; keep <see cref="ClientSecret"/> in environment variables
/// (<c>MicrosoftGraphMail__ClientSecret</c>), never in tracked appsettings.
/// </summary>
public sealed class MicrosoftGraphMailOptions
{
    public const string SectionName = "MicrosoftGraphMail";

    public const string CallbackPath = "/visa-mail/callback";

    public const string ConnectPath = "/visa-mail/connect";

    /// <summary>Delegated scopes. Mail.ReadWrite is needed only to upload attachments larger than 3 MB to a draft.</summary>
    public const string Scopes = "offline_access openid profile User.Read Mail.Send Mail.ReadWrite";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    /// <summary><c>organizations</c> (any work account) or the company tenant id / domain.</summary>
    public string Tenant { get; set; } = "organizations";

    /// <summary>
    /// Public base URL of this slot (for example <c>https://visa.calik.local</c>) used to build the
    /// redirect URI registered in Entra. When empty the incoming request scheme and host are used.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public long MaxTotalAttachmentBytes { get; set; } = 20L * 1024 * 1024;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    public string AuthorityBase => $"https://login.microsoftonline.com/{(string.IsNullOrWhiteSpace(Tenant) ? "organizations" : Tenant.Trim())}/oauth2/v2.0";
}
