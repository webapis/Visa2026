namespace Visa2026.Blazor.Server.Mail;

/// <summary>
/// Fallback SMTP settings when Configuration → Mail server is empty. Bound from <c>SmtpMail</c>.
/// Prefer the Mail server screen; keep <see cref="Password"/> out of tracked appsettings
/// (<c>SmtpMail__Password</c> env only).
/// </summary>
public sealed class SmtpMailOptions
{
    public const string SectionName = "SmtpMail";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    /// <summary><c>StartTls</c> (port 587), <c>SslOnConnect</c> (port 465), <c>Auto</c>, or <c>None</c> (LAN relay only).</summary>
    public string Security { get; set; } = "StartTls";

    /// <summary>Login name; leave empty for a relay that accepts mail without authentication.</summary>
    public string? UserName { get; set; }

    public string? Password { get; set; }

    /// <summary>From address. Office 365 rejects any address the <see cref="UserName"/> mailbox may not send as.</summary>
    public string? FromAddress { get; set; }

    public string FromName { get; set; } = "Visa2026";

    /// <summary>Send the officer a Bcc copy so the email is also in their own mailbox.</summary>
    public bool BccOfficer { get; set; } = true;

    public long MaxTotalAttachmentBytes { get; set; } = 20L * 1024 * 1024;

    public int TimeoutSeconds { get; set; } = 120;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
