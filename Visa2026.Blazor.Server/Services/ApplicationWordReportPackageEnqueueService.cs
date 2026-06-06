using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

    public Task<ApplicationWordReportGeneratedFile?> TryGeneratePreviewAsync(
        Guid applicationId,
        string entryKey) =>
        TryGeneratePreviewAsync(applicationId, entryKey, applicationItemIds: null);

    public async Task<ApplicationWordReportGeneratedFile?> TryGeneratePreviewAsync(
        Guid applicationId,
        string entryKey,
        IReadOnlyList<Guid>? applicationItemIds)
    {
        var bundle = await TryBuildPreviewBundleAsync(applicationId, entryKey, applicationItemIds).ConfigureAwait(false);
        return bundle?.Original;
    }

    public Task<ApplicationWordReportPackagePreviewBundle?> TryBuildPreviewBundleAsync(
        Guid applicationId,
        string entryKey) =>
        TryBuildPreviewBundleAsync(applicationId, entryKey, applicationItemIds: null);

    public async Task<ApplicationWordReportPackagePreviewBundle?> TryBuildPreviewBundleAsync(
        Guid applicationId,
        string entryKey,
        IReadOnlyList<Guid>? applicationItemIds)
    {
        if (applicationId == Guid.Empty || string.IsNullOrWhiteSpace(entryKey))
            return null;

        using var objectSpace = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Application>();
        var application = objectSpace.GetObjectByKey<Application>(applicationId);
        if (application == null)
            return null;

        var context = applicationItemIds is { Count: > 0 }
            ? WordReportGenerationContext.ForApplicationItems(applicationItemIds)
            : WordReportGenerationContext.ForApplication();

        var generatedOutputs = await entryGenerator
            .GenerateManyAsync(objectSpace, application, new HashSet<string>([entryKey], StringComparer.Ordinal), context)
            .ConfigureAwait(false);

        if (generatedOutputs.Count == 0)
            return null;

        var originals = new List<ApplicationWordReportGeneratedFile>();
        var officeParts = new List<(byte[] Content, string FileName)>();

        try
        {
            foreach (var (fileName, stream) in generatedOutputs)
            {
                stream.Position = 0;
                var bytes = stream.ToArray();
                if (bytes.Length == 0)
                    continue;

                originals.Add(new ApplicationWordReportGeneratedFile
                {
                    FileName = fileName,
                    Content = bytes,
                    ContentType = GetContentType(fileName)
                });
                officeParts.Add((bytes, fileName));
            }
        }
        finally
        {
            foreach (var (_, stream) in generatedOutputs)
                stream.Dispose();
        }

        if (originals.Count == 0)
            return null;

        var pdfContent = previewPdfConverter.TryConvertManyToMergedPdf(officeParts);
        return new ApplicationWordReportPackagePreviewBundle
        {
            Originals = originals,
            PdfContent = pdfContent
        };
    }

    private static string GetContentType(string fileName)
    {
        if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase))
        {
            return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }

        return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
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
        IReadOnlyList<string>? selectedEntryKeys = null) =>
        EnqueuePackageAsync(applicationId, selectedEntryKeys, applicationItemIds: null);

    public Task<ApplicationWordReportPackageEnqueueOutcome> EnqueuePackageAsync(
        Guid applicationId,
        IReadOnlyList<string>? selectedEntryKeys,
        IReadOnlyList<Guid>? applicationItemIds)
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

        var context = applicationItemIds is { Count: > 0 }
            ? WordReportGenerationContext.ForApplicationItems(applicationItemIds)
            : WordReportGenerationContext.ForApplication();

        if (!batchEnqueueService.TryEnqueueApplication(
                objectSpace,
                application,
                userName,
                selectedEntryKeys,
                context,
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
