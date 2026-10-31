using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Process completion 0–100 for an instance ListView: Phase 1 timeline (max 40) from
/// done steps, plus Phase 2 issued-document coverage (max 60) only after AS / process number.
/// Rejection chips are excluded from Phase 2. Empty roster (0/0) earns no Phase 2 points.
/// </summary>
public static class ApplicationProfileInstanceCompletionPercentage
{
    public const int Phase1Weight = 40;
    public const int Phase2Weight = 60;

    /// <summary>
    /// AS / process number received — past “Awaiting AS (CO) number”.
    /// Uses denormalized <see cref="ApplicationProfileInstance.ProcessNumber"/> and/or
    /// a <c>PROCESS_STARTED</c> history row (direct routes may only set the number).
    /// </summary>
    public static bool HasPassedAsGate(ApplicationProfileInstance? application)
    {
        if (application == null)
            return false;

        if (!string.IsNullOrWhiteSpace(ApplicationProcessNumberHelper.Normalize(application.ProcessNumber)))
            return true;

        return ApplicationProcessNumberHelper.HasProcessStartedStep(application);
    }

    public static int Calculate(ApplicationProfileInstance? application) =>
        Calculate(
            application?.ListViewProgressSteps,
            application?.ListViewResultCoverageChips,
            HasPassedAsGate(application));

    public static int Calculate(
        IReadOnlyList<ApplicationWorkspaceListProgressSteps.Step>? steps,
        IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip>? chips,
        bool asGatePassed)
    {
        // Always score the timeline from actual step states — do not lock Phase 1 at 40
        // just because a process number exists (incomplete current step must stay below 100%).
        var phase1 = CalculatePhase1Points(steps);
        if (!asGatePassed)
            return Math.Clamp(phase1, 0, Phase1Weight);

        var phase2 = CalculatePhase2Points(chips);
        return Math.Clamp(phase1 + phase2, 0, 100);
    }

    /// <summary>
    /// Phase 1 points (0–40): done steps / total steps. Current / pending do not count as done.
    /// </summary>
    public static int CalculatePhase1Points(IReadOnlyList<ApplicationWorkspaceListProgressSteps.Step>? steps)
    {
        if (steps == null || steps.Count == 0)
            return 0;

        var done = 0;
        for (var i = 0; i < steps.Count; i++)
        {
            if (string.Equals(steps[i].State, "done", StringComparison.OrdinalIgnoreCase))
                done++;
        }

        return (int)Math.Round(Phase1Weight * (double)done / steps.Count);
    }

    /// <summary>
    /// Phase 2 points (0–60): sum(coverage) / sum(expected) over required chips.
    /// No required expected (empty roster / 0/0 chips) → 0 Phase 2 points (not complete).
    /// </summary>
    public static int CalculatePhase2Points(IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip>? chips)
    {
        if (chips == null || chips.Count == 0)
            return 0;

        var issued = 0;
        var expected = 0;
        for (var i = 0; i < chips.Count; i++)
        {
            var chip = chips[i];
            if (chip.IsOptional)
                continue;

            issued += Math.Max(0, chip.CoverageCount);
            expected += Math.Max(0, chip.ExpectedCount);
        }

        // 0/0 on required chips (no linked people) is incomplete — not a free 60 points.
        if (expected <= 0)
            return 0;

        return Math.Clamp((int)Math.Round(Phase2Weight * (double)issued / expected), 0, Phase2Weight);
    }

    public static void ApplyTo(ApplicationProfileInstance application)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.SetListViewCompletionPercentage(Calculate(application));
    }
}