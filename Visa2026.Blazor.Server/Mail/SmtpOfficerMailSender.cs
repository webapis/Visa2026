using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects.Email;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Mail;

/// <summary>
/// Sends document-copies email from one shared SMTP account. Settings come from
/// <see cref="EmailSmtpSettings"/> (Configuration → Mail server); appsettings / env are a fallback.
/// The officer is Reply-To and gets a Bcc copy.
/// </summary>
public sealed class SmtpOfficerMailSender : IOfficerMailSender
{
    private readonly SmtpMailOptions fileOptions;
    private readonly INonSecuredObjectSpaceFactory objectSpaceFactory;
    private readonly IOfficerMailSecretProtector secretProtector;
    private readonly ILogger<SmtpOfficerMailSender> logger;

    public SmtpOfficerMailSender(
        IOptions<SmtpMailOptions> options,
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        IOfficerMailSecretProtector secretProtector,
        ILogger<SmtpOfficerMailSender> logger)
    {
        fileOptions = options.Value;
        this.objectSpaceFactory = objectSpaceFactory;
        this.secretProtector = secretProtector;
        this.logger = logger;
    }

    public bool IsConfigured => LoadEffective().IsConfigured;

    public bool RequiresUserConnection => false;

    public long MaxTotalAttachmentBytes
    {
        get
        {
            var bytes = LoadEffective().MaxTotalAttachmentBytes;
            return bytes > 0 ? bytes : 20L * 1024 * 1024;
        }
    }

    public string CreateConnectUrl(Guid userId) =>
        throw new NotSupportedException("The shared SMTP account needs no per-officer connection.");

    public Task<OfficerMailConnectionInfo?> GetConnectionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = LoadEffective();
        return Task.FromResult(settings.IsConfigured
            ? new OfficerMailConnectionInfo { AccountEmail = settings.FromAddress, AccountDisplayName = settings.FromName }
            : null);
    }

    public Task DisconnectAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task SendAsync(Guid userId, OfficerMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var settings = LoadEffective();
        if (!settings.IsConfigured)
            throw new OfficerMailException("SMTP is not configured. Open Configuration → Mail server.");
        if (!string.IsNullOrWhiteSpace(settings.UserName) && string.IsNullOrEmpty(settings.Password))
            throw new OfficerMailException("The mail server password is not saved. Open Configuration → Mail server, enter the password, and Save.");

        var mime = BuildMessage(settings, message);
        await ConnectAuthenticateAndRunAsync(settings, async (client, token) =>
        {
            await client.SendAsync(mime, token).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var settings = LoadEffective();
        if (!settings.IsConfigured)
            return "SMTP is not configured. Enter Host and From address on Configuration → Mail server.";
        if (!string.IsNullOrWhiteSpace(settings.UserName) && string.IsNullOrEmpty(settings.Password))
            return "The mail server password is not saved. Enter it on Configuration → Mail server.";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await ConnectAuthenticateAndRunAsync(settings, (_, _) => Task.CompletedTask, timeout.Token, timeoutMs: 20_000)
                .ConfigureAwait(false);
            return null;
        }
        catch (OfficerMailException ex)
        {
            return ex.Message;
        }
        catch (OperationCanceledException)
        {
            return $"The mail server {settings.Host}:{settings.Port} did not respond within 20 seconds.";
        }
    }

    private async Task ConnectAuthenticateAndRunAsync(
        EffectiveSettings settings,
        Func<SmtpClient, CancellationToken, Task> action,
        CancellationToken cancellationToken,
        int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Math.Max(10, settings.TimeoutSeconds) * 1000;
        using var client = new SmtpClient { Timeout = timeout };
        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(settings.UserName))
                await client.AuthenticateAsync(settings.UserName, settings.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
            await action(client, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OfficerMailException($"The mail server {settings.Host}:{settings.Port} did not respond in time.");
        }
        catch (AuthenticationException ex)
        {
            logger.LogWarning(ex, "SMTP authentication failed for {UserName} on {Host}.", settings.UserName, settings.Host);
            throw new OfficerMailException(
                "The SMTP server rejected the username or password. For Office 365 the mailbox needs Authenticated SMTP enabled and no MFA prompt. Try the full mailbox address as the user name.",
                inner: ex);
        }
        catch (SmtpCommandException ex)
        {
            logger.LogWarning(ex, "SMTP command failed ({StatusCode}) on {Host}.", ex.StatusCode, settings.Host);
            throw new OfficerMailException($"The SMTP server refused the message: {ex.Message}", inner: ex);
        }
        catch (SmtpProtocolException ex)
        {
            logger.LogWarning(ex, "SMTP protocol error on {Host}.", settings.Host);
            throw new OfficerMailException($"SMTP error: {ex.Message}", inner: ex);
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or SslHandshakeException or TimeoutException)
        {
            logger.LogWarning(ex, "Could not reach SMTP server {Host}:{Port}.", settings.Host, settings.Port);
            throw new OfficerMailException($"Could not reach the mail server {settings.Host}:{settings.Port}: {ex.Message}", inner: ex);
        }
    }

    private EffectiveSettings LoadEffective()
    {
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace<EmailSmtpSettings>();
        var db = EmailSmtpSettings.TryGetInstance(objectSpace);
        var host = First(db?.Host, fileOptions.Host) ?? EmailSmtpSettings.DefaultHost;
        var fromAddress = First(db?.FromAddress, fileOptions.FromAddress);
        var userName = Office365UserName(First(db?.UserName, fileOptions.UserName), fromAddress);
        var password = secretProtector.Unprotect(db?.ProtectedPassword) ?? fileOptions.Password;
        return new EffectiveSettings
        {
            Host = host,
            Port = db is { Port: > 0 } ? db.Port : (fileOptions.Port > 0 ? fileOptions.Port : EmailSmtpSettings.DefaultPort),
            Security = ResolveSecurity(db?.Security, fileOptions.Security),
            UserName = userName,
            Password = password,
            FromAddress = fromAddress ?? string.Empty,
            FromName = First(db?.FromName, fileOptions.FromName) ?? EmailSmtpSettings.DefaultFromName,
            BccOfficer = db?.BccOfficer ?? fileOptions.BccOfficer,
            MaxTotalAttachmentBytes = db is { MaxTotalAttachmentBytes: > 0 }
                ? db.MaxTotalAttachmentBytes
                : fileOptions.MaxTotalAttachmentBytes,
            TimeoutSeconds = db is { TimeoutSeconds: > 0 } ? db.TimeoutSeconds : fileOptions.TimeoutSeconds,
        };
    }

    private static MimeMessage BuildMessage(EffectiveSettings settings, OfficerMailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName ?? string.Empty, settings.FromAddress));
        AddAll(mime.To, message.To);
        AddAll(mime.Cc, message.Cc);
        if (settings.BccOfficer)
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

    private static SecureSocketOptions ResolveSecurity(SmtpMailSecurity? db, string? file) =>
        db switch
        {
            SmtpMailSecurity.None => SecureSocketOptions.None,
            SmtpMailSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpMailSecurity.Auto => SecureSocketOptions.Auto,
            SmtpMailSecurity.StartTls => SecureSocketOptions.StartTls,
            _ => (file ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "none" => SecureSocketOptions.None,
                "sslonconnect" or "ssl" => SecureSocketOptions.SslOnConnect,
                "auto" => SecureSocketOptions.Auto,
                _ => SecureSocketOptions.StartTls,
            },
        };

    /// <summary>
    /// Office 365 SMTP AUTH expects a UPN (usually the mailbox). A short account such as G31207
    /// is sent as-is by MailKit and Outlook answers 535 5.7.3.
    /// </summary>
    private static string? Office365UserName(string? userName, string? fromAddress)
    {
        if (!string.IsNullOrWhiteSpace(userName) && userName.Contains('@', StringComparison.Ordinal))
            return userName.Trim();
        if (!string.IsNullOrWhiteSpace(fromAddress) && fromAddress.Contains('@', StringComparison.Ordinal))
            return fromAddress.Trim();
        return string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
    }

    private static string? First(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    private sealed class EffectiveSettings
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; }
        public SecureSocketOptions Security { get; init; }
        public string? UserName { get; init; }
        public string? Password { get; init; }
        public string FromAddress { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;
        public bool BccOfficer { get; init; }
        public long MaxTotalAttachmentBytes { get; init; }
        public int TimeoutSeconds { get; init; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
    }
}
