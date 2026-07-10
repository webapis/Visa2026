using System;
using System.Linq;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

public static class ApplicationMigrationSlaHelper
{
    public static ApplicationProgressSlaResult Resolve(Application? application, ApplicationProgress? latest = null)
    {
        if (application?.ProgressHistory == null)
            return default;

        latest ??= ApplicationProgressHelper.GetLatest(application.ProgressHistory);
        if (latest?.State?.Code == null || latest.Date == default)
            return default;

        if (!IsMigrationServiceProcessStartedStep(latest.State.Code, latest.Location?.Code))
            return default;

        var profile = application.ApplicationType?.MigrationSlaProfile;
        if (profile?.MaxDaysInReview is not > 0)
            return default;

        var workingDays = WorkingDaysHelper.CountWorkingDaysInclusive(latest.Date, DateTime.Today);
        var maxDays = profile.MaxDaysInReview.Value;
        var warningDays = profile.WarningDaysBeforeMax;
        var label = ResolveProfileDisplayLabel(profile);

        var status = ResolveStatus(workingDays, maxDays, warningDays);
        return new ApplicationProgressSlaResult(status, workingDays, maxDays, warningDays, label);
    }

    public static string FormatStatement(ApplicationProgressSlaResult sla)
    {
        if (sla.Status == ApplicationProgressSlaStatus.None
            || sla.WorkingDaysInCurrentStep is not int days
            || sla.MaxDaysInReview is not int max)
            return string.Empty;

        var label = string.IsNullOrWhiteSpace(sla.MinistryShortName)
            ? VisaUiMessages.Get("ApplicationMigration.Sla.DefaultLabel")
            : sla.MinistryShortName!;
        return sla.Status switch
        {
            ApplicationProgressSlaStatus.Overdue => VisaUiMessages.Format(
                "ApplicationMigration.Sla.Overdue",
                label,
                days,
                max),
            ApplicationProgressSlaStatus.Warning => VisaUiMessages.Format(
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

    /// <summary>UI label for the migration SLA tier; follows current UI culture (not report <see cref="LookupBase.NameTm"/>).</summary>
    public static string ResolveProfileDisplayLabel(ApplicationMigrationSlaProfile? profile)
    {
        if (profile == null)
            return VisaUiMessages.Get("ApplicationMigration.Sla.DefaultLabel");

        var code = profile.Code?.Trim();
        if (!string.IsNullOrEmpty(code))
        {
            var key = "ApplicationMigration.Sla.Profile." + code;
            var localized = VisaUiMessages.Get(key);
            if (!string.Equals(localized, key, StringComparison.Ordinal))
                return localized;
        }

        if (!string.IsNullOrWhiteSpace(profile.NameTm))
            return profile.NameTm.Trim();

        if (!string.IsNullOrWhiteSpace(code))
            return code;

        return VisaUiMessages.Get("ApplicationMigration.Sla.DefaultLabel");
    }

    public static bool IsMigrationServiceProcessStartedStep(string? stateCode, string? locationCode) =>
        string.Equals(stateCode, ApplicationProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase)
        && string.Equals(locationCode, ApplicationProgressLocationCodes.AtMigrationService, StringComparison.OrdinalIgnoreCase);

    public static bool IsTerminalMigrationState(string? stateCode) =>
        stateCode != null
        && (string.Equals(stateCode, ApplicationProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stateCode, ApplicationProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stateCode, ApplicationProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase));

    private static ApplicationProgressSlaStatus ResolveStatus(
        int workingDays,
        int maxDays,
        int? warningDaysBeforeMax)
    {
        if (workingDays > maxDays)
            return ApplicationProgressSlaStatus.Overdue;

        if (warningDaysBeforeMax is > 0 && workingDays > warningDaysBeforeMax)
            return ApplicationProgressSlaStatus.Warning;

        return ApplicationProgressSlaStatus.Ok;
    }
}
