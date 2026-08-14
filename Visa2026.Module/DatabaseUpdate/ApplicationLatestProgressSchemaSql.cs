namespace Visa2026.Module.DatabaseUpdate;

public static class ApplicationLatestProgressSchemaSql
{
    internal const string EnsureColumnsSql = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.ApplicationProfileInstances', N'LatestProgressId') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstances ADD LatestProgressId uniqueidentifier NULL;

        IF COL_LENGTH(N'dbo.ApplicationProfileInstances', N'LatestPrimaryStateCode') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstances ADD LatestPrimaryStateCode nvarchar(64) NULL;

        IF COL_LENGTH(N'dbo.ApplicationProfileInstances', N'LatestProgressDisplay') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstances ADD LatestProgressDisplay nvarchar(255) NULL;

        IF NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_Applications_ApplicationProfileInstanceProgresses_LatestProgressId')
        BEGIN
            ALTER TABLE dbo.ApplicationProfileInstances WITH CHECK ADD CONSTRAINT FK_Applications_ApplicationProfileInstanceProgresses_LatestProgressId
                FOREIGN KEY (LatestProgressId) REFERENCES dbo.ApplicationProfileInstanceProgresses (ID)
                ON DELETE NO ACTION;
        END;
        """;

    internal const string BackfillLatestProgressIdSql = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NULL
            OR OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NULL
            RETURN;

        UPDATE a
        SET LatestProgressId = latestProgress.ID
        FROM dbo.ApplicationProfileInstances a
        OUTER APPLY (
            SELECT TOP (1) ap.ID
            FROM dbo.ApplicationProfileInstanceProgresses ap
            WHERE ap.ApplicationProfileInstanceID = a.ID
              AND (ap.GCRecord IS NULL OR ap.GCRecord = 0)
            ORDER BY ap.ProgressOrder DESC, ap.Date DESC, ap.ID DESC
        ) latestProgress
        WHERE (a.GCRecord IS NULL OR a.GCRecord = 0)
          AND a.LatestProgressId IS NULL
          AND latestProgress.ID IS NOT NULL;
        """;
}