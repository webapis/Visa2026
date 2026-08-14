using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Progress history list labels: state text plus ministry short name when applicable.
/// </summary>
internal static class ApplicationProfileInstanceProgressListLabelHelper
{
    internal static string FormatStatusLabel(string? stateLabel, string? ministryShortName)
    {
        var state = stateLabel?.Trim() ?? string.Empty;
        var suffix = ministryShortName?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(suffix))
            return state;

        if (string.IsNullOrEmpty(state))
            return suffix;

        return $"{state} - {suffix}";
    }
}