using System;
namespace Visa2026.Module.BusinessObjects
{
    /// <summary>Stable <see cref="ApplicationState.Code"/> values from <c>application-state.json</c>.</summary>
    public static class ApplicationProfileInstanceProgressStateCodes
    {
        public const string IsBeingPrepared = "IS_BEING_PREPARED";

        /// <summary>First-leg only — "Ylalaşyga Iberildi".</summary>
        public const string Review1Started = "1_REVIEW_STARTED";
        public const string Review1Approved = "1_REVIEW_APPROVED";
        public const string Review1Rejected = "1_REVIEW_REJECTED";

        /// <summary>Legacy — retained for existing rows only; not offered in new progress.</summary>
        public const string Review2Started = "2_REVIEW_STARTED";
        public const string Review2Approved = "2_REVIEW_APPROVED";
        public const string Review2Rejected = "2_REVIEW_REJECTED";

        public const string ProcessStarted = "PROCESS_STARTED";
        public const string ProcessIssued = "PROCESS_ISSUED";
        public const string ProcessRejected = "PROCESS_REJECTED";
        public const string ProcessCancelled = "PROCESS_CANCELLED";

        /// <summary>
        /// Final application-process outcomes: Issued, Cancelled, Process Rejected, or any *_REVIEW_REJECTED.
        /// </summary>
        public static bool IsTerminalOutcome(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var trimmed = code.Trim();
            if (string.Equals(trimmed, ProcessIssued, StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, ProcessRejected, StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, ProcessCancelled, StringComparison.OrdinalIgnoreCase))
                return true;

            return trimmed.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Stable <see cref="ApplicationLocation.Code"/> values from <c>application-location.json</c>.</summary>
    public static class ApplicationProfileInstanceProgressLocationCodes
    {
        public const string AtOffice = "AT_OFFICE";
        public const string AtMinistry1 = "AT_THE_MINISTERY_1";
        public const string AtMinistry2 = "AT_THE_MINISTERY_2";
        public const string AtMigrationService = "AT_MIGRATION_SERVICE";
    }
}
