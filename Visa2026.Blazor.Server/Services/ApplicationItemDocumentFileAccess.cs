using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationItemDocumentFileResult
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }
}

public sealed class ApplicationItemDocumentFileAccess
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ApplicationItemDocumentCopyPdfMerger pdfMerger;
    private readonly ApplicationItemDocumentBatchSummaryPdfBuilder batchSummaryPdfBuilder;
    private readonly IConfiguration configuration;
    private readonly IPdfFormFillerService pdfFillerService;

    public ApplicationItemDocumentFileAccess(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ApplicationItemDocumentCopyPdfMerger pdfMerger,
        ApplicationItemDocumentBatchSummaryPdfBuilder batchSummaryPdfBuilder,
        IConfiguration configuration,
        IPdfFormFillerService pdfFillerService)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.pdfMerger = pdfMerger;
        this.batchSummaryPdfBuilder = batchSummaryPdfBuilder;
        this.configuration = configuration;
        this.pdfFillerService = pdfFillerService;
    }

    public bool TryGetFile(Guid applicationItemId, Guid fileDataId, out ApplicationItemDocumentFileResult? result)
    {
        result = null;
        if (applicationItemId == Guid.Empty || fileDataId == Guid.Empty)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationItem>();
        var item = objectSpace.GetObjectByKey<ApplicationItem>(applicationItemId);
        if (item == null)
            return false;

        var snapshot = ApplicationItemLinkedDocumentsResolver.Resolve(objectSpace, item);
        if (!snapshot.ContainsFile(fileDataId))
            return false;

        var file = objectSpace.GetObjectByKey<FileData>(fileDataId);
        if (file == null || file.Size <= 0)
            return false;

        var content = file.Content;
        if (content == null || content.Length == 0)
        {
            content = objectSpace.GetObjectsQuery<FileData>()
                .Where(f => f.ID == fileDataId)
                .Select(f => f.Content)
                .FirstOrDefault();
        }

        if (content == null || content.Length == 0)
            return false;

        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "document" : file.FileName;
        result = new ApplicationItemDocumentFileResult
        {
            Content = content,
            FileName = fileName,
            ContentType = DocumentFileContentTypes.GetContentType(fileName)
        };
        return true;
    }

    public bool TryGetMergedSlotPdf(
        IReadOnlyList<Guid> applicationItemIds,
        string slotKey,
        out ApplicationItemDocumentFileResult? result)
    {
        result = null;
        if (applicationItemIds == null || applicationItemIds.Count == 0 || string.IsNullOrWhiteSpace(slotKey))
            return false;

        var itemIds = applicationItemIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (itemIds.Count == 0)
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

        if (!pdfMerger.TryBuildMergedPdf(
                itemIds,
                slotKey,
                mergedGroup.SlotLabel,
                mergedGroup.Files,
                out var content,
                out var fileName)
            || content == null
            || content.Length == 0
            || string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        result = new ApplicationItemDocumentFileResult
        {
            Content = content,
            FileName = fileName,
            ContentType = "application/pdf"
        };
        return true;
    }

    public bool TryGetBatchSummaryPdf(
        IReadOnlyList<Guid> applicationItemIds,
        ApplicationItemDocumentBatchSummaryKind kind,
        ApplicationItemDocumentPackageOptions packageOptions,
        out ApplicationItemDocumentFileResult? result)
    {
        result = null;

        if (!batchSummaryPdfBuilder.TryBuild(
                applicationItemIds,
                kind,
                packageOptions,
                out var content,
                out var fileName)
            || content == null
            || content.Length == 0
            || string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        result = new ApplicationItemDocumentFileResult
        {
            Content = content,
            FileName = fileName,
            ContentType = "application/pdf"
        };
        return true;
    }

    public bool TryGetFilledApplicationFormPdf(
        IReadOnlyList<Guid> applicationItemIds,
        out ApplicationItemDocumentFileResult? result,
        out string? errorMessageKey)
    {
        result = null;
        errorMessageKey = null;

        if (applicationItemIds == null || applicationItemIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        var itemIds = applicationItemIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (itemIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        var relativeTemplatePath = configuration["PdfSettings:TemplatePath"];
        if (string.IsNullOrWhiteSpace(relativeTemplatePath))
        {
            errorMessageKey = "ApplicationPdf.TemplatePathNotConfigured";
            return false;
        }

        string? temporaryTemplatePath = null;
        try
        {
            var templatePath = ApplicationFilledFormPdfGenerator.ResolveTemplatePath(
                relativeTemplatePath,
                out temporaryTemplatePath);
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                errorMessageKey = "ApplicationPdf.TemplateNotFound";
                return false;
            }

            using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationItem>();
            var items = itemIds
                .Select(id => objectSpace.GetObjectByKey<ApplicationItem>(id))
                .Where(item => item != null)
                .Cast<ApplicationItem>()
                .ToList();

            if (!ApplicationFilledFormPdfGenerator.TryGenerate(
                    objectSpace,
                    pdfFillerService,
                    templatePath,
                    items,
                    out var content,
                    out var fileName,
                    out var contentType,
                    out errorMessageKey)
                || content == null
                || content.Length == 0
                || string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            result = new ApplicationItemDocumentFileResult
            {
                Content = content,
                FileName = fileName,
                ContentType = contentType
            };
            return true;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryTemplatePath))
            {
                try
                {
                    File.Delete(temporaryTemplatePath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
