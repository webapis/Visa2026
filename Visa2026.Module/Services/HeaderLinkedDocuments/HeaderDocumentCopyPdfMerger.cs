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
        out string? fileName)
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

        var pdfStreams = new List<MemoryStream>();
        try
        {
            foreach (var file in files)
            {
                if (!TryLoadFileContent(objectSpace, file.FileDataId, out var fileContent, out var fileNameForExt))
                    continue;

                if (!TryCreateMergeSlicePdfStream(fileContent, fileNameForExt, recordKey, out var pdfStream))
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

    private IObjectSpace? CreateObjectSpace(HeaderDocumentCopiesFamily family) =>
        family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<WorkPermit>(),
            HeaderDocumentCopiesFamily.Invitation => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Invitation>(),
            HeaderDocumentCopiesFamily.Rejection => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Rejection>(),
            HeaderDocumentCopiesFamily.BorderZone => nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<BorderZone>(),
            _ => null,
        };

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
        out MemoryStream pdfStream)
    {
        pdfStream = null!;
        if (content == null || content.Length == 0)
            return false;

        string ext = Path.GetExtension(sourceFileName ?? string.Empty);

        if (DocumentFileUploadConstraints.IsLikelyPdf(content))
        {
            var copy = new MemoryStream(content.Length);
            copy.Write(content, 0, content.Length);
            copy.Position = 0;
            pdfStream = copy;
            return true;
        }

        if (IsPdfExtension(ext))
        {
            logger.LogWarning(
                "Header document copies merge: file {FileName} has PDF extension but payload is not a PDF signature; trying image decode for record {RecordKey}.",
                sourceFileName,
                recordKey);
        }

        var outMs = new MemoryStream();
        if (!SupportingDocumentsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes(content, outMs, logger, landscape: false))
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
