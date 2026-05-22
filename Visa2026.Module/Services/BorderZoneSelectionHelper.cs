namespace Visa2026.Module.Services;

/// <summary>Backward-compatible alias for border-zone comma-separated values.</summary>
public static class BorderZoneSelectionHelper
{
    public const string NoneValue = CommaSeparatedSelectionHelper.NoneValue;

    public static IReadOnlyList<string> ParseSelected(string? stored) =>
        CommaSeparatedSelectionHelper.ParseSelected(stored);

    public static string FormatSelected(IEnumerable<string>? selected) =>
        CommaSeparatedSelectionHelper.FormatSelected(selected);

    public static bool IsNoneValue(string? stored) =>
        CommaSeparatedSelectionHelper.IsNoneValue(stored);
}
