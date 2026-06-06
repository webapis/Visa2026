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

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

/// <summary>
/// Builds batch-style merged summary PDFs (same content as ZIP entries like <c>CurrentPassports.pdf</c>).
/// </summary>
public sealed class ApplicationItemDocumentBatchSummaryPdfBuilder
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ApplicationItemDocumentCopyPdfMerger slotMerger;
    private readonly ILogger<ApplicationItemDocumentBatchSummaryPdfBuilder> logger;

    public ApplicationItemDocumentBatchSummaryPdfBuilder(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ApplicationItemDocumentCopyPdfMerger slotMerger,
        ILogger<ApplicationItemDocumentBatchSummaryPdfBuilder> logger)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.slotMerger = slotMerger;
        this.logger = logger;
    }

    public bool TryBuild(
        IReadOnlyList<Guid> applicationItemIds,
        ApplicationItemDocumentBatchSummaryKind kind,
        ApplicationItemDocumentPackageOptions packageOptions,
        out byte[]? content,
        out string? fileName)
    {
        content = null;
        fileName = ApplicationItemDocumentBatchSummaryKindMapping.GetDownloadFileName(kind);

        var itemIds = applicationItemIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList() ?? new List<Guid>();

        if (itemIds.Count == 0)
            return false;

        if (kind == ApplicationItemDocumentBatchSummaryKind.AllDiplomas)
            return TryBuildAllDiplomasPdf(itemIds, packageOptions, out content);

        string? slotKey = kind switch
        {
            ApplicationItemDocumentBatchSummaryKind.CurrentPassports => "Passport.Current",
            ApplicationItemDocumentBatchSummaryKind.CurrentVisas => "Visa.Current",
            ApplicationItemDocumentBatchSummaryKind.CurrentWorkPermits => "WorkPermit.Current",
            _ => null
        };

        if (slotKey == null)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationItem>();
        var items = itemIds
            .Select(id => objectSpace.GetObjectByKey<ApplicationItem>(id))
            .Where(item => item != null)
            .Cast<ApplicationItem>()
            .ToList();

        if (items.Count != itemIds.Count)
            return false;

        var lines = ApplicationItemLinkedDocumentsResolver.ResolveMany(objectSpace, items);
        var mergedGroup = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines)
            .FirstOrDefault(g => string.Equals(g.SlotKey, slotKey, StringComparison.Ordinal));

        if (mergedGroup == null || mergedGroup.Files.Count == 0)
            return false;

        if (!slotMerger.TryBuildMergedPdf(
                itemIds,
                slotKey,
                mergedGroup.SlotLabel,
                mergedGroup.Files,
                out content,
                out _)
            || content == null
            || content.Length == 0)
        {
            return false;
        }

        return true;
    }

    private bool TryBuildAllDiplomasPdf(
        IReadOnlyList<Guid> itemIds,
        ApplicationItemDocumentPackageOptions packageOptions,
        out byte[]? content)
    {
        content = null;
        if (packageOptions.DiplomaScope != PdfBatchDiplomaScope.AllEducations)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationItem>();
        var pdfStreams = new List<MemoryStream>();

        try
        {
            foreach (var itemId in itemIds)
            {
                var item = objectSpace.GetObjectByKey<ApplicationItem>(itemId);
                if (item?.Person == null)
                    continue;

                var educations = objectSpace.GetObjectsQuery<Education>()
                    .Where(e => e.Person.ID == item.Person.ID)
                    .Include(e => e.EducationInstitution)
                    .AsEnumerable()
                    .OrderByDescending(e => ParseGraduationYearForSort(e.GraduationYear))
                    .ThenBy(e => e.EducationInstitution?.Name ?? string.Empty)
                    .ToList();

                foreach (var education in educations)
                {
                    var docs = objectSpace.GetObjectsQuery<EducationDocument>()
                        .Where(d => d.Education.ID == education.ID)
                        .OrderBy(d => d.ID)
                        .Include(d => d.File)
                        .ToList();

                    foreach (var doc in docs)
                    {
                        if (!TryLoadDocumentContent(objectSpace, doc, out var fileContent, out var sourceFileName))
                            continue;

                        if (!TryCreateMergeSlicePdfStream(fileContent, sourceFileName, landscape: false, out var slice))
                            continue;

                        pdfStreams.Add(slice);
                    }
                }
            }

            if (pdfStreams.Count == 0)
                return false;

            using var merged = new MemoryStream();
            SupportingDocumentsPdfSharpHelper.MergePdfStreams(pdfStreams, merged);
            content = merged.ToArray();
            return content.Length > 0;
        }
        finally
        {
            foreach (var stream in pdfStreams)
                stream.Dispose();
        }
    }

    private bool TryLoadDocumentContent(
        IObjectSpace objectSpace,
        DocumentBase doc,
        out byte[] content,
        out string fileName)
    {
        content = Array.Empty<byte>();
        fileName = "document";

        var file = doc.File;
        if (file == null || file.Size <= 0)
            return false;

        content = file.Content;
        if (content == null || content.Length == 0)
        {
            content = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == file.ID)
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
        bool landscape,
        out MemoryStream pdfStream)
    {
        pdfStream = null!;
        if (content.Length == 0)
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

        var outMs = new MemoryStream();
        if (!SupportingDocumentsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes(content, outMs, logger, landscape))
            return false;

        outMs.Position = 0;
        pdfStream = outMs;
        return true;
    }

    private static int ParseGraduationYearForSort(string? graduationYear)
    {
        if (string.IsNullOrWhiteSpace(graduationYear))
            return 0;

        return int.TryParse(graduationYear.Trim(), out var year) ? year : 0;
    }
}
