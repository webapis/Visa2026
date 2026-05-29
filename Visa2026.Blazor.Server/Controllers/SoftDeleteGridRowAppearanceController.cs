using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Conditional appearance <c>BackColor</c> does not apply reliably to nested ListViews (e.g. Person.FamilyMembers).
/// Applies a gray row style in all <see cref="ISoftDelete"/> DxGrid list views when <see cref="ISoftDelete.IsDeleted"/> is true.
/// </summary>
public sealed class SoftDeleteGridRowAppearanceController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;

    public SoftDeleteGridRowAppearanceController()
    {
        TargetObjectType = typeof(ISoftDelete);
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
            ApplySoftDeletedRowStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private static void ApplySoftDeletedRowStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataRow || e.VisibleIndex < 0)
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not ISoftDelete { IsDeleted: true })
            return;

        e.CssClass = string.IsNullOrEmpty(e.CssClass)
            ? "visa-soft-deleted-row"
            : $"{e.CssClass} visa-soft-deleted-row";
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
