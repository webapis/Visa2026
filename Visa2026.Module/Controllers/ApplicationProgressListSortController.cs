using DevExpress.ExpressApp;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

public sealed class ApplicationProfileInstanceProgressListSortController : ViewController<ListView>
{
    private static readonly string[] TargetListViewIds =
    [
        "Application_ProgressHistory_ListView",
        "ApplicationProfileInstanceProgress_ListView",
    ];

    public ApplicationProfileInstanceProgressListSortController()
    {
        TargetObjectType = typeof(ApplicationProfileInstanceProgress);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (!TargetListViewIds.Contains(View.Id))
            return;

        ApplyTimelineSort();
        View.CollectionSource.CollectionChanged += CollectionSource_CollectionChanged;
    }

    protected override void OnDeactivated()
    {
        if (TargetListViewIds.Contains(View.Id))
            View.CollectionSource.CollectionChanged -= CollectionSource_CollectionChanged;

        base.OnDeactivated();
    }

    private void CollectionSource_CollectionChanged(object? sender, EventArgs e) => ApplyTimelineSort();

    private void ApplyTimelineSort()
    {
        View.CollectionSource.CanApplySorting = true;
        View.CollectionSource.Sorting =
        [
            new SortProperty(nameof(ApplicationProfileInstanceProgress.Order), SortingDirection.Ascending),
        ];
    }
}
