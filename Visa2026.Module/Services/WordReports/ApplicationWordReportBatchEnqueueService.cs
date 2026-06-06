using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

public sealed class ApplicationWordReportBatchEnqueueResult
{
    public Guid BatchId { get; init; }

    public int ReportCount { get; init; }
}

/// <summary>
/// Queues a <see cref="WordReportGenerationBatch"/> for one <see cref="Application"/>.
/// Shared by the Resminamalar report package dialog and background worker.
/// </summary>
public sealed class ApplicationWordReportBatchEnqueueService
{
    private readonly INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    private readonly ApplicationWordReportPackageCatalogService catalogService;

    public ApplicationWordReportBatchEnqueueService(
        INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory,
        ApplicationWordReportPackageCatalogService catalogService)
    {
        this.nonSecuredObjectSpaceFactory = nonSecuredObjectSpaceFactory;
        this.catalogService = catalogService;
    }

    public bool TryEnqueueApplication(
        IObjectSpace objectSpace,
        Application application,
        string requestedBy,
        out ApplicationWordReportBatchEnqueueResult? result,
        out string? errorMessageKey) =>
        TryEnqueueApplication(objectSpace, application, requestedBy, selectedEntryKeys: null, out result, out errorMessageKey);

    public bool TryEnqueueApplication(
        IObjectSpace objectSpace,
        Application application,
        string requestedBy,
        IReadOnlyList<string>? selectedEntryKeys,
        out ApplicationWordReportBatchEnqueueResult? result,
        out string? errorMessageKey)
    {
        result = null;
        errorMessageKey = null;

        if (application == null)
        {
            errorMessageKey = "WordReports.EnqueueErrorNoApplication";
            return false;
        }

        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            errorMessageKey = "WordReports.EnqueueErrorNotSignedIn";
            return false;
        }

        var applicationId = (Guid)objectSpace.GetKeyValue(application);
        if (applicationId == Guid.Empty)
        {
            errorMessageKey = "WordReports.EnqueueErrorNoApplication";
            return false;
        }

        var catalog = catalogService.Build(objectSpace, application);
        if (catalog.TotalCount == 0)
        {
            errorMessageKey = "WordReports.NoApplicableReports";
            return false;
        }

        var normalizedSelection = ApplicationWordReportPackageSelectionHelper.NormalizeSelection(
            catalog.Entries,
            selectedEntryKeys);

        if (normalizedSelection.Count == 0)
        {
            errorMessageKey = "ApplicationReportPackage.EnqueueErrorNoSelection";
            return false;
        }

        using var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<WordReportGenerationBatch>();
        var batch = os.CreateObject<WordReportGenerationBatch>();
        batch.RequestedBy = requestedBy;
        batch.ApplicationID = applicationId;
        batch.TotalReports = normalizedSelection.Count;
        batch.ProcessedReports = 0;
        batch.SelectedReportKeysJson = ApplicationWordReportPackageSelectionHelper.Serialize(normalizedSelection.ToList());
        batch.Status = WordReportGenerationBatchStatus.Queued;
        os.CommitChanges();

        result = new ApplicationWordReportBatchEnqueueResult
        {
            BatchId = (Guid)os.GetKeyValue(batch)!,
            ReportCount = normalizedSelection.Count
        };
        return true;
    }
}
