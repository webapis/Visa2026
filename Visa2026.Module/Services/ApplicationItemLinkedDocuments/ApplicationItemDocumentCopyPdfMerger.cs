using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public sealed class ApplicationItemDocumentCopyPdfMerger
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ILogger<ApplicationItemDocumentCopyPdfMerger> logger;

    public ApplicationItemDocumentCopyPdfMerger(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ILogger<ApplicationItemDocumentCopyPdfMerger> logger)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.logger = logger;
    }

    public bool TryBuildMergedPdf(
        IReadOnlyList<Guid> applicationItemIds,
        string slotKey,
        string slotLabel,
        IReadOnlyList<ApplicationItemLinkedDocumentFileEntry> entries,
        out byte[]? content,
        out string? fileName)
    {
        content = null;
        fileName = null;

        if (applicationItemIds == null || applicationItemIds.Count == 0 || string.IsNullOrWhiteSpace(slotKey))
            return false;

        if (entries == null || entries.Count == 0)
            return false;

        var allowedItemIds = applicationItemIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        if (allowedItemIds.Count == 0)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationItem>();
        var snapshots = new Dictionary<Guid, ApplicationItemLinkedDocumentsSnapshot>();

        foreach (var itemId in allowedItemIds)
        {
            var item = objectSpace.GetObjectByKey<ApplicationItem>(itemId);
            if (item == null)
                return false;

            snapshots[itemId] = ApplicationItemLinkedDocumentsResolver.Resolve(objectSpace, item);
        }

        foreach (var entry in entries)
        {
            if (!allowedItemIds.Contains(entry.ApplicationItemId))
                return false;

            if (!snapshots.TryGetValue(entry.ApplicationItemId, out var snapshot))
                return false;

            if (!snapshot.ContainsFile(entry.File.FileDataId))
                return false;
        }

        var pdfStreams = new List<MemoryStream>();
        try
        {
            foreach (var entry in entries)
            {
                if (!TryLoadFileContent(objectSpace, entry.File.FileDataId, out var fileContent, out var fileNameForExt))
                    continue;

                if (!TryCreateMergeSlicePdfStream(fileContent, fileNameForExt, slotKey, out var pdfStream))
                    continue;

                pdfStreams.Add(pdfStream);
            }

            if (pdfStreams.Count == 0)
                return false;

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(pdfStreams, merged);
            content = merged.ToArray();
            fileName = BuildMergedFileName(entries, slotLabel);
            return content.Length > 0;
        }
        finally
        {
            foreach (var stream in pdfStreams)
                stream.Dispose();
        }
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
        string slotKey,
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
                "Document copies merge: file {FileName} has PDF extension but payload is not a PDF signature; trying image decode for slot {SlotKey}.",
                sourceFileName,
                slotKey);
        }

        bool landscapePage = slotKey.StartsWith("Visa.", StringComparison.OrdinalIgnoreCase);
        var outMs = new MemoryStream();
        if (!SupportingDocumentsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes(content, outMs, logger, landscapePage))
            return false;

        outMs.Position = 0;
        pdfStream = outMs;
        return true;
    }

    private static bool IsPdfExtension(string ext) =>
        ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    private static string BuildMergedFileName(
        IReadOnlyList<ApplicationItemLinkedDocumentFileEntry> entries,
        string slotLabel)
    {
        if (entries.Count == 1)
        {
            var singleName = entries[0].File.FileName;
            if (!string.IsNullOrWhiteSpace(singleName))
            {
                string baseName = Path.GetFileNameWithoutExtension(singleName);
                if (!string.IsNullOrWhiteSpace(baseName))
                    return SanitizeFileName(baseName) + ".pdf";
            }
        }

        if (!string.IsNullOrWhiteSpace(slotLabel))
            return SanitizeFileName(slotLabel) + ".pdf";

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
