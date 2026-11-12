using System;
using Visa2026.Module.Appearance;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.BusinessObjects;

internal readonly struct ApplicationListViewDisplayState
{
    public string PrimaryStateCode { get; init; }
    public string CurrentState { get; init; }
    public DateTime? LatestProgressDate { get; init; }
    public int? WorkingDaysInCurrentStep { get; init; }
    public string ProgressSlaStatement { get; init; }
    public int? WorkingDaysInMigrationStep { get; init; }
    public string MigrationSlaStatement { get; init; }
    public string ProgressSlaAppearanceCode { get; init; }
    public string ListRowAppearanceStateCode { get; init; }
    public string ListRowCssClass { get; init; }
    public int ProgressListSortKey { get; init; }

    public static ApplicationListViewDisplayState Resolve(ApplicationProfileInstance application)
    {
        var latest = ApplicationLatestProgressSyncHelper.ResolveLatestForDisplay(application);
        var progressSla = ApplicationProfileInstanceProgressSlaHelper.Resolve(application, latest);
        var migrationSla = ApplicationMigrationSlaHelper.Resolve(application, latest);

        var primaryStateCode = ApplicationProfileInstanceProgressPrimaryStateCodeResolver.ResolveFromLatest(latest) ?? string.Empty;
        var progressSlaCode = progressSla.AppearanceStateCode ?? string.Empty;
        var migrationSlaCode = migrationSla.AppearanceStateCode ?? string.Empty;
        var slaAppearanceCode = !string.IsNullOrEmpty(progressSlaCode) ? progressSlaCode : migrationSlaCode;
        var listRowAppearance = !string.IsNullOrEmpty(slaAppearanceCode)
            ? slaAppearanceCode
            : CompletedRouteAppearance(application, primaryStateCode);
        // Green > AS/CO yellow > Submitted yellow > Ministrlikden çykan (last approval) > approval-leg teal > At Office peach > SLA.
        if (ApplicationWorkspaceViaMinistryIssuanceGreenGate.IsGreen(application, primaryStateCode))
            listRowAppearance = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        else if (string.Equals(
            primaryStateCode,
            ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
            StringComparison.OrdinalIgnoreCase))
            listRowAppearance = ApplicationProfileInstanceProgressStateCodes.ProcessStarted;
        else if (string.Equals(
            primaryStateCode,
            ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration,
            StringComparison.OrdinalIgnoreCase))
            listRowAppearance = ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration;
        else if (ApplicationProfileInstanceProgressLegCodes.IsLastApprovalLegComplete(
            primaryStateCode,
            ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(application)))
            listRowAppearance = ApplicationProfileInstanceProgressLegCodes.LastApprovalCompleteListAppearance;
        else if (ApplicationProfileInstanceProgressLegCodes.IsApprovalLegListRowTintState(primaryStateCode))
            listRowAppearance = primaryStateCode;
        else if (string.Equals(
            primaryStateCode,
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                primaryStateCode,
                ApplicationProfileInstanceProgressLocationCodes.AtOffice,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                listRowAppearance,
                ApplicationProfileInstanceProgressLocationCodes.AtOffice,
                StringComparison.OrdinalIgnoreCase))
            listRowAppearance = string.Equals(
                primaryStateCode,
                ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
                StringComparison.OrdinalIgnoreCase)
                ? ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared
                : ApplicationProfileInstanceProgressLocationCodes.AtOffice;
        var listRowCssClass = ResolveListRowCssClass(listRowAppearance);

        return new ApplicationListViewDisplayState
        {
            PrimaryStateCode = primaryStateCode,
            CurrentState = ApplicationProfileInstanceProgressPrimaryStateCodeResolver.ResolveDisplayName(application) ?? string.Empty,
            LatestProgressDate = latest?.Date,
            WorkingDaysInCurrentStep = progressSla.WorkingDaysInCurrentStep,
            ProgressSlaStatement = ApplicationProfileInstanceProgressSlaHelper.FormatStatement(progressSla),
            WorkingDaysInMigrationStep = migrationSla.WorkingDaysInCurrentStep,
            MigrationSlaStatement = ApplicationMigrationSlaHelper.FormatStatement(migrationSla),
            ProgressSlaAppearanceCode = slaAppearanceCode,
            ListRowAppearanceStateCode = listRowAppearance,
            ListRowCssClass = listRowCssClass,
            ProgressListSortKey = ApplicationProfileInstanceProgressListSort.Compose(listRowAppearance, primaryStateCode),
        };
    }

    /// <summary>
    /// A finished route uses the same green as Issued, including a short route such as
    /// Seretmezlik that ends at hand-in instead of an AS number or Issued step.
    /// </summary>
    private static string CompletedRouteAppearance(ApplicationProfileInstance application, string stateCode)
    {
        if (ApplicationProfileInstanceProgressTransitionHelper.IsTerminalStateCode(stateCode, application)
            && ApplicationMigrationSubmission.IsState(stateCode))
            return ApplicationProfileInstanceProgressStateCodes.ProcessIssued;

        return stateCode;
    }

    private static string ResolveListRowCssClass(string stateCode)
    {
        if (string.IsNullOrEmpty(stateCode)
            || !BoStateAppearanceColors.TryGet(stateCode, out var appearance))
            return string.Empty;

        return $"{appearance.RowCssClass} visa-progress-row";
    }
}