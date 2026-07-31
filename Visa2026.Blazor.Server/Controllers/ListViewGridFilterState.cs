using DevExpress.Blazor;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Shared helpers for detecting / clearing DxGrid column filters and search text
/// so ListViews can surface an obvious "filtered" empty state.
/// </summary>
internal static class ListViewGridFilterState
{
    public static bool HasActiveFilter(IGrid? grid)
    {
        if (grid is null)
            return false;

        if (!ReferenceEquals(grid.GetFilterCriteria(), null))
            return true;

        return !string.IsNullOrWhiteSpace(grid.SearchText);
    }

    public static void Clear(IGrid? grid)
    {
        if (grid is null)
            return;

        // DxGrid parameter properties (e.g. SearchText) must be changed between BeginUpdate/EndUpdate
        // when set outside component markup; otherwise DevExpress throws InvalidOperationException.
        grid.BeginUpdate();
        try
        {
            grid.ClearFilter();
            if (!string.IsNullOrWhiteSpace(grid.SearchText))
                grid.SearchText = string.Empty;
        }
        finally
        {
            grid.EndUpdate();
        }
    }
}