using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Case workspace progress line: implied office, predetermined approval legs,
/// then migration — filled from append-only history when rows exist.
/// </summary>
internal static class ApplicationWorkspaceProgressTimeline
{
    public const string OfficeKey = "office";
    public const string OfficeLabel = "Office preparation";
    public const string MigrationKey = "migration";
    public const string MigrationLabel = "Migration service";

    public static IReadOnlyList<ApplicationWorkspaceCaseProgressStep> Build(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        ApplicationProfileInstanceProgressSlaResult ministrySla,
        IObjectSpace? objectSpace)
    {
        var history = application.ProgressHistory?
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Date)
            .ThenBy(p => p.ID)
            .ToList() ?? [];
        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        var slotAnchor = SlotAnchorForCurrent(latest, history);
        var advanceOptions = BuildAdvanceOptions(application, latest, objectSpace);
        var canAdvance = advanceOptions.Count > 0;
        var advanceBlockedReason = canAdvance
            ? string.Empty
            : (ApplicationProfileInstanceProgressTransitionHelper.IsTerminalStateCode(latest?.State?.Code)
                ? "This application has reached a terminal progress state."
                : "No further progress steps are available for this route.");
        var currentSla = ResolveCurrentSla(application, profile, latest, ministrySla);
        var latestCode = latest?.State?.Code;
        var slotCode = slotAnchor?.State?.Code;
        var latestOnMigration = IsCurrentMigration(slotCode, history);
        var legs = ResolveApprovalLegs(application, profile);
        var latestLeg = ResolveCurrentMinistrySlot(slotCode, legs.Count, latestOnMigration);
        var awaitingMigration = !latestOnMigration
            && IsLastMinistryApproved(slotCode, legs.Count);
        var atOffice = slotAnchor == null;
        var cancelledOnCurrent = IsProcessCancelled(latestCode);

        var steps = new List<ApplicationWorkspaceCaseProgressStep>
        {
            BuildOfficeStep(
                application,
                isCurrent: atOffice,
                currentSla,
                canAdvance,
                advanceBlockedReason,
                advanceOptions,
                history,
                latest),
        };

        foreach (var leg in legs)
        {
            var row = LatestRowForLeg(history, leg.Sequence);
            var slotState = ResolveLegSlotState(atOffice, latestOnMigration, latestLeg, leg.Sequence, row);
            var isCurrent = slotState == "current";
            if (cancelledOnCurrent && isCurrent)
                row = latest;
            var letterRow = FindLetterRow(history, leg.Sequence);
            steps.Add(BuildFilledStep(
                $"leg-{leg.Sequence}",
                leg.Name,
                row,
                slotState,
                isCurrent ? currentSla : default,
                isCurrent && canAdvance,
                isCurrent ? advanceBlockedReason : string.Empty,
                isCurrent ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
                letterRow,
                history,
                latest));
        }

        var migrationRow = LatestMigrationRow(history);
        var migrationState = ResolveMigrationSlotState(atOffice, latestOnMigration, slotCode, awaitingMigration);
        var migrationCurrent = migrationState == "current";
        if (cancelledOnCurrent && migrationCurrent)
            migrationRow = latest;
        steps.Add(BuildFilledStep(
            MigrationKey,
            MigrationLabel,
            migrationRow,
            migrationState,
            migrationCurrent ? currentSla : default,
            migrationCurrent && canAdvance,
            migrationCurrent ? advanceBlockedReason : string.Empty,
            migrationCurrent ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
            letterRow: null,
            history,
            latest));

        return steps;
    }

    internal static string FormatChromeCurrentStep(IReadOnlyList<ApplicationWorkspaceCaseProgressStep> steps)
    {
        var current = steps.FirstOrDefault(s => s.State == "current");
        if (current == null)
        {
            var lastDone = steps.LastOrDefault(s => s.State == "done");
            if (lastDone == null)
                return OfficeLabel;

            return string.IsNullOrWhiteSpace(lastDone.CurrentStateLabel)
                ? lastDone.Label
                : $"{lastDone.Label} · {lastDone.CurrentStateLabel}";
        }

        if (string.IsNullOrWhiteSpace(current.CurrentStateLabel)
            || string.Equals(current.CurrentStateLabel, current.Label, StringComparison.OrdinalIgnoreCase))
            return current.Label;

        return $"{current.Label} · {current.CurrentStateLabel}";
    }

    internal static string FormatProfileStateLabel(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return string.Empty;

        var catalogCode = MapToCatalogStateCode(stateCode) ?? stateCode.Trim();
        var catalog = ApplicationProfileProgressStateCatalog.All
            .FirstOrDefault(r => string.Equals(r.StateCode, catalogCode, StringComparison.OrdinalIgnoreCase));
        return catalog?.DisplayName ?? stateCode.Trim();
    }

    internal static ApplicationProfileInstanceProgressSlaResult ResolveCurrentSla(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        ApplicationProfileInstanceProgress? latest,
        ApplicationProfileInstanceProgressSlaResult ministrySla)
    {
        if (latest != null
            && ministrySla.MaxDaysInReview is > 0
            && ministrySla.WorkingDaysInCurrentStep is int)
            return ministrySla;

        if (latest != null)
        {
            var migrationSla = ApplicationMigrationSlaHelper.Resolve(application, latest);
            if (migrationSla.MaxDaysInReview is > 0 && migrationSla.WorkingDaysInCurrentStep is int)
                return migrationSla;
        }

        var maxDays = ResolveProfileSlaDays(application, profile);
        if (maxDays <= 0)
            return default;

        var anchor = latest?.Date is { } date && date != default
            ? date
            : application.ApplicationDate;
        if (anchor == default)
            return default;

        var elapsed = WorkingDaysHelper.CountWorkingDaysInclusive(anchor, DateTime.Today);
        return new ApplicationProfileInstanceProgressSlaResult(
            ApplicationProfileInstanceProgressSlaStatus.Ok,
            elapsed,
            maxDays,
            null,
            null);
    }

    internal static int ResolveProfileSlaDays(ApplicationProfileInstance application, ApplicationProfile? profile)
    {
        var route = ApplicationProfileConfigurationResolver.GetProgressRoute(application);
        if (route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
        {
            if (profile is { MigrationSlaDays: > 0 })
                return profile.MigrationSlaDays;
            return ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays(application);
        }

        if (profile is { MinistrySlaDays: > 0 })
            return profile.MinistrySlaDays;
        return ApplicationProfileConfigurationResolver.GetMinistrySlaMaxDays(application);
    }

    internal static IReadOnlyList<(int Sequence, string Name)> ResolveApprovalLegs(
        ApplicationProfileInstance application,
        ApplicationProfile? profile)
    {
        var route = ApplicationProfileConfigurationResolver.GetProgressRoute(application);
        if (route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
            return [];

        var liveProfile = profile ?? application.ApplicationProfile;
        return ApplicationProfileApprovalLegVersionHelper.ResolveLegsForInstance(application, liveProfile);
    }

    private static ApplicationProfileInstanceProgress? SlotAnchorForCurrent(
        ApplicationProfileInstanceProgress? latest,
        IReadOnlyList<ApplicationProfileInstanceProgress> history)
    {
        if (!IsProcessCancelled(latest?.State?.Code) || history.Count == 0)
            return latest;

        var index = history.ToList().FindIndex(p => ReferenceEquals(p, latest));
        if (index < 0)
            index = history.Count - 1;

        return index > 0 ? history[index - 1] : null;
    }

    private static bool IsProcessCancelled(string? stateCode) =>
        !string.IsNullOrWhiteSpace(stateCode)
        && string.Equals(
            stateCode.Trim(),
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            StringComparison.OrdinalIgnoreCase);

    internal static int? ResolveCurrentMinistrySlot(string? latestCode, int legCount, bool latestOnMigration)
    {
        if (latestOnMigration || legCount <= 0 || string.IsNullOrWhiteSpace(latestCode))
            return null;

        if (ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(latestCode, out var parsed))
        {
            parsed = Math.Clamp(parsed, 1, legCount);
            if (IsMinistryApproved(latestCode) && parsed < legCount)
                return parsed + 1;
            if (IsMinistryApproved(latestCode) && parsed >= legCount)
                return null;

            return parsed;
        }

        return IsMinistryTrackCode(latestCode) ? 1 : null;
    }

    internal static bool IsMinistryTrackCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        if (ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out _))
            return true;

        if (string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stateCode, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
            return false;

        var catalogCode = MapToCatalogStateCode(stateCode) ?? stateCode.Trim();
        return ApplicationProfileProgressStateCatalog.All.Any(r =>
            r.Track == ApplicationProfileProgressStateTrack.Ministry
            && string.Equals(r.StateCode, catalogCode, StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationWorkspaceCaseProgressStep BuildOfficeStep(
        ApplicationProfileInstance application,
        bool isCurrent,
        ApplicationProfileInstanceProgressSlaResult currentSla,
        bool canAdvance,
        string advanceBlockedReason,
        IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> advanceOptions,
        IReadOnlyList<ApplicationProfileInstanceProgress> history,
        ApplicationProfileInstanceProgress? latest)
    {
        var cancelled = isCurrent && IsProcessCancelled(latest?.State?.Code);
        var submitted = cancelled ? null : FindSubmittedRow(history);
        var date = cancelled && latest?.Date is { } cancelledDate && cancelledDate != default
            ? FormatStepDate(cancelledDate)
            : submitted?.Date is { } submittedDate && submittedDate != default
                ? FormatStepDate(submittedDate)
                : application.ApplicationDate == default
                    ? string.Empty
                    : FormatStepDate(application.ApplicationDate);
        var stateLabel = cancelled
            ? FormatProfileStateLabel(latest?.State?.Code)
            : submitted != null
                ? FormatProfileStateLabel(submitted.State?.Code)
                : isCurrent ? OfficeLabel : string.Empty;

        return new ApplicationWorkspaceCaseProgressStep
        {
            Key = OfficeKey,
            Label = OfficeLabel,
            Date = date,
            State = isCurrent ? "current" : "done",
            CurrentStateLabel = stateLabel,
            SlaTargetDate = isCurrent ? FormatSlaTarget(application.ApplicationDate, currentSla) : string.Empty,
            SlaDaysRemaining = isCurrent ? DaysLeft(currentSla) : null,
            OfficerNotes = isCurrent ? application.OfficePreparationNotes ?? string.Empty : string.Empty,
            CanAdvance = isCurrent && canAdvance,
            CanRevert = cancelled,
            CanRevertToHere = !isCurrent && history.Count > 0,
            AdvanceBlockedReason = isCurrent ? advanceBlockedReason : string.Empty,
            AdvanceOptions = isCurrent ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
            ResultOptions = isCurrent
                ? ApplicationWorkspaceProgressAdvancePreview.ResultOptions(OfficeKey, advanceOptions)
                : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
            OutcomeKind = cancelled ? "cancelled" : (isCurrent ? "current" : "ok"),
        };
    }

    private static ApplicationProfileInstanceProgress? FindSubmittedRow(
        IReadOnlyList<ApplicationProfileInstanceProgress> history) =>
        history.FirstOrDefault(row =>
            ApplicationWorkspaceProgressAdvancePreview.IsOfficeSubmitted("office", row.State?.Code));

    private static string FormatStepDate(DateTime date) =>
        date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    private static ApplicationWorkspaceCaseProgressStep BuildFilledStep(
        string key,
        string label,
        ApplicationProfileInstanceProgress? row,
        string slotState,
        ApplicationProfileInstanceProgressSlaResult currentSla,
        bool canAdvance,
        string advanceBlockedReason,
        IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> advanceOptions,
        ApplicationProfileInstanceProgress? letterRow,
        IReadOnlyList<ApplicationProfileInstanceProgress> history,
        ApplicationProfileInstanceProgress? latest)
    {
        var isCurrent = slotState == "current";
        var stateLabel = row == null
            ? string.Empty
            : FormatProfileStateLabel(row.State?.Code);
        var letter = letterRow ?? (string.IsNullOrWhiteSpace(row?.MinistryLetterFileName) ? null : row);
        var letterId = letter != null && letter.ID != Guid.Empty
            ? letter.ID
            : (row != null && row.ID != Guid.Empty ? row.ID : (Guid?)null);

        var resultOptions = isCurrent
            ? ApplicationWorkspaceProgressAdvancePreview.ResultOptions(key, advanceOptions)
            : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();
        var decisionRow = row?.IsMinistryDecisionStep == true ? row : null;

        return new ApplicationWorkspaceCaseProgressStep
        {
            Key = key,
            Label = label,
            Date = row?.Date is { } date && date != default
                ? date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
                : string.Empty,
            State = slotState,
            CurrentStateLabel = stateLabel,
            SlaTargetDate = isCurrent && row != null ? FormatSlaTarget(row.Date, currentSla) : string.Empty,
            SlaDaysRemaining = isCurrent ? DaysLeft(currentSla) : null,
            ProgressId = letterId,
            OfficerNotes = isCurrent ? row?.Description ?? string.Empty : string.Empty,
            MinistryLetterFileName = letter?.MinistryLetterFileName ?? string.Empty,
            ShowMinistryLetterUpload = isCurrent
                && key.StartsWith("leg-", StringComparison.OrdinalIgnoreCase)
                && resultOptions.Count > 0,
            DecisionProgressId = decisionRow != null && decisionRow.ID != Guid.Empty ? decisionRow.ID : null,
            CanAdvance = isCurrent && canAdvance,
            CanRevert = CanRevertLast(latest, key) || (isCurrent && latest != null),
            CanRevertToHere = slotState == "done"
                && ApplicationProfileInstanceProgressRevertHelper.RowsToDelete(history, key).Count > 0,
            AdvanceBlockedReason = isCurrent ? advanceBlockedReason : string.Empty,
            AdvanceOptions = isCurrent ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
            ResultOptions = resultOptions,
            OutcomeKind = ResolveOutcomeKind(slotState, row?.State?.Code),
        };
    }

    private static bool CanRevertLast(ApplicationProfileInstanceProgress? latest, string stepKey)
    {
        if (latest == null || string.IsNullOrWhiteSpace(stepKey))
            return false;

        if (IsProcessCancelled(latest.State?.Code))
            return false;

        return string.Equals(
            ApplicationProfileInstanceProgressRevertHelper.SlotKeyFor(latest),
            stepKey,
            StringComparison.OrdinalIgnoreCase);
    }

    internal static string ResolveOutcomeKind(string slotState, string? stateCode)
    {
        if (!string.IsNullOrWhiteSpace(stateCode))
        {
            var trimmed = stateCode.Trim();
            var catalog = MapToCatalogStateCode(trimmed) ?? trimmed;
            if (trimmed.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(catalog, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase))
            {
                return "rejected";
            }

            if (string.Equals(catalog, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
                return "cancelled";

            if (string.Equals(catalog, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
                return "issued";
        }

        if (string.Equals(slotState, "pending", StringComparison.OrdinalIgnoreCase))
            return "pending";
        if (string.Equals(slotState, "current", StringComparison.OrdinalIgnoreCase))
            return "current";
        return "ok";
    }

    private static string ResolveLegSlotState(
        bool atOffice,
        bool latestOnMigration,
        int? latestLeg,
        int sequence,
        ApplicationProfileInstanceProgress? row)
    {
        if (atOffice)
            return "pending";
        if (latestOnMigration)
            return row == null ? "pending" : "done";
        if (latestLeg is int currentLeg)
        {
            if (sequence < currentLeg)
                return "done";
            if (sequence == currentLeg)
                return "current";
            return "pending";
        }

        return row == null ? "pending" : "done";
    }

    private static string ResolveMigrationSlotState(
        bool atOffice,
        bool latestOnMigration,
        string? latestCode,
        bool awaitingMigration)
    {
        if (atOffice)
            return "pending";
        if (awaitingMigration)
            return "current";
        if (!latestOnMigration)
            return "pending";

        if (string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return "done";

        return "current";
    }

    private static bool IsLastMinistryApproved(string? latestCode, int legCount)
    {
        if (legCount <= 0 || !IsMinistryApproved(latestCode))
            return false;

        return ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(latestCode, out var parsed)
            && Math.Clamp(parsed, 1, ApplicationProfileInstanceProgressLegCodes.MaxLegCount) >= legCount;
    }

    private static bool IsMinistryApproved(string? stateCode) =>
        !string.IsNullOrWhiteSpace(stateCode)
        && stateCode.EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase);

    private static bool IsCurrentMigration(string? latestCode, IReadOnlyList<ApplicationProfileInstanceProgress> history)
    {
        if (string.IsNullOrWhiteSpace(latestCode))
            return false;

        if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(latestCode)
            || string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || latestCode.StartsWith("MIGRATION_", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return false;

        return history.Any(p => ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(p.State?.Code));
    }

    private static ApplicationProfileInstanceProgress? LatestRowForLeg(
        IReadOnlyList<ApplicationProfileInstanceProgress> history,
        int sequence) =>
        history.LastOrDefault(p => MatchesMinistryDisplayLeg(p.State?.Code, sequence));

    private static ApplicationProfileInstanceProgress? FindLetterRow(
        IReadOnlyList<ApplicationProfileInstanceProgress> history,
        int sequence) =>
        history.LastOrDefault(p =>
            MatchesMinistryDisplayLeg(p.State?.Code, sequence)
            && !string.IsNullOrWhiteSpace(p.MinistryLetterFileName));

    private static bool MatchesMinistryDisplayLeg(string? stateCode, int sequence)
    {
        if (!ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out var leg))
            return false;

        return Math.Clamp(leg, 1, ApplicationProfileInstanceProgressLegCodes.MaxLegCount) == sequence;
    }

    private static ApplicationProfileInstanceProgress? LatestMigrationRow(
        IReadOnlyList<ApplicationProfileInstanceProgress> history) =>
        history.LastOrDefault(p => IsCurrentMigration(p.State?.Code, history));

    private static IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> BuildAdvanceOptions(
        ApplicationProfileInstance application,
        ApplicationProfileInstanceProgress? latest,
        IObjectSpace? objectSpace)
    {
        var codes = ApplicationProfileInstanceProgressTransitionHelper.GetAllowedNextStateCodes(application, latest);
        if (codes.Count == 0)
            return Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();

        return codes
            .Select(code => new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = code,
                Label = ResolveStateLabel(objectSpace, code),
            })
            .ToList();
    }

    private static string? MapToCatalogStateCode(string stateCode)
    {
        if (ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out _))
        {
            if (ApplicationProfileInstanceProgressLegCodes.IsMinistryReviewStartedStateCode(stateCode))
                return ApplicationProfileInstanceProgressStateCodes.Review1Started;
            if (stateCode.EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase))
                return ApplicationProfileInstanceProgressStateCodes.Review1Approved;
            if (stateCode.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase))
                return ApplicationProfileInstanceProgressStateCodes.Review1Rejected;
        }

        return stateCode;
    }

    private static string ResolveStateLabel(IObjectSpace? objectSpace, string stateCode)
    {
        var catalog = ApplicationProfileProgressStateCatalog.All
            .FirstOrDefault(r => string.Equals(r.StateCode, MapToCatalogStateCode(stateCode) ?? stateCode, StringComparison.OrdinalIgnoreCase));
        if (catalog != null)
            return catalog.DisplayName;

        if (objectSpace != null)
        {
            var state = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(s => s.Code == stateCode);
            if (state != null)
            {
                return state.LocalizedDisplayName
                    ?? state.NameTm
                    ?? state.Code
                    ?? stateCode;
            }
        }

        return stateCode;
    }

    private static int? DaysLeft(ApplicationProfileInstanceProgressSlaResult sla)
    {
        if (sla.MaxDaysInReview is not int maxDays || sla.WorkingDaysInCurrentStep is not int elapsed)
            return null;

        return Math.Max(0, maxDays - elapsed);
    }

    private static string FormatSlaTarget(DateTime anchor, ApplicationProfileInstanceProgressSlaResult sla)
    {
        if (anchor == default || sla.MaxDaysInReview is not int maxDays || maxDays <= 0)
            return string.Empty;

        return WorkingDaysHelper.AddWorkingDaysInclusive(anchor, maxDays)
            .ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }
}