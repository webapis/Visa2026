using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Application Item list views had fixed pixel column widths in the model, leaving empty space on wide layouts.
/// After the grid is created, best-fit widths so visible columns span the available width.
/// </summary>
public sealed class ApplicationItemGridColumnFitController : ViewController<ListView>
{
    private static readonly string[] TargetListViewIds =
    {
        "Application_ApplicationItems_ListView",
        "ApplicationItem_ListView"
    };

    private EventHandler<ComponentInstanceCapturedEventArgs<IGrid>> gridCapturedHandler;

    public ApplicationItemGridColumnFitController()
    {
        TargetObjectType = typeof(ApplicationItem);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (!TargetListViewIds.Contains(View.Id))
            return;

        if (View.Editor is not DxGridListEditor gridListEditor)
            return;

        gridCapturedHandler ??= (_, e) => e.ComponentInstance.AutoFitColumnWidths();
        gridListEditor.GridModel.ComponentInstanceCaptured += gridCapturedHandler;
        gridListEditor.GridModel.ComponentInstance?.AutoFitColumnWidths();
    }

    protected override void OnDeactivated()
    {
        if (gridCapturedHandler != null && View.Editor is DxGridListEditor gridListEditor)
            gridListEditor.GridModel.ComponentInstanceCaptured -= gridCapturedHandler;
        base.OnDeactivated();
    }
}
