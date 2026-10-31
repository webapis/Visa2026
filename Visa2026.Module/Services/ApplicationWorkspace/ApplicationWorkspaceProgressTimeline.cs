using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
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
    public const string MigrationLabel = "At the migration service";
    public const string AwaitingAsCoNumberLabel = "Awaiting AS (CO) number";
    public const string AsNumberReceivedLabel = "AS number received";
    public const string IssuedLabel = "Issued";

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
        return BuildCore(application, profile, ministrySla, objectSpace, history, includeAdvance: true);
    }

    /// <summary>
    /// ListView stepper: same slots as the workspace, without advance/SLA/letter work.
    /// </summary>
    internal static IReadOnlyList<ApplicationWorkspaceCaseProgressStep> BuildDisplay(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationProfileInstanceProgress> history)
    {
        var ordered = history?
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Date)
            .ThenBy(p => p.ID)
            .ToList() ?? [];
        return BuildCore(application, profile, default, objectSpace: null, ordered, includeAdvance: false);
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseProgressStep> BuildCore(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        ApplicationProfileInstanceProgressSlaResult ministrySla,
        IObjectSpace? objectSpace,
        IReadOnlyList<ApplicationProfileInstanceProgress> history,
        bool includeAdvance)
    {
        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(history, objectSpace)
            ?? application.LatestProgress;
        var slotAnchor = SlotAnchorForCurrent(latest, history);
        var advanceOptions = includeAdvance
            ? BuildAdvanceOptions(application, latest, objectSpace)
            : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();
        var canAdvance = includeAdvance && advanceOptions.Count > 0;
        var advanceBlockedReason = !includeAdvance
            ? string.Empty
            : canAdvance
                ? string.Empty
                : (ApplicationProfileInstanceProgressTransitionHelper.IsTerminalStateCode(latest?.State?.Code, application)
                    ? "This application has reached a terminal progress state."
                    : "No further progress steps are available for this route.");
        var currentSla = includeAdvance
            ? ResolveCurrentSla(application, profile, latest, ministrySla)
            : default;
        var latestCode = latest?.State?.Code;
        var slotCode = slotAnchor?.State?.Code;
        var latestOnMigration = IsCurrentMigration(slotCode, history);
        var legs = ResolveApprovalLegs(application, profile);
        var latestLeg = ResolveCurrentMinistrySlot(slotCode, legs.Count, latestOnMigration);
        var submissionRecorded = ApplicationMigrationSubmission.IsState(slotCode);
        var route = ApplicationProfileConfigurationResolver.GetProgressRoute(application);
        var isDirect = route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService;
        var liveProfile = profile ?? application.ApplicationProfile;
        var directEndsAtSubmission = ApplicationProfileConfigurationResolver.DirectEndsAtMigrationSubmission(application)
            || (isDirect && ApplicationProfileConfigurationResolver.DirectEndsAtMigrationSubmission(liveProfile));
        var awaitingSubmission = !latestOnMigration
            && !submissionRecorded
            && (
                (legs.Count > 0 && IsLastMinistryApproved(slotCode, legs.Count))
                || (isDirect && IsDirectLeftOfficeAwaitingSubmission(slotCode)));
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

        // Via ministry: after last approval. Direct: after Office (same timeline label).
        if (legs.Count > 0 || isDirect)
        {
            var submissionRow = LatestSubmissionRow(history);
            var submissionState = ResolveSubmissionSlotState(
                atOffice,
                latestOnMigration,
                awaitingSubmission,
                submissionRecorded,
                cancelledOnCurrent);
            var submissionMissing = !ApplicationMigrationSubmission.IsComplete(submissionRow);
            var submissionCurrent = submissionState == "current";
            // Via ministry requires document number; Direct only Date + optional file.
            var showSubmissionFields = submissionCurrent
                || (!isDirect && submissionState == "done" && submissionMissing);
            var submissionAdvance = submissionCurrent
                ? FilterDirectHandInAdvanceOptions(advanceOptions, directEndsAtSubmission)
                : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();
            steps.Add(BuildFilledStep(
                ApplicationMigrationSubmission.TimelineKey,
                ApplicationMigrationSubmission.TimelineLabel,
                submissionRow,
                submissionState,
                submissionCurrent ? currentSla : default,
                submissionCurrent && canAdvance && submissionAdvance.Count > 0,
                submissionCurrent ? advanceBlockedReason : string.Empty,
                submissionAdvance,
                letterRow: null,
                history,
                latest,
                showMigrationSubmission: showSubmissionFields,
                requireSubmissionDocumentNumber: !isDirect && showSubmissionFields,
                submissionDocumentNumber: submissionRow?.SubmissionDocumentNumber?.Trim() ?? string.Empty,
                submissionDocumentFileName: submissionRow?.SubmissionDocumentFileName ?? string.Empty,
                // Direct hand-in: Advance writes SUBMITTED_MIGRATION (no Result picker).
                suppressResultDropdown: isDirect && submissionCurrent));
        }

        // Direct Registration/Cancellation/Business trip ends at hand-in — no AS/Issued node.
        if (!directEndsAtSubmission)
        {
            var migrationRow = LatestMigrationRow(history);
            var migrationState = ResolveMigrationSlotState(
                atOffice,
                latestOnMigration,
                slotCode,
                submissionRecorded,
                cancelledOnCurrent);
            var migrationCurrent = migrationState == "current";
            if (cancelledOnCurrent && migrationCurrent)
                migrationRow = latest;
            var migrationLabel = isDirect
                ? ApplicationProfileConfigurationResolver.DirectFinalTimelineLabel(liveProfile)
                : AsNumberReceivedLabel;
            var migrationStatusLabel = isDirect
                ? ResolveDirectMigrationStatusLabel(migrationRow, migrationState)
                : ResolveMigrationStatusLabel(migrationRow, migrationState);
            // Issuance/Change after hand-in: auto Issued (no Result). Legacy PROCESS_STARTED keeps Results.
            var directFinalAdvance = isDirect
                && migrationCurrent
                && canAdvance
                && ApplicationMigrationSubmission.IsState(slotCode);
            steps.Add(BuildFilledStep(
                MigrationKey,
                migrationLabel,
                migrationRow,
                migrationState,
                migrationCurrent ? currentSla : default,
                migrationCurrent && canAdvance,
                migrationCurrent ? advanceBlockedReason : string.Empty,
                migrationCurrent ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
                letterRow: null,
                history,
                latest,
                stateLabelOverride: migrationStatusLabel,
                processNumber: ResolveMigrationProcessNumber(application, history, migrationRow, migrationState),
                suppressResultDropdown: directFinalAdvance,
                requireProcessNumberOnAdvance: directFinalAdvance,
                showOptionalOutcomeFiles: directFinalAdvance));
        }

        ApplyDateBounds(steps);
        return steps;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> FilterDirectHandInAdvanceOptions(
        IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> options,
        bool directEndsAtSubmission)
    {
        if (!directEndsAtSubmission || options.Count == 0)
            return options;

        // Final hand-in for non–Issuance/Change: no Cancelled.
        return options
            .Where(o => o.StateCode == null
                || !o.StateCode.Contains("CANCELLED", StringComparison.OrdinalIgnoreCase))
            .ToList();
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
        var fileRow = cancelled ? latest : submitted;
        var officeFiles = ApplicationProfileInstanceProgressFiles.List(
            fileRow,
            ApplicationProfileInstanceProgressFiles.MinistryLetter);
        var officeFileName = officeFiles.Count > 0
            ? officeFiles[0].FileName
            : string.IsNullOrWhiteSpace(fileRow?.MinistryLetterFileName)
                ? string.Empty
                : fileRow.MinistryLetterFileName;
        var officeDateRow = cancelled ? latest : submitted;
        var officeDateId = officeDateRow != null && officeDateRow.ID != Guid.Empty
            ? officeDateRow.ID
            : (Guid?)null;
        var officeCanCorrect = CanCorrectStoredDate(
            isCurrent ? "current" : "done",
            isCurrent && canAdvance,
            officeDateId);

        return new ApplicationWorkspaceCaseProgressStep
        {
            Key = OfficeKey,
            Label = OfficeLabel,
            Date = date,
            DateValue = officeDateRow?.Date is { } officeDate && officeDate != default ? officeDate : null,
            DateProgressId = officeDateId,
            CanCorrectDate = officeCanCorrect,
            CanEditFiles = officeCanCorrect,
            Files = officeFiles,
            FileSlot = ApplicationProfileInstanceProgressFiles.MinistryLetter,
            State = isCurrent ? "current" : "done",
            CurrentStateLabel = stateLabel,
            SlaTargetDate = isCurrent ? FormatSlaTarget(application.ApplicationDate, currentSla) : string.Empty,
            SlaDaysRemaining = isCurrent ? DaysLeft(currentSla) : null,
            ProgressId = officeDateId,
            OfficerNotes = isCurrent ? application.OfficePreparationNotes ?? string.Empty : string.Empty,
            MinistryLetterFileName = officeFileName,
            ShowMinistryLetterUpload = isCurrent && (canAdvance || cancelled),
            DecisionProgressId = cancelled && latest?.ID is Guid cancelledId && cancelledId != Guid.Empty
                ? cancelledId
                : null,
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

    private static bool CanCorrectStoredDate(string slotState, bool canAdvance, Guid? progressId)
    {
        if (progressId is not Guid id || id == Guid.Empty)
            return false;
        if (string.Equals(slotState, "pending", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.Equals(slotState, "current", StringComparison.OrdinalIgnoreCase) && canAdvance)
            return false;
        return true;
    }

    private static void ApplyDateBounds(List<ApplicationWorkspaceCaseProgressStep> steps)
    {
        var seen = new HashSet<Guid>();
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].DateProgressId is Guid progressId && !seen.Add(progressId))
            {
                steps[i].CanCorrectDate = false;
                steps[i].CanEditFiles = false;
                steps[i].CanEditSubmissionNumber = false;
                steps[i].CanEditResultNumber = false;
            }

            if (!steps[i].CanCorrectDate)
            {
                steps[i].CanEditFiles = false;
                steps[i].CanEditSubmissionNumber = false;
                steps[i].CanEditResultNumber = false;
                continue;
            }

            // Every date-correctable step (office, ministry, submission, issued, …) can edit its files.
            steps[i].CanEditFiles = true;

            DateTime? previous = null;
            for (var j = i - 1; j >= 0; j--)
            {
                if (steps[j].DateValue is DateTime earlier)
                {
                    previous = earlier;
                    break;
                }
            }

            DateTime? next = null;
            for (var j = i + 1; j < steps.Count; j++)
            {
                if (steps[j].DateValue is DateTime later)
                {
                    next = later;
                    break;
                }
            }

            steps[i].EarliestDate = previous;
            steps[i].LatestDate = next;
        }
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
        ApplicationProfileInstanceProgress? latest,
        bool showMigrationSubmission = false,
        bool requireSubmissionDocumentNumber = false,
        string submissionDocumentNumber = "",
        string submissionDocumentFileName = "",
        string? stateLabelOverride = null,
        string? processNumber = null,
        bool suppressResultDropdown = false,
        bool requireProcessNumberOnAdvance = false,
        bool showOptionalOutcomeFiles = false)
    {
        var isCurrent = slotState == "current";
        var stateLabel = stateLabelOverride
            ?? (row == null
                ? string.Empty
                : FormatProfileStateLabel(row.State?.Code));
        var letter = letterRow
            ?? (row?.IsMinistryDecisionStep == true && !string.IsNullOrWhiteSpace(row.MinistryLetterFileName)
                ? row
                : null);
        var resultOptions = isCurrent
            ? ApplicationWorkspaceProgressAdvancePreview.ResultOptions(key, advanceOptions)
            : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();
        var cancelOption = resultOptions.FirstOrDefault(o =>
            !string.IsNullOrWhiteSpace(o.StateCode)
            && o.StateCode.Contains("CANCELLED", StringComparison.OrdinalIgnoreCase));
        if (suppressResultDropdown)
            resultOptions = Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();
        var isMigration = string.Equals(key, MigrationKey, StringComparison.OrdinalIgnoreCase);
        var isSubmissionTimeline = string.Equals(
            key, ApplicationMigrationSubmission.TimelineKey, StringComparison.OrdinalIgnoreCase);
        var decisionRow = row?.IsMinistryDecisionStep == true ? row : null;
        var submissionStep = !isMigration
            && (isSubmissionTimeline || ApplicationMigrationSubmission.IsState(row?.State?.Code));
        var fileOwner = isMigration
            ? ResolveMigrationDocumentRow(history, row)
            : submissionStep
                ? row
                : decisionRow ?? letter;
        var fileSlot = submissionStep
            ? ApplicationProfileInstanceProgressFiles.SubmissionDocument
            : ApplicationProfileInstanceProgressFiles.MinistryLetter;
        var storedFiles = ApplicationProfileInstanceProgressFiles.List(fileOwner, fileSlot);
        var letterId = letter != null && letter.ID != Guid.Empty
            ? letter.ID
            : (row != null && row.ID != Guid.Empty ? row.ID : (Guid?)null);
        if (isMigration && fileOwner != null && fileOwner.ID != Guid.Empty)
            letterId = fileOwner.ID;
        var awaitingMigrationDocuments = isMigration
            && isCurrent
            && string.Equals(stateLabel, AwaitingAsCoNumberLabel, StringComparison.OrdinalIgnoreCase);
        var hasLetter = storedFiles.Count > 0 || !string.IsNullOrWhiteSpace(letter?.MinistryLetterFileName);
        var missingLetter = ApplicationWorkspaceProgressLetterCompleteness.ResolveMissing(
            key,
            row?.IsMinistryDecisionStep == true,
            hasLetter);
        var dateProgressId = row != null && row.ID != Guid.Empty ? row.ID : (Guid?)null;
        var canCorrect = CanCorrectStoredDate(slotState, isCurrent && canAdvance, dateProgressId);
        var fileName = storedFiles.Count > 0
            ? storedFiles[0].FileName
            : letter?.MinistryLetterFileName ?? string.Empty;

        return new ApplicationWorkspaceCaseProgressStep
        {
            Key = key,
            Label = label,
            Date = row?.Date is { } date && date != default
                ? date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
                : string.Empty,
            DateValue = row?.Date is { } storedDate && storedDate != default ? storedDate : null,
            DateProgressId = dateProgressId,
            CanCorrectDate = canCorrect,
            CanEditFiles = canCorrect,
            CanEditSubmissionNumber = canCorrect && submissionStep,
            CanEditResultNumber = canCorrect && decisionRow != null,
            Files = storedFiles,
            FileSlot = fileSlot,
            State = slotState,
            CurrentStateLabel = stateLabel,
            SlaTargetDate = isCurrent && row != null ? FormatSlaTarget(row.Date, currentSla) : string.Empty,
            SlaDaysRemaining = isCurrent ? DaysLeft(currentSla) : null,
            ProgressId = letterId,
            OfficerNotes = isCurrent ? row?.Description ?? string.Empty : string.Empty,
            MinistryLetterFileName = fileName,
            ShowMinistryLetterUpload = awaitingMigrationDocuments
                || showOptionalOutcomeFiles
                || (!isMigration
                    && isCurrent
                    && key.StartsWith("leg-", StringComparison.OrdinalIgnoreCase)
                    && resultOptions.Count > 0)
                || (!isMigration && missingLetter),
            MissingMinistryLetter = missingLetter,
            DecisionProgressId = decisionRow != null && decisionRow.ID != Guid.Empty
                ? decisionRow.ID
                : (missingLetter && row is { ID: var rowId } && rowId != Guid.Empty ? rowId : null),
            CanAdvance = isCurrent && canAdvance,
            CanRevert = CanRevertLast(latest, key) || (isCurrent && latest != null),
            CanRevertToHere = slotState == "done"
                && ApplicationProfileInstanceProgressRevertHelper.RowsToDelete(history, key).Count > 0,
            AdvanceBlockedReason = isCurrent ? advanceBlockedReason : string.Empty,
            AdvanceOptions = isCurrent ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
            ResultOptions = resultOptions,
            OutcomeKind = ResolveOutcomeKind(slotState, row?.State?.Code),
            ResultNumber = ResolveResultNumber(key, history),
            ProcessNumber = processNumber?.Trim() ?? string.Empty,
            ShowMigrationSubmission = showMigrationSubmission,
            RequireSubmissionDocumentNumber = requireSubmissionDocumentNumber,
            RequireProcessNumberOnAdvance = requireProcessNumberOnAdvance,
            ShowCancelProgress = suppressResultDropdown && cancelOption != null && isCurrent && canAdvance,
            CancelStateCode = cancelOption?.StateCode ?? string.Empty,
            SubmissionDocumentNumber = submissionDocumentNumber ?? string.Empty,
            SubmissionDocumentFileName = submissionDocumentFileName ?? string.Empty,
        };
    }

    /// <summary>Direct: left office (Submitted) and not yet handed to migration.</summary>
    private static bool IsDirectLeftOfficeAwaitingSubmission(string? stateCode) =>
        string.Equals(
            stateCode?.Trim(),
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
            StringComparison.OrdinalIgnoreCase);

    private static string ResolveResultNumber(
        string key,
        IReadOnlyList<ApplicationProfileInstanceProgress> history)
    {
        if (!key.StartsWith("leg-", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(key.AsSpan(4), out var sequence))
            return string.Empty;

        var decision = history.LastOrDefault(p =>
            MatchesMinistryDisplayLeg(p.State?.Code, sequence)
            && ApplicationProfileInstanceProgressLegCodes.IsMinistryDecisionStateCode(p.State?.Code));
        return string.IsNullOrWhiteSpace(decision?.ResultNumber)
            ? string.Empty
            : decision.ResultNumber.Trim();
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

    private static string ResolveSubmissionSlotState(
        bool atOffice,
        bool latestOnMigration,
        bool awaitingSubmission,
        bool submissionRecorded,
        bool cancelled)
    {
        if (atOffice)
            return "pending";
        if (cancelled && (awaitingSubmission || submissionRecorded))
            return "current";
        if (latestOnMigration || submissionRecorded)
            return "done";
        if (awaitingSubmission)
            return "current";
        return "pending";
    }

    private static string ResolveMigrationSlotState(
        bool atOffice,
        bool latestOnMigration,
        string? latestCode,
        bool submissionRecorded,
        bool cancelled)
    {
        if (atOffice)
            return "pending";
        if (submissionRecorded && !cancelled)
            return "current";
        if (!latestOnMigration)
            return "pending";

        if (string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(latestCode, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return "done";

        return "current";
    }

    private static string ResolveMigrationStatusLabel(
        ApplicationProfileInstanceProgress? row,
        string slotState)
    {
        var code = row?.State?.Code?.Trim();
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
            return "Processing";
        // Issued is the application-result chips, not a status on this step.
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase))
            return "Process rejected";
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return FormatProfileStateLabel(code);

        return string.Equals(slotState, "current", StringComparison.OrdinalIgnoreCase)
            ? AwaitingAsCoNumberLabel
            : string.Empty;
    }

    /// <summary>Direct final node: no "Awaiting AS" sub-status; Issued/Cancelled when done.</summary>
    private static string ResolveDirectMigrationStatusLabel(
        ApplicationProfileInstanceProgress? row,
        string slotState)
    {
        var code = row?.State?.Code?.Trim();
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
            return "Process issued";
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase))
            return "Process rejected";
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return FormatProfileStateLabel(code);
        if (string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
            return "Processing";

        _ = slotState;
        return string.Empty;
    }

    private static string ResolveMigrationProcessNumber(
        ApplicationProfileInstance application,
        IReadOnlyList<ApplicationProfileInstanceProgress> history,
        ApplicationProfileInstanceProgress? migrationRow,
        string slotState)
    {
        var status = ResolveMigrationStatusLabel(migrationRow, slotState);
        var issued = string.Equals(
            migrationRow?.State?.Code,
            ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            StringComparison.OrdinalIgnoreCase);
        if (!issued && status is not ("Processing" or "Process rejected"))
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(migrationRow?.ProcessNumber))
            return migrationRow.ProcessNumber.Trim();

        for (var i = history.Count - 1; i >= 0; i--)
        {
            var row = history[i];
            if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(row.State?.Code)
                && !string.IsNullOrWhiteSpace(row.ProcessNumber))
                return row.ProcessNumber.Trim();
        }

        return application.ProcessNumber?.Trim() ?? string.Empty;
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
            && ApplicationProfileInstanceProgressLegCodes.IsMinistryDecisionStateCode(p.State?.Code)
            && !string.IsNullOrWhiteSpace(p.MinistryLetterFileName));

    private static bool MatchesMinistryDisplayLeg(string? stateCode, int sequence)
    {
        if (!ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out var leg))
            return false;

        return Math.Clamp(leg, 1, ApplicationProfileInstanceProgressLegCodes.MaxLegCount) == sequence;
    }

    private static ApplicationProfileInstanceProgress? LatestSubmissionRow(
        IReadOnlyList<ApplicationProfileInstanceProgress> history) =>
        history.LastOrDefault(p => ApplicationMigrationSubmission.IsState(p.State?.Code));

    private static ApplicationProfileInstanceProgress? LatestMigrationRow(
        IReadOnlyList<ApplicationProfileInstanceProgress> history) =>
        history.LastOrDefault(p => IsCurrentMigration(p.State?.Code, history));

    /// <summary>
    /// Document copies on the migration slot stay on the outcome row that received them
    /// (process started, issued, or rejected), even after a later outcome row exists.
    /// </summary>
    private static ApplicationProfileInstanceProgress? ResolveMigrationDocumentRow(
        IReadOnlyList<ApplicationProfileInstanceProgress> history,
        ApplicationProfileInstanceProgress? latestRow)
    {
        ApplicationProfileInstanceProgress? withFiles = null;
        foreach (var candidate in history)
        {
            if (!IsMigrationOutcomeRow(candidate?.State?.Code))
                continue;
            if (ApplicationProfileInstanceProgressFiles.List(
                    candidate,
                    ApplicationProfileInstanceProgressFiles.MinistryLetter).Count == 0)
                continue;
            withFiles = candidate;
        }

        return withFiles ?? latestRow;
    }

    private static bool IsMigrationOutcomeRow(string? code) =>
        ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(code)
        || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
        || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase);

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
            return ApplicationProfileLocalization.ProgressOutcome(catalog.DisplayName);

        if (objectSpace != null)
        {
            var state = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(s => s.Code == stateCode);
            if (state != null)
            {
                return ApplicationProfileLocalization.ProgressOutcome(
                    state.LocalizedDisplayName
                    ?? state.NameTm
                    ?? state.Code
                    ?? stateCode);
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