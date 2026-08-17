using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Workspace backward progress: delete the last history row, or all rows after a chosen slot
/// (including office = empty history / implied office).
/// </summary>
public static class ApplicationProfileInstanceProgressRevertHelper
{
    public static IReadOnlyList<ApplicationProfileInstanceProgress> OrderedHistory(
        IEnumerable<ApplicationProfileInstanceProgress>? history)
    {
        return (history ?? Array.Empty<ApplicationProfileInstanceProgress>())
            .Where(row => row != null)
            .OrderBy(row => row, Comparer<ApplicationProfileInstanceProgress>.Create(
                ApplicationProfileInstanceProgressOrderHelper.CompareSiblingOrder))
            .ToList();
    }

    public static string SlotKeyFor(ApplicationProfileInstanceProgress? row) =>
        SlotKeyFor(row?.State?.Code);

    public static string SlotKeyFor(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return string.Empty;

        if (string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase))
            return "office";

        if (ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out var leg))
            return "leg-" + Math.Clamp(leg, 1, ApplicationProfileInstanceProgressLegCodes.MaxLegCount);

        return "migration";
    }

    /// <summary>
    /// Rows to remove for a revert. Empty <paramref name="stepKey"/> means last step only.
    /// <c>office</c> removes the whole history (implied office).
    /// </summary>
    public static IReadOnlyList<ApplicationProfileInstanceProgress> RowsToDelete(
        IEnumerable<ApplicationProfileInstanceProgress>? history,
        string? stepKey)
    {
        var ordered = OrderedHistory(history);
        if (ordered.Count == 0)
            return Array.Empty<ApplicationProfileInstanceProgress>();

        if (string.IsNullOrWhiteSpace(stepKey))
            return new[] { ordered[^1] };

        if (string.Equals(stepKey, "office", StringComparison.OrdinalIgnoreCase))
            return ordered;

        var lastKeep = -1;
        for (var i = 0; i < ordered.Count; i++)
        {
            if (string.Equals(SlotKeyFor(ordered[i]), stepKey, StringComparison.OrdinalIgnoreCase))
                lastKeep = i;
        }

        if (lastKeep < 0)
            return Array.Empty<ApplicationProfileInstanceProgress>();

        return ordered.Skip(lastKeep + 1).ToList();
    }
}