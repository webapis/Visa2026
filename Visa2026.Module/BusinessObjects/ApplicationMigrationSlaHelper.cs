using System;
using System.Linq;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

public static class ApplicationMigrationSlaHelper
{
    public static ApplicationProfileInstanceProgressSlaResult Resolve(ApplicationProfileInstance? application, ApplicationProfileInstanceProgress? latest = null)
    {
        if (application?.ProgressHistory == null)
            return default;

        latest ??= ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory);
        if (latest?.State?.Code == null || latest.Date == default)
            return default;

        if (!IsMigrationServiceProcessStartedStep(latest.State.Code))
            return default;

        if (application.ApplicationProfile is { MigrationSlaDays: > 0 } applicationProfile)
        {
            var workingDays = WorkingDaysHelper.CountWorkingDaysInclusive(latest.Date, DateTime.Today);
            var maxDays = applicationProfile.MigrationSlaDays;
            var label = VisaUiMessages.Get("ApplicationMigration.Sla.DefaultLabel");
            var status = ResolveStatus(workingDays, maxDays, warningDaysBeforeMax: null);
            return new ApplicationProfileInstanceProgressSlaResult(status, workingDays, maxDays, null, label);
        }

        return default;
    }

    public static string FormatStatement(ApplicationProfileInstanceProgressSlaResult sla)
    {
        if (sla.Status == ApplicationProfileInstanceProgressSlaStatus.None
            || sla.WorkingDaysInCurrentStep is not int days
            || sla.MaxDaysInReview is not int max)
            return string.Empty;

        var label = string.IsNullOrWhiteSpace(sla.MinistryShortName)
            ? VisaUiMessages.Get("ApplicationMigration.Sla.DefaultLabel")
            : sla.MinistryShortName!;
        return sla.Status switch
        {
            ApplicationProfileInstanceProgressSlaStatus.Overdue => VisaUiMessages.Format(
                "ApplicationMigration.Sla.Overdue",
                label,
                days,
                max),
            ApplicationProfileInstanceProgressSlaStatus.Warning => VisaUiMessages.Format(
                "ApplicationMigration.Sla.Warning",
                label,
                days,
                max),
            _ => VisaUiMessages.Format(
                "ApplicationMigration.Sla.Ok",
                label,
                days,
                max)
        };
    }

    public static bool IsMigrationServiceProcessStartedStep(string? stateCode) =>
        string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase);

    public static bool IsTerminalMigrationState(string? stateCode) =>
        stateCode != null
        && (string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase));

    private static ApplicationProfileInstanceProgressSlaStatus ResolveStatus(
        int workingDays,
        int maxDays,
        int? warningDaysBeforeMax)
    {
        if (workingDays > maxDays)
            return ApplicationProfileInstanceProgressSlaStatus.Overdue;

        if (warningDaysBeforeMax is > 0 && workingDays > warningDaysBeforeMax)
            return ApplicationProfileInstanceProgressSlaStatus.Warning;

        return ApplicationProfileInstanceProgressSlaStatus.Ok;
    }
}
