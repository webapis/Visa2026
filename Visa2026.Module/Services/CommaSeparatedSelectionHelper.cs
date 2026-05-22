using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services;

/// <summary>
/// Parses and formats comma-separated label lists stored on string properties
/// (e.g. <see cref="BusinessObjects.ApplicationItem.BorderZoneLocation"/>).
/// </summary>
public static class CommaSeparatedSelectionHelper
{
    public const string NoneValue = "Ýok";

    /// <summary>How many checkboxes are shown before "Show more".</summary>
    public const int CollapsedVisibleCount = 8;

    public static IReadOnlyList<string> ParseSelected(string? stored, string? noneValue = null)
    {
        if (IsNoneValue(stored, noneValue))
        {
            return Array.Empty<string>();
        }

        return stored!
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(z => !IsNoneValue(z, noneValue))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string FormatSelected(IEnumerable<string>? selected, string? noneValue = null)
    {
        var effectiveNone = noneValue ?? NoneValue;
        if (selected == null)
        {
            return effectiveNone;
        }

        var items = selected
            .Where(z => !string.IsNullOrWhiteSpace(z) && !IsNoneValue(z, noneValue))
            .Select(z => z.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items.Count == 0 ? effectiveNone : string.Join(", ", items);
    }

    public static bool IsNoneValue(string? stored, string? noneValue = null)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return true;
        }

        var effectiveNone = noneValue ?? NoneValue;
        return !string.IsNullOrEmpty(effectiveNone)
            && string.Equals(stored.Trim(), effectiveNone, StringComparison.OrdinalIgnoreCase);
    }
}
