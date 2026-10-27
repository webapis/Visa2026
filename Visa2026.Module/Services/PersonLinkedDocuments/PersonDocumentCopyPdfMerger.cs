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

        var files = record.Files.Where(file => file.HasContent && file.FileDataId != Guid.Empty).ToList();
        if (files.Count == 0)
            return false;

        var layout = printLayout ?? DocumentCopyPrintLayout.DefaultFor(recordKey);
        byte[]? passportBytes = TryLoadLatestPassportScan(objectSpace, snapshot, recordKey);
        var pdfStreams = new List<MemoryStream>();
        try
        {
            foreach (var file in files)
            {
                if (!TryLoadFileContent(objectSpace, file.FileDataId, out var fileContent, out var fileNameForExt))
                    continue;

                if (passportBytes != null
                    && TryCreateVisaBesidePassportStream(passportBytes, fileContent, layout, out var paired))
                {
                    pdfStreams.Add(paired);
                    continue;
                }

                if (!TryCreateMergeSlicePdfStream(
                        fileContent,
                        fileNameForExt,
                        recordKey,
                        layout,
                        out var pdfStream))
                    continue;

                pdfStreams.Add(pdfStream);
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
}
