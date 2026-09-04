using System;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Progress nav + step cue for ministry Approved/Unapproved with no letter.
/// Cue only — does not block Advance.
/// </summary>
public static class ApplicationWorkspaceProgressLetterCompleteness
{
    public static bool ResolveMissing(string? stepKey, bool isMinistryDecision, bool hasLetter)
    {
        if (string.IsNullOrWhiteSpace(stepKey)
            || !stepKey.StartsWith("leg-", StringComparison.OrdinalIgnoreCase))
            return false;

        return isMinistryDecision && !hasLetter;
    }

    public static bool IsMissingLetter(ApplicationWorkspaceCaseProgressStep? step) =>
        step != null && step.MissingMinistryLetter;

    public static int MissingCount(ApplicationWorkspaceCaseView? view) =>
        view?.ProgressSteps?.Count(IsMissingLetter) ?? 0;
}