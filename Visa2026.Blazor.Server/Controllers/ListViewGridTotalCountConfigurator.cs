using DevExpress.Blazor;
using DevExpress.Data;
using DevExpress.ExpressApp.Blazor.Editors;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Wires a hidden grid Count summary so the filtered (visible) row count can be read for the toolbar label.
/// Blazor does not apply column SummaryType from the application model.
/// </summary>
internal static class ListViewGridTotalCountConfigurator
{
    public static void EnsureCountSummary(DxGridListEditor gridListEditor)
    {
        if (gridListEditor.GridModel is null)
            return;

        var column = gridListEditor.Columns.FirstOrDefault();
        if (column is null)
            return;

        if (gridListEditor.GridSummary.TotalSummary.Any(item =>
                item is DxGridSummaryItemWrapper wrapper &&
                wrapper.SummaryItemModel.SummaryType == GridSummaryItemType.Count))
            return;

        var summaryItem = (DxGridSummaryItemWrapper)gridListEditor.GridSummary.CreateItem(
            column.Id, SummaryItemType.Count);
        summaryItem.SummaryItemModel.Visible = false;
        gridListEditor.GridSummary.TotalSummary.Add(summaryItem);
    }

    public static int ResolveFilteredCount(IGrid grid)
    {
        foreach (var summaryItem in grid.GetTotalSummaryItems())
        {
            if (summaryItem.SummaryType != GridSummaryItemType.Count)
                continue;

            var value = grid.GetTotalSummaryValue(summaryItem);
            if (value is int intValue)
                return intValue;
            if (value is long longValue)
                return (int)longValue;
            if (value is decimal decimalValue)
                return (int)decimalValue;
            if (value != null && int.TryParse(value.ToString(), out var parsed))
                return parsed;
        }

        return grid.GetVisibleRowCount();
    }
}
