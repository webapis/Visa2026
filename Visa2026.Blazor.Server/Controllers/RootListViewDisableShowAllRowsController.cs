using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.SystemModule;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Root ListViews must page / virtual-scroll. <see cref="NestedListViewShowAllRowsController"/>
/// is for nested collections only. ShowAllRows on a large root list (e.g. WorkPermitItem)
/// keeps the DxGrid overlay on Loading forever.
/// </summary>
public sealed class RootListViewDisableShowAllRowsController : ViewController<ListView>
{
    public RootListViewDisableShowAllRowsController()
    {
        TargetViewNesting = Nesting.Root;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (ShouldSkipListView())
            return;

        if (View.Model is IModelListViewBlazor blazorModel && blazorModel.ShowAllRows)
        {
            blazorModel.ShowAllRows = false;
            blazorModel.VirtualScrollingEnabled = true;
        }

        // Grouping a root item list forces the grid to materialize every row.
        if (string.Equals(View.Id, "WorkPermitItem_ListView", StringComparison.Ordinal))
            View.Model.IsGroupPanelVisible = false;
    }

    private bool ShouldSkipListView() =>
        View.Id.EndsWith("_LookupListView", StringComparison.Ordinal);
}