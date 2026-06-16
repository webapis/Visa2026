using System;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Progress history list labels: state text plus ministry short name or office / migration context.
/// </summary>
internal static class ApplicationProgressListLabelHelper
{
    internal static string FormatStatusLabel(
        string? stateLabel,
        string? locationCode,
        string? ministryShortName)
    {
        var state = stateLabel?.Trim() ?? string.Empty;
        var suffix = ResolveContextSuffix(locationCode, ministryShortName)?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(suffix))
            return state;

        if (string.IsNullOrEmpty(state))
            return suffix;

        return $"{state} - {suffix}";
    }

    internal static string? ResolveContextSuffix(string? locationCode, string? ministryShortName)
    {
        if (!string.IsNullOrWhiteSpace(ministryShortName))
            return ministryShortName.Trim();

        if (string.Equals(
                locationCode,
                ApplicationProgressLocationCodes.AtOffice,
                StringComparison.OrdinalIgnoreCase))
        {
            return VisaUiMessages.Get("ApplicationProgress.ListLabel.Office");
        }

        if (string.Equals(
                locationCode,
                ApplicationProgressLocationCodes.AtMigrationService,
                StringComparison.OrdinalIgnoreCase))
        {
            return VisaUiMessages.Get("ApplicationProgress.ListLabel.MigrationService");
        }

        return null;
    }
}
