using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.Email;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.EmailDocumentCopies;

public sealed class EmailDocumentCopiesSendRequest
{
    public Guid UserId { get; init; }

    public string OfficerName { get; init; } = string.Empty;

    /// <summary>
    /// Officer's own address. With a shared sending account it becomes Reply-To and a Bcc copy, and is
    /// remembered on <see cref="ApplicationUser.Email"/>.
    /// </summary>
    public string? OfficerEmail { get; init; }

    public IReadOnlyList<EmailDocumentCopiesPersonSelection> Persons { get; init; } = Array.Empty<EmailDocumentCopiesPersonSelection>();

    /// <summary>Largest attachment total per email; capped at what the mail server accepts.</summary>
    public long MaxBytesPerEmail { get; init; }

    public IReadOnlyList<EmailRecipient> To { get; init; } = Array.Empty<EmailRecipient>();

    public IReadOnlyList<EmailRecipient> Cc { get; init; } = Array.Empty<EmailRecipient>();

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public bool SaveNewAddresses { get; init; } = true;

    public string? SourceDescription { get; init; }
}

public sealed class EmailDocumentCopiesSendResult
{
    /// <summary>Every email went out.</summary>
    public bool Succeeded { get; init; }

    /// <summary>Some, but not all, emails of a split send went out.</summary>
    public bool PartiallySent => SentCount > 0 && SentCount < MessageCount;

    public int SentCount { get; init; }

    public int MessageCount { get; init; }

    public int AttachmentCount { get; init; }

    public int RecipientCount { get; init; }

    public string Message { get; init; } = string.Empty;

    /// <summary>True when the officer has to connect Outlook again before sending.</summary>
    public bool ConnectionLost { get; init; }

    public IReadOnlyList<Guid> SentEmailIds { get; init; } = Array.Empty<Guid>();

    /// <summary>Records without a readable scan, or that alone exceed the size limit.</summary>
    public IReadOnlyList<string> SkippedLabels { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Builds per-person PDFs, splits them into emails under the size limit, sends each through
/// <see cref="IOfficerMailSender"/>, and records one <see cref="SentEmail"/> per email (sent or failed).
/// </summary>
public sealed class EmailDocumentCopiesSendService
{
    private readonly INonSecuredObjectSpaceFactory objectSpaceFactory;
    private readonly EmailDocumentCopiesAttachmentBuilder attachmentBuilder;
    private readonly IOfficerMailSender mailSender;
    private readonly ILogger<EmailDocumentCopiesSendService> logger;

    public EmailDocumentCopiesSendService(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        EmailDocumentCopiesAttachmentBuilder attachmentBuilder,
        IOfficerMailSender mailSender,
        ILogger<EmailDocumentCopiesSendService> logger)
    {
        this.objectSpaceFactory = objectSpaceFactory;
        this.attachmentBuilder = attachmentBuilder;
        this.mailSender = mailSender;
        this.logger = logger;
    }

    /// <summary>Limit actually used for a requested per-email size.</summary>
    public long EffectiveLimit(long requestedBytes) =>
        requestedBytes <= 0 ? mailSender.MaxTotalAttachmentBytes : Math.Min(requestedBytes, mailSender.MaxTotalAttachmentBytes);

    public async Task<EmailDocumentCopiesSendResult> SendAsync(EmailDocumentCopiesSendRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var to = Normalize(request.To);
        var cc = Normalize(request.Cc);
        if (to.Count == 0)
            return Fail("EmailDocumentCopies.Error.NoRecipients");
        if (to.Concat(cc).Any(r => !EmailContactDirectory.IsValidEmail(r.Email)))
            return Fail("EmailDocumentCopies.Error.InvalidRecipient");
        if (string.IsNullOrWhiteSpace(request.Subject))
            return Fail("EmailDocumentCopies.Error.NoSubject");

        string? officerEmail = string.IsNullOrWhiteSpace(request.OfficerEmail) ? null : request.OfficerEmail.Trim();
        if (officerEmail != null && !EmailContactDirectory.IsValidEmail(officerEmail))
            return Fail("EmailDocumentCopies.Error.InvalidOfficerEmail");

        var built = request.Persons
            .Where(p => p.PersonId != Guid.Empty && p.RecordKeys.Count > 0)
            .Select(p => attachmentBuilder.BuildRecords(p.PersonId, p.RecordKeys))
            .ToList();
        var pdfs = built.SelectMany(b => b.RecordPdfs).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
        long limit = EffectiveLimit(request.MaxBytesPerEmail);
        var plan = EmailDocumentCopiesPlanner.Split(built.Select(b => b.Plan), limit);

        var skipped = built.SelectMany(b => b.SkippedLabels)
            .Concat(plan.TooLarge.SelectMany(a => a.Records.Select(r => $"{a.PersonDisplayName} - {r.Label}")))
            .ToList();
        if (plan.Messages.Count == 0)
        {
            return new EmailDocumentCopiesSendResult
            {
                Message = VisaUiMessages.Get(plan.TooLarge.Count > 0 ? "EmailDocumentCopies.Error.AllTooLarge" : "EmailDocumentCopies.Error.NoAttachments"),
                SkippedLabels = skipped,
            };
        }

        var officer = officerEmail == null || mailSender.RequiresUserConnection
            ? null
            : new EmailRecipient { Email = officerEmail, Name = request.OfficerName };
        var fromAddress = (await mailSender.GetConnectionAsync(request.UserId, cancellationToken))?.AccountEmail;
        int count = plan.Messages.Count;
        Guid? batchId = count > 1 ? Guid.NewGuid() : null;
        int? sizeMb = (int)Math.Max(1, limit / EmailDocumentCopiesPlanner.MegaByte);

        var sentIds = new List<Guid>();
        var errors = new List<string>();
        int sentCount = 0;
        int attachmentCount = 0;
        bool connectionLost = false;
        foreach (var planned in plan.Messages)
        {
            var attachments = planned.Attachments.Select(a => EmailDocumentCopiesAttachmentBuilder.Merge(a, pdfs)).ToList();
            EnsureUniqueFileNames(attachments);
            var message = new OfficerMailMessage
            {
                Subject = count > 1
                    ? $"{request.Subject.Trim()} {VisaUiMessages.Format("EmailDocumentCopies.Split.SubjectSuffix", planned.Number, count)}"
                    : request.Subject.Trim(),
                Body = count > 1 ? AppendPartLine(request.Body, planned, count) : request.Body ?? string.Empty,
                To = to,
                Cc = cc,
                Bcc = officer == null || to.Concat(cc).Any(r => string.Equals(r.Email, officer.Email, StringComparison.OrdinalIgnoreCase))
                    ? Array.Empty<EmailRecipient>()
                    : new[] { officer },
                ReplyTo = officer,
                Attachments = attachments,
            };

            string? error = null;
            if (connectionLost)
            {
                error = VisaUiMessages.Get("EmailDocumentCopies.Error.NotConnected");
            }
            else
            {
                try
                {
                    await mailSender.SendAsync(request.UserId, message, cancellationToken);
                    sentCount++;
                    attachmentCount += attachments.Count;
                }
                catch (OfficerMailException ex)
                {
                    error = ex.Message;
                    connectionLost = ex.ConnectionLost;
                    logger.LogWarning(ex, "Email document copies: send of email {Part}/{Count} failed for user {UserId}.", planned.Number, count, request.UserId);
                }
            }

            if (error != null)
                errors.Add(error);
            sentIds.Add(Record(request, to, cc, message, planned, count, batchId, fromAddress, officerEmail, sizeMb, error));
        }

        int recipients = to.Count + cc.Count;
        string resultMessage;
        if (sentCount == count)
        {
            resultMessage = count == 1
                ? VisaUiMessages.Format("EmailDocumentCopies.Sent", attachmentCount, recipients)
                : VisaUiMessages.Format("EmailDocumentCopies.Sent.Split", count, recipients, attachmentCount);
        }
        else if (sentCount > 0)
        {
            resultMessage = VisaUiMessages.Format("EmailDocumentCopies.Sent.Partial", sentCount, count, errors.FirstOrDefault() ?? string.Empty);
        }
        else
        {
            resultMessage = VisaUiMessages.Format("EmailDocumentCopies.Error.SendFailed", errors.FirstOrDefault() ?? string.Empty);
        }

        return new EmailDocumentCopiesSendResult
        {
            Succeeded = sentCount == count,
            SentCount = sentCount,
            MessageCount = count,
            AttachmentCount = attachmentCount,
            RecipientCount = recipients,
            Message = resultMessage,
            ConnectionLost = connectionLost,
            SentEmailIds = sentIds,
            SkippedLabels = skipped,
        };
    }

    private static string AppendPartLine(string? body, EmailPlanMessage planned, int count)
    {
        var contents = string.Join(", ", planned.Attachments.Select(a => a.PartCount > 1
            ? $"{a.PersonDisplayName} ({string.Join(", ", a.Types.Select(t => VisaUiMessages.Get(EmailDocumentTypeCatalog.LabelKey(t))))})"
            : a.PersonDisplayName));
        var line = VisaUiMessages.Format("EmailDocumentCopies.Split.BodyPart", planned.Number, count, contents);
        return string.IsNullOrWhiteSpace(body) ? line : $"{body.TrimEnd()}{Environment.NewLine}{Environment.NewLine}{line}";
    }

    private Guid Record(
        EmailDocumentCopiesSendRequest request,
        IReadOnlyList<EmailRecipient> to,
        IReadOnlyList<EmailRecipient> cc,
        OfficerMailMessage message,
        EmailPlanMessage planned,
        int partCount,
        Guid? batchId,
        string? fromAddress,
        string? officerEmail,
        int? sizeMb,
        string? error)
    {
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace<SentEmail>();
        var user = objectSpace.GetObjectByKey<ApplicationUser>(request.UserId);
        if (user != null && officerEmail != null && !string.Equals(user.Email, officerEmail, StringComparison.OrdinalIgnoreCase))
            user.Email = officerEmail;

        var sent = objectSpace.CreateObject<SentEmail>();
        sent.SentOnUtc = DateTime.UtcNow;
        sent.Status = error == null ? SentEmailStatus.Sent : SentEmailStatus.Failed;
        sent.SentBy = user;
        sent.SentByName = Truncate(request.OfficerName, 256);
        sent.FromAddress = Truncate(fromAddress, 320);
        sent.ToAddresses = string.Join("; ", to.Select(r => r.ToString()));
        sent.CcAddresses = cc.Count == 0 ? null : string.Join("; ", cc.Select(r => r.ToString()));
        sent.Subject = Truncate(message.Subject, 512);
        sent.Body = message.Body;
        sent.ErrorMessage = Truncate(error, 2048);
        sent.AttachmentCount = message.Attachments.Count;
        sent.AttachmentTotalBytes = message.Attachments.Sum(a => (long)a.Content.Length);
        sent.AttachmentNames = string.Join(Environment.NewLine, message.Attachments.Select(a => a.FileName));
        sent.SourceDescription = Truncate(request.SourceDescription, 256);
        sent.BatchId = batchId;
        sent.PartNumber = planned.Number;
        sent.PartCount = partCount;

        for (int i = 0; i < planned.Attachments.Count; i++)
        {
            var attachment = planned.Attachments[i];
            var line = objectSpace.CreateObject<SentEmailPerson>();
            line.SentEmail = sent;
            line.Person = objectSpace.GetObjectByKey<Person>(attachment.PersonId);
            line.PersonDisplayName = Truncate(attachment.PersonDisplayName, 512);
            line.RecordKeys = string.Join("\n", attachment.Records.Select(r => r.RecordKey));
            line.RecordLabels = string.Join(Environment.NewLine, attachment.Records.Select(r => r.Label));
            line.AttachmentFileName = Truncate(i < message.Attachments.Count ? message.Attachments[i].FileName : null, 512);
        }
        sent.PersonCount = planned.Attachments.Select(a => a.PersonId).Distinct().Count();

        if (error == null)
            EmailContactDirectory.RecordUsage(objectSpace, request.UserId, to.Concat(cc), request.SaveNewAddresses, sizeMb);

        objectSpace.CommitChanges();
        return sent.ID;
    }

    private static List<EmailRecipient> Normalize(IEnumerable<EmailRecipient>? recipients) =>
        (recipients ?? Enumerable.Empty<EmailRecipient>())
            .Where(r => !string.IsNullOrWhiteSpace(r?.Email))
            .GroupBy(r => r.Email.Trim().ToLowerInvariant())
            .Select(g => new EmailRecipient { Email = g.First().Email.Trim(), Name = g.First().Name?.Trim() })
            .ToList();

    private static void EnsureUniqueFileNames(List<OfficerMailAttachment> attachments)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < attachments.Count; i++)
        {
            var name = attachments[i].FileName;
            var baseName = System.IO.Path.GetFileNameWithoutExtension(name);
            var extension = System.IO.Path.GetExtension(name);
            for (int n = 2; !used.Add(name); n++)
                name = $"{baseName}_{n}{extension}";
            if (!ReferenceEquals(name, attachments[i].FileName))
            {
                attachments[i] = new OfficerMailAttachment
                {
                    FileName = name,
                    ContentType = attachments[i].ContentType,
                    Content = attachments[i].Content,
                };
            }
        }
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

    private static EmailDocumentCopiesSendResult Fail(string key) =>
        new() { Message = VisaUiMessages.Get(key) };
}
