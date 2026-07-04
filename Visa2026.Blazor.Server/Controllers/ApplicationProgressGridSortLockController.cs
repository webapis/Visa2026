using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Progress history is a workflow timeline - disable interactive column sorting on its ListViews.
/// Configures the XAF grid component model only (never the live DxGrid instance during render).
/// </summary>
public sealed class ApplicationProgressGridSortLockController : ViewController<ListView>
{
    private static readonly string[] TargetListViewIds =
    [
        "Application_ProgressHistory_ListView",
        "ApplicationProgress_ListView",
    ];

    public ApplicationProgressGridSortLockController()
    {
        TargetObjectType = typeof(ApplicationProgress);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (!TargetListViewIds.Contains(View.Id))
            return;

        if (View.Editor is not DxGridListEditor gridListEditor)
            return;

        gridListEditor.BeginUpdate();
        try
        {
            ApplySortLock(gridListEditor);
        }
        finally
        {
            gridListEditor.EndUpdate();
        }
    }

    private static void ApplySortLock(DxGridListEditor gridListEditor)
    {
        gridListEditor.GridModel.AllowSort = false;

        foreach (DxGridDataColumnModel columnModel in gridListEditor.GridDataColumnModels)
            columnModel.AllowSort = false;

        ApplyFixedTimelineSort(gridListEditor.GridDataColumnModels);
    }

    private static void ApplyFixedTimelineSort(IEnumerable<DxGridDataColumnModel> columnModels)
    {
        foreach (var columnModel in columnModels)
        {
            columnModel.AllowSort = false;
            columnModel.SortIndex = -1;
            columnModel.SortOrder = GridColumnSortOrder.None;
        }

        var orderColumn = columnModels.FirstOrDefault(column =>
            string.Equals(column.FieldName, nameof(ApplicationProgress.Order), StringComparison.Ordinal));
        if (orderColumn == null)
            return;

        orderColumn.AllowSort = false;
        orderColumn.SortIndex = 0;
        orderColumn.SortOrder = GridColumnSortOrder.Ascending;
    }
}
