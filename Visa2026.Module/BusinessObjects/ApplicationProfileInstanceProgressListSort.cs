using System;
using Visa2026.Module.Appearance;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// ListView sort key for Application progress: row appearance (color) then workflow stage.
/// </summary>
public static class ApplicationProfileInstanceProgressListSort
{
    private const int AppearanceScale = 1000;
    private const int UnknownAppearancePriority = 200;
    private const int UnknownStage = 500;

    public static int ResolveKey(ApplicationProfileInstance? application)
    {
        if (application == null)
            return int.MaxValue;

        var display = ApplicationListViewDisplayState.Resolve(application);
        return Compose(display.ListRowAppearanceStateCode, display.PrimaryStateCode);
    }

    /// <summary>
    /// Ascending: same colors group together (by <see cref="BoStateAppearance.DisplayPriority"/>),
    /// then earlier workflow stages before later ones.
    /// </summary>
    public static int Compose(string? appearanceStateCode, string? primaryStateCode)
    {
        var appearancePriority = ResolveAppearancePriority(appearanceStateCode, primaryStateCode);
        // Prefer primary stage; when SLA paints the row, still stage by the real progress code.
        var stage = ResolveWorkflowStage(primaryStateCode);
        if (stage == UnknownStage || stage == 0)
            stage = ResolveWorkflowStage(appearanceStateCode);
        return (appearancePriority * AppearanceScale) + stage;
    }

    private static int ResolveAppearancePriority(string? appearanceStateCode, string? primaryStateCode)
    {
        if (BoStateAppearanceColors.TryGet(appearanceStateCode, out var appearance))
            return appearance.DisplayPriority;

        if (BoStateAppearanceColors.TryGet(primaryStateCode, out appearance))
            return appearance.DisplayPriority;

        return UnknownAppearancePriority;
    }

    /// <summary>Earlier workflow steps sort before later ones (within the same color priority).</summary>
    public static int ResolveWorkflowStage(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return 0;

        var code = stateCode.Trim();
        if (string.Equals(code, "DRAFT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "OFFICE_PREPARATION", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProfileInstanceProgressLocationCodes.AtOffice, StringComparison.OrdinalIgnoreCase))
            return 10;

        if (TryParseReviewLeg(code, "REVIEW_STARTED", out var startedLeg))
            return 20 + startedLeg;
        if (TryParseReviewLeg(code, "REVIEW_APPROVED", out var approvedLeg))
            return 40 + approvedLeg;
        if (TryParseReviewLeg(code, "REVIEW_REJECTED", out var rejectedLeg))
            return 60 + rejectedLeg;

        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration, StringComparison.OrdinalIgnoreCase))
            return 80;
        if (string.Equals(code, ApplicationProfileInstanceProgressLocationCodes.AtMigrationService, StringComparison.OrdinalIgnoreCase))
            return 85;
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
            return 90;
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
            return 100;
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase))
            return 110;
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return 120;

        if (string.Equals(code, ApplicationProfileInstanceProgressSlaCodes.Warning, StringComparison.OrdinalIgnoreCase))
            return 200;
        if (string.Equals(code, ApplicationProfileInstanceProgressSlaCodes.Overdue, StringComparison.OrdinalIgnoreCase))
            return 210;

        return UnknownStage;
    }

    private static bool TryParseReviewLeg(string code, string suffix, out int leg)
    {
        leg = 0;
        var marker = "_" + suffix;
        var index = code.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index <= 0)
            return false;

        return int.TryParse(code.AsSpan(0, index), out leg) && leg > 0;
    }
}