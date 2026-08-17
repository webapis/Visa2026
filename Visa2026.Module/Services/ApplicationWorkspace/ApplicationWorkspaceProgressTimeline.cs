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
        var advanceOptions = BuildAdvanceOptions(application, profile, latest, objectSpace);
        var canAdvance = advanceOptions.Count > 0;
        var advanceBlockedReason = canAdvance
            ? string.Empty
            : (ApplicationProfileInstanceProgressTransitionHelper.IsTerminalStateCode(latest?.State?.Code)
                ? "This application has reached a terminal progress state."
                : "No further progress steps are available for this route.");
        var currentSla = ResolveCurrentSla(application, profile, latest, ministrySla);
        var latestCode = latest?.State?.Code;
        var latestOnMigration = IsCurrentMigration(latestCode, history);
        var legs = ResolveApprovalLegs(application, profile);
        var latestLeg = ResolveCurrentMinistrySlot(latestCode, legs.Count, latestOnMigration);

        var steps = new List<ApplicationWorkspaceCaseProgressStep>
        {
            BuildOfficeStep(
                application,
                isCurrent: latest == null,
                currentSla,
                canAdvance,
                advanceBlockedReason,
                advanceOptions,
                history),
        };

        foreach (var leg in legs)
        {
            var row = LatestRowForLeg(history, leg.Sequence);
            var slotState = ResolveLegSlotState(latest == null, latestOnMigration, latestLeg, leg.Sequence, row);
            var isCurrent = slotState == "current";
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
        var migrationState = ResolveMigrationSlotState(latest == null, latestOnMigration, latestCode);
        var migrationCurrent = migrationState == "current";
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

        if (current.Key == OfficeKey || string.IsNullOrWhiteSpace(current.CurrentStateLabel))
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
        var fromProfile = liveProfile?.ApprovalLegs?
            .Where(l => l.ApprovingMinistry != null)
            .OrderBy(l => l.Sequence ?? int.MaxValue)
            .Select((l, i) => (
                Sequence: i + 1,
                Name: l.ApprovingMinistry!.ShortNameTm
                    ?? l.ApprovingMinistry.NameTm
                    ?? $"Ministry {i + 1}"))
            .ToList() ?? [];
        if (fromProfile.Count > 0)
            return fromProfile;

        return application.ApprovalLegSnapshots?
            .Where(s => !string.IsNullOrWhiteSpace(s.MinistryShortName))
            .OrderBy(s => s.Sequence ?? int.MaxValue)
            .Select((s, i) => (
                Sequence: i + 1,
                Name: s.MinistryShortName.Trim()))
            .ToList() ?? [];
    }

    internal static int? ResolveCurrentMinistrySlot(string? latestCode, int legCount, bool latestOnMigration)
    {
        if (latestOnMigration || legCount <= 0 || string.IsNullOrWhiteSpace(latestCode))
            return null;

        if (ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(latestCode, out var parsed))
            return Math.Clamp(parsed, 1, legCount);

        return IsMinistryTrackCode(latestCode) ? 1 : null;
    }

    internal static bool IsMinistryTrackCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        if (ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out _))
            return true;

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
        IReadOnlyList<ApplicationProfileInstanceProgress> history)
    {
        var date = application.ApplicationDate == default
            ? string.Empty
            : application.ApplicationDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

        return new ApplicationWorkspaceCaseProgressStep
        {
            Key = OfficeKey,
            Label = OfficeLabel,
            Date = date,
            State = isCurrent ? "current" : "done",
            CurrentStateLabel = isCurrent ? OfficeLabel : string.Empty,
            SlaTargetDate = isCurrent ? FormatSlaTarget(application.ApplicationDate, currentSla) : string.Empty,
            SlaDaysRemaining = isCurrent ? DaysLeft(currentSla) : null,
            OfficerNotes = isCurrent ? application.OfficePreparationNotes ?? string.Empty : string.Empty,
            CanAdvance = isCurrent && canAdvance,
            CanRevert = false,
            CanRevertToHere = !isCurrent && history.Count > 0,
            AdvanceBlockedReason = isCurrent ? advanceBlockedReason : string.Empty,
            AdvanceOptions = isCurrent ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
            OutcomeKind = isCurrent ? "current" : "ok",
        };
    }

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
            ShowMinistryLetterUpload = isCurrent && row?.IsMinistryDecisionStep == true,
            CanAdvance = isCurrent && canAdvance,
            CanRevert = CanRevertLast(latest, key),
            CanRevertToHere = slotState == "done"
                && ApplicationProfileInstanceProgressRevertHelper.RowsToDelete(history, key).Count > 0,
            AdvanceBlockedReason = isCurrent ? advanceBlockedReason : string.Empty,
            AdvanceOptions = isCurrent ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
            OutcomeKind = ResolveOutcomeKind(slotState, row?.State?.Code),
        };
    }

    private static bool CanRevertLast(ApplicationProfileInstanceProgress? latest, string stepKey)
    {
        if (latest == null || string.IsNullOrWhiteSpace(stepKey))
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

    private static string ResolveMigrationSlotState(bool atOffice, bool latestOnMigration, string? latestCode)
    {
        if (atOffice || !latestOnMigration)
            return "pending";

        if (string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return "done";

        return "current";
    }

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
        ApplicationProfile? profile,
        ApplicationProfileInstanceProgress? latest,
        IObjectSpace? objectSpace)
    {
        var codes = ApplicationProfileInstanceProgressTransitionHelper.GetAllowedNextStateCodes(application, latest);
        if (codes.Count == 0)
            return Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();

        return codes
            .Where(code => IsIncludedNextState(profile ?? application.ApplicationProfile, code))
            .Select(code => new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = code,
                Label = ResolveStateLabel(objectSpace, code),
            })
            .ToList();
    }

    private static bool IsIncludedNextState(ApplicationProfile? profile, string stateCode)
    {
        var settings = profile?.ProgressStateSettings;
        if (settings == null || settings.Count == 0)
            return true;

        var catalogCode = MapToCatalogStateCode(stateCode);
        if (string.IsNullOrEmpty(catalogCode))
            return true;

        var matches = settings
            .Where(s => string.Equals(s.StateCode, catalogCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0)
            return true;

        return matches.Any(s => s.IsIncluded);
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

        return anchor.Date.AddDays(maxDays).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    }
}