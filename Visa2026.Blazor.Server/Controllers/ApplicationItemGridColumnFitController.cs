using DevExpress.Blazor;
using DevExpress.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.SystemModule;
using DevExpress.ExpressApp.Model;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Application Item list views had fixed pixel column widths in the model, leaving empty space on wide layouts.
/// After the grid is created, best-fit widths so visible columns span the available width.
/// For the nested Application Items list, also ensure ShowAllRows wins over global virtual scrolling.
/// Blazor does not apply column SummaryType from the model for total footers — wire Count in code.
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

            ConfigureTotalItemCount(gridListEditor);
        }

        gridCapturedHandler ??= (_, e) => e.ComponentInstance.AutoFitColumnWidths();
        gridListEditor.GridModel.ComponentInstanceCaptured += gridCapturedHandler;
        gridListEditor.GridModel.ComponentInstance?.AutoFitColumnWidths();
    }

    private static void ConfigureTotalItemCount(DxGridListEditor gridListEditor)
    {
        if (gridListEditor.GridModel is not { } gridModel)
            return;

        gridModel.FooterDisplayMode = GridFooterDisplayMode.Always;
        gridModel.CustomizeSummaryDisplayText = e =>
        {
            if (e.Item.SummaryType == GridSummaryItemType.Count && e.Value != null)
            {
                e.DisplayText = VisaUiMessages.Format("Grid.TotalCount", e.Value);
            }
        };

        var column = gridListEditor.Columns
            .FirstOrDefault(c => c.PropertyName == nameof(ApplicationItem.ApplicationItemName));
        if (column == null)
            return;

        if (gridListEditor.GridSummary.TotalSummary.Any(item =>
                item is DxGridSummaryItemWrapper wrapper &&
                wrapper.SummaryItemModel.SummaryType == GridSummaryItemType.Count &&
                wrapper.SummaryItemModel.FieldName == column.PropertyName))
            return;

        var summaryItem = (DxGridSummaryItemWrapper)gridListEditor.GridSummary.CreateItem(column.Id, SummaryItemType.Count);
        summaryItem.SummaryItemModel.FooterColumnName = column.Id;
        gridListEditor.GridSummary.TotalSummary.Add(summaryItem);
    }

    protected override void OnDeactivated()
    {
        if (gridCapturedHandler != null && View.Editor is DxGridListEditor gridListEditor)
            gridListEditor.GridModel.ComponentInstanceCaptured -= gridCapturedHandler;
        base.OnDeactivated();
    }
}
