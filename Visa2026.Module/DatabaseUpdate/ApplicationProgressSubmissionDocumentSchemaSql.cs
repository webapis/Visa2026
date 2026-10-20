using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent columns for the migration-submission progress step.
/// Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).
/// </summary>
public static class ApplicationProfileInstanceProgressSubmissionDocumentSchemaSql
{
    internal const string EnsureColumnsPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileInstanceProgresses"') IS NULL THEN
            RETURN;
          END IF;

          ALTER TABLE "ApplicationProfileInstanceProgresses" ADD COLUMN IF NOT EXISTS "SubmissionDocumentNumber" character varying(100) NULL;
          ALTER TABLE "ApplicationProfileInstanceProgresses" ADD COLUMN IF NOT EXISTS "SubmissionDocumentFileID" uuid NULL;
        END $$;
        """;

    internal const string EnsureColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'SubmissionDocumentNumber') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstanceProgresses ADD SubmissionDocumentNumber nvarchar(100) NULL;
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'SubmissionDocumentFileID') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstanceProgresses ADD SubmissionDocumentFileID uniqueidentifier NULL;
        """;

    internal const string EnsureStatePostgres = """
        INSERT INTO "ApplicationStates" ("ID", "GCRecord", "OptimisticLockField", "NameTm", "LocalizationKey", "Code", "IsDefault")
        SELECT gen_random_uuid(), 0, 0, 'Migrasiýa gullugyna tabşyryldy', 'SUBMITTED_TO_MIGRATION', 'SUBMITTED_MIGRATION', false
        WHERE to_regclass('public."ApplicationStates"') IS NOT NULL
          AND NOT EXISTS (
            SELECT 1 FROM "ApplicationStates" WHERE "Code" = 'SUBMITTED_MIGRATION'
          );
        """;

    internal const string EnsureStateSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationStates', N'U') IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.ApplicationStates WHERE Code = N'SUBMITTED_MIGRATION')
            INSERT INTO dbo.ApplicationStates (ID, GCRecord, OptimisticLockField, NameTm, LocalizationKey, Code, IsDefault)
            VALUES (NEWID(), 0, 0, N'Migrasiýa gullugyna tabşyryldy', N'SUBMITTED_TO_MIGRATION', N'SUBMITTED_MIGRATION', 0);
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            using var connection = new NpgsqlConnection(cleaned);
            connection.Open();
            Execute(connection, EnsureColumnsPostgres);
            Execute(connection, EnsureStatePostgres);
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        Execute(sqlConnection, EnsureColumnsSqlServer);
        Execute(sqlConnection, EnsureStateSqlServer);
    }

    private static void Execute(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
