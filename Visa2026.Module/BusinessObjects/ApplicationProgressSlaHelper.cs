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

        if (!TryResolvePendingMinistryLeg(application, latest, out var leg, out var anchorDate))
            return default;

        if (ApplicationProgressLegCodes.IsMinistryReviewStartedStateCode(latest.State.Code))
        {
            var previous = GetPreviousProgress(application, latest);
            if (previous?.Date != default)
                anchorDate = previous.Date;
        }

        var snapshot = application.ApprovalLegSnapshots?
            .FirstOrDefault(s => s.Sequence == leg);
        if (snapshot?.MaxDaysInReview is not > 0)
            return default;

        var workingDays = WorkingDaysHelper.CountWorkingDaysInclusive(anchorDate, DateTime.Today);
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

    private static bool TryResolvePendingMinistryLeg(
        Application application,
        ApplicationProgress latest,
        out int leg,
        out DateTime anchorDate)
    {
        leg = 0;
        anchorDate = default;

        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (route != ApplicationProgressRouteKind.ViaMinistries)
            return false;

        var legCount = ApplicationProgressProfileResolver.GetMinistryLegCount(application);
        if (legCount <= 0)
            return false;

        var stateCode = latest.State?.Code;
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        if (ApplicationProgressTransitionHelper.IsTerminalStateCode(stateCode))
            return false;

        if (ApplicationProgressLegCodes.IsMinistryReviewStartedStateCode(stateCode)
            && ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out leg))
        {
            anchorDate = latest.Date;
            return true;
        }

        if (IsOfficePreparation(latest))
        {
            leg = 1;
            anchorDate = latest.Date;
            return true;
        }

        if (stateCode.Trim().EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase)
            && ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out var approvedLeg)
            && approvedLeg < legCount)
        {
            leg = approvedLeg + 1;
            anchorDate = latest.Date;
            return true;
        }

        return false;
    }

    private static bool IsOfficePreparation(ApplicationProgress progress) =>
        string.Equals(progress.State?.Code, ApplicationProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase)
        && string.Equals(progress.Location?.Code, ApplicationProgressLocationCodes.AtOffice, StringComparison.OrdinalIgnoreCase);

    private static ApplicationProgress? GetPreviousProgress(Application application, ApplicationProgress current)
    {
        var others = application.ProgressHistory?
            .Where(p => p != current)
            .ToList();
        if (others == null || others.Count == 0)
            return null;

        return others
            .Where(p => ApplicationProgressOrderHelper.CompareTimelineOrder(p, current) < 0)
            .OrderByDescending(p => p, Comparer<ApplicationProgress>.Create(ApplicationProgressOrderHelper.CompareTimelineOrder))
            .FirstOrDefault();
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
