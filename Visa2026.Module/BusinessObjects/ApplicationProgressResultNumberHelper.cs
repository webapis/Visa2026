using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Approval-leg result number on a ministry Approved or Unapproved progress row.
/// </summary>
public static class ApplicationProgressResultNumberHelper
{
    public const int MaxLength = 100;

    public static bool AppliesTo(string? stateCode) =>
        ApplicationProfileInstanceProgressLegCodes.IsMinistryDecisionStateCode(stateCode);

    public static bool TryNormalize(string? value, bool required, out string? normalized, out string? error)
    {
        normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        error = null;

        if (normalized != null && normalized.Length > MaxLength)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.ResultNumberTooLong");
            normalized = null;
            return false;
        }

        if (required && normalized == null)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.ResultNumberRequired");
            return false;
        }

        return true;
    }
}