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
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Module.Services.HeaderLinkedDocuments;

public sealed class HeaderDocumentCopyPdfMerger
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ILogger<HeaderDocumentCopyPdfMerger> logger;

    public HeaderDocumentCopyPdfMerger(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ILogger<HeaderDocumentCopyPdfMerger> logger)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.logger = logger;
    }

    public bool TryBuildMergedPdf(
        HeaderDocumentCopiesFamily family,
        Guid parentId,
        string recordKey,
        string recordLabel,
        out byte[]? content,
        out string? fileName,
        DocumentCopyPrintLayout? printLayout = null)
    {
        content = null;
        fileName = null;

        if (parentId == Guid.Empty || string.IsNullOrWhiteSpace(recordKey))
            return false;

        using var objectSpace = CreateObjectSpace(family);
        if (objectSpace == null)
            return false;

        var snapshot = HeaderLinkedDocumentsResolver.Resolve(objectSpace, family, parentId);
        var record = snapshot.FindRecord(recordKey);
        if (record == null)
            return false;

        var files = record.Files.Where(file => file.HasContent && file.FileDataId != Guid.Empty).ToList();
        if (files.Count == 0)
            return false;

        var layout = printLayout ?? DocumentCopyPrintLayout.DefaultFor(recordKey);
        byte[]? passportBytes = family == HeaderDocumentCopiesFamily.Visa
            && DocumentCopyPrintLayout.IsVisaKey(recordKey)
                ? LoadLatestPassportScan(objectSpace, parentId)
                : null;
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

    /// <summary>Every scan on the header, in catalog order, as one PDF.</summary>
    public bool TryBuildAllCopiesPdf(
        HeaderDocumentCopiesFamily family,
        Guid parentId,
        string headerLabel,
        out byte[]? content,
        out string? fileName)
    {
        content = null;
        fileName = null;

        if (parentId == Guid.Empty)
            return false;

        using var objectSpace = CreateObjectSpace(family);
        if (objectSpace == null)
            return false;

        var snapshot = HeaderLinkedDocumentsResolver.Resolve(objectSpace, family, parentId);
        byte[]? passportBytes = family == HeaderDocumentCopiesFamily.Visa
            ? LoadLatestPassportScan(objectSpace, parentId)
            : null;
        var pdfStreams = new List<MemoryStream>();
        try
        {
            foreach (var record in snapshot.Records)
            {
                if (string.IsNullOrWhiteSpace(record.RecordKey))
                    continue;

                var layout = DocumentCopyPrintLayout.DefaultFor(record.RecordKey);
                bool pairWithPassport = passportBytes != null
                    && DocumentCopyPrintLayout.IsVisaKey(record.RecordKey);

                foreach (var file in record.Files.Where(file => file.HasContent && file.FileDataId != Guid.Empty))
                {
                    if (!TryLoadFileContent(objectSpace, file.FileDataId, out var fileContent, out var fileNameForExt))
                        continue;

                    if (pairWithPassport
                        && TryCreateVisaBesidePassportStream(passportBytes!, fileContent, layout, out var paired))
                    {
                        pdfStreams.Add(paired);
                        continue;
                    }

                    if (!TryCreateMergeSlicePdfStream(
                            fileContent,
                            fileNameForExt,
                            record.RecordKey,
                            layout,
                            out var pdfStream))
                        continue;

                    pdfStreams.Add(pdfStream);
                }
            }

            if (pdfStreams.Count == 0)
                return false;

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(pdfStreams, merged);
            content = merged.ToArray();
            fileName = SanitizeFileName(string.IsNullOrWhiteSpace(headerLabel) ? "document-copies" : headerLabel) + ".pdf";
            return content.Length > 0;
        }
        finally
        {
            foreach (var stream in pdfStreams)
                stream.Dispose();
        }
    }

    private IObjectSpace? CreateObjectSpace(HeaderDocumentCopiesFamily family) =>
        family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<WorkPermit>(),
            HeaderDocumentCopiesFamily.Invitation => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Invitation>(),
            HeaderDocumentCopiesFamily.Rejection => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Rejection>(),
            HeaderDocumentCopiesFamily.BorderZone => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<BorderZone>(),
            HeaderDocumentCopiesFamily.Visa => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Visa>(),
            _ => null,
        };

    /// <summary>
    /// Latest PassportDocument on the passport that owns the visa. Those rows have no created date;
    /// files are listed by id, and the last content file is the latest row.
    /// </summary>
    private byte[]? LoadLatestPassportScan(IObjectSpace objectSpace, Guid visaId)
    {
        Guid passportId = objectSpace.GetObjectsQuery<Visa>()
            .Where(visa => visa.ID == visaId && visa.Passport != null)
            .Select(visa => visa.Passport.ID)
            .FirstOrDefault();
        if (passportId == Guid.Empty)
            return null;

        ApplicationItemLinkedDocumentFile? latest = LatestPassportContentFile(
            ApplicationItemLinkedDocumentsResolver.LoadDocumentFiles<PassportDocument>(
                objectSpace,
                document => document.Passport.ID == passportId));
        if (latest == null)
            return null;

        return TryLoadFileContent(objectSpace, latest.FileDataId, out byte[] content, out _)
            ? content
            : null;
    }

    internal static ApplicationItemLinkedDocumentFile? LatestPassportContentFile(
        IReadOnlyList<ApplicationItemLinkedDocumentFile> files) =>
        files?.LastOrDefault(file => file.HasContent && file.FileDataId != Guid.Empty);

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
                "Header document copies merge: file {FileName} has PDF extension but payload is not a PDF signature; trying image decode for record {RecordKey}.",
                sourceFileName,
                recordKey);
        }

        var outMs = new MemoryStream();
        if (!SupportingDocumentsPdfSharpHelper.TryWriteSlice(content, outMs, logger, layout, fitA4Landscape: false))
            return false;

        outMs.Position = 0;
        pdfStream = outMs;
        return true;
    }

    private static bool IsPdfExtension(string ext) =>
        ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    private static string BuildMergedFileName(
        IReadOnlyList<HeaderLinkedDocumentFile> files,
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

        return "document-copies.pdf";
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "document-copies";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = value
            .Select(ch => invalid.Contains(ch) ? '-' : ch)
            .ToArray();
        var sanitized = new string(chars).Trim('-', ' ');
        return string.IsNullOrWhiteSpace(sanitized) ? "document-copies" : sanitized;
    }
}