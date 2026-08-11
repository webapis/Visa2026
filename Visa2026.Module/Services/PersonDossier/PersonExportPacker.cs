using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using DevExpress.ExpressApp;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonLinkedDocuments;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>Outcome of one person export, used to name the ZIP and report gaps back to the batch.</summary>
public sealed class PersonExportResult
{
    public int RecordCount { get; init; }

    public int WrittenRecordCount { get; init; }

    public string ZipFileName { get; init; } = string.Empty;

    /// <summary>Text also written as <c>EXPORT_NOTES.txt</c>.</summary>
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Builds the director hand-over ZIP for one person: the dossier document at the root, plus one
/// merged PDF per document-copies record inside a folder per section.
/// </summary>
/// <remarks>
/// Separate from <c>ApplicationSupportingDocumentsPacker</c> on purpose — that packer is organized
/// by ministry slot across application lines, while this one follows the person's own record
/// structure. <c>FamilyMemberImage</c> / <c>Person.Images</c> byte-array photos are excluded, the
/// same gap the copies catalog has (see <c>docs/PERSON_DOCUMENT_COPIES.md</c>).
/// </remarks>
public sealed class PersonExportPacker
{
    private const string DossierEntryName = "Dossier.pdf";
    private const string NotesEntryName = "EXPORT_NOTES.txt";

    private readonly PersonDossierPdfBuilder dossierPdfBuilder;
    private readonly PersonDocumentCopyPdfMerger merger;
    private readonly ILogger<PersonExportPacker> logger;

    public PersonExportPacker(
        PersonDossierPdfBuilder dossierPdfBuilder,
        PersonDocumentCopyPdfMerger merger,
        ILogger<PersonExportPacker> logger)
    {
        this.dossierPdfBuilder = dossierPdfBuilder;
        this.merger = merger;
        this.logger = logger;
    }

    public PersonExportResult BuildZip(
        IObjectSpace objectSpace,
        Person person,
        string? cultureName,
        Stream zipStream,
        Action<int, int>? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(zipStream);

        var dossier = PersonDossierResolver.Resolve(objectSpace, person);
        var copies = PersonLinkedDocumentsResolver.Resolve(objectSpace, person);

        var recordsToPack = copies.Sections
            .SelectMany(section => section.Records.Select(record => (Section: section, Record: record)))
            .Where(pair => pair.Record.Files.Any(file => file.HasContent && file.FileDataId != Guid.Empty))
            .ToList();

        int total = recordsToPack.Count + 1; // +1 for the dossier document itself
        int processed = 0;
        int written = 0;
        var gaps = new List<string>();
        var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (dossierPdfBuilder.TryBuildPdf(dossier, cultureName, out var dossierPdf) && dossierPdf != null)
            {
                WriteEntry(archive, DossierEntryName, dossierPdf);
                usedEntryNames.Add(DossierEntryName);
                written++;
            }
            else
            {
                gaps.Add(VisaUiMessages.Get("PersonDossier.Export.Notes.DossierFailed", cultureName));
            }

            processed++;
            onProgress?.Invoke(processed, total);

            foreach (var (section, record) in recordsToPack)
            {
                if (merger.TryBuildMergedPdf(objectSpace, copies, record.RecordKey, record.RecordLabel, out var content, out var fileName)
                    && content != null)
                {
                    string entryName = BuildEntryName(section, record, fileName, cultureName, usedEntryNames);
                    WriteEntry(archive, entryName, content);
                    written++;
                }
                else
                {
                    gaps.Add($"{section.SectionLabel} — {record.RecordLabel}");
                    logger.LogWarning(
                        "Person export: no readable PDF produced for record {RecordKey} of person {PersonId}.",
                        record.RecordKey,
                        person.ID);
                }

                processed++;
                onProgress?.Invoke(processed, total);
            }

            string notes = BuildNotes(gaps, cultureName);
            WriteEntry(archive, NotesEntryName, Encoding.UTF8.GetBytes(notes));

            return new PersonExportResult
            {
                RecordCount = total,
                WrittenRecordCount = written,
                ZipFileName = BuildZipFileName(person),
                Notes = notes
            };
        }
    }

    private static void WriteEntry(ZipArchive archive, string entryName, byte[] content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
        using var entryStream = entry.Open();
        entryStream.Write(content, 0, content.Length);
    }

    /// <summary>
    /// Folder overrides for records the copies catalog nests under a parent section. A visa is
    /// listed under its passport there, but a director reading the package expects Visas/ next to
    /// Passports/, so the record's own document class wins over the section it was collected in.
    /// </summary>
    private static readonly Dictionary<Type, string> FolderKeyByRecordType = new()
    {
        [typeof(Visa)] = "PersonDocumentCopies.Section.Visas"
    };

    /// <summary>Visible to Module.Tests for entry-name / folder-override / uniqueness coverage.</summary>
    internal static string BuildEntryName(
        PersonLinkedDocumentSection section,
        PersonLinkedDocumentRecord record,
        string? mergedFileName,
        string? cultureName,
        HashSet<string> usedEntryNames)
    {
        string folderLabel = record.SourceObjectType != null
            && FolderKeyByRecordType.TryGetValue(record.SourceObjectType, out var folderKey)
                ? VisaUiMessages.Get(folderKey, cultureName)
                : section.SectionLabel;

        string folder = ZipEntryFileNameSanitizer.Sanitize(folderLabel, maxLength: 60);

        // The record label ("Passport U40412139") beats the merger's file name, which falls back to
        // the uploaded scan's own name when a record holds a single file — meaningful in the copies
        // preview, but opaque in a hand-over package.
        string leaf = !string.IsNullOrWhiteSpace(record.RecordLabel)
            ? record.RecordLabel + ".pdf"
            : mergedFileName ?? "document.pdf";

        // Uniqueness must consider the folder: two sections may legitimately hold the same leaf name.
        string candidate = $"{folder}/{ZipEntryFileNameSanitizer.Sanitize(leaf, maxLength: 90)}";
        if (usedEntryNames.Add(candidate))
            return candidate;

        string ext = Path.GetExtension(candidate);
        string baseName = candidate[..^ext.Length];
        for (int suffix = 2; ; suffix++)
        {
            string next = $"{baseName}_{suffix.ToString(CultureInfo.InvariantCulture)}{ext}";
            if (usedEntryNames.Add(next))
                return next;
        }
    }

    private static string BuildNotes(IReadOnlyList<string> gaps, string? cultureName)
    {
        var text = new StringBuilder();
        text.AppendLine(VisaUiMessages.Get("PersonDossier.Export.Notes.Title", cultureName));
        text.AppendLine();

        if (gaps.Count == 0)
        {
            text.AppendLine(VisaUiMessages.Get("PersonDossier.Export.Notes.NoGaps", cultureName));
            return text.ToString();
        }

        text.AppendLine(VisaUiMessages.Get("PersonDossier.Export.Notes.GapsHeader", cultureName));
        foreach (var gap in gaps)
            text.AppendLine("  - " + gap);

        return text.ToString();
    }

    /// <summary>Visible to Module.Tests. <paramref name="stamp"/> defaults to local <see cref="DateTime.Now"/>.</summary>
    internal static string BuildZipFileName(Person person, DateTime? stamp = null)
    {
        // FullName is "" (not null) when name parts are empty — do not let Sanitize map that to "report.bin".
        string rawName = string.IsNullOrWhiteSpace(person.FullName) ? "Person" : person.FullName;
        string name = ZipEntryFileNameSanitizer.Sanitize(rawName, maxLength: 60);
        string baseName = Path.GetFileNameWithoutExtension(name);
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Person";

        string stampText = (stamp ?? DateTime.Now).ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
        return $"Dossier_{baseName}_{stampText}.zip";
    }
}
