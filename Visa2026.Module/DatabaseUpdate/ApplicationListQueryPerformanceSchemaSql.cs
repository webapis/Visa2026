namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent indexes for <see cref="BusinessObjects.ApplicationProfileInstance"/> ListView query performance.
/// </summary>
public static class ApplicationListQueryPerformanceSchemaSql
{
    internal const string EnsureIndexesSql = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationProfileInstanceProgresses_ApplicationProfileInstanceID_ProgressOrder'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_ApplicationProfileInstanceProgresses_ApplicationProfileInstanceID_ProgressOrder
            ON dbo.ApplicationProfileInstanceProgresses (ApplicationProfileInstanceID, ProgressOrder DESC)
            INCLUDE (StateID, Date);
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceApprovalLegSnapshots', N'U') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationProfileInstanceApprovalLegSnapshots_ApplicationProfileInstanceId'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileInstanceApprovalLegSnapshots'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_ApplicationProfileInstanceApprovalLegSnapshots_ApplicationProfileInstanceId
            ON dbo.ApplicationProfileInstanceApprovalLegSnapshots (ApplicationProfileInstanceId)
            INCLUDE (Sequence, MaxDaysInReview, WarningDaysBeforeMax, MinistryShortName);
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NOT NULL
        AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_Applications_ApplicationTypeID_List'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileInstances'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_Applications_ApplicationTypeID_List
            ON dbo.ApplicationProfileInstances (ApplicationTypeID)
            INCLUDE (Year, Month, ApplicationDate, FullApplicationNumber, ApplicationNumber, AppNumberPrefix);
        END;
        """;
}