using Microsoft.Data.SqlClient;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.Operations.ApplicationRuntimeLog"/> when XAF schema
/// update did not run (e.g. hot reload after pulling new Module DLLs).
/// </summary>
public static class ApplicationRuntimeLogSchemaSql
{
    internal const string EnsureTableSql = """
        IF OBJECT_ID(N'dbo.ApplicationRuntimeLogs', N'U') IS NOT NULL
            RETURN;

        CREATE TABLE dbo.ApplicationRuntimeLogs (
            ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationRuntimeLogs PRIMARY KEY,
            OccurredAtUtc datetime2 NOT NULL,
            Severity int NOT NULL,
            ErrorCode nvarchar(64) NULL,
            Category nvarchar(512) NULL,
            Message nvarchar(4000) NULL,
            ExceptionType nvarchar(512) NULL,
            StackTrace nvarchar(max) NULL,
            UserName nvarchar(128) NULL,
            CorrelationId nvarchar(64) NULL,
            RequestPath nvarchar(512) NULL,
            MachineName nvarchar(128) NULL,
            DeploymentEnvironment int NOT NULL,
            ApplicationVersion nvarchar(128) NULL,
            RelatedBatchId uniqueidentifier NULL,
            SentryEventId nvarchar(32) NULL,
            ResolutionStatus int NOT NULL CONSTRAINT DF_ApplicationRuntimeLogs_ResolutionStatus DEFAULT (0),
            AcknowledgedAtUtc datetime2 NULL,
            ResolvedAtUtc datetime2 NULL,
            ResolvedBy nvarchar(128) NULL,
            ResolutionNotes nvarchar(4000) NULL,
            FixCommitHash nvarchar(64) NULL,
            AgentRunId nvarchar(128) NULL,
            GCRecord int NOT NULL CONSTRAINT DF_ApplicationRuntimeLogs_GCRecord DEFAULT (0),
            OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationRuntimeLogs_OLF DEFAULT (0)
        );

        CREATE INDEX IX_ApplicationRuntimeLogs_OccurredAtUtc ON dbo.ApplicationRuntimeLogs (OccurredAtUtc);
        CREATE INDEX IX_ApplicationRuntimeLogs_Severity ON dbo.ApplicationRuntimeLogs (Severity);
        CREATE INDEX IX_ApplicationRuntimeLogs_CorrelationId ON dbo.ApplicationRuntimeLogs (CorrelationId);
        CREATE INDEX IX_ApplicationRuntimeLogs_ResolutionStatus ON dbo.ApplicationRuntimeLogs (ResolutionStatus);
        """;

    internal const string EnsureSentryEventIdColumnSql = """
        IF OBJECT_ID(N'dbo.ApplicationRuntimeLogs', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.ApplicationRuntimeLogs', N'SentryEventId') IS NULL
            ALTER TABLE dbo.ApplicationRuntimeLogs ADD SentryEventId nvarchar(32) NULL;
        """;

    internal const string EnsureResolutionColumnsSql = """
        IF OBJECT_ID(N'dbo.ApplicationRuntimeLogs', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.ApplicationRuntimeLogs', N'ResolutionStatus') IS NULL
            ALTER TABLE dbo.ApplicationRuntimeLogs ADD ResolutionStatus int NOT NULL
                CONSTRAINT DF_ApplicationRuntimeLogs_ResolutionStatus DEFAULT (0);

        IF COL_LENGTH(N'dbo.ApplicationRuntimeLogs', N'AcknowledgedAtUtc') IS NULL
            ALTER TABLE dbo.ApplicationRuntimeLogs ADD AcknowledgedAtUtc datetime2 NULL;

        IF COL_LENGTH(N'dbo.ApplicationRuntimeLogs', N'ResolvedAtUtc') IS NULL
            ALTER TABLE dbo.ApplicationRuntimeLogs ADD ResolvedAtUtc datetime2 NULL;

        IF COL_LENGTH(N'dbo.ApplicationRuntimeLogs', N'ResolvedBy') IS NULL
            ALTER TABLE dbo.ApplicationRuntimeLogs ADD ResolvedBy nvarchar(128) NULL;

        IF COL_LENGTH(N'dbo.ApplicationRuntimeLogs', N'ResolutionNotes') IS NULL
            ALTER TABLE dbo.ApplicationRuntimeLogs ADD ResolutionNotes nvarchar(4000) NULL;

        IF COL_LENGTH(N'dbo.ApplicationRuntimeLogs', N'FixCommitHash') IS NULL
            ALTER TABLE dbo.ApplicationRuntimeLogs ADD FixCommitHash nvarchar(64) NULL;

        IF COL_LENGTH(N'dbo.ApplicationRuntimeLogs', N'AgentRunId') IS NULL
            ALTER TABLE dbo.ApplicationRuntimeLogs ADD AgentRunId nvarchar(128) NULL;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationRuntimeLogs_ResolutionStatus'
              AND object_id = OBJECT_ID(N'dbo.ApplicationRuntimeLogs'))
            CREATE INDEX IX_ApplicationRuntimeLogs_ResolutionStatus ON dbo.ApplicationRuntimeLogs (ResolutionStatus);
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = EnsureTableSql;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = EnsureSentryEventIdColumnSql;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = EnsureResolutionColumnsSql;
            command.ExecuteNonQuery();
        }
    }
}
