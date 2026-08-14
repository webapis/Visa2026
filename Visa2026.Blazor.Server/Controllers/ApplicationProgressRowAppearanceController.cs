using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies workflow row background on <see cref="Application"/> ListViews when XAF conditional appearance is insufficient.
/// </summary>
public sealed class ApplicationProfileInstanceProgressRowAppearanceController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;
    private ApplicationListViewPreloadController? preloadController;

    public ApplicationProfileInstanceProgressRowAppearanceController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        preloadController = Frame.GetController<ApplicationListViewPreloadController>();
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

        preloadController?.EnsureScrollAheadIfNeeded(e.Grid, e.VisibleIndex);

        if (e.Grid.GetDataItem(e.VisibleIndex) is not ApplicationProfileInstance application)
            return;

        var rowCssClass = application.ListRowCssClass;
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
        preloadController = null;
        base.OnDeactivated();
    }
}
