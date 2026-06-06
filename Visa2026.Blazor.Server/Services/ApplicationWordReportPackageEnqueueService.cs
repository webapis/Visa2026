using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.AspNetCore.Http;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationWordReportPackageFileAccess
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ApplicationWordReportEntryGenerator entryGenerator;
    private readonly ApplicationWordReportOfficePreviewPdfConverter previewPdfConverter;

    public ApplicationWordReportPackageFileAccess(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ApplicationWordReportEntryGenerator entryGenerator,
        ApplicationWordReportOfficePreviewPdfConverter previewPdfConverter)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.entryGenerator = entryGenerator;
        this.previewPdfConverter = previewPdfConverter;
    }

    public async Task<ApplicationWordReportGeneratedFile?> TryGeneratePreviewAsync(
        Guid applicationId,
        string entryKey)
    {
        var bundle = await TryBuildPreviewBundleAsync(applicationId, entryKey).ConfigureAwait(false);
        return bundle?.Original;
    }

    public async Task<ApplicationWordReportPackagePreviewBundle?> TryBuildPreviewBundleAsync(
        Guid applicationId,
        string entryKey)
    {
        if (applicationId == Guid.Empty || string.IsNullOrWhiteSpace(entryKey))
            return null;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Application>();
        var application = objectSpace.GetObjectByKey<Application>(applicationId);
        if (application == null)
            return null;

        var generated = await entryGenerator.TryGenerateSingleAsync(objectSpace, application, entryKey)
            .ConfigureAwait(false);
        if (generated == null || generated.Content.Length == 0)
            return null;

        var pdfContent = previewPdfConverter.TryConvertToPdf(generated.Content, generated.FileName);
        return new ApplicationWordReportPackagePreviewBundle
        {
            Original = generated,
            PdfContent = pdfContent
        };
    }
}

public sealed class ApplicationWordReportPackageEnqueueService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ApplicationWordReportBatchEnqueueService batchEnqueueService;
    private readonly IWordReportBatchTrackNotifier batchTrackNotifier;

    public ApplicationWordReportPackageEnqueueService(
        IHttpContextAccessor httpContextAccessor,
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ApplicationWordReportBatchEnqueueService batchEnqueueService,
        IWordReportBatchTrackNotifier batchTrackNotifier)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.batchEnqueueService = batchEnqueueService;
        this.batchTrackNotifier = batchTrackNotifier;
    }

    public Task<ApplicationWordReportPackageEnqueueOutcome> EnqueuePackageAsync(
        Guid applicationId,
        IReadOnlyList<string>? selectedEntryKeys = null)
    {
        string? userName = httpContextAccessor.HttpContext?.User?.Identity?.Name
            ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(userName))
        {
            return Task.FromResult(new ApplicationWordReportPackageEnqueueOutcome
            {
                Success = false,
                ErrorMessage = VisaUiMessages.Get("WordReports.EnqueueErrorNotSignedIn")
            });
        }

        if (applicationId == Guid.Empty)
        {
            return Task.FromResult(new ApplicationWordReportPackageEnqueueOutcome
            {
                Success = false,
                ErrorMessage = VisaUiMessages.Get("WordReports.EnqueueErrorNoApplication")
            });
        }

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Application>();
        var application = objectSpace.GetObjectByKey<Application>(applicationId);
        if (application == null)
        {
            return Task.FromResult(new ApplicationWordReportPackageEnqueueOutcome
            {
                Success = false,
                ErrorMessage = VisaUiMessages.Get("WordReports.EnqueueErrorNoApplication")
            });
        }

        if (!batchEnqueueService.TryEnqueueApplication(
                objectSpace,
                application,
                userName,
                selectedEntryKeys,
                out var result,
                out var errorMessageKey)
            || result == null)
        {
            return Task.FromResult(new ApplicationWordReportPackageEnqueueOutcome
            {
                Success = false,
                ErrorMessage = !string.IsNullOrWhiteSpace(errorMessageKey)
                    ? VisaUiMessages.Get(errorMessageKey)
                    : VisaUiMessages.Get("WordReports.EnqueueError")
            });
        }

        batchTrackNotifier.TrackQueuedBatch(result.BatchId, userName);

        return Task.FromResult(new ApplicationWordReportPackageEnqueueOutcome
        {
            Success = true,
            BatchId = result.BatchId,
            ReportCount = result.ReportCount
        });
    }
}

public sealed class ApplicationWordReportPackageEnqueueOutcome
{
    public bool Success { get; init; }

    public Guid BatchId { get; init; }

    public int ReportCount { get; init; }

    public string? ErrorMessage { get; init; }
}
