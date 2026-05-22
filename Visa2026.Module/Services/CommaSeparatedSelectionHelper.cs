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

    public static string ReplaceLabel(string? stored, string oldLabel, string newLabel, string? noneValue = null)
    {
        if (string.IsNullOrWhiteSpace(stored)
            || string.IsNullOrWhiteSpace(oldLabel)
            || string.IsNullOrWhiteSpace(newLabel))
        {
            return stored ?? FormatSelected(null, noneValue);
        }

        var items = ParseSelected(stored, noneValue).ToList();
        var changed = false;
        for (var i = 0; i < items.Count; i++)
        {
            if (string.Equals(items[i], oldLabel.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                items[i] = newLabel.Trim();
                changed = true;
            }
        }

        return changed ? FormatSelected(items, noneValue) : stored;
    }

    public static bool ContainsLabel(string? stored, string label, string? noneValue = null) =>
        !string.IsNullOrWhiteSpace(label)
        && ParseSelected(stored, noneValue).Any(z => string.Equals(z, label.Trim(), StringComparison.OrdinalIgnoreCase));
}
