using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Batch-loads <see cref="Invitation.TotalPersonCount"/> / <see cref="WorkPermit.TotalPersonCount"/>
/// for root ListViews (same pattern as Application person count preload).
/// </summary>
public sealed class InvitationListViewPersonCountController : ViewController<ListView>
{
    private EventHandler? collectionReloadedHandler;
    private bool suppressReload;

    public InvitationListViewPersonCountController()
    {
        TargetObjectType = typeof(Invitation);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        collectionReloadedHandler ??= (_, _) =>
        {
            if (!suppressReload)
                ApplyCounts(refresh: true);
        };
        View.CollectionSource.CollectionReloaded += collectionReloadedHandler;
        ApplyCounts(refresh: false);
    }

    protected override void OnDeactivated()
    {
        if (collectionReloadedHandler != null)
            View.CollectionSource.CollectionReloaded -= collectionReloadedHandler;
        base.OnDeactivated();
    }

    private void ApplyCounts(bool refresh)
    {
        if (View?.CollectionSource.List == null || View.CollectionSource.List.Count == 0)
            return;

        var invitations = View.CollectionSource.List.OfType<Invitation>().ToList();
        var ids = invitations.Select(i => i.ID).Distinct().ToList();
        if (ids.Count == 0)
            return;

        var counts = ObjectSpace.GetObjectsQuery<InvitationItem>()
            .Where(item => ids.Contains(item.Invitation.ID))
            .GroupBy(item => item.Invitation.ID)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToDictionary(x => x.Id, x => x.Count);

        foreach (var invitation in invitations)
            invitation.SetListViewTotalPersonCount(counts.GetValueOrDefault(invitation.ID, 0));

        if (!refresh || View == null)
            return;

        suppressReload = true;
        try
        {
            View.Refresh();
        }
        finally
        {
            suppressReload = false;
        }
    }
}

/// <summary>
/// Batch-loads <see cref="WorkPermit.TotalPersonCount"/> for root ListViews.
/// </summary>
public sealed class WorkPermitListViewPersonCountController : ViewController<ListView>
{
    private EventHandler? collectionReloadedHandler;
    private bool suppressReload;

    public WorkPermitListViewPersonCountController()
    {
        TargetObjectType = typeof(WorkPermit);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        collectionReloadedHandler ??= (_, _) =>
        {
            if (!suppressReload)
                ApplyCounts(refresh: true);
        };
        View.CollectionSource.CollectionReloaded += collectionReloadedHandler;
        ApplyCounts(refresh: false);
    }

    protected override void OnDeactivated()
    {
        if (collectionReloadedHandler != null)
            View.CollectionSource.CollectionReloaded -= collectionReloadedHandler;
        base.OnDeactivated();
    }

    private void ApplyCounts(bool refresh)
    {
        if (View?.CollectionSource.List == null || View.CollectionSource.List.Count == 0)
            return;

        var workPermits = View.CollectionSource.List.OfType<WorkPermit>().ToList();
        var ids = workPermits.Select(wp => wp.ID).Distinct().ToList();
        if (ids.Count == 0)
            return;

        var counts = ObjectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(item => ids.Contains(item.WorkPermit.ID))
            .GroupBy(item => item.WorkPermit.ID)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToDictionary(x => x.Id, x => x.Count);

        foreach (var workPermit in workPermits)
            workPermit.SetListViewTotalPersonCount(counts.GetValueOrDefault(workPermit.ID, 0));

        if (!refresh || View == null)
            return;

        suppressReload = true;
        try
        {
            View.Refresh();
        }
        finally
        {
            suppressReload = false;
        }
    }
}
