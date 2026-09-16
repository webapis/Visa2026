using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent indexes for ApplicationProfileInstance ListView load performance
/// (date window on ApplicationDate; staged/in-process queue filters).
/// </summary>
public static class ApplicationProfileInstanceListQueryIndexesSchemaSql
{
    internal const string EnsureIndexesPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileInstances"') IS NULL THEN
            RETURN;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = 'IX_ApplicationProfileInstances_ApplicationDate') THEN
            CREATE INDEX "IX_ApplicationProfileInstances_ApplicationDate"
                ON "ApplicationProfileInstances" ("ApplicationDate")
                WHERE "GCRecord" IS NULL;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = 'IX_ApplicationProfileInstances_StagedQueue_State') THEN
            CREATE INDEX "IX_ApplicationProfileInstances_StagedQueue_State"
                ON "ApplicationProfileInstances" ("HasLeftStagedQueue", "LatestPrimaryStateCode")
                WHERE "GCRecord" IS NULL;
          END IF;
        END $$;
        """;

    internal const string EnsureIndexesSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NULL
            RETURN;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationProfileInstances_ApplicationDate'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileInstances'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_ApplicationProfileInstances_ApplicationDate
            ON dbo.ApplicationProfileInstances (ApplicationDate)
            WHERE GCRecord IS NULL;
        END;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationProfileInstances_StagedQueue_State'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileInstances'))
        BEGIN
            CREATE NONCLUSTERED INDEX IX_ApplicationProfileInstances_StagedQueue_State
            ON dbo.ApplicationProfileInstances (HasLeftStagedQueue, LatestPrimaryStateCode)
            WHERE GCRecord IS NULL;
        END;
        """;

    /// <summary>Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).</summary>
    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            using var connection = new NpgsqlConnection(cleaned);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = EnsureIndexesPostgres;
            command.ExecuteNonQuery();
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = EnsureIndexesSqlServer;
        sqlCommand.ExecuteNonQuery();
    }
}