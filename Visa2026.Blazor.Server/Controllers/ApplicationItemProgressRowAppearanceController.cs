using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies parent-application progress row background on <see cref="ApplicationItem"/> ListViews.
/// Line-cancelled rows skip progress CSS so <c>ApplicationItem_LineCancelledRow</c> Appearance wins.
/// </summary>
public sealed class ApplicationItemProgressRowAppearanceController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;

    public ApplicationItemProgressRowAppearanceController()
    {
        TargetObjectType = typeof(ApplicationItem);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyRowAppearance();
    }

    private void ApplyRowAppearance()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
        {
            return;
        }

        if (customizeElementHandler != null)
        {
            gridModel.CustomizeElement = previousCustomizeElement;
            customizeElementHandler = null;
            previousCustomizeElement = null;
        }

        previousCustomizeElement = gridModel.CustomizeElement;
        customizeElementHandler = e =>
        {
            if (e.ElementType != GridElementType.DataRow)
                previousCustomizeElement?.Invoke(e);
            ApplyProgressRowStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private void ApplyProgressRowStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataRow || e.VisibleIndex < 0)
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not ApplicationItem item)
            return;

        if (item.IsLineCancelled)
            return;

        var rowCssClass = item.ListRowCssClass;
        if (string.IsNullOrEmpty(rowCssClass))
            return;

        e.CssClass = string.IsNullOrEmpty(e.CssClass)
            ? rowCssClass
            : $"{e.CssClass} {rowCssClass}";
    }

    protected override void OnDeactivated()
    {
        if (customizeElementHandler != null
            && View?.Editor is DxGridListEditor { GridModel: { } gridModel })
        {
            gridModel.CustomizeElement = previousCustomizeElement;
        }

        customizeElementHandler = null;
        previousCustomizeElement = null;
        base.OnDeactivated();
    }
}
