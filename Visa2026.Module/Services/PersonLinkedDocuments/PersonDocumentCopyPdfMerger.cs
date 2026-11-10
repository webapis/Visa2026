using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

public sealed class PersonDocumentCopyPdfMerger
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ILogger<PersonDocumentCopyPdfMerger> logger;

    public PersonDocumentCopyPdfMerger(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ILogger<PersonDocumentCopyPdfMerger> logger)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.logger = logger;
    }

    public bool TryBuildMergedPdf(
        Guid personId,
        string recordKey,
        string recordLabel,
        out byte[]? content,
        out string? fileName,
        DocumentCopyPrintLayout? printLayout = null)
    {
        content = null;
        fileName = null;

        if (personId == Guid.Empty || string.IsNullOrWhiteSpace(recordKey))
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Person>();
        var person = objectSpace.GetObjectByKey<Person>(personId);
        if (person == null)
            return false;

        var snapshot = PersonLinkedDocumentsResolver.Resolve(objectSpace, person);
        return TryBuildMergedPdf(objectSpace, snapshot, recordKey, recordLabel, out content, out fileName, printLayout);
    }

    /// <summary>
    /// Merges file-bearing records in one catalog section across many people (e.g. Passports
    /// for every checked roster person) into a single PDF, person order preserved.
    /// </summary>
    public bool TryBuildMergedSectionPdf(
        IReadOnlyList<Guid> personIds,
        string sectionId,
        string sectionLabel,
        out byte[]? content,
        out string? fileName,
        DocumentCopyPrintLayout? printLayout = null)
    {
        content = null;
        fileName = null;

        if (personIds == null || personIds.Count == 0 || string.IsNullOrWhiteSpace(sectionId))
            return false;

        var pdfStreams = new List<MemoryStream>();
        try
        {
            using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Person>();
            var documentTypeLabel = string.IsNullOrWhiteSpace(sectionLabel)
                ? PersonDocumentCopiesProfileSectionFilter.PersonSectionLabel(sectionId)
                : sectionLabel.Trim();

            foreach (var personId in personIds.Where(id => id != Guid.Empty).Distinct())
            {
                var person = objectSpace.GetObjectByKey<Person>(personId);
                if (person == null)
                    continue;

                var snapshot = PersonLinkedDocumentsResolver.Resolve(objectSpace, person);
                var catalogSectionId = PersonDocumentCopiesProfileSectionFilter.NormalizeCatalogSectionId(sectionId);
                var section = snapshot.Sections.FirstOrDefault(s =>
                    string.Equals(s.SectionId, catalogSectionId, StringComparison.OrdinalIgnoreCase));

                var records = section?.Records
                    .Where(r => r.Files.Any(f => f.HasContent && f.FileDataId != Guid.Empty))
                    .ToList()
                    ?? new List<PersonLinkedDocumentRecord>();

                var beforeCount = pdfStreams.Count;
                foreach (var record in EnumerateSectionRecordsForMerge(sectionId, records))
                {
                    // Per-record layout so visas use beside-passport pairing under their passport.
                    var layoutForRecord = DocumentCopyPrintLayout.DefaultFor(record.RecordKey);
                    if (!TryBuildMergedPdf(
                            objectSpace,
                            snapshot,
                            record.RecordKey,
                            record.RecordLabel,
                            out var recordPdf,
                            out _,
                            layoutForRecord)
                        || recordPdf == null
                        || recordPdf.Length == 0)
                    {
                        // Visa-beside path failed — still show the passport scan for Passports.
                        if (DocumentCopyPrintLayout.IsVisaKey(record.RecordKey)
                            && TryPassportRecordKey(record.RecordKey, out var passportKey)
                            && TryBuildMergedPdf(
                                objectSpace,
                                snapshot,
                                passportKey,
                                record.RecordLabel,
                                out recordPdf,
                                out _,
                                DocumentCopyPrintLayout.DefaultFor(passportKey))
                            && recordPdf != null
                            && recordPdf.Length > 0)
                        {
                            pdfStreams.Add(new MemoryStream(recordPdf));
                        }

                        continue;
                    }

                    pdfStreams.Add(new MemoryStream(recordPdf));
                }

                // No scan for this person + document type → printable placeholder page.
                if (pdfStreams.Count == beforeCount)
                {
                    var personName = string.IsNullOrWhiteSpace(person.FullName)
                        ? person.ToString() ?? personId.ToString("N")
                        : person.FullName;
                    var placeholder = new MemoryStream();
                    if (SupportingDocumentsPdfSharpHelper.TryWriteMissingDocumentCopyPlaceholderPage(
                            placeholder,
                            personName,
                            documentTypeLabel,
                            VisaUiMessages.Get("PersonDocumentCopies.Preview.MissingFile.Title"),
                            VisaUiMessages.Get("PersonDocumentCopies.Preview.MissingFile.Person"),
                            VisaUiMessages.Get("PersonDocumentCopies.Preview.MissingFile.DocumentType")))
                    {
                        placeholder.Position = 0;
                        pdfStreams.Add(placeholder);
                    }
                    else
                    {
                        placeholder.Dispose();
                    }
                }
            }

            if (pdfStreams.Count == 0)
                return false;

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(pdfStreams, merged);
            content = merged.ToArray();
            var safeLabel = string.IsNullOrWhiteSpace(sectionLabel) ? sectionId : sectionLabel;
            foreach (var c in Path.GetInvalidFileNameChars())
                safeLabel = safeLabel.Replace(c, '_');
            fileName = $"{safeLabel.Trim()}.pdf";
            return content.Length > 0;
        }
        finally
        {
            foreach (var stream in pdfStreams)
                stream.Dispose();
        }
    }

    /// <summary>
    /// Passports: each passport, then its nested visas (visa pages pair beside that passport).
    /// Other sections: non-nested records when present; otherwise all file-bearing rows.
    /// </summary>
    private static IEnumerable<PersonLinkedDocumentRecord> EnumerateSectionRecordsForMerge(
        string sectionId,
        IReadOnlyList<PersonLinkedDocumentRecord> records)
    {
        if (records == null || records.Count == 0)
            yield break;

        var catalogSectionId = PersonDocumentCopiesProfileSectionFilter.NormalizeCatalogSectionId(sectionId);
        if (string.Equals(catalogSectionId, PersonDocumentCopiesProfileSectionFilter.SectionPassports, StringComparison.OrdinalIgnoreCase))
        {
            // Prefer the current passport when it has files; otherwise every passport with files.
            var passports = records.Where(r => !r.IsNested).ToList();
            var currentPassports = passports.Where(r => r.IsCurrent).ToList();
            if (currentPassports.Count > 0)
                passports = currentPassports;

            var includeVisas = PersonDocumentCopiesProfileSectionFilter.IncludesNestedVisas(sectionId);
            foreach (var passport in passports)
            {
                if (!includeVisas)
                {
                    yield return passport;
                    continue;
                }

                var passportPrefix = passport.RecordKey + "/";
                var passportFileIds = passport.Files
                    .Where(f => f.HasContent && f.FileDataId != Guid.Empty)
                    .Select(f => f.FileDataId)
                    .ToHashSet();

                // Every linked visa with its own scan; skip rows that only reuse the passport FileDataId.
                var visas = records
                    .Where(r =>
                        r.IsNested
                        && r.RecordKey.StartsWith(passportPrefix, StringComparison.OrdinalIgnoreCase)
                        && r.Files.Any(f =>
                            f.HasContent
                            && f.FileDataId != Guid.Empty
                            && !passportFileIds.Contains(f.FileDataId)))
                    .ToList();

                // Visa-beside pages already include the passport — emit visas when present;
                // otherwise passport only for that person/passport.
                if (visas.Count > 0)
                {
                    foreach (var visa in visas)
                        yield return visa;
                }
                else
                {
                    yield return passport;
                }
            }

            yield break;
        }

        var preferred = records.Where(r => !r.IsNested).ToList();
        foreach (var record in preferred.Count > 0 ? preferred : records)
            yield return record;
    }

    /// <summary>
    /// Merges one record against an already-resolved snapshot. Bulk callers (the person export
    /// packer) use this so a person with many records does not re-resolve the whole catalog per row.
    /// </summary>
    public bool TryBuildMergedPdf(
        IObjectSpace objectSpace,
        PersonLinkedDocumentsSnapshot snapshot,
        string recordKey,
        string recordLabel,
        out byte[]? content,
        out string? fileName,
        DocumentCopyPrintLayout? printLayout = null)
    {
        content = null;
        fileName = null;

        if (objectSpace == null || snapshot == null || string.IsNullOrWhiteSpace(recordKey))
            return false;

        var record = snapshot.FindRecord(recordKey);
        if (record == null)
            return false;

        // One FileDataId once — duplicate PassportDocument/VisaDocument rows were producing
        // repeated pages (and passport|passport when a visa row reused the passport file).
        var files = record.Files
            .Where(file => file.HasContent && file.FileDataId != Guid.Empty)
            .GroupBy(file => file.FileDataId)
            .Select(group => group.Last())
            .ToList();
        if (files.Count == 0)
            return false;

        var layout = printLayout ?? DocumentCopyPrintLayout.DefaultFor(recordKey);
        var passportScan = LatestPassportScan(snapshot, recordKey);
        byte[]? passportBytes = passportScan == null
            ? null
            : (TryLoadFileContent(objectSpace, passportScan.FileDataId, out var loadedPassport, out _)
                ? loadedPassport
                : null);
        var pdfStreams = new List<MemoryStream>();
        try
        {
            foreach (var file in files)
            {
                if (!TryLoadFileContent(objectSpace, file.FileDataId, out var fileContent, out var fileNameForExt))
                    continue;

                // Do not place passport|passport when the visa attachment is the same file/bytes.
                var sameFileAsPassport = passportScan != null
                    && file.FileDataId == passportScan.FileDataId;
                var sameBytesAsPassport = passportBytes != null
                    && BytesEqual(passportBytes, fileContent);
                var sameAsPassport = sameFileAsPassport || sameBytesAsPassport;

                if (passportBytes != null
                    && !sameAsPassport
                    && TryCreateVisaBesidePassportStream(passportBytes, fileContent, layout, out var paired))
                {
                    pdfStreams.Add(paired);
                    continue;
                }

                // Same scan as the passport: skip the visa half; emit passport alone below.
                // If passport bytes could not be loaded, fall through and render this file once.
                if (sameAsPassport && passportBytes != null)
                    continue;

                if (!TryCreateMergeSlicePdfStream(
                        fileContent,
                        fileNameForExt,
                        recordKey,
                        layout,
                        out var pdfStream))
                    continue;

                pdfStreams.Add(pdfStream);
            }

            // Visa rows that only reused the passport scan: emit the passport alone once.
            if (pdfStreams.Count == 0
                && passportBytes != null
                && DocumentCopyPrintLayout.IsVisaKey(recordKey))
            {
                var passportKeyForLayout = TryPassportRecordKey(recordKey, out var passportKey)
                    ? passportKey
                    : recordKey;
                if (TryCreateMergeSlicePdfStream(
                        passportBytes,
                        "passport.pdf",
                        passportKeyForLayout,
                        DocumentCopyPrintLayout.DefaultFor(passportKeyForLayout),
                        out var passportOnly))
                {
                    pdfStreams.Add(passportOnly);
                }
            }

            // Prefer a single passport page over failing the whole Passports section preview.
            if (pdfStreams.Count == 0 && DocumentCopyPrintLayout.IsPassportKey(recordKey))
            {
                foreach (var file in files)
                {
                    if (!TryLoadFileContent(objectSpace, file.FileDataId, out var fileContent, out var fileNameForExt))
                        continue;
                    if (!TryCreateMergeSlicePdfStream(
                            fileContent,
                            fileNameForExt,
                            recordKey,
                            DocumentCopyPrintLayout.DefaultFor(recordKey),
                            out var passportOnly))
                        continue;
                    pdfStreams.Add(passportOnly);
                    break;
                }
            }

            if (pdfStreams.Count == 0)
                return false;

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(pdfStreams, merged);
            content = merged.ToArray();
            fileName = BuildMergedFileName(files, recordLabel);
            return content.Length > 0;
        }
        finally
        {
            foreach (var stream in pdfStreams)
                stream.Dispose();
        }
    }

    /// <summary>
    /// Latest scan on the passport that owns this visa. PassportDocument has no created date;
    /// the resolver lists files by id, and the last content file is the latest row.
    /// </summary>
    internal static PersonLinkedDocumentFile? LatestPassportScan(
        PersonLinkedDocumentsSnapshot snapshot,
        string visaRecordKey)
    {
        if (snapshot == null || !TryPassportRecordKey(visaRecordKey, out string passportKey))
            return null;

        PersonLinkedDocumentRecord? passport = snapshot.FindRecord(passportKey);
        return passport?.Files.LastOrDefault(file => file.HasContent && file.FileDataId != Guid.Empty);
    }

    internal static bool TryPassportRecordKey(string visaRecordKey, out string passportKey)
    {
        passportKey = string.Empty;
        if (string.IsNullOrWhiteSpace(visaRecordKey))
            return false;

        const string marker = "/Visa:";
        int index = visaRecordKey.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index <= 0)
            return false;

        passportKey = visaRecordKey.Substring(0, index);
        return passportKey.StartsWith("Passport:", StringComparison.OrdinalIgnoreCase);
    }

    private byte[]? TryLoadLatestPassportScan(
        IObjectSpace objectSpace,
        PersonLinkedDocumentsSnapshot snapshot,
        string visaRecordKey)
    {
        if (!DocumentCopyPrintLayout.IsVisaKey(visaRecordKey))
            return null;

        PersonLinkedDocumentFile? latest = LatestPassportScan(snapshot, visaRecordKey);
        if (latest == null)
            return null;

        return TryLoadFileContent(objectSpace, latest.FileDataId, out byte[] content, out _)
            ? content
            : null;
    }

    private bool TryCreateVisaBesidePassportStream(
        byte[] passportBytes,
        byte[] visaBytes,
        DocumentCopyPrintLayout layout,
        out MemoryStream pdfStream)
    {
        pdfStream = null!;
        var outMs = new MemoryStream();
        if (!SupportingDocumentsPdfSharpHelper.TryWriteVisaBesidePassport(
                passportBytes,
                visaBytes,
                outMs,
                logger,
                layout)
            || outMs.Length == 0)
        {
            outMs.Dispose();
            return false;
        }

        outMs.Position = 0;
        pdfStream = outMs;
        return true;
    }

    private bool TryLoadFileContent(
        IObjectSpace objectSpace,
        Guid fileDataId,
        out byte[] content,
        out string fileName)
    {
        content = Array.Empty<byte>();
        fileName = "document";

        var file = objectSpace.GetObjectByKey<FileData>(fileDataId);
        if (file == null || file.Size <= 0)
            return false;

        content = file.Content;
        if (content == null || content.Length == 0)
        {
            content = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == fileDataId)
                .Select(f => f.Content)
                .FirstOrDefault() ?? Array.Empty<byte>();
        }

        if (content.Length == 0)
            return false;

        fileName = string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName;
        return true;
    }

    private bool TryCreateMergeSlicePdfStream(
        byte[] content,
        string sourceFileName,
        string recordKey,
        DocumentCopyPrintLayout layout,
        out MemoryStream pdfStream)
    {
        pdfStream = null!;
        if (content == null || content.Length == 0)
            return false;

        string ext = Path.GetExtension(sourceFileName ?? string.Empty);
        if (IsPdfExtension(ext) && !DocumentFileUploadConstraints.IsLikelyPdf(content))
        {
            logger.LogWarning(
                "Person document copies merge: file {FileName} has PDF extension but payload is not a PDF signature; trying image decode for record {RecordKey}.",
                sourceFileName,
                recordKey);
        }

        bool landscapePage = !layout.UsesDocumentSize
            && recordKey.Contains("/Visa:", StringComparison.OrdinalIgnoreCase);
        var outMs = new MemoryStream();
        if (!SupportingDocumentsPdfSharpHelper.TryWriteSlice(content, outMs, logger, layout, landscapePage))
            return false;

        outMs.Position = 0;
        pdfStream = outMs;
        return true;
    }

    private static bool IsPdfExtension(string ext) =>
        ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    private static string BuildMergedFileName(
        IReadOnlyList<PersonLinkedDocumentFile> files,
        string recordLabel)
    {
        if (files.Count == 1)
        {
            var singleName = files[0].FileName;
            if (!string.IsNullOrWhiteSpace(singleName))
            {
                string baseName = Path.GetFileNameWithoutExtension(singleName);
                if (!string.IsNullOrWhiteSpace(baseName))
                    return SanitizeFileName(baseName) + ".pdf";
            }
        }

        if (!string.IsNullOrWhiteSpace(recordLabel))
            return SanitizeFileName(recordLabel) + ".pdf";

        return "person-document-copies.pdf";
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "person-document-copies";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value
            .Select(ch => invalid.Contains(ch) ? '-' : ch)
            .ToArray();
        var sanitized = new string(chars).Trim('-', ' ');
        return string.IsNullOrWhiteSpace(sanitized) ? "person-document-copies" : sanitized;
    }

    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
            return false;
        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }
}
