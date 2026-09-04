using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Officer Result on the current timeline node: Approved / Disapproved / Issued / …
/// plus Cancelled last. Office also lists Submitted (leave-office). Other entry states
/// start the next node and are not a Result of the current ministry.
/// </summary>
public static class ApplicationWorkspaceProgressAdvancePreview
{
    public static IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> ResultOptions(
        string stepKey,
        IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> options)
    {
        if (options == null || options.Count == 0)
            return Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();

        var results = options
            .Where(o => IsResultForStep(stepKey, o.StateCode) && !IsCancelled(o.StateCode))
            .ToList();

        foreach (var cancelled in options.Where(o => IsCancelled(o.StateCode)))
        {
            if (!results.Any(r => string.Equals(r.StateCode, cancelled.StateCode, StringComparison.OrdinalIgnoreCase)))
                results.Add(cancelled);
        }

        return results;
    }

    public static string? PreferredAdvanceCode(
        string stepKey,
        IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> options,
        string? selectedResultCode)
    {
        var results = ResultOptions(stepKey, options);
        if (!string.IsNullOrWhiteSpace(selectedResultCode)
            && results.Any(o => string.Equals(o.StateCode, selectedResultCode, StringComparison.OrdinalIgnoreCase)))
            return selectedResultCode;

        var preferredResult = results.FirstOrDefault(o => !IsCancelled(o.StateCode));
        if (preferredResult != null)
            return preferredResult.StateCode;

        return PreferredEntryCode(options);
    }

    public static string? PreferredEntryCode(IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> options)
    {
        if (options == null || options.Count == 0)
            return null;

        var started = options.FirstOrDefault(o => IsEntryState(o.StateCode));
        if (started != null)
            return started.StateCode;

        var notCancel = options.FirstOrDefault(o => !IsCancelled(o.StateCode));
        return (notCancel ?? options[0]).StateCode;
    }

    public static bool IsResultForStep(string stepKey, string? nextStateCode)
    {
        if (string.IsNullOrWhiteSpace(stepKey) || string.IsNullOrWhiteSpace(nextStateCode))
            return false;

        if (IsOfficeSubmitted(stepKey, nextStateCode))
            return true;

        if (IsEntryState(nextStateCode))
            return false;

        if (IsCancelled(nextStateCode))
            return true;

        var slot = ApplicationProfileInstanceProgressRevertHelper.SlotKeyFor(nextStateCode);
        return !string.IsNullOrEmpty(slot)
            && string.Equals(slot, stepKey, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOfficeSubmitted(string stepKey, string? nextStateCode)
    {
        if (!string.Equals(stepKey, "office", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(nextStateCode))
            return false;

        var code = nextStateCode.Trim();
        return string.Equals(code, ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1), StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase);
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

    private static bool IsEntryState(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var code = stateCode.Trim();
        return code.EndsWith("_REVIEW_STARTED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCancelled(string? stateCode) =>
        !string.IsNullOrWhiteSpace(stateCode)
        && stateCode.Contains("CANCELLED", StringComparison.OrdinalIgnoreCase);
}