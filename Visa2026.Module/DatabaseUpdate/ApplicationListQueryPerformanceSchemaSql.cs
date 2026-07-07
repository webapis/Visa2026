namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent indexes for <see cref="BusinessObjects.Application"/> ListView query performance.
/// </summary>
public static class ApplicationListQueryPerformanceSchemaSql
{
    internal const string EnsureIndexesSql = """
        IF OBJECT_ID(N'dbo.ApplicationProgresses', N'U') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationProgresses_ApplicationID_ProgressOrder'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProgresses'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_ApplicationProgresses_ApplicationID_ProgressOrder
            ON dbo.ApplicationProgresses (ApplicationID, ProgressOrder DESC)
            INCLUDE (StateID, LocationID, Date);
        END;

        IF OBJECT_ID(N'dbo.ApplicationApprovalLegSnapshots', N'U') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationApprovalLegSnapshots_ApplicationId'
              AND object_id = OBJECT_ID(N'dbo.ApplicationApprovalLegSnapshots'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_ApplicationApprovalLegSnapshots_ApplicationId
            ON dbo.ApplicationApprovalLegSnapshots (ApplicationId)
            INCLUDE (Sequence, MaxDaysInReview, WarningDaysBeforeMax, MinistryShortName);
        END;

        IF OBJECT_ID(N'dbo.Applications', N'U') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_Applications_ApplicationTypeID_List'
              AND object_id = OBJECT_ID(N'dbo.Applications'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_Applications_ApplicationTypeID_List
            ON dbo.Applications (ApplicationTypeID)
            INCLUDE (Year, Month, ApplicationDate, FullApplicationNumber, ApplicationNumber, AppNumberPrefix);
        END;
        """;
}