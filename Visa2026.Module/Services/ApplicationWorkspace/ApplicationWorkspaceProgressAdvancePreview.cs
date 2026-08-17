using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Live preview of the officer-selected Next step on the current timeline node.
/// Only applies when that next state belongs to the same slot (same ministry or migration).
/// </summary>
public static class ApplicationWorkspaceProgressAdvancePreview
{
    public static bool IsResultForStep(string stepKey, string? nextStateCode)
    {
        if (string.IsNullOrWhiteSpace(stepKey) || string.IsNullOrWhiteSpace(nextStateCode))
            return false;

        var slot = ApplicationProfileInstanceProgressRevertHelper.SlotKeyFor(nextStateCode);
        return !string.IsNullOrEmpty(slot)
            && string.Equals(slot, stepKey, StringComparison.OrdinalIgnoreCase);
    }

    public static string OutcomeKind(string? nextStateCode)
    {
        if (string.IsNullOrWhiteSpace(nextStateCode))
            return "current";

        var code = nextStateCode.Trim();
        if (code.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase))
            return "rejected";

        if (code.Contains("CANCELLED", StringComparison.OrdinalIgnoreCase))
            return "cancelled";

        if (code.Contains("ISSUED", StringComparison.OrdinalIgnoreCase))
            return "issued";

        return "current";
    }
}