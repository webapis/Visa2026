using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Mail;

/// <summary>
/// Sends document-copies email from one shared SMTP account (for example an Office 365 mailbox on
/// <c>smtp.office365.com:587</c>). The officer is Reply-To and gets a Bcc copy.
/// </summary>
public sealed class SmtpOfficerMailSender : IOfficerMailSender
{
    private readonly SmtpMailOptions options;
    private readonly ILogger<SmtpOfficerMailSender> logger;

    public SmtpOfficerMailSender(IOptions<SmtpMailOptions> options, ILogger<SmtpOfficerMailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public bool IsConfigured => options.IsConfigured;

    public bool RequiresUserConnection => false;

    public long MaxTotalAttachmentBytes => options.MaxTotalAttachmentBytes > 0 ? options.MaxTotalAttachmentBytes : 20L * 1024 * 1024;

    public string CreateConnectUrl(Guid userId) =>
        throw new NotSupportedException("The shared SMTP account needs no per-officer connection.");

    public Task<OfficerMailConnectionInfo?> GetConnectionAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(IsConfigured
            ? new OfficerMailConnectionInfo { AccountEmail = options.FromAddress!.Trim(), AccountDisplayName = options.FromName }
            : null);

    public Task DisconnectAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task SendAsync(Guid userId, OfficerMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsConfigured)
            throw new OfficerMailException("SMTP is not configured on this server.");

        var mime = BuildMessage(message);
        using var client = new SmtpClient { Timeout = Math.Max(10, options.TimeoutSeconds) * 1000 };
        try
        {
            await client.ConnectAsync(options.Host!.Trim(), options.Port, ResolveSecurity(), cancellationToken);
            if (!string.IsNullOrWhiteSpace(options.UserName))
                await client.AuthenticateAsync(options.UserName.Trim(), options.Password ?? string.Empty, cancellationToken);
            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (AuthenticationException ex)
        {
            logger.LogWarning(ex, "SMTP authentication failed for {UserName} on {Host}.", options.UserName, options.Host);
            throw new OfficerMailException(
                "The SMTP server rejected the username or password. For Office 365 the mailbox needs Authenticated SMTP enabled and no MFA prompt.",
                inner: ex);
        }
        catch (SmtpCommandException ex)
        {
            logger.LogWarning(ex, "SMTP command failed ({StatusCode}) on {Host}.", ex.StatusCode, options.Host);
            throw new OfficerMailException($"The SMTP server refused the message: {ex.Message}", inner: ex);
        }
        catch (SmtpProtocolException ex)
        {
            logger.LogWarning(ex, "SMTP protocol error on {Host}.", options.Host);
            throw new OfficerMailException($"SMTP error: {ex.Message}", inner: ex);
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or SslHandshakeException or TimeoutException)
        {
            logger.LogWarning(ex, "Could not reach SMTP server {Host}:{Port}.", options.Host, options.Port);
            throw new OfficerMailException($"Could not reach the mail server {options.Host}:{options.Port}: {ex.Message}", inner: ex);
        }
    }

    private MimeMessage BuildMessage(OfficerMailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(options.FromName ?? string.Empty, options.FromAddress!.Trim()));
        AddAll(mime.To, message.To);
        AddAll(mime.Cc, message.Cc);
        if (options.BccOfficer)
            AddAll(mime.Bcc, message.Bcc);
        if (message.ReplyTo != null)
            mime.ReplyTo.Add(ToMailbox(message.ReplyTo));
        mime.Subject = message.Subject;

        var body = new BodyBuilder { TextBody = message.Body };
        foreach (var attachment in message.Attachments)
            body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        mime.Body = body.ToMessageBody();
        return mime;
    }

    private static void AddAll(InternetAddressList list, IEnumerable<EmailRecipient> recipients)
    {
        foreach (var recipient in recipients)
            list.Add(ToMailbox(recipient));
    }

    private static MailboxAddress ToMailbox(EmailRecipient recipient) =>
        new(recipient.Name ?? string.Empty, recipient.Email);

    private SecureSocketOptions ResolveSecurity() => (options.Security ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "none" => SecureSocketOptions.None,
        "sslonconnect" or "ssl" => SecureSocketOptions.SslOnConnect,
        "auto" => SecureSocketOptions.Auto,
        _ => SecureSocketOptions.StartTls,
    };
}
