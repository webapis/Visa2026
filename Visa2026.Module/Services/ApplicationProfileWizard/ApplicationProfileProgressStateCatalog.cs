using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>Static catalog rows for wizard step 3 (matches prototype labels).</summary>
public static class ApplicationProfileProgressStateCatalog
{
    public sealed record StateRow(
        ApplicationProfileProgressStateTrack Track,
        string StateCode,
        string DisplayName,
        bool DefaultIncluded,
        bool DefaultSlaTracked);

    public static IReadOnlyList<StateRow> All { get; } =
    [
        // Ministry — prototype step 3
        new(ApplicationProfileProgressStateTrack.Ministry, ApplicationProgressStateCodes.Review1Started, "Submitted", true, true),
        new(ApplicationProfileProgressStateTrack.Ministry, ApplicationProgressStateCodes.Review1Approved, "Approved", true, false),
        new(ApplicationProfileProgressStateTrack.Ministry, ApplicationProgressStateCodes.Review1Rejected, "Disapproved", true, false),
        new(ApplicationProfileProgressStateTrack.Ministry, ApplicationProgressStateCodes.ProcessCancelled, "Cancelled", true, false),
        new(ApplicationProfileProgressStateTrack.Ministry, "MINISTRY_POSTPONED", "Postponed", true, false),

        // Migration — prototype step 3
        new(ApplicationProfileProgressStateTrack.Migration, ApplicationProgressStateCodes.ProcessStarted, "Submitted", true, true),
        new(ApplicationProfileProgressStateTrack.Migration, "MIGRATION_ON_PROCESS", "On process", true, true),
        new(ApplicationProfileProgressStateTrack.Migration, "MIGRATION_PROCESS_COMPLETE", "Process complete", true, false),
        new(ApplicationProfileProgressStateTrack.Migration, "MIGRATION_POSTPONED", "Postponed", true, false),
        new(ApplicationProfileProgressStateTrack.Migration, ApplicationProgressStateCodes.ProcessIssued, "Issued", true, false),
        new(ApplicationProfileProgressStateTrack.Migration, ApplicationProgressStateCodes.ProcessRejected, "Rejected", true, false),
        new(ApplicationProfileProgressStateTrack.Migration, ApplicationProgressStateCodes.ProcessCancelled, "Cancelled", true, false),
    ];
}
