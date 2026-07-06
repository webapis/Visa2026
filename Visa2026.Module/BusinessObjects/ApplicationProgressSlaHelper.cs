using System;
using System.Linq;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

public static class ApplicationProgressSlaHelper
{
    public static ApplicationProgressSlaResult Resolve(Application? application, ApplicationProgress? latest = null)
    {
        if (application?.ProgressHistory == null)
            return default;

        latest ??= ApplicationProgressHelper.GetLatest(application.ProgressHistory);
        if (latest?.State?.Code == null || latest.Date == default)
            return default;

        if (!IsMinistryReviewStartedStep(latest.State.Code))
            return default;

        if (!ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(latest.State.Code, out var leg))
            return default;

        var snapshot = application.ApprovalLegSnapshots?
            .FirstOrDefault(s => s.Sequence == leg);
        if (snapshot?.MaxDaysInReview is not > 0)
            return default;

        var workingDays = WorkingDaysHelper.CountWorkingDaysInclusive(latest.Date, DateTime.Today);
        var maxDays = snapshot.MaxDaysInReview.Value;
        var warningDays = snapshot.WarningDaysBeforeMax;
        var ministry = snapshot.MinistryShortName;

        var status = ResolveStatus(workingDays, maxDays, warningDays);
        return new ApplicationProgressSlaResult(status, workingDays, maxDays, warningDays, ministry);
    }

    public static string FormatStatement(ApplicationProgressSlaResult sla)
    {
        if (sla.Status == ApplicationProgressSlaStatus.None
            || sla.WorkingDaysInCurrentStep is not int days
            || sla.MaxDaysInReview is not int max)
            return string.Empty;

        var ministry = string.IsNullOrWhiteSpace(sla.MinistryShortName) ? "—" : sla.MinistryShortName!;
        return sla.Status switch
        {
            ApplicationProgressSlaStatus.Overdue => VisaUiMessages.Format(
                "ApplicationProgress.Sla.Overdue",
                ministry,
                days,
                max),
            ApplicationProgressSlaStatus.Warning => VisaUiMessages.Format(
                "ApplicationProgress.Sla.Warning",
                ministry,
                days,
                max),
            _ => VisaUiMessages.Format(
                "ApplicationProgress.Sla.Ok",
                ministry,
                days,
                max)
        };
    }

    private static bool IsMinistryReviewStartedStep(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        return stateCode.Trim().EndsWith("_REVIEW_STARTED", StringComparison.OrdinalIgnoreCase)
            && ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out _);
    }

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
