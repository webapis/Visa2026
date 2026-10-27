using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Visa2026.Module.Services.EmailDocumentCopies;

public sealed class EmailRecipient
{
    public string Email { get; init; } = string.Empty;

    public string? Name { get; init; }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? Email : $"{Name} <{Email}>";
}

public sealed class OfficerMailAttachment
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/pdf";

    public byte[] Content { get; init; } = Array.Empty<byte>();
}

public sealed class OfficerMailMessage
{
    public string Subject { get; init; } = string.Empty;

    /// <summary>Plain text; sent as a text body so officer input is never interpreted as HTML.</summary>
    public string Body { get; init; } = string.Empty;

    public IReadOnlyList<EmailRecipient> To { get; init; } = Array.Empty<EmailRecipient>();

    public IReadOnlyList<EmailRecipient> Cc { get; init; } = Array.Empty<EmailRecipient>();

    /// <summary>Hidden copy recipients (the sending officer when mail goes out from a shared account).</summary>
    public IReadOnlyList<EmailRecipient> Bcc { get; init; } = Array.Empty<EmailRecipient>();

    /// <summary>Where replies go when the From address is a shared account.</summary>
    public EmailRecipient? ReplyTo { get; init; }

    public IReadOnlyList<OfficerMailAttachment> Attachments { get; init; } = Array.Empty<OfficerMailAttachment>();
}

public sealed class OfficerMailConnectionInfo
{
    public string AccountEmail { get; init; } = string.Empty;

    public string? AccountDisplayName { get; init; }
}

/// <summary>Send failed; <see cref="Exception.Message"/> is safe to show to the officer.</summary>
public sealed class OfficerMailException : Exception
{
    public OfficerMailException(string message, bool connectionLost = false, Exception? inner = null)
        : base(message, inner)
    {
        ConnectionLost = connectionLost;
    }

    /// <summary>True when the stored Microsoft sign-in no longer works and the officer must reconnect.</summary>
    public bool ConnectionLost { get; }
}

/// <summary>
/// Sends document-copies mail. Implemented in the Blazor host: a shared SMTP account (default) or the
/// officer's own Office 365 mailbox through Microsoft Graph; see <c>docs/EMAIL_DOCUMENT_COPIES.md</c>.
/// </summary>
public interface IOfficerMailSender
{
    /// <summary>False when the transport (SMTP server or Microsoft app registration) is not configured on this host.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// True when each officer must connect their own mailbox first (Microsoft Graph). False for a shared
    /// sending account (SMTP), where <see cref="GetConnectionAsync"/> returns that account for every user.
    /// </summary>
    bool RequiresUserConnection { get; }

    long MaxTotalAttachmentBytes { get; }

    /// <summary>Relative URL that starts the Microsoft sign-in for <paramref name="userId"/>; open it in a new tab.</summary>
    string CreateConnectUrl(Guid userId);

    Task<OfficerMailConnectionInfo?> GetConnectionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task DisconnectAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <exception cref="OfficerMailException">The server rejected the message or the connection is gone.</exception>
    Task SendAsync(Guid userId, OfficerMailMessage message, CancellationToken cancellationToken = default);

    /// <summary>Connects and logs in (SMTP) without sending. Null when the check succeeded.</summary>
    Task<string?> TestConnectionAsync(CancellationToken cancellationToken = default);
}
