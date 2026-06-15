using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Legal <see cref="ApplicationProgress"/> transitions and canonical (state, location) pairs per route.
/// </summary>
public static class ApplicationProgressTransitionHelper
{
    private readonly record struct ProgressStep(string StateCode, string LocationCode)
    {
        public static ProgressStep Parse(ApplicationProgress? progress) =>
            progress?.State?.Code == null || progress.Location?.Code == null
                ? default
                : new ProgressStep(progress.State.Code, progress.Location.Code);

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

    public static bool IsCanonicalStateLocationPair(string? stateCode, string? locationCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode) || string.IsNullOrWhiteSpace(locationCode))
            return false;

        return GetCanonicalLocationCodesForState(stateCode)
            .Contains(locationCode.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> GetCanonicalLocationCodesForState(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return Array.Empty<string>();

        var trimmed = stateCode.Trim();
        if (string.Equals(trimmed, ApplicationProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase))
            return [ApplicationProgressLocationCodes.AtOffice];

        if (ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(trimmed, out var leg))
            return [ApplicationProgressLegCodes.AtMinistry(leg)];

        if (string.Equals(trimmed, ApplicationProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
            return [ApplicationProgressLocationCodes.AtMigrationService];

        if (string.Equals(trimmed, ApplicationProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
            return
            [
                ApplicationProgressLocationCodes.AtMigrationService,
                ApplicationProgressLocationCodes.AtOffice
            ];

        if (string.Equals(trimmed, ApplicationProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, ApplicationProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
        {
            var locations = new List<string>
            {
                ApplicationProgressLocationCodes.AtOffice,
                ApplicationProgressLocationCodes.AtMigrationService
            };
            locations.AddRange(ApplicationProgressLegCodes.GetMinistryLocationCodesUpToLegCount(ApplicationProgressLegCodes.MaxLegCount));
            return locations;
        }

        return Array.Empty<string>();
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
            return [ApplicationProgressStateCodes.IsBeingPrepared];

        if (IsTerminalStateCode(afterStep.State?.Code))
            return Array.Empty<string>();

        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (!route.HasValue)
            return ApplicationProgressRouteHelper.GetAllowedStateCodes(application);

        var legCount = ApplicationProgressProfileResolver.GetMinistryLegCount(application);
        var fromStep = ProgressStep.Parse(afterStep);
        var routeAllowed = ApplicationProgressRouteHelper.GetAllowedStateCodes(route.Value, legCount)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
            codes.Add(ApplicationProgressStateCodes.IsBeingPrepared);
            return codes.ToList();
        }

        if (IsTerminalStateCode(afterStep.State?.Code))
            return codes.ToList();

        foreach (var code in GetAllowedNextStateCodes(progress.Application, afterStep, progress))
            codes.Add(code);

        return codes.ToList();
    }

    /// <summary>
    /// Location codes allowed for the row's selected state (canonical ∩ route; keeps current value when editing).
    /// </summary>
    public static IReadOnlyList<string> GetAllowedLocationCodesForProgressRow(
        ApplicationProgress progress,
        IObjectSpace? objectSpace)
    {
        if (progress.Application == null)
            return Array.Empty<string>();

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(progress.Location?.Code))
            codes.Add(progress.Location.Code.Trim());

        if (string.IsNullOrWhiteSpace(progress.State?.Code))
            return codes.ToList();

        foreach (var code in GetAllowedLocationCodesForState(progress.Application, progress.State.Code))
            codes.Add(code);

        return codes.ToList();
    }

    public static IReadOnlyList<string> GetAllowedLocationCodesForState(
        Application? application,
        string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return Array.Empty<string>();

        var canonical = GetCanonicalLocationCodesForState(stateCode);
        if (application == null)
            return canonical;

        var routeAllowed = ApplicationProgressRouteHelper.GetAllowedLocationCodes(application)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filtered = canonical.Where(routeAllowed.Contains).ToList();
        return filtered.Count > 0 ? filtered : canonical;
    }

    public static (string StateCode, string LocationCode)? GetSuggestedNextStep(
        Application? application,
        ApplicationProgress? latestExcludingCurrent)
    {
        var nextStates = GetAllowedNextStateCodes(application, latestExcludingCurrent);
        if (nextStates.Count == 0)
            return null;

        var stateCode = nextStates[0];
        var locations = GetAllowedLocationCodesForState(application, stateCode);
        if (locations.Count == 0)
            return null;

        return (stateCode, locations[0]);
    }

    public static void TryApplySuggestedNextStep(ApplicationProgress progress)
    {
        if (progress.Application == null)
            return;

        if (progress.State == null)
        {
            var objectSpace = ObjectSpaceHelper.Get(progress.Application) ?? ObjectSpaceHelper.Get(progress);
            if (objectSpace == null)
                return;

            var afterStep = GetLatestProgress(progress.Application, progress, objectSpace);
            var suggested = GetSuggestedNextStep(progress.Application, afterStep);
            if (!suggested.HasValue)
                return;

            var state = FindStateByCode(objectSpace, suggested.Value.StateCode);
            var location = FindLocationByCode(objectSpace, suggested.Value.LocationCode);
            if (state == null || location == null)
                return;

            progress.State = state;
            progress.Location = location;
            return;
        }

        TryApplyDefaultLocationForState(progress);
    }

    /// <summary>
    /// When <see cref="ApplicationProgress.State"/> is set and only one location is legal, pre-fill <see cref="ApplicationProgress.Location"/>.
    /// </summary>
    public static void TryApplyDefaultLocationForState(ApplicationProgress progress)
    {
        if (progress.Application == null || progress.State?.Code == null || progress.Location != null)
            return;

        var objectSpace = ObjectSpaceHelper.Get(progress.Application) ?? ObjectSpaceHelper.Get(progress);
        if (objectSpace == null)
            return;

        var locationCodes = GetAllowedLocationCodesForState(progress.Application, progress.State.Code);
        if (locationCodes.Count != 1)
            return;

        var location = FindLocationByCode(objectSpace, locationCodes[0]);
        if (location != null)
            progress.Location = location;
    }

    private static ApplicationState? FindStateByCode(IObjectSpace objectSpace, string code) =>
        objectSpace.GetObjectsQuery<ApplicationState>()
            .FirstOrDefault(s => s.Code == code);

    private static ApplicationLocation? FindLocationByCode(IObjectSpace objectSpace, string code) =>
        objectSpace.GetObjectsQuery<ApplicationLocation>()
            .FirstOrDefault(l => l.Code == code);

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

        if (progress.State?.Code != null && progress.Location?.Code != null
            && !IsCanonicalStateLocationPair(progress.State.Code, progress.Location.Code))
        {
            errorMessage = VisaUiMessages.Format(
                "ApplicationProgress.InvalidStateLocationPair",
                progress.State.Code,
                progress.Location.Code);
            return false;
        }

        var previous = GetPreviousProgress(progress.Application, progress, objectSpace);
        if (previous == null)
        {
            if (!IsInitialOfficePreparation(progress))
            {
                errorMessage = VisaUiMessages.Get("ApplicationProgress.FirstStepMustBeOfficePreparation");
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
            if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(
                    progress.State?.Code,
                    progress.Location?.Code)
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

    private static bool IsInitialOfficePreparation(ApplicationProgress progress) =>
        string.Equals(progress.State?.Code, ApplicationProgressDefaults.InitialStateCode, StringComparison.OrdinalIgnoreCase)
        && string.Equals(progress.Location?.Code, ApplicationProgressDefaults.InitialLocationCode, StringComparison.OrdinalIgnoreCase);

    private static bool IsTransitionAllowed(
        ApplicationProgressRouteKind route,
        int ministryLegCount,
        ProgressStep from,
        ProgressStep to)
    {
        if (from.IsDefault || to.IsDefault)
            return false;

        return GetTransitions(route, ministryLegCount).Any(t => StepsEqual(t.From, from) && StepsEqual(t.To, to));
    }

    private static IEnumerable<ProgressTransition> GetTransitions(
        ApplicationProgressRouteKind route,
        int ministryLegCount)
    {
        var prep = Step(ApplicationProgressStateCodes.IsBeingPrepared, ApplicationProgressLocationCodes.AtOffice);
        var processStarted = Step(ApplicationProgressStateCodes.ProcessStarted, ApplicationProgressLocationCodes.AtMigrationService);
        var edges = new List<ProgressTransition>();

        if (route == ApplicationProgressRouteKind.DirectToMigrationService)
        {
            edges.Add(new ProgressTransition(prep, processStarted));
            AddProcessOutcomes(edges, processStarted);
            return edges;
        }

        var legCount = Math.Clamp(ministryLegCount, 1, ApplicationProgressLegCodes.MaxLegCount);
        var ministrySteps = new List<ProgressStep>();

        for (var leg = 1; leg <= legCount; leg++)
        {
            var review = Step(ApplicationProgressLegCodes.ReviewStarted(leg), ApplicationProgressLegCodes.AtMinistry(leg));
            var approved = Step(ApplicationProgressLegCodes.ReviewApproved(leg), ApplicationProgressLegCodes.AtMinistry(leg));
            var rejected = Step(ApplicationProgressLegCodes.ReviewRejected(leg), ApplicationProgressLegCodes.AtMinistry(leg));
            ministrySteps.Add(review);
            ministrySteps.Add(approved);

            if (leg == 1)
                edges.Add(new ProgressTransition(prep, review));
            else
                edges.Add(new ProgressTransition(ministrySteps[(leg - 2) * 2 + 1], review));

            edges.Add(new ProgressTransition(review, approved));
            edges.Add(new ProgressTransition(review, rejected));
        }

        var lastApproved = ministrySteps[^1];
        edges.Add(new ProgressTransition(lastApproved, processStarted));

        AddProcessOutcomes(edges, processStarted);
        var cancellationFrom = new List<ProgressStep> { prep, processStarted };
        cancellationFrom.AddRange(ministrySteps.Where((_, i) => i % 2 == 0));
        AddCancellationFromActiveSteps(edges, cancellationFrom.ToArray());

        return edges;
    }

    private static void AddProcessOutcomes(List<ProgressTransition> edges, ProgressStep processStarted)
    {
        foreach (var issuedLoc in GetCanonicalLocationCodesForState(ApplicationProgressStateCodes.ProcessIssued))
            edges.Add(new ProgressTransition(processStarted, Step(ApplicationProgressStateCodes.ProcessIssued, issuedLoc)));

        foreach (var loc in GetCanonicalLocationCodesForState(ApplicationProgressStateCodes.ProcessRejected))
            edges.Add(new ProgressTransition(processStarted, Step(ApplicationProgressStateCodes.ProcessRejected, loc)));
    }

    private static void AddCancellationFromActiveSteps(List<ProgressTransition> edges, params ProgressStep[] fromSteps)
    {
        foreach (var from in fromSteps)
        {
            foreach (var loc in GetCanonicalLocationCodesForState(ApplicationProgressStateCodes.ProcessCancelled))
                edges.Add(new ProgressTransition(from, Step(ApplicationProgressStateCodes.ProcessCancelled, loc)));
        }
    }

    private static ProgressStep Step(string stateCode, string locationCode) => new(stateCode, locationCode);

    private static bool StepsEqual(ProgressStep a, ProgressStep b) =>
        string.Equals(a.StateCode, b.StateCode, StringComparison.OrdinalIgnoreCase)
        && string.Equals(a.LocationCode, b.LocationCode, StringComparison.OrdinalIgnoreCase);

    private static string FormatStep(ProgressStep step) =>
        $"{step.StateCode} @ {step.LocationCode}";
}
