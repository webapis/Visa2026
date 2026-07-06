using DevExpress.ExpressApp;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;

namespace Visa2026.Module.Controllers;

/// <summary>Locks ministry legs nested list to ascending <see cref="ApprovalLegProfileMinistryLeg.Sequence"/>.</summary>
public sealed class ApprovalLegProfileMinistryLegListSortController : ViewController<ListView>
{
    public ApprovalLegProfileMinistryLegListSortController()
    {
        TargetObjectType = typeof(ApprovalLegProfileMinistryLeg);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (View.Id != ApprovalLegProfileMinistryLegViewsUpdater.NestedListViewId)
            return;

        ApplySequenceSort();
        View.CollectionSource.CollectionChanged += CollectionSource_CollectionChanged;
    }

    protected override void OnDeactivated()
    {
        if (View.Id == ApprovalLegProfileMinistryLegViewsUpdater.NestedListViewId)
            View.CollectionSource.CollectionChanged -= CollectionSource_CollectionChanged;

        base.OnDeactivated();
    }

    private void CollectionSource_CollectionChanged(object? sender, EventArgs e) => ApplySequenceSort();

    private void ApplySequenceSort()
    {
        View.CollectionSource.CanApplySorting = true;
        View.CollectionSource.Sorting =
        [
            new SortProperty(nameof(ApprovalLegProfileMinistryLeg.Sequence), SortingDirection.Ascending),
        ];
    }
}