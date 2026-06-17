using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Makes ministry letter file names clickable in the nested progress history grid.
/// </summary>
public sealed class ApplicationProgressMinistryLetterFileNameClickController : ViewController<ListView>
{
    private Action<GridCustomizeElementEventArgs>? customizeElementHandler;
    private Action<GridCustomizeElementEventArgs>? previousCustomizeElement;

    public ApplicationProgressMinistryLetterFileNameClickController()
    {
        TargetObjectType = typeof(ApplicationProgress);
        TargetViewId = "Application_ProgressHistory_ListView";
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyFileNameClickHandlers();
    }

    private void ApplyFileNameClickHandlers()
    {
        if (View?.Editor is not DxGridListEditor { GridModel: { } gridModel })
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
            ApplyFileNameCellStyle(e);
        };
        gridModel.CustomizeElement = customizeElementHandler;
    }

    private static void ApplyFileNameCellStyle(GridCustomizeElementEventArgs e)
    {
        if (e.ElementType != GridElementType.DataCell || e.VisibleIndex < 0)
            return;

        if (!IsMinistryLetterFileNameColumn(e.Column))
            return;

        if (e.Grid.GetDataItem(e.VisibleIndex) is not ApplicationProgress progress)
            return;

        if (progress.MinistryLetterFile == null || string.IsNullOrWhiteSpace(progress.MinistryLetterFileName))
            return;

        var linkClass = "app-progress-letter-link";
        e.CssClass = string.IsNullOrEmpty(e.CssClass) ? linkClass : $"{e.CssClass} {linkClass}";
        e.Attributes["role"] = "button";
        e.Attributes["tabindex"] = "0";
        e.Attributes["title"] = progress.MinistryLetterFileName;

        // Block the grid's default row-open on pointer/mouse down, then trigger the drawer on click.
        var clickHandler =
            $"window.visaPreviewDrawer.open('progress-letter','{progress.ID}',event); return false;";
        e.Attributes["onclick"] = clickHandler;
        e.Attributes["onpointerdown"] = "event.preventDefault(); event.stopPropagation(); event.stopImmediatePropagation();";
        e.Attributes["onmousedown"] = "event.preventDefault(); event.stopPropagation(); event.stopImmediatePropagation();";
        e.Attributes["onkeydown"] =
            $"if(event.key==='Enter'||event.key===' ')" +
            $"{{window.visaPreviewDrawer.open('progress-letter','{progress.ID}',event); return false;}}";
    }

    private static bool IsMinistryLetterFileNameColumn(IGridColumn? column)
    {
        if (column == null)
            return false;

        if (column is DxGridDataColumn dataColumn)
        {
            return string.Equals(
                dataColumn.FieldName,
                nameof(ApplicationProgress.MinistryLetterFileName),
                StringComparison.Ordinal);
        }

        return string.Equals(
            column.Name,
            nameof(ApplicationProgress.MinistryLetterFileName),
            StringComparison.Ordinal);
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
