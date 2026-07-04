using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.SystemModule;
using DevExpress.ExpressApp.Model;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Application Item list views had fixed pixel column widths in the model, leaving empty space on wide layouts.
/// After the grid is created, best-fit widths so visible columns span the available width.
/// For the nested Application Items list, also ensure ShowAllRows wins over global virtual scrolling.
/// Total item count is handled globally by <see cref="ListViewTotalCountController"/>.
/// </summary>
public sealed class ApplicationItemGridColumnFitController : ViewController<ListView>
{
    private const string NestedApplicationItemsListViewId = "Application_ApplicationItems_ListView";

    private static readonly string[] TargetListViewIds =
    {
        NestedApplicationItemsListViewId,
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

        if (View.Id == NestedApplicationItemsListViewId)
        {
            if (View.Model is IModelListViewBlazor blazorModel)
            {
                blazorModel.ShowAllRows = true;
                blazorModel.VirtualScrollingEnabled = false;
            }
        }

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
