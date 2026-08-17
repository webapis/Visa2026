using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationItemDocumentFileResult
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    /// <summary>Filled XFA PDFs for pdf.js browser preview (Chrome cannot iframe XFA).</summary>
    public IReadOnlyList<byte[]>? XfaDocuments { get; init; }

    /// <summary>Person.Photo data URIs aligned with <see cref="XfaDocuments"/> (Spire drops XFA images on save).</summary>
    public IReadOnlyList<string?>? PhotoDataUris { get; init; }
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

    public bool TryGetFile(Guid applicationPersonId, Guid fileDataId, out ApplicationItemDocumentFileResult? result)
    {
        result = null;
        if (applicationPersonId == Guid.Empty || fileDataId == Guid.Empty)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Person>();
        var person = objectSpace.GetObjectByKey<Person>(applicationPersonId);
        if (person == null)
            return false;

        ApplicationItemLinkedDocumentsSnapshot? snapshot = null;
        foreach (var application in person.ApplicationProfileInstances ?? [])
        {
            var candidate = ApplicationPersonLinkedDocumentsResolver.Resolve(objectSpace, application, person);
            if (candidate.ContainsFile(fileDataId))
            {
                snapshot = candidate;
                break;
            }
        }

        if (snapshot == null)
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
        IReadOnlyList<Guid> applicationPersonIds,
        string slotKey,
        out ApplicationItemDocumentFileResult? result,
        Guid applicationId = default)
    {
        result = null;
        if (applicationPersonIds == null || applicationPersonIds.Count == 0 || string.IsNullOrWhiteSpace(slotKey))
            return false;

        var rowIds = applicationPersonIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (rowIds.Count == 0)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstance>();
        if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                objectSpace,
                rowIds,
                applicationId,
                out var application,
                out var people)
            || application == null
            || people.Count != rowIds.Count)
        {
            return false;
        }

        var lines = ApplicationPersonLinkedDocumentsResolver.ResolveMany(objectSpace, application, people);
        var mergedGroup = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines)
            .FirstOrDefault(g => string.Equals(g.SlotKey, slotKey, StringComparison.Ordinal));

        if (mergedGroup == null || mergedGroup.Files.Count == 0)
            return false;

        if (!pdfMerger.TryBuildMergedPdfForRoster(
                rowIds,
                slotKey,
                mergedGroup.SlotLabel,
                mergedGroup.Files,
                out var content,
                out var fileName,
                application.ID)
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

    public bool TryGetMergedFamilyPdf(
        IReadOnlyList<Guid> applicationPersonIds,
        string familyKey,
        out ApplicationItemDocumentFileResult? result,
        Guid applicationId = default)
    {
        result = null;
        if (applicationPersonIds == null
            || applicationPersonIds.Count == 0
            || string.IsNullOrWhiteSpace(familyKey))
        {
            return false;
        }

        var rowIds = applicationPersonIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (rowIds.Count == 0)
            return false;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstance>();
        if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                objectSpace,
                rowIds,
                applicationId,
                out var application,
                out var people)
            || application == null
            || people.Count != rowIds.Count)
        {
            return false;
        }

        var lines = ApplicationPersonLinkedDocumentsResolver.ResolveMany(objectSpace, application, people);
        var files = ApplicationItemDocumentCopiesTypeCatalog.CollectFamilyFiles(lines, familyKey);
        if (files.Count == 0)
            return false;

        var title = ApplicationItemDocumentCopiesTypeCatalog.FamilyTitle(familyKey);
        if (!pdfMerger.TryBuildMergedPdfForRoster(
                rowIds,
                familyKey + ".",
                title,
                files,
                out var content,
                out var fileName,
                application.ID)
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
        IReadOnlyList<Guid> applicationPersonIds,
        ApplicationItemDocumentBatchSummaryKind kind,
        ApplicationItemDocumentPackageOptions packageOptions,
        out ApplicationItemDocumentFileResult? result,
        Guid applicationId = default)
    {
        result = null;
        if (!batchSummaryPdfBuilder.TryBuildForRoster(
                applicationPersonIds,
                kind,
                packageOptions,
                out var content,
                out var fileName,
                applicationId)
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
        IReadOnlyList<Guid> applicationPersonIds,
        out ApplicationItemDocumentFileResult? result,
        out string? errorMessageKey,
        Guid applicationId = default)
    {
        result = null;
        errorMessageKey = null;

        if (applicationPersonIds == null || applicationPersonIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        var rowIds = applicationPersonIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (rowIds.Count == 0)
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

            using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstance>();
            if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                    objectSpace,
                    rowIds,
                    applicationId,
                    out var application,
                    out var people)
                || application == null)
            {
                errorMessageKey = "ApplicationItemDocumentCopies.Preview.Error";
                return false;
            }

            var projections = people
                .Select(person => ApplicationProfileInstancePersonPdfPackageLineHydrator.Hydrate(objectSpace, application, person))
                .ToList();

            if (!ApplicationFilledFormPdfGenerator.TryGenerate(
                    objectSpace,
                    pdfFillerService,
                    templatePath,
                    projections,
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

    public bool TryGetFilledApplicationFormPreview(
        IReadOnlyList<Guid> applicationPersonIds,
        out ApplicationItemDocumentFileResult? preview,
        out ApplicationItemDocumentFileResult? download,
        out string? errorMessageKey,
        Guid applicationId = default)
    {
        preview = null;
        download = null;
        errorMessageKey = null;

        if (applicationPersonIds == null || applicationPersonIds.Count == 0)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        var rowIds = applicationPersonIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (rowIds.Count == 0)
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

            using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ApplicationProfileInstance>();
            if (!ApplicationRosterHelper.TryLoadSharedApplicationPeople(
                    objectSpace,
                    rowIds,
                    applicationId,
                    out var application,
                    out var people)
                || application == null)
            {
                errorMessageKey = "ApplicationItemDocumentCopies.Preview.Error";
                return false;
            }

            var projections = people
                .Select(person => ApplicationProfileInstancePersonPdfPackageLineHydrator.Hydrate(objectSpace, application, person))
                .ToList();

            if (!ApplicationFilledFormPdfGenerator.TryGenerateFilledPdfs(
                    objectSpace,
                    pdfFillerService,
                    templatePath,
                    projections,
                    out var filledPdfs,
                    out errorMessageKey)
                || filledPdfs.Count == 0)
            {
                return false;
            }

            var xfaBytes = filledPdfs.Select(item => item.Content).ToList();
            var photoDataUris = filledPdfs
                .Select(item => PdfPersonPhotoDataUri.FromBytes(item.Item?.Person?.Photo))
                .ToList();

            byte[] downloadContent;
            string downloadFileName;
            string downloadContentType;
            if (filledPdfs.Count == 1)
            {
                downloadContent = filledPdfs[0].Content;
                downloadFileName = filledPdfs[0].FileName;
                downloadContentType = "application/pdf";
            }
            else
            {
                downloadContent = ApplicationFilledFormPdfGenerator.BuildZipArchive(filledPdfs);
                downloadFileName = ApplicationFilledFormPdfGenerator.BuildZipFileName(
                    filledPdfs.Select(item => item.Item).ToList());
                downloadContentType = "application/zip";
            }

            preview = new ApplicationItemDocumentFileResult
            {
                Content = xfaBytes[0],
                FileName = filledPdfs.Count == 1
                    ? filledPdfs[0].FileName
                    : downloadFileName.Replace(".zip", ".pdf", StringComparison.OrdinalIgnoreCase),
                ContentType = "application/pdf",
                XfaDocuments = xfaBytes,
                PhotoDataUris = photoDataUris
            };
            download = new ApplicationItemDocumentFileResult
            {
                Content = downloadContent,
                FileName = downloadFileName,
                ContentType = downloadContentType
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
