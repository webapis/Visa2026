using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.Model;

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

        gridCapturedHandler ??= (_, e) => FitColumns(e.ComponentInstance);
        gridListEditor.GridModel.ComponentInstanceCaptured += gridCapturedHandler;
        if (gridListEditor.GridModel.ComponentInstance != null)
            FitColumns(gridListEditor.GridModel.ComponentInstance);
    }

    private void FitColumns(IGrid grid)
    {
        Frame.GetController<ApplicationListViewProgressStepsController>()?.SuppressWidthSave();
        grid.AutoFitColumnWidths();
        Frame.GetController<ApplicationListViewProgressStepsController>()?.RestoreProgressColumnWidth();
    }

    protected override void OnDeactivated()
    {
        if (gridCapturedHandler != null && View?.Editor is DxGridListEditor gridListEditor)
            gridListEditor.GridModel.ComponentInstanceCaptured -= gridCapturedHandler;
        base.OnDeactivated();
    }

    private bool ShouldSkipListView() =>
        View.Id.EndsWith("_LookupListView", StringComparison.Ordinal)
        || PersonLinkedApplicationsColumnWidths.SkipAutoFit(View.Id)
        || PersonRosterListViews.IsRoster(View.Id);
}
