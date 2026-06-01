namespace Visa2026.Module.BusinessObjects
{
    /// <summary>Stable <see cref="ApplicationState.Code"/> values from <c>application-state.json</c>.</summary>
    public static class ApplicationProgressStateCodes
    {
        public const string IsBeingPrepared = "IS_BEING_PREPARED";

        public const string Review1Started = "1_REVIEW_STARTED";
        public const string Review1Approved = "1_REVIEW_APPROVED";
        public const string Review1Rejected = "1_REVIEW_REJECTED";

        public const string Review2Started = "2_REVIEW_STARTED";
        public const string Review2Approved = "2_REVIEW_APPROVED";
        public const string Review2Rejected = "2_REVIEW_REJECTED";

        public const string ProcessStarted = "PROCESS_STARTED";
        public const string ProcessIssued = "PROCESS_ISSUED";
        public const string ProcessRejected = "PROCESS_REJECTED";
        public const string ProcessCancelled = "PROCESS_CANCELLED";
    }

    /// <summary>Stable <see cref="ApplicationLocation.Code"/> values from <c>application-location.json</c>.</summary>
    public static class ApplicationProgressLocationCodes
    {
        public const string AtOffice = "AT_OFFICE";
        public const string AtMinistry1 = "AT_THE_MINISTERY_1";
        public const string AtMinistry2 = "AT_THE_MINISTERY_2";
        public const string AtMigrationService = "AT_MIGRATION_SERVICE";
    }
}
