using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Auto-fits DxGrid columns on ListView open (same as header "Best Fit (all columns)"),
/// so officers do not need to re-apply fit after each deploy. Skips lookup pickers.
/// Nested ShowAllRows is handled by <see cref="NestedListViewShowAllRowsController"/>.
/// </summary>
public sealed class ListViewGridColumnFitController : ViewController<ListView>
{
    private EventHandler<ComponentInstanceCapturedEventArgs<IGrid>> gridCapturedHandler;

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (ShouldSkipListView())
            return;

        if (View.Editor is not DxGridListEditor gridListEditor)
            return;

        gridCapturedHandler ??= (_, e) => e.ComponentInstance.AutoFitColumnWidths();
        gridListEditor.GridModel.ComponentInstanceCaptured += gridCapturedHandler;
        gridListEditor.GridModel.ComponentInstance?.AutoFitColumnWidths();
    }

    protected override void OnDeactivated()
    {
        if (gridCapturedHandler != null && View?.Editor is DxGridListEditor gridListEditor)
            gridListEditor.GridModel.ComponentInstanceCaptured -= gridCapturedHandler;
        base.OnDeactivated();
    }

    private bool ShouldSkipListView() =>
        View.Id.EndsWith("_LookupListView", StringComparison.Ordinal);
}