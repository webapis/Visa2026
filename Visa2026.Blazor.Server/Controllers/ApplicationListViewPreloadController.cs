using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// EF Include + display-cache warmup for large <see cref="Application"/> ListViews.
/// Sync preloads the first viewport; background + scroll-ahead cover the rest.
/// </summary>
public sealed class ApplicationListViewPreloadController : ViewController<ListView>
{
    private const int BatchSize = 200;
    private const int InitialSyncBatchCount = 4;
    private const int ScrollAheadRows = 160;
    private const int ScrollBehindRows = 40;
    private const int BackgroundYieldEveryBatches = 3;

    private readonly HashSet<Guid> preloadedIds = new();
    private readonly Dictionary<Guid, IReadOnlyList<ApplicationWorkspaceListProgressSteps.Step>> progressById = new();
    private readonly Dictionary<Guid, IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip>> resultById = new();
    private EventHandler? collectionReloadedHandler;
    private EventHandler? collectionChangedHandler;
    private EventHandler<ComponentInstanceCapturedEventArgs<IGrid>>? gridCapturedHandler;
    private CancellationTokenSource? preloadCts;
    private int lastScrollAheadVisibleIndex = -1;

    public ApplicationListViewPreloadController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        // Toolbar Refresh recreates the collection (CollectionChanged). It does not raise CollectionReloaded.
        collectionReloadedHandler ??= (_, _) => StartPreload(syncBatches: InitialSyncBatchCount);
        collectionChangedHandler ??= (_, _) => StartPreload(syncBatches: InitialSyncBatchCount);
        View.CollectionSource.CollectionReloaded += collectionReloadedHandler;
        View.CollectionSource.CollectionChanged += collectionChangedHandler;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (View.Editor is DxGridListEditor gridListEditor)
        {
            gridListEditor.GridModel.TextWrapEnabled = false;
            gridCapturedHandler ??= (_, _) => StartPreload(syncBatches: InitialSyncBatchCount);
            gridListEditor.GridModel.ComponentInstanceCaptured += gridCapturedHandler;
        }

        StartPreload(syncBatches: InitialSyncBatchCount);
    }

    protected override void OnDeactivated()
    {
        preloadCts?.Cancel();
        preloadCts?.Dispose();
        preloadCts = null;
        preloadedIds.Clear();
        lastScrollAheadVisibleIndex = -1;

        if (gridCapturedHandler != null && View.Editor is DxGridListEditor gridListEditor)
            gridListEditor.GridModel.ComponentInstanceCaptured -= gridCapturedHandler;

        if (collectionReloadedHandler != null)
            View.CollectionSource.CollectionReloaded -= collectionReloadedHandler;
        if (collectionChangedHandler != null)
            View.CollectionSource.CollectionChanged -= collectionChangedHandler;

        base.OnDeactivated();
    }

    internal IReadOnlyList<ApplicationWorkspaceListProgressSteps.Step> ProgressStepsFor(Guid id) =>
        id != Guid.Empty && progressById.TryGetValue(id, out var steps)
            ? steps
            : Array.Empty<ApplicationWorkspaceListProgressSteps.Step>();

    internal IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip> ResultChipsFor(Guid id) =>
        id != Guid.Empty && resultById.TryGetValue(id, out var chips)
            ? chips
            : Array.Empty<ApplicationWorkspaceIssuedResultListCoverage.Chip>();

    internal void EnsureScrollAheadIfNeeded(IGrid grid, int visibleIndex)
    {
        if (visibleIndex < 0)
            return;

        if (lastScrollAheadVisibleIndex >= 0
            && visibleIndex >= lastScrollAheadVisibleIndex
            && visibleIndex < lastScrollAheadVisibleIndex + 40)
            return;

        EnsureScrollAhead(grid, visibleIndex);
    }

    private void EnsureScrollAhead(IGrid grid, int visibleIndex)
    {
        var pendingIds = new List<Guid>();
        for (var rowIndex = Math.Max(0, visibleIndex - ScrollBehindRows); rowIndex < visibleIndex + ScrollAheadRows; rowIndex++)
        {
            if (grid.GetDataItem(rowIndex) is not ApplicationProfileInstance application)
                break;

            if (!preloadedIds.Contains(application.ID))
                pendingIds.Add(application.ID);
        }

        if (pendingIds.Count == 0)
            return;

        lastScrollAheadVisibleIndex = visibleIndex;
        PreloadByIds(pendingIds);
    }

    private void StartPreload(int syncBatches = 0)
    {
        if (View?.CollectionSource.List == null || View.CollectionSource.List.Count == 0)
            return;

        // Paint the previous fill onto the new instances before the grid renders.
        ApplyIdCacheToListRows();

        preloadCts?.Cancel();
        preloadCts?.Dispose();
        preloadCts = new CancellationTokenSource();
        preloadedIds.Clear();
        lastScrollAheadVisibleIndex = -1;
        _ = PreloadListApplicationsAsync(preloadCts.Token, syncBatches);
    }

    private async Task PreloadListApplicationsAsync(CancellationToken cancellationToken, int syncBatches)
    {
        if (View?.CollectionSource.List == null || View.CollectionSource.List.Count == 0)
            return;

        var ids = View.CollectionSource.List.OfType<ApplicationProfileInstance>().Select(a => a.ID).Distinct().ToList();
        var syncRowCount = Math.Min(syncBatches * BatchSize, ids.Count);
        for (var offset = 0; offset < syncRowCount; offset += BatchSize)
            PreloadByIds(ids.Skip(offset).Take(BatchSize).ToList());

        if (syncRowCount > 0)
            RepaintLoadedRows();

        if (syncRowCount >= ids.Count)
            return;

        await Task.Yield();
        var batchesSinceYield = 0;
        for (var offset = syncRowCount; offset < ids.Count; offset += BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PreloadByIds(ids.Skip(offset).Take(BatchSize).ToList());
            if (++batchesSinceYield < BackgroundYieldEveryBatches)
                continue;

            batchesSinceYield = 0;
            await Task.Yield();
        }
    }

    private void PreloadByIds(IReadOnlyList<Guid> ids)
    {
        if (ids.Count == 0)
            return;

        var pendingIds = ids.Where(id => !preloadedIds.Contains(id)).Distinct().ToList();
        if (pendingIds.Count == 0)
            return;

        for (var offset = 0; offset < pendingIds.Count; offset += BatchSize)
            PreloadBatch(pendingIds.Skip(offset).Take(BatchSize).ToList());
    }

    private void PreloadBatch(List<Guid> batchIds)
    {
        if (batchIds.Count == 0)
            return;

        var applications = ObjectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(application => batchIds.Contains(application.ID))
            .Include(application => application.LatestProgress!).ThenInclude(progress => progress.State)
            .Include(application => application.LatestProgress!).ThenInclude(progress => progress.State)
            .Include(application => application.ApplicationProfile)
            .Include(application => application.ApplicationType!)
            .Include(application => application.ApprovalLegProfile!)
                .ThenInclude(profile => profile.MinistryLegs)
                .ThenInclude(leg => leg.ApprovingMinistry)
            .Include(application => application.Urgency)
            .Include(application => application.VisaPeriod)
            .Include(application => application.VisaType)
            .Include(application => application.MigrationService)
            .Include(application => application.ProjectContract)
            .Include(application => application.ApprovalLegSnapshots)
            .AsSplitQuery()
            .ToList();

        var personCounts = ObjectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(application => batchIds.Contains(application.ID))
            .Select(application => new { ApplicationProfileInstanceId = application.ID, Count = application.People.Count })
            .ToDictionary(x => x.ApplicationProfileInstanceId, x => x.Count);

        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(ObjectSpace, applications);
        ApplicationWorkspaceListProgressSteps.ApplyTo(ObjectSpace, applications);

        foreach (var application in applications)
        {
            application.SetListViewTotalPersonCount(personCounts.GetValueOrDefault(application.ID, 0));
            application.InvalidateListViewDisplayCache();
            application.WarmListViewDisplayCache();
            progressById[application.ID] = application.ListViewProgressSteps;
            resultById[application.ID] = application.ListViewResultCoverageChips;
            preloadedIds.Add(application.ID);
        }

        CopyDisplayCacheToListRows(applications);
    }

    /// <summary>
    /// The grid is bound to the collection instances. A separate query can return
    /// different objects; the progress and result caches have to be on the rows shown.
    /// </summary>
    private void CopyDisplayCacheToListRows(IReadOnlyList<ApplicationProfileInstance> loaded)
    {
        var list = View?.CollectionSource.List;
        if (list == null || loaded.Count == 0)
            return;

        var loadedById = new Dictionary<Guid, ApplicationProfileInstance>(loaded.Count);
        foreach (var application in loaded)
            loadedById[application.ID] = application;

        foreach (var row in list.OfType<ApplicationProfileInstance>())
        {
            if (!loadedById.TryGetValue(row.ID, out var source) || ReferenceEquals(source, row))
                continue;

            row.SetListViewTotalPersonCount(source.TotalPersonCount);
            row.SetListViewResultCoverage(source.ListViewResultCoverageChips);
            row.SetListViewProgressSteps(source.ListViewProgressSteps);
            row.InvalidateListViewDisplayCache();
            row.WarmListViewDisplayCache();
        }
    }

    /// <summary>
    /// Copies the id cache onto whatever instances the recreated collection is holding.
    /// </summary>
    private void ApplyIdCacheToListRows()
    {
        var list = View?.CollectionSource.List;
        if (list == null)
            return;

        foreach (var row in list.OfType<ApplicationProfileInstance>())
        {
            if (row.ListViewProgressSteps.Count == 0
                && progressById.TryGetValue(row.ID, out var steps)
                && steps.Count > 0)
                row.SetListViewProgressSteps(steps);

            if (row.ListViewResultCoverageChips.Count == 0
                && resultById.TryGetValue(row.ID, out var chips)
                && chips.Count > 0)
                row.SetListViewResultCoverage(chips);
        }
    }

    /// <summary>
    /// Puts the stepper and chip templates back on the columns. Does not reload the grid,
    /// because a reload replaces the instances and drops the in-memory progress.
    /// </summary>
    private void RepaintLoadedRows()
    {
        ApplyIdCacheToListRows();
        Frame.GetController<ApplicationListViewProgressStepsController>()?.ApplyStepperTemplate();
        Frame.GetController<ApplicationListViewResultCoverageController>()?.ApplyChipTemplate();
        Frame.GetController<ApplicationListViewCompletionPercentageController>()?.ApplyPercentTemplate();

        if (View != null
            && !PersonLinkedApplicationsColumnWidths.SkipAutoFit(View.Id)
            && View.Editor is DxGridListEditor { GridModel.ComponentInstance: { } grid })
            grid.AutoFitColumnWidths();
    }
}
