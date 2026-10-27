using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.Services.PersonLinkedDocuments;

namespace Visa2026.Module.Services.EmailDocumentCopies;

/// <summary>Document type the officer ticks once for every selected person.</summary>
public enum EmailDocumentType
{
    Passport,
    Visa,
    WorkPermit,
    Invitation,
    Education,
    FamilyProof,
    MedicalRecord,
    AddressOfResidence,
}

/// <summary>Person plus the person document-copies record keys to attach for them.</summary>
public sealed class EmailDocumentCopiesPersonSelection
{
    public Guid PersonId { get; init; }

    public IReadOnlyList<string> RecordKeys { get; init; } = Array.Empty<string>();
}

/// <summary>Maps <see cref="PersonLinkedDocumentsResolver"/> record keys to <see cref="EmailDocumentType"/>.</summary>
public static class EmailDocumentTypeCatalog
{
    public static readonly IReadOnlyList<EmailDocumentType> All = new[]
    {
        EmailDocumentType.Passport,
        EmailDocumentType.Visa,
        EmailDocumentType.WorkPermit,
        EmailDocumentType.Invitation,
        EmailDocumentType.AddressOfResidence,
        EmailDocumentType.Education,
        EmailDocumentType.FamilyProof,
        EmailDocumentType.MedicalRecord,
    };

    public static IReadOnlyList<EmailDocumentType> DefaultSelection { get; } = new[]
    {
        EmailDocumentType.Passport,
        EmailDocumentType.Visa,
    };

    public static string LabelKey(EmailDocumentType type) => $"EmailDocumentCopies.Type.{type}";

    public static EmailDocumentType? Classify(string? recordKey)
    {
        if (string.IsNullOrEmpty(recordKey))
            return null;
        if (recordKey.Contains("/Visa:", StringComparison.Ordinal))
            return EmailDocumentType.Visa;
        if (recordKey.StartsWith("Passport:", StringComparison.Ordinal))
            return EmailDocumentType.Passport;
        if (recordKey.StartsWith("AddressOfResidence:", StringComparison.Ordinal))
            return EmailDocumentType.AddressOfResidence;
        if (recordKey.StartsWith("WorkPermitItem:", StringComparison.Ordinal))
            return EmailDocumentType.WorkPermit;
        if (recordKey.StartsWith("InvitationItem:", StringComparison.Ordinal))
            return EmailDocumentType.Invitation;
        if (recordKey.StartsWith("Education:", StringComparison.Ordinal))
            return EmailDocumentType.Education;
        if (recordKey.StartsWith("MedicalRecord:", StringComparison.Ordinal))
            return EmailDocumentType.MedicalRecord;
        if (recordKey.StartsWith("FamilyRelationDocument:", StringComparison.Ordinal))
            return EmailDocumentType.FamilyProof;
        return null;
    }

    /// <summary>Family proof records always go together; they have no "current" one.</summary>
    public static bool HasCurrentRecord(EmailDocumentType type) => type != EmailDocumentType.FamilyProof;
}

/// <summary>One record (passport, visa, …) that will be merged into a person's PDF.</summary>
public sealed class EmailPlanRecord
{
    public string RecordKey { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public EmailDocumentType Type { get; init; }

    /// <summary>Scan bytes before PDF merging (estimate) or the built PDF bytes (at send time).</summary>
    public long Bytes { get; init; }
}

/// <summary>What one selected type yields for one person.</summary>
public sealed class EmailPlanTypeLine
{
    public EmailDocumentType Type { get; init; }

    /// <summary>Records with a scanned file that will be attached.</summary>
    public IReadOnlyList<EmailPlanRecord> Records { get; init; } = Array.Empty<EmailPlanRecord>();

    /// <summary>The type has records, but the chosen one has no scanned file.</summary>
    public bool NoFile { get; init; }

    /// <summary>The person has no record of this type at all.</summary>
    public bool NoRecord { get; init; }

    public string? FirstLabel => Records.Count > 0 ? Records[0].Label : null;
}

public sealed class EmailPlanPerson
{
    public Guid PersonId { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyList<EmailPlanTypeLine> Lines { get; init; } = Array.Empty<EmailPlanTypeLine>();

    public IEnumerable<EmailPlanRecord> Records => Lines.SelectMany(l => l.Records);

    public long Bytes => Records.Sum(r => r.Bytes);

    public bool HasAttachment => Lines.Any(l => l.Records.Count > 0);
}

/// <summary>One PDF: a whole person, or one part of a person split by document type.</summary>
public sealed class EmailPlanAttachment
{
    public Guid PersonId { get; init; }

    public string PersonDisplayName { get; init; } = string.Empty;

    public int PartNumber { get; set; } = 1;

    public int PartCount { get; set; } = 1;

    public IReadOnlyList<EmailPlanRecord> Records { get; init; } = Array.Empty<EmailPlanRecord>();

    public long Bytes => Records.Sum(r => r.Bytes);

    /// <summary>A single record bigger than the limit: it cannot be emailed.</summary>
    public bool TooLarge { get; init; }

    public IEnumerable<EmailDocumentType> Types => Records.Select(r => r.Type).Distinct();
}

public sealed class EmailPlanMessage
{
    public int Number { get; init; }

    public IReadOnlyList<EmailPlanAttachment> Attachments { get; init; } = Array.Empty<EmailPlanAttachment>();

    public long Bytes => Attachments.Sum(a => a.Bytes);
}

public sealed class EmailSendPlan
{
    public long LimitBytes { get; init; }

    public IReadOnlyList<EmailPlanMessage> Messages { get; init; } = Array.Empty<EmailPlanMessage>();

    /// <summary>Records that alone exceed the limit; left out of every message.</summary>
    public IReadOnlyList<EmailPlanAttachment> TooLarge { get; init; } = Array.Empty<EmailPlanAttachment>();

    public long Bytes => Messages.Sum(m => m.Bytes);

    public bool IsSplit => Messages.Count > 1;
}

/// <summary>
/// Picks records per document type for each person and splits the result into emails under a size
/// limit. Pure logic: estimates use scan sizes, sending re-runs <see cref="Split"/> with built PDF sizes.
/// See <c>docs/EMAIL_DOCUMENT_COPIES.md</c> §10.1.
/// </summary>
public static class EmailDocumentCopiesPlanner
{
    public const long MegaByte = 1024L * 1024L;

    public static readonly IReadOnlyList<int> SizeChoicesMb = new[] { 10, 15, 20 };

    /// <summary>
    /// For each selected type: the current record (when flagged) or the latest one; family proof and
    /// <paramref name="includeAll"/> take every record that has a scanned file. A lodging under the chosen
    /// address of residence goes with it.
    /// </summary>
    public static EmailPlanPerson PlanPerson(
        PersonLinkedDocumentsSnapshot snapshot,
        IEnumerable<EmailDocumentType> types,
        bool includeAll)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var records = snapshot.Sections.SelectMany(s => s.Records).ToList();
        var lines = new List<EmailPlanTypeLine>();
        foreach (var type in types.Distinct().OrderBy(t => IndexOf(t)))
        {
            var ofType = records.Where(r => EmailDocumentTypeCatalog.Classify(r.RecordKey) == type).ToList();
            if (ofType.Count == 0)
            {
                lines.Add(new EmailPlanTypeLine { Type = type, NoRecord = true });
                continue;
            }

            List<PersonLinkedDocumentRecord> chosen;
            if (includeAll || !EmailDocumentTypeCatalog.HasCurrentRecord(type))
            {
                chosen = ofType;
            }
            else
            {
                var top = ofType.Where(r => !r.IsNested || type == EmailDocumentType.Visa).ToList();
                var pick = top.FirstOrDefault(r => r.IsCurrent) ?? top.FirstOrDefault(HasFile) ?? top.FirstOrDefault();
                chosen = pick == null ? new List<PersonLinkedDocumentRecord>() : new List<PersonLinkedDocumentRecord> { pick };
                if (pick != null && type == EmailDocumentType.AddressOfResidence)
                    chosen.AddRange(ofType.Where(r => r.IsNested && r.RecordKey.StartsWith(pick.RecordKey + "/", StringComparison.Ordinal)));
            }

            var withFiles = chosen.Where(HasFile).Select(r => ToPlanRecord(r, type)).ToList();
            lines.Add(new EmailPlanTypeLine { Type = type, Records = withFiles, NoFile = withFiles.Count == 0 });
        }

        return new EmailPlanPerson { PersonId = snapshot.PersonId, DisplayName = snapshot.PersonDisplayName, Lines = lines };
    }

    /// <summary>Exact records (Resend). Keys that no longer exist or lost their scan become "no file".</summary>
    public static EmailPlanPerson PlanPersonFromKeys(PersonLinkedDocumentsSnapshot snapshot, IEnumerable<string> recordKeys)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var byType = new Dictionary<EmailDocumentType, List<EmailPlanRecord>>();
        var missing = new HashSet<EmailDocumentType>();
        foreach (var key in recordKeys.Distinct(StringComparer.Ordinal))
        {
            var type = EmailDocumentTypeCatalog.Classify(key);
            if (type == null)
                continue;
            var record = snapshot.FindRecord(key);
            if (record == null || !HasFile(record))
            {
                missing.Add(type.Value);
                continue;
            }
            if (!byType.TryGetValue(type.Value, out var list))
                byType[type.Value] = list = new List<EmailPlanRecord>();
            list.Add(ToPlanRecord(record, type.Value));
        }

        var lines = byType.Keys.Concat(missing).Distinct().OrderBy(IndexOf)
            .Select(t => byType.TryGetValue(t, out var list)
                ? new EmailPlanTypeLine { Type = t, Records = list }
                : new EmailPlanTypeLine { Type = t, NoFile = true })
            .ToList();
        return new EmailPlanPerson { PersonId = snapshot.PersonId, DisplayName = snapshot.PersonDisplayName, Lines = lines };
    }

    /// <summary>
    /// One PDF per person while it fits; a bigger person is split by document type (then by record) into
    /// parts. PDFs are packed into emails in selection order, starting a new email when the next one
    /// would exceed <paramref name="limitBytes"/>.
    /// </summary>
    public static EmailSendPlan Split(IEnumerable<EmailPlanPerson> persons, long limitBytes)
    {
        if (limitBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(limitBytes));

        var attachments = new List<EmailPlanAttachment>();
        var tooLarge = new List<EmailPlanAttachment>();
        foreach (var person in persons.Where(p => p.HasAttachment))
        {
            if (person.Bytes <= limitBytes)
            {
                attachments.Add(new EmailPlanAttachment
                {
                    PersonId = person.PersonId,
                    PersonDisplayName = person.DisplayName,
                    Records = person.Records.ToList(),
                });
                continue;
            }

            var items = new List<List<EmailPlanRecord>>();
            foreach (var line in person.Lines.Where(l => l.Records.Count > 0))
            {
                long lineBytes = line.Records.Sum(r => r.Bytes);
                if (lineBytes <= limitBytes)
                    items.Add(line.Records.ToList());
                else
                    items.AddRange(line.Records.Select(r => new List<EmailPlanRecord> { r }));
            }

            var parts = new List<EmailPlanAttachment>();
            var current = new List<EmailPlanRecord>();
            foreach (var item in items)
            {
                long itemBytes = item.Sum(r => r.Bytes);
                if (itemBytes > limitBytes)
                {
                    tooLarge.Add(new EmailPlanAttachment
                    {
                        PersonId = person.PersonId,
                        PersonDisplayName = person.DisplayName,
                        Records = item,
                        TooLarge = true,
                    });
                    continue;
                }
                if (current.Count > 0 && current.Sum(r => r.Bytes) + itemBytes > limitBytes)
                {
                    parts.Add(NewPart(person, current));
                    current = new List<EmailPlanRecord>();
                }
                current.AddRange(item);
            }
            if (current.Count > 0)
                parts.Add(NewPart(person, current));

            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].PartNumber = i + 1;
                parts[i].PartCount = parts.Count;
            }
            attachments.AddRange(parts);
        }

        var messages = new List<EmailPlanMessage>();
        var pending = new List<EmailPlanAttachment>();
        long pendingBytes = 0;
        foreach (var attachment in attachments)
        {
            if (pending.Count > 0 && pendingBytes + attachment.Bytes > limitBytes)
            {
                messages.Add(new EmailPlanMessage { Number = messages.Count + 1, Attachments = pending });
                pending = new List<EmailPlanAttachment>();
                pendingBytes = 0;
            }
            pending.Add(attachment);
            pendingBytes += attachment.Bytes;
        }
        if (pending.Count > 0)
            messages.Add(new EmailPlanMessage { Number = messages.Count + 1, Attachments = pending });

        return new EmailSendPlan { LimitBytes = limitBytes, Messages = messages, TooLarge = tooLarge };
    }

    /// <summary>Picker value (MB) never above what the mail server accepts.</summary>
    public static IReadOnlyList<int> AvailableSizesMb(long senderMaxBytes)
    {
        var sizes = SizeChoicesMb.Where(mb => mb * MegaByte <= senderMaxBytes).ToList();
        if (sizes.Count == 0)
            sizes.Add((int)Math.Max(1, senderMaxBytes / MegaByte));
        return sizes;
    }

    private static EmailPlanAttachment NewPart(EmailPlanPerson person, List<EmailPlanRecord> records) =>
        new() { PersonId = person.PersonId, PersonDisplayName = person.DisplayName, Records = records };

    private static bool HasFile(PersonLinkedDocumentRecord record) => record.Files.Any(f => f.HasContent);

    private static EmailPlanRecord ToPlanRecord(PersonLinkedDocumentRecord record, EmailDocumentType type) => new()
    {
        RecordKey = record.RecordKey,
        Label = record.RecordLabel,
        Type = type,
        Bytes = record.Files.Where(f => f.HasContent).Sum(f => (long)f.SizeBytes),
    };

    private static int IndexOf(EmailDocumentType type)
    {
        for (int i = 0; i < EmailDocumentTypeCatalog.All.Count; i++)
        {
            if (EmailDocumentTypeCatalog.All[i] == type)
                return i;
        }
        return int.MaxValue;
    }
}
