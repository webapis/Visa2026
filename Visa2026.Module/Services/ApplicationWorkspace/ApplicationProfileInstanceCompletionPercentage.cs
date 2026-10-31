using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Process completion 0–100 for an instance ListView: Phase 1 timeline (max 40)
/// until AS / process number, then Phase 1 locked at 40 plus issued-document coverage (max 60).
/// Rejection chips are excluded from Phase 2.
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
        if (asGatePassed)
        {
            var phase2 = CalculatePhase2Points(chips);
            return Math.Clamp(Phase1Weight + phase2, 0, 100);
        }

        return Math.Clamp(CalculatePhase1Points(steps), 0, Phase1Weight);
    }

    /// <summary>
    /// Phase 1 points (0–40): done steps / total steps. Current / pending do not count as done
    /// (migration awaiting AS stays current → less than 40 until the gate).
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
    /// No required expected → full Phase 2 weight (after gate).
    /// </summary>
    public static int CalculatePhase2Points(IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip>? chips)
    {
        if (chips == null || chips.Count == 0)
            return Phase2Weight;

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

        if (expected <= 0)
            return Phase2Weight;

        return Math.Clamp((int)Math.Round(Phase2Weight * (double)issued / expected), 0, Phase2Weight);
    }

    public static void ApplyTo(ApplicationProfileInstance application)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.SetListViewCompletionPercentage(Calculate(application));
    }
}