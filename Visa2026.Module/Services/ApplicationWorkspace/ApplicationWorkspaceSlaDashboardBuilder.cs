using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// One SLA model for the case header, rings, timeline, and deadlines table.
/// Remaining clocks stop when the process is issued, rejected, or cancelled.
/// </summary>
internal static class ApplicationWorkspaceSlaDashboardBuilder
{
    private const string DateFormat = "dd MMM yyyy";

    public static ApplicationWorkspaceCaseSlaDashboard Build(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        ApplicationProfileInstanceProgressSlaResult currentSla,
        ApplicationWorkspaceCaseChrome chrome,
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep> progressSteps)
    {
        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory);
        var outcome = ResolveOutcome(progressSteps, latest?.State?.Code);
        var isTerminal = outcome is "issued" or "rejected" or "cancelled";
        var start = application.ApplicationDate == default ? (DateTime?)null : application.ApplicationDate.Date;
        var completedOn = ResolveCompletedOn(application, progressSteps, latest);
        var totalSla = ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays(application);
        if (totalSla <= 0)
            totalSla = currentSla.MaxDaysInReview ?? 0;

        var today = DateTime.Today;
        var elapsed = 0;
        if (start is { } startDate)
        {
            var end = isTerminal && completedOn is { } done ? done : today;
            elapsed = WorkingDaysHelper.CountWorkingDaysInclusive(startDate, end);
        }

        var deadlines = BuildDeadlines(application, profile, progressSteps, currentSla, start, isTerminal, today);
        var currentRow = deadlines.FirstOrDefault(d => d.IsCurrent);
        int? currentRemaining = isTerminal ? null : currentRow?.DaysLeftNumber;
        var currentDue = isTerminal ? string.Empty : currentRow?.DueDate ?? string.Empty;
        var currentLabel = isTerminal
            ? OutcomeLabel(outcome)
            : (currentRow?.Step ?? progressSteps.FirstOrDefault(s => s.State == "current")?.Label ?? "Current step");

        int? caseRemaining = null;
        if (!isTerminal && totalSla > 0)
            caseRemaining = Math.Max(0, totalSla - elapsed);

        var expected = isTerminal && completedOn is { } actual
            ? FormatDate(actual)
            : deadlines.LastOrDefault(d => !string.IsNullOrWhiteSpace(d.DueDate))?.DueDate
                ?? (start is { } s && totalSla > 0 ? FormatDate(WorkingDaysHelper.AddWorkingDaysInclusive(s, totalSla)) : string.Empty);

        var alert = string.Empty;
        if (!isTerminal
            && currentRemaining is int stepDays
            && stepDays <= 10
            && !string.IsNullOrWhiteSpace(currentDue))
        {
            alert = $"{currentLabel} deadline approaching. Due in {stepDays} days on {currentDue}.";
        }

        return new ApplicationWorkspaceCaseSlaDashboard
        {
            IsTerminal = isTerminal,
            ProcessOutcome = outcome,
            CaseStatus = isTerminal ? OutcomeLabel(outcome) : ToneLabel(caseRemaining),
            CaseDaysRemaining = caseRemaining,
            TotalSlaDays = totalSla,
            ElapsedDays = elapsed,
            CurrentStepDaysRemaining = currentRemaining,
            CurrentStepDueDate = currentDue,
            CurrentStepLabel = currentLabel,
            StartedOn = chrome.StartedOn,
            MinistryDueDate = currentDue,
            ExpectedCompletionDate = expected,
            MigrationSlaLabel = totalSla > 0 ? $"{totalSla} days" : "—",
            ProfileSlaSource = profile?.Name ?? "Profile template",
            AlertMessage = alert,
            Deadlines = deadlines,
        };
    }

    public static ApplicationWorkspaceCaseChrome WithHeaderRemaining(
        ApplicationWorkspaceCaseChrome chrome,
        ApplicationWorkspaceCaseSlaDashboard sla) =>
        new()
        {
            DisplayNumber = chrome.DisplayNumber,
            ProcessNumber = chrome.ProcessNumber,
            TemplateFamilyKey = chrome.TemplateFamilyKey,
            TemplateFamilyLabel = chrome.TemplateFamilyLabel,
            StartedOn = chrome.StartedOn,
            CurrentStep = chrome.CurrentStep,
            ProjectName = chrome.ProjectName,
            SlaDaysRemaining = sla.IsTerminal ? null : sla.CurrentStepDaysRemaining,
            PeopleNames = chrome.PeopleNames,
            MergedFromCount = chrome.MergedFromCount,
            ShowProcessNumber = chrome.ShowProcessNumber,
            ProfileTemplateName = chrome.ProfileTemplateName,
            ResolvedLinksLocked = chrome.ResolvedLinksLocked,
        };

    private static IReadOnlyList<ApplicationWorkspaceCaseSlaDeadline> BuildDeadlines(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep> progressSteps,
        ApplicationProfileInstanceProgressSlaResult currentSla,
        DateTime? start,
        bool isTerminal,
        DateTime today)
    {
        var deadlines = new List<ApplicationWorkspaceCaseSlaDeadline>();
        var forecast = start;
        DateTime? lastActual = start;

        foreach (var step in progressSteps)
        {
            var slaDays = ResolveStepSlaDays(application, profile, step.Key);
            var actual = TryParseDate(step.Date);
            DateTime? anchor;
            if (string.Equals(step.Key, ApplicationWorkspaceProgressTimeline.OfficeKey, StringComparison.OrdinalIgnoreCase))
                anchor = start;
            else if (step.State == "pending")
                anchor = forecast;
            else
                anchor = lastActual ?? forecast;

            var dueDate = slaDays > 0 && anchor is { } a
                ? WorkingDaysHelper.AddWorkingDaysInclusive(a, slaDays)
                : (DateTime?)null;
            var dueText = dueDate is { } due ? FormatDate(due) : string.Empty;

            var status = isTerminal || step.State == "done"
                ? "completed"
                : step.State == "current"
                    ? "inprogress"
                    : "pending";
            var isCurrent = !isTerminal && step.State == "current";

            int? daysLeft = null;
            if (isCurrent)
            {
                if (currentSla.MaxDaysInReview is int max && currentSla.WorkingDaysInCurrentStep is int used)
                    daysLeft = Math.Max(0, max - used);
                else if (slaDays > 0 && anchor is { } currentAnchor)
                    daysLeft = Math.Max(0, slaDays - WorkingDaysHelper.CountWorkingDaysInclusive(currentAnchor, today));

                var usedDays = currentSla.WorkingDaysInCurrentStep
                    ?? (slaDays > 0 && anchor is { } usedAnchor
                        ? WorkingDaysHelper.CountWorkingDaysInclusive(usedAnchor, today)
                        : (int?)null);
                var maxDays = currentSla.MaxDaysInReview ?? slaDays;
                if (usedDays is int elapsedInStep && maxDays > 0 && elapsedInStep > maxDays)
                    status = "overdue";
            }

            deadlines.Add(new ApplicationWorkspaceCaseSlaDeadline
            {
                Step = step.Label,
                DueDate = dueText,
                DaysLeft = daysLeft is int n ? n.ToString(CultureInfo.InvariantCulture) : "—",
                DaysLeftNumber = daysLeft,
                Status = status,
                IsCurrent = isCurrent,
            });

            if (dueDate is { } nextDue)
                forecast = WorkingDaysHelper.NextWorkingDay(nextDue);
            if (actual is { } actualDate)
                lastActual = actualDate;
        }

        return deadlines;
    }

    private static int ResolveStepSlaDays(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        string stepKey)
    {
        if (string.Equals(stepKey, ApplicationWorkspaceProgressTimeline.OfficeKey, StringComparison.OrdinalIgnoreCase))
        {
            if (profile is { MinistrySlaDays: > 0 })
                return profile.MinistrySlaDays;
            return ApplicationProfileConfigurationResolver.GetMinistrySlaMaxDays(application);
        }

        if (string.Equals(stepKey, ApplicationWorkspaceProgressTimeline.MigrationKey, StringComparison.OrdinalIgnoreCase))
            return ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays(application);

        if (stepKey.StartsWith("leg-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(stepKey.AsSpan(4), out var sequence))
        {
            var snapshot = application.ApprovalLegSnapshots?
                .FirstOrDefault(s => s.Sequence == sequence);
            if (snapshot?.MaxDaysInReview is > 0)
                return snapshot.MaxDaysInReview.Value;
            if (profile is { MinistrySlaDays: > 0 })
                return profile.MinistrySlaDays;
            return ApplicationProfileConfigurationResolver.GetMinistrySlaMaxDays(application);
        }

        return 0;
    }

    private static string ResolveOutcome(
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep> progressSteps,
        string? latestCode)
    {
        var fromStep = progressSteps.LastOrDefault(s =>
            s.OutcomeKind is "issued" or "rejected" or "cancelled")?.OutcomeKind;
        if (!string.IsNullOrWhiteSpace(fromStep))
            return fromStep;

        if (ApplicationProfileInstanceProgressTransitionHelper.IsTerminalStateCode(latestCode))
        {
            if (string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
                return "issued";
            if (string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase))
                return "rejected";
            if (string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
                return "cancelled";
        }

        return "inprocess";
    }

    private static DateTime? ResolveCompletedOn(
        ApplicationProfileInstance application,
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep> progressSteps,
        ApplicationProfileInstanceProgress? latest)
    {
        if (latest?.Date is { } latestDate && latestDate != default)
            return latestDate.Date;

        foreach (var step in progressSteps.Reverse())
        {
            var parsed = TryParseDate(step.Date);
            if (parsed != null)
                return parsed;
        }

        return application.ApplicationDate == default ? null : application.ApplicationDate.Date;
    }

    private static DateTime? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTime.TryParseExact(
            value.Trim(),
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date.Date
            : null;
    }

    private static string FormatDate(DateTime date) =>
        date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static string OutcomeLabel(string outcome) => outcome switch
    {
        "issued" => "Issued",
        "rejected" => "Rejected",
        "cancelled" => "Cancelled",
        _ => "In process",
    };

    private static string ToneLabel(int? daysRemaining) => daysRemaining switch
    {
        null => "—",
        <= 0 => "Due",
        1 => "Due tomorrow",
        <= 10 => "Due soon",
        _ => "On track",
    };
}
