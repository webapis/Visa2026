using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Legal <see cref="ApplicationProfileInstanceProgress"/> state transitions per route (location removed from progress model).
/// </summary>
public static class ApplicationProfileInstanceProgressTransitionHelper
{
    private readonly record struct ProgressStep(string StateCode)
    {
        public static ProgressStep Parse(ApplicationProfileInstanceProgress? progress) =>
            progress?.State?.Code == null
                ? default
                : new ProgressStep(progress.State.Code.Trim());

        public bool IsDefault => string.IsNullOrEmpty(StateCode);
    }

    private readonly record struct ProgressTransition(ProgressStep From, ProgressStep To);

    private static readonly HashSet<string> TerminalStateCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
        ApplicationProfileInstanceProgressStateCodes.ProcessRejected,
        ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
        ApplicationProfileInstanceProgressStateCodes.Review1Rejected,
        ApplicationProfileInstanceProgressStateCodes.Review2Rejected
    };

    static ApplicationProfileInstanceProgressTransitionHelper()
    {
        for (var leg = 3; leg <= ApplicationProfileInstanceProgressLegCodes.MaxLegCount; leg++)
            TerminalStateCodes.Add(ApplicationProfileInstanceProgressLegCodes.ReviewRejected(leg));
    }

    public static bool IsTerminalStateCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var trimmed = stateCode.Trim();
        return TerminalStateCodes.Contains(trimmed)
            || ApplicationProfileInstanceProgressLegCodes.IsReviewRejectedStateCode(trimmed);
    }

    public static IReadOnlyList<string> GetAllowedNextStateCodes(
        ApplicationProfileInstance? application,
        ApplicationProfileInstanceProgress? afterStep,
        ApplicationProfileInstanceProgress? currentRow = null)
    {
        if (application == null)
            return Array.Empty<string>();

        afterStep ??= GetLatestProgress(application, currentRow, null);
        if (afterStep == null)
            return GetAllowedFirstStateCodes(application);

        if (IsTerminalStateCode(afterStep.State?.Code))
            return Array.Empty<string>();

        var route = ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (!route.HasValue)
            return ApplicationProfileInstanceProgressRouteHelper.GetAllowedStateCodes(application);

        var legCount = ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(application);
        var fromStep = ProgressStep.Parse(afterStep);
        var routeAllowed = ApplicationProfileInstanceProgressRouteHelper.GetAllowedStateCodes(route.Value, legCount)
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
        ApplicationProfileInstanceProgress progress,
        IObjectSpace? objectSpace)
    {
        if (progress.ApplicationProfileInstance == null)
            return Array.Empty<string>();

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(progress.State?.Code))
            codes.Add(progress.State.Code.Trim());

        ApplicationProfileInstanceProgress? afterStep;
        if (objectSpace != null && objectSpace.IsNewObject(progress))
            afterStep = GetLatestProgress(progress.ApplicationProfileInstance, progress, objectSpace);
        else
            afterStep = GetPreviousProgress(progress.ApplicationProfileInstance, progress, objectSpace);

        if (afterStep == null)
        {
            foreach (var code in GetAllowedFirstStateCodes(progress.ApplicationProfileInstance))
                codes.Add(code);
            return codes.ToList();
        }

        if (IsTerminalStateCode(afterStep.State?.Code))
            return codes.ToList();

        foreach (var code in GetAllowedNextStateCodes(progress.ApplicationProfileInstance, afterStep, progress))
            codes.Add(code);

        return codes.ToList();
    }

    public static string? GetSuggestedNextStateCode(
        ApplicationProfileInstance? application,
        ApplicationProfileInstanceProgress? latestExcludingCurrent)
    {
        var nextStates = GetAllowedNextStateCodes(application, latestExcludingCurrent);
        return nextStates.Count == 0 ? null : nextStates[0];
    }

    public static void TryApplySuggestedNextStep(ApplicationProfileInstanceProgress progress)
    {
        if (progress.ApplicationProfileInstance == null || progress.State != null)
            return;

        var objectSpace = ObjectSpaceHelper.Get(progress.ApplicationProfileInstance) ?? ObjectSpaceHelper.Get(progress);
        if (objectSpace == null)
            return;

        var afterStep = GetLatestProgress(progress.ApplicationProfileInstance, progress, objectSpace);
        var suggested = GetSuggestedNextStateCode(progress.ApplicationProfileInstance, afterStep);
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
        ApplicationProfileInstanceProgress? progress,
        IObjectSpace? objectSpace,
        out string? errorMessage)
    {
        errorMessage = null;
        if (progress?.ApplicationProfileInstance == null)
            return true;

        if (!ApplicationProfileInstanceProgressRouteHelper.TryValidateProgressStep(progress, out errorMessage))
            return false;

        if (!ApplicationProfileInstanceProgressProfileResolver.TryValidateProjectContractForProgress(progress, objectSpace, out errorMessage))
            return false;

        var previous = GetPreviousProgress(progress.ApplicationProfileInstance, progress, objectSpace);
        if (previous == null)
        {
            if (!IsAllowedFirstState(progress.ApplicationProfileInstance, progress.State?.Code))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.FirstStepMustBeOfficePreparation");
                return false;
            }

            if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(progress.State?.Code)
                && !ApplicationProfileConfigurationResolver.HasMigrationSlaConfigured(progress.ApplicationProfileInstance))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.MigrationSlaProfileRequired");
                return false;
            }

            if (progress.State?.Code != null
                && string.Equals(progress.State.Code.Trim(), ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1), StringComparison.OrdinalIgnoreCase)
                && ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(progress.ApplicationProfileInstance)
                    == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
                && IsMinistryReviewSlaMissing(progress.ApplicationProfileInstance, objectSpace))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.MinistryReviewSlaRequired");
                return false;
            }

            return true;
        }

        if (IsTerminalStateCode(previous.State?.Code))
        {
            errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.CannotAdvanceFromTerminal");
            return false;
        }

        if (progress.Date.Date < previous.Date.Date)
        {
            errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.DateCannotBeBeforePrevious");
            return false;
        }

        var route = ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(progress.ApplicationProfileInstance);
        if (!route.HasValue)
            return true;

        var legCount = ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(progress.ApplicationProfileInstance);
        var fromStep = ProgressStep.Parse(previous);
        var toStep = ProgressStep.Parse(progress);

        if (IsTransitionAllowed(route.Value, legCount, fromStep, toStep))
        {
            if (progress.State?.Code != null
                && (string.Equals(progress.State.Code.Trim(), ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1), StringComparison.OrdinalIgnoreCase)
                    || (progress.State.Code.Trim().EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase)
                        && ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(progress.State.Code, out _)))
                && route.Value == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
                && IsMinistryReviewSlaMissing(progress.ApplicationProfileInstance, objectSpace))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.MinistryReviewSlaRequired");
                return false;
            }

            if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(progress.State?.Code)
                && !ApplicationProfileConfigurationResolver.HasMigrationSlaConfigured(progress.ApplicationProfileInstance))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.MigrationSlaProfileRequired");
                return false;
            }

            return true;
        }

        errorMessage = VisaUiMessages.Format(
            "ApplicationProfileInstanceProgress.InvalidTransition",
            FormatStep(fromStep),
            FormatStep(toStep));
        return false;
    }

    private static ApplicationProfileInstanceProgress? GetLatestProgress(
        ApplicationProfileInstance application,
        ApplicationProfileInstanceProgress? exclude,
        IObjectSpace? objectSpace) =>
        ApplicationProfileInstanceProgressHelper.GetLatest(
            application.ProgressHistory?.Where(p => p != exclude && (objectSpace == null || !objectSpace.IsObjectToDelete(p))),
            objectSpace);

    private static ApplicationProfileInstanceProgress? GetPreviousProgress(
        ApplicationProfileInstance application,
        ApplicationProfileInstanceProgress current,
        IObjectSpace? objectSpace)
    {
        var others = application.ProgressHistory?
            .Where(p => p != current && (objectSpace == null || !objectSpace.IsObjectToDelete(p)))
            .ToList();
        if (others == null || others.Count == 0)
            return null;

        var isNew = current.ID == Guid.Empty
            || (objectSpace != null && objectSpace.IsNewObject(current));
        if (isNew)
            return ApplicationProfileInstanceProgressHelper.GetLatest(others, objectSpace);

        return others
            .Where(p => p.Date < current.Date || (p.Date == current.Date && p.ID != Guid.Empty && current.ID != Guid.Empty && p.ID < current.ID))
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.ID)
            .FirstOrDefault();
    }

    private static bool IsMinistryReviewSlaMissing(
        ApplicationProfileInstance application,
        IObjectSpace? objectSpace)
    {
        if (ApplicationProfileConfigurationResolver.HasMinistrySlaConfigured(application))
            return false;

        if (objectSpace == null)
            return false;

        return !MinistryReviewSlaHelper.TryValidateConfigured(objectSpace, out _);
    }

    private static bool IsLegacyOfficePreparation(string? stateCode) =>
        string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> GetAllowedFirstStateCodes(ApplicationProfileInstance? application)
    {
        var route = ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
        {
            return
            [
                ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
                ApplicationProfileInstanceProgressStateCodes.ProcessCancelled
            ];
        }

        // Via ministries (or unknown): first explicit step is first-leg started (office is implied).
        return
        [
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
            ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1),
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled
        ];
    }

    private static bool IsAllowedFirstState(ApplicationProfileInstance? application, string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        return GetAllowedFirstStateCodes(application)
            .Contains(stateCode.Trim(), StringComparer.OrdinalIgnoreCase)
            // Historical seed rows may still exist.
            || IsLegacyOfficePreparation(stateCode);
    }

    private static bool IsTransitionAllowed(
        ApplicationProfileInstanceProgressRouteKind route,
        int ministryLegCount,
        ProgressStep from,
        ProgressStep to)
    {
        if (from.IsDefault || to.IsDefault)
            return false;

        if (IsLegacyOfficePreparation(from.StateCode))
        {
            if (route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
            {
                return string.Equals(to.StateCode, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(to.StateCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(to.StateCode, ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1), StringComparison.OrdinalIgnoreCase)
                || string.Equals(to.StateCode, ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1), StringComparison.OrdinalIgnoreCase)
                || string.Equals(to.StateCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase);
        }

        return GetTransitions(route, ministryLegCount).Any(t => StepsEqual(t.From, from) && StepsEqual(t.To, to));
    }

    private static IEnumerable<ProgressTransition> GetTransitions(
        ApplicationProfileInstanceProgressRouteKind route,
        int ministryLegCount)
    {
        var processStarted = Step(ApplicationProfileInstanceProgressStateCodes.ProcessStarted);
        var edges = new List<ProgressTransition>();

        if (route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
        {
            AddProcessOutcomes(edges, processStarted);
            AddCancellationFromActiveSteps(edges, processStarted);
            return edges;
        }

        var legCount = Math.Clamp(ministryLegCount, 1, ApplicationProfileInstanceProgressLegCodes.MaxLegCount);
        var started1 = Step(ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1));
        var approvedSteps = new List<ProgressStep>();

        for (var leg = 1; leg <= legCount; leg++)
        {
            var approved = Step(ApplicationProfileInstanceProgressLegCodes.ReviewApproved(leg));
            var rejected = Step(ApplicationProfileInstanceProgressLegCodes.ReviewRejected(leg));
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
        edges.Add(new ProgressTransition(processStarted, Step(ApplicationProfileInstanceProgressStateCodes.ProcessIssued)));
        edges.Add(new ProgressTransition(processStarted, Step(ApplicationProfileInstanceProgressStateCodes.ProcessRejected)));
    }

    private static void AddCancellationFromActiveSteps(List<ProgressTransition> edges, params ProgressStep[] fromSteps)
    {
        foreach (var from in fromSteps)
            edges.Add(new ProgressTransition(from, Step(ApplicationProfileInstanceProgressStateCodes.ProcessCancelled)));
    }

    private static ProgressStep Step(string stateCode) => new(stateCode);

    private static bool StepsEqual(ProgressStep a, ProgressStep b) =>
        string.Equals(a.StateCode, b.StateCode, StringComparison.OrdinalIgnoreCase);

    private static string FormatStep(ProgressStep step) => step.StateCode;
}