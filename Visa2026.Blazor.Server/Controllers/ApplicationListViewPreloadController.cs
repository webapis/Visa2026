using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

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
    private EventHandler? collectionReloadedHandler;
    private EventHandler<ComponentInstanceCapturedEventArgs<IGrid>>? gridCapturedHandler;
    private CancellationTokenSource? preloadCts;
    private int lastScrollAheadVisibleIndex = -1;
    private bool suppressCollectionReloadPreload;

    public ApplicationListViewPreloadController()
    {
        TargetObjectType = typeof(Application);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        collectionReloadedHandler ??= (_, _) =>
        {
            if (suppressCollectionReloadPreload)
                return;
            StartPreload(syncBatches: InitialSyncBatchCount);
        };
        View.CollectionSource.CollectionReloaded += collectionReloadedHandler;
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

        base.OnDeactivated();
    }

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
            if (grid.GetDataItem(rowIndex) is not Application application)
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

        var ids = View.CollectionSource.List.OfType<Application>().Select(a => a.ID).Distinct().ToList();
        var syncRowCount = Math.Min(syncBatches * BatchSize, ids.Count);
        for (var offset = 0; offset < syncRowCount; offset += BatchSize)
            PreloadByIds(ids.Skip(offset).Take(BatchSize).ToList());

        if (syncRowCount > 0 && View != null)
        {
            // Grid may have bound empty NotMapped SLA values before MigrationSlaProfile was included.
            // Force rebind so Migration deadline / working days pick up the warmed cache.
            suppressCollectionReloadPreload = true;
            try
            {
                View.Refresh();
            }
            finally
            {
                suppressCollectionReloadPreload = false;
            }
        }

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

        var applications = ObjectSpace.GetObjectsQuery<Application>()
            .Where(application => batchIds.Contains(application.ID))
            .Include(application => application.LatestProgress!).ThenInclude(progress => progress.State)
            .Include(application => application.LatestProgress!).ThenInclude(progress => progress.State)
            .Include(application => application.ApplicationType!).ThenInclude(applicationType => applicationType.MigrationSlaProfile)
            .Include(application => application.ApprovalLegProfile!)
                .ThenInclude(profile => profile.MinistryLegs)
                .ThenInclude(leg => leg.ApprovingMinistry)
            .Include(application => application.Urgency)
            .Include(application => application.VisaPeriod)
            .Include(application => application.VisaType)
            .Include(application => application.ApprovalLegSnapshots)
            .AsSplitQuery()
            .ToList();

        var personCounts = ObjectSpace.GetObjectsQuery<ApplicationPerson>()
            .Where(row => batchIds.Contains(row.ApplicationId))
            .GroupBy(row => row.ApplicationId)
            .Select(group => new { ApplicationId = group.Key, Count = group.Count() })
            .ToDictionary(x => x.ApplicationId, x => x.Count);

        foreach (var applicationId in batchIds.Where(id => !personCounts.ContainsKey(id)))
        {
            var legacyCount = ObjectSpace.GetObjectsQuery<ApplicationItem>()
                .Count(item => item.Application != null && item.Application.ID == applicationId);
            if (legacyCount > 0)
                personCounts[applicationId] = legacyCount;
        }

        foreach (var application in applications)
        {
            application.SetListViewTotalPersonCount(personCounts.GetValueOrDefault(application.ID, 0));
            application.InvalidateListViewDisplayCache();
            application.WarmListViewDisplayCache();
            preloadedIds.Add(application.ID);
        }
    }
}