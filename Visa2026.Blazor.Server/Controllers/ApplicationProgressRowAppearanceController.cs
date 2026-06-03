using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.Appearance;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Applies workflow row background on <see cref="Application"/> ListViews when XAF conditional appearance is insufficient.
/// </summary>
public sealed class ApplicationProgressRowAppearanceController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;

    public ApplicationProgressRowAppearanceController()
    {
        TargetObjectType = typeof(Application);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyRowAppearance();
        _ = ApplyRowAppearanceDeferredAsync();
    }

    private async Task ApplyRowAppearanceDeferredAsync()
    {
        await Task.Delay(150);
        if (View is { IsDisposed: false })
            ApplyRowAppearance();
    }

    private void ApplyRowAppearance()
    {
        if (View.Editor is not DxGridListEditor { GridModel: { } gridModel })
            return;

        if (customizeElementHandler != null)
        {
            gridModel.CustomizeElement = previousCustomizeElement;
            customizeElementHandler = null;
            previousCustomizeElement = null;
        }

        previousCustomizeElement = gridModel.CustomizeElement;
        customizeElementHandler = e =>
        {
            previousCustomizeElement?.Invoke(e);
            ApplyProgressRowStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private static void ApplyProgressRowStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataRow || e.VisibleIndex < 0)
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not Application application)
            return;

        var stateCode = application.PrimaryStateCode;
        if (string.IsNullOrEmpty(stateCode)
            || !BoStateAppearanceColors.TryGet(stateCode, out var appearance))
            return;

        var rowClass = $"{appearance.RowCssClass} visa-progress-row";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? rowClass : $"{e.CssClass} {rowClass}";
    }

    protected override void OnDeactivated()
    {
        if (customizeElementHandler != null
            && View.Editor is DxGridListEditor { GridModel: { } gridModel })
            gridModel.CustomizeElement = previousCustomizeElement;
        customizeElementHandler = null;
        previousCustomizeElement = null;
        base.OnDeactivated();
    }
}
