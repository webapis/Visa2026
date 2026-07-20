using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Legal <see cref="ApplicationProgress"/> state transitions per route (location removed from progress model).
/// </summary>
public static class ApplicationProgressTransitionHelper
{
    private readonly record struct ProgressStep(string StateCode)
    {
        public static ProgressStep Parse(ApplicationProgress? progress) =>
            progress?.State?.Code == null
                ? default
                : new ProgressStep(progress.State.Code.Trim());

        public bool IsDefault => string.IsNullOrEmpty(StateCode);
    }

    private readonly record struct ProgressTransition(ProgressStep From, ProgressStep To);

    private static readonly HashSet<string> TerminalStateCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationProgressStateCodes.ProcessIssued,
        ApplicationProgressStateCodes.ProcessRejected,
        ApplicationProgressStateCodes.ProcessCancelled,
        ApplicationProgressStateCodes.Review1Rejected,
        ApplicationProgressStateCodes.Review2Rejected
    };

    static ApplicationProgressTransitionHelper()
    {
        for (var leg = 3; leg <= ApplicationProgressLegCodes.MaxLegCount; leg++)
            TerminalStateCodes.Add(ApplicationProgressLegCodes.ReviewRejected(leg));
    }

    public static bool IsTerminalStateCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var trimmed = stateCode.Trim();
        return TerminalStateCodes.Contains(trimmed)
            || ApplicationProgressLegCodes.IsReviewRejectedStateCode(trimmed);
    }

    public static IReadOnlyList<string> GetAllowedNextStateCodes(
        Application? application,
        ApplicationProgress? afterStep,
        ApplicationProgress? currentRow = null)
    {
        if (application == null)
            return Array.Empty<string>();

        afterStep ??= GetLatestProgress(application, currentRow, null);
        if (afterStep == null)
            return GetAllowedFirstStateCodes(application);

        if (IsTerminalStateCode(afterStep.State?.Code))
            return Array.Empty<string>();

        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (!route.HasValue)
            return ApplicationProgressRouteHelper.GetAllowedStateCodes(application);

        var legCount = ApplicationProgressProfileResolver.GetMinistryLegCount(application);
        var fromStep = ProgressStep.Parse(afterStep);
        var routeAllowed = ApplicationProgressRouteHelper.GetAllowedStateCodes(route.Value, legCount)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Legacy prep rows: allow leaving office into the first active step.
        if (IsLegacyOfficePreparation(afterStep.State?.Code))
        {
            return GetAllowedFirstStateCodes(application)
                .Where(routeAllowed.Contains)
                .ToList();
        }

        return GetTransitions(route.Value, legCount)
            .Where(t => StepsEqual(t.From, fromStep))
            .Select(t => t.To.StateCode)
            .Where(routeAllowed.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// State codes allowed in the UI for this progress row (transition from prior step; keeps current value when editing).
    /// </summary>
    public static IReadOnlyList<string> GetAllowedStateCodesForProgressRow(
        ApplicationProgress progress,
        IObjectSpace? objectSpace)
    {
        if (progress.Application == null)
            return Array.Empty<string>();

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(progress.State?.Code))
            codes.Add(progress.State.Code.Trim());

        ApplicationProgress? afterStep;
        if (objectSpace != null && objectSpace.IsNewObject(progress))
            afterStep = GetLatestProgress(progress.Application, progress, objectSpace);
        else
            afterStep = GetPreviousProgress(progress.Application, progress, objectSpace);

        if (afterStep == null)
        {
            foreach (var code in GetAllowedFirstStateCodes(progress.Application))
                codes.Add(code);
            return codes.ToList();
        }

        if (IsTerminalStateCode(afterStep.State?.Code))
            return codes.ToList();

        foreach (var code in GetAllowedNextStateCodes(progress.Application, afterStep, progress))
            codes.Add(code);

        return codes.ToList();
    }

    public static string? GetSuggestedNextStateCode(
        Application? application,
        ApplicationProgress? latestExcludingCurrent)
    {
        var nextStates = GetAllowedNextStateCodes(application, latestExcludingCurrent);
        return nextStates.Count == 0 ? null : nextStates[0];
    }

    public static void TryApplySuggestedNextStep(ApplicationProgress progress)
    {
        if (progress.Application == null || progress.State != null)
            return;

        var objectSpace = ObjectSpaceHelper.Get(progress.Application) ?? ObjectSpaceHelper.Get(progress);
        if (objectSpace == null)
            return;

        var afterStep = GetLatestProgress(progress.Application, progress, objectSpace);
        var suggested = GetSuggestedNextStateCode(progress.Application, afterStep);
        if (string.IsNullOrWhiteSpace(suggested))
            return;

        var state = FindStateByCode(objectSpace, suggested);
        if (state != null)
            progress.State = state;
    }

    private static ApplicationState? FindStateByCode(IObjectSpace objectSpace, string code) =>
        objectSpace.GetObjectsQuery<ApplicationState>()
            .FirstOrDefault(s => s.Code == code);

    public static bool TryValidateProgressStep(
        ApplicationProgress? progress,
        IObjectSpace? objectSpace,
        out string? errorMessage)
    {
        errorMessage = null;
        if (progress?.Application == null)
            return true;

        if (!ApplicationProgressRouteHelper.TryValidateProgressStep(progress, out errorMessage))
            return false;

        if (!ApplicationProgressProfileResolver.TryValidateProjectContractForProgress(progress, objectSpace, out errorMessage))
            return false;

        var previous = GetPreviousProgress(progress.Application, progress, objectSpace);
        if (previous == null)
        {
            if (!IsAllowedFirstState(progress.Application, progress.State?.Code))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProgress.FirstStepMustBeOfficePreparation");
                return false;
            }

            if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(progress.State?.Code)
                && progress.Application?.ApplicationType?.MigrationSlaProfile?.MaxDaysInReview is not > 0)
            {
                errorMessage = VisaUiMessages.Get("ApplicationProgress.MigrationSlaProfileRequired");
                return false;
            }

            if (progress.State?.Code != null
                && string.Equals(progress.State.Code.Trim(), ApplicationProgressLegCodes.ReviewStarted(1), StringComparison.OrdinalIgnoreCase)
                && objectSpace != null
                && ApplicationProgressRouteHelper.GetTypePickerRouteFilter(progress.Application)
                    == ApplicationProgressRouteKind.ViaMinistries
                && !MinistryReviewSlaHelper.TryValidateConfigured(objectSpace, out _))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProgress.MinistryReviewSlaRequired");
                return false;
            }

            return true;
        }

        if (IsTerminalStateCode(previous.State?.Code))
        {
            errorMessage = VisaUiMessages.Get("ApplicationProgress.CannotAdvanceFromTerminal");
            return false;
        }

        if (progress.Date.Date < previous.Date.Date)
        {
            errorMessage = VisaUiMessages.Get("ApplicationProgress.DateCannotBeBeforePrevious");
            return false;
        }

        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(progress.Application);
        if (!route.HasValue)
            return true;

        var legCount = ApplicationProgressProfileResolver.GetMinistryLegCount(progress.Application);
        var fromStep = ProgressStep.Parse(previous);
        var toStep = ProgressStep.Parse(progress);

        if (IsTransitionAllowed(route.Value, legCount, fromStep, toStep))
        {
            if (progress.State?.Code != null
                && (string.Equals(progress.State.Code.Trim(), ApplicationProgressLegCodes.ReviewStarted(1), StringComparison.OrdinalIgnoreCase)
                    || (progress.State.Code.Trim().EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase)
                        && ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(progress.State.Code, out _)))
                && objectSpace != null
                && route.Value == ApplicationProgressRouteKind.ViaMinistries
                && !MinistryReviewSlaHelper.TryValidateConfigured(objectSpace, out _))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProgress.MinistryReviewSlaRequired");
                return false;
            }

            if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(progress.State?.Code)
                && progress.Application?.ApplicationType?.MigrationSlaProfile?.MaxDaysInReview is not > 0)
            {
                errorMessage = VisaUiMessages.Get("ApplicationProgress.MigrationSlaProfileRequired");
                return false;
            }

            return true;
        }

        errorMessage = VisaUiMessages.Format(
            "ApplicationProgress.InvalidTransition",
            FormatStep(fromStep),
            FormatStep(toStep));
        return false;
    }

    private static ApplicationProgress? GetLatestProgress(
        Application application,
        ApplicationProgress? exclude,
        IObjectSpace? objectSpace) =>
        ApplicationProgressHelper.GetLatest(
            application.ProgressHistory?.Where(p => p != exclude && (objectSpace == null || !objectSpace.IsObjectToDelete(p))),
            objectSpace);

    private static ApplicationProgress? GetPreviousProgress(
        Application application,
        ApplicationProgress current,
        IObjectSpace? objectSpace)
    {
        var others = application.ProgressHistory?
            .Where(p => p != current && (objectSpace == null || !objectSpace.IsObjectToDelete(p)))
            .ToList();
        if (others == null || others.Count == 0)
            return null;

        return others
            .Where(p => p.Date < current.Date || (p.Date == current.Date && p.ID != Guid.Empty && current.ID != Guid.Empty && p.ID < current.ID))
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.ID)
            .FirstOrDefault();
    }

    private static bool IsLegacyOfficePreparation(string? stateCode) =>
        string.Equals(stateCode, ApplicationProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> GetAllowedFirstStateCodes(Application? application)
    {
        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (route == ApplicationProgressRouteKind.DirectToMigrationService)
        {
            return
            [
                ApplicationProgressStateCodes.ProcessStarted,
                ApplicationProgressStateCodes.ProcessCancelled
            ];
        }

        // Via ministries (or unknown): first explicit step is first-leg started (office is implied).
        return
        [
            ApplicationProgressLegCodes.ReviewStarted(1),
            ApplicationProgressLegCodes.ReviewRejected(1),
            ApplicationProgressStateCodes.ProcessCancelled
        ];
    }

    private static bool IsAllowedFirstState(Application? application, string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        return GetAllowedFirstStateCodes(application)
            .Contains(stateCode.Trim(), StringComparer.OrdinalIgnoreCase)
            // Historical seed rows may still exist.
            || IsLegacyOfficePreparation(stateCode);
    }

    private static bool IsTransitionAllowed(
        ApplicationProgressRouteKind route,
        int ministryLegCount,
        ProgressStep from,
        ProgressStep to)
    {
        if (from.IsDefault || to.IsDefault)
            return false;

        if (IsLegacyOfficePreparation(from.StateCode))
        {
            if (route == ApplicationProgressRouteKind.DirectToMigrationService)
            {
                return string.Equals(to.StateCode, ApplicationProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(to.StateCode, ApplicationProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(to.StateCode, ApplicationProgressLegCodes.ReviewStarted(1), StringComparison.OrdinalIgnoreCase)
                || string.Equals(to.StateCode, ApplicationProgressLegCodes.ReviewRejected(1), StringComparison.OrdinalIgnoreCase)
                || string.Equals(to.StateCode, ApplicationProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase);
        }

        return GetTransitions(route, ministryLegCount).Any(t => StepsEqual(t.From, from) && StepsEqual(t.To, to));
    }

    private static IEnumerable<ProgressTransition> GetTransitions(
        ApplicationProgressRouteKind route,
        int ministryLegCount)
    {
        var processStarted = Step(ApplicationProgressStateCodes.ProcessStarted);
        var edges = new List<ProgressTransition>();

        if (route == ApplicationProgressRouteKind.DirectToMigrationService)
        {
            AddProcessOutcomes(edges, processStarted);
            AddCancellationFromActiveSteps(edges, processStarted);
            return edges;
        }

        var legCount = Math.Clamp(ministryLegCount, 1, ApplicationProgressLegCodes.MaxLegCount);
        var started1 = Step(ApplicationProgressLegCodes.ReviewStarted(1));
        var approvedSteps = new List<ProgressStep>();

        for (var leg = 1; leg <= legCount; leg++)
        {
            var approved = Step(ApplicationProgressLegCodes.ReviewApproved(leg));
            var rejected = Step(ApplicationProgressLegCodes.ReviewRejected(leg));
            approvedSteps.Add(approved);

            if (leg == 1)
            {
                edges.Add(new ProgressTransition(started1, approved));
                edges.Add(new ProgressTransition(started1, rejected));
            }
            else
            {
                var priorApproved = approvedSteps[leg - 2];
                edges.Add(new ProgressTransition(priorApproved, approved));
                edges.Add(new ProgressTransition(priorApproved, rejected));
            }
        }

        var lastApproved = approvedSteps[^1];
        edges.Add(new ProgressTransition(lastApproved, processStarted));

        AddProcessOutcomes(edges, processStarted);
        var cancellationFrom = new List<ProgressStep> { started1, processStarted };
        cancellationFrom.AddRange(approvedSteps);
        AddCancellationFromActiveSteps(edges, cancellationFrom.ToArray());

        return edges;
    }

    private static void AddProcessOutcomes(List<ProgressTransition> edges, ProgressStep processStarted)
    {
        edges.Add(new ProgressTransition(processStarted, Step(ApplicationProgressStateCodes.ProcessIssued)));
        edges.Add(new ProgressTransition(processStarted, Step(ApplicationProgressStateCodes.ProcessRejected)));
    }

    private static void AddCancellationFromActiveSteps(List<ProgressTransition> edges, params ProgressStep[] fromSteps)
    {
        foreach (var from in fromSteps)
            edges.Add(new ProgressTransition(from, Step(ApplicationProgressStateCodes.ProcessCancelled)));
    }

    private static ProgressStep Step(string stateCode) => new(stateCode);

    private static bool StepsEqual(ProgressStep a, ProgressStep b) =>
        string.Equals(a.StateCode, b.StateCode, StringComparison.OrdinalIgnoreCase);

    private static string FormatStep(ProgressStep step) => step.StateCode;
}