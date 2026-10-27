using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonLinkedDocuments;

namespace Visa2026.Module.Services.EmailDocumentCopies;

/// <summary>Per-record PDFs of one person, ready to be grouped into attachments.</summary>
public sealed class EmailBuiltPerson
{
    /// <summary>Plan whose record sizes are the built PDF sizes (records that failed to build are left out).</summary>
    public EmailPlanPerson Plan { get; init; } = new();

    public IReadOnlyDictionary<string, byte[]> RecordPdfs { get; init; } = new Dictionary<string, byte[]>();

    public IReadOnlyList<string> SkippedLabels { get; init; } = Array.Empty<string>();
}

/// <summary>Builds PDFs for person document-copies records and merges them into one attachment per part.</summary>
public sealed class EmailDocumentCopiesAttachmentBuilder
{
    private readonly INonSecuredObjectSpaceFactory objectSpaceFactory;
    private readonly PersonDocumentCopyPdfMerger pdfMerger;

    public EmailDocumentCopiesAttachmentBuilder(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        PersonDocumentCopyPdfMerger pdfMerger)
    {
        this.objectSpaceFactory = objectSpaceFactory;
        this.pdfMerger = pdfMerger;
    }

    public EmailBuiltPerson BuildRecords(Guid personId, IReadOnlyList<string> recordKeys)
    {
        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace<Person>();
        var person = objectSpace.GetObjectByKey<Person>(personId);
        if (person == null)
            return new EmailBuiltPerson { Plan = new EmailPlanPerson { PersonId = personId } };

        var snapshot = PersonLinkedDocumentsResolver.Resolve(objectSpace, person);
        var pdfs = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var lines = new List<EmailPlanTypeLine>();
        var skipped = new List<string>();
        var keysByType = recordKeys
            .Distinct(StringComparer.Ordinal)
            .Select(key => (Key: key, Type: EmailDocumentTypeCatalog.Classify(key)))
            .Where(x => x.Type != null)
            .GroupBy(x => x.Type!.Value);

        foreach (var group in keysByType)
        {
            var records = new List<EmailPlanRecord>();
            foreach (var (key, _) in group)
            {
                var record = snapshot.FindRecord(key);
                if (record == null)
                    continue;

                if (pdfMerger.TryBuildMergedPdf(objectSpace, snapshot, key, record.RecordLabel, out var content, out _)
                    && content is { Length: > 0 })
                {
                    pdfs[key] = content;
                    records.Add(new EmailPlanRecord { RecordKey = key, Label = record.RecordLabel, Type = group.Key, Bytes = content.Length });
                }
                else
                {
                    skipped.Add(record.RecordLabel);
                }
            }
            lines.Add(new EmailPlanTypeLine { Type = group.Key, Records = records, NoFile = records.Count == 0 });
        }

        var orderedLines = lines.OrderBy(l => IndexOf(l.Type)).ToList();
        return new EmailBuiltPerson
        {
            Plan = new EmailPlanPerson { PersonId = personId, DisplayName = snapshot.PersonDisplayName, Lines = orderedLines },
            RecordPdfs = pdfs,
            SkippedLabels = skipped,
        };
    }

    /// <summary>Merges the records of one planned attachment into a single PDF.</summary>
    public static OfficerMailAttachment Merge(EmailPlanAttachment attachment, IReadOnlyDictionary<string, byte[]> recordPdfs)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        var streams = attachment.Records
            .Where(r => recordPdfs.ContainsKey(r.RecordKey))
            .Select(r => new MemoryStream(recordPdfs[r.RecordKey]))
            .ToList();
        try
        {
            using var merged = new MemoryStream();
            if (streams.Count == 1)
                streams[0].CopyTo(merged);
            else
                SupportingDocumentsPdfSharpHelper.MergePdfStreams(streams, merged);

            return new OfficerMailAttachment
            {
                FileName = BuildFileName(attachment.PersonDisplayName, attachment.PartNumber, attachment.PartCount),
                ContentType = "application/pdf",
                Content = merged.ToArray(),
            };
        }
        finally
        {
            foreach (var stream in streams)
                stream.Dispose();
        }
    }

    public static string BuildFileName(string personDisplayName, int partNumber = 1, int partCount = 1)
    {
        var name = string.IsNullOrWhiteSpace(personDisplayName) ? "person" : personDisplayName.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) || char.IsWhiteSpace(ch) ? '_' : ch).ToArray()).Trim('_');
        var part = partCount > 1 ? $"_part{partNumber}" : string.Empty;
        return $"{(string.IsNullOrWhiteSpace(cleaned) ? "person" : cleaned)}_documents{part}.pdf";
    }

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
