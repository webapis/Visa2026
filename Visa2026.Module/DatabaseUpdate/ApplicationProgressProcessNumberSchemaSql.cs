using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationProgress.ProcessNumber"/>
/// and denormalized <see cref="BusinessObjects.Application.ProcessNumber"/>.
/// </summary>
public static class ApplicationProgressProcessNumberSchemaSql
{
    /// <summary>
    /// Greenfield-safe: skip when tables do not exist yet (empty EasyTest / first --updateDatabase).
    /// Single DO block so XAF <c>ExecuteNonQueryCommand</c> does not split mid-script.
    /// </summary>
    internal const string EnsureColumnsPostgres = """
        DO $ensure$
        BEGIN
          IF to_regclass('public."ApplicationProgresses"') IS NOT NULL THEN
            ALTER TABLE "ApplicationProgresses"
              ADD COLUMN IF NOT EXISTS "ProcessNumber" character varying(100) NULL;
          END IF;
          IF to_regclass('public."Applications"') IS NOT NULL THEN
            ALTER TABLE "Applications"
              ADD COLUMN IF NOT EXISTS "ProcessNumber" character varying(100) NULL;
          END IF;
        END
        $ensure$;
        """;

    internal const string EnsureColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProgresses', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProgresses', N'ProcessNumber') IS NULL
            ALTER TABLE dbo.ApplicationProgresses ADD ProcessNumber nvarchar(100) NULL;

        IF OBJECT_ID(N'dbo.Applications', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.Applications', N'ProcessNumber') IS NULL
            ALTER TABLE dbo.Applications ADD ProcessNumber nvarchar(100) NULL;
        """;

    /// <summary>
    /// Legacy import stored process number in Description on PROCESS_STARTED.
    /// </summary>
    internal const string BackfillProgressFromDescriptionSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProgresses', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationProgresses', N'ProcessNumber') IS NULL
            RETURN;
        IF OBJECT_ID(N'dbo.ApplicationStates', N'U') IS NULL
            RETURN;

        UPDATE ap
        SET ProcessNumber = LEFT(LTRIM(RTRIM(ap.Description)), 100)
        FROM dbo.ApplicationProgresses ap
        INNER JOIN dbo.ApplicationStates st ON st.ID = ap.StateID
        WHERE st.Code = N'PROCESS_STARTED'
          AND ap.ProcessNumber IS NULL
          AND ap.Description IS NOT NULL
          AND LTRIM(RTRIM(ap.Description)) <> N'';
        """;

    internal const string BackfillProgressFromDescriptionPostgres = """
        DO $bf$
        BEGIN
          IF to_regclass('public."ApplicationProgresses"') IS NULL
             OR to_regclass('public."ApplicationStates"') IS NULL THEN
            RETURN;
          END IF;

          UPDATE "ApplicationProgresses" ap
          SET "ProcessNumber" = LEFT(TRIM(ap."Description"), 100)
          FROM "ApplicationStates" st
          WHERE st."ID" = ap."StateID"
            AND st."Code" = 'PROCESS_STARTED'
            AND ap."ProcessNumber" IS NULL
            AND ap."Description" IS NOT NULL
            AND TRIM(ap."Description") <> '';
        END
        $bf$;
        """;

    internal const string BackfillApplicationFromProgressSqlServer = """
        IF OBJECT_ID(N'dbo.Applications', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.Applications', N'ProcessNumber') IS NULL
            RETURN;
        IF OBJECT_ID(N'dbo.ApplicationProgresses', N'U') IS NULL
            RETURN;
        IF OBJECT_ID(N'dbo.ApplicationStates', N'U') IS NULL
            RETURN;

        UPDATE a
        SET ProcessNumber = src.ProcessNumber
        FROM dbo.Applications a
        INNER JOIN (
            SELECT ap.ApplicationID, ap.ProcessNumber,
                   ROW_NUMBER() OVER (
                       PARTITION BY ap.ApplicationID
                       ORDER BY ap.ProgressOrder ASC, ap.Date ASC, ap.ID ASC) AS rn
            FROM dbo.ApplicationProgresses ap
            INNER JOIN dbo.ApplicationStates st ON st.ID = ap.StateID
            WHERE st.Code = N'PROCESS_STARTED'
              AND ap.ProcessNumber IS NOT NULL
              AND LTRIM(RTRIM(ap.ProcessNumber)) <> N''
        ) src ON src.ApplicationID = a.ID AND src.rn = 1
        WHERE a.ProcessNumber IS NULL;
        """;

    internal const string BackfillApplicationFromProgressPostgres = """
        DO $bf$
        BEGIN
          IF to_regclass('public."Applications"') IS NULL
             OR to_regclass('public."ApplicationProgresses"') IS NULL
             OR to_regclass('public."ApplicationStates"') IS NULL THEN
            RETURN;
          END IF;

          UPDATE "Applications" a
          SET "ProcessNumber" = src."ProcessNumber"
          FROM (
              SELECT ap."ApplicationID", ap."ProcessNumber",
                     ROW_NUMBER() OVER (
                         PARTITION BY ap."ApplicationID"
                         ORDER BY ap."ProgressOrder" ASC, ap."Date" ASC, ap."ID" ASC) AS rn
              FROM "ApplicationProgresses" ap
              INNER JOIN "ApplicationStates" st ON st."ID" = ap."StateID"
              WHERE st."Code" = 'PROCESS_STARTED'
                AND ap."ProcessNumber" IS NOT NULL
                AND TRIM(ap."ProcessNumber") <> ''
          ) src
          WHERE src."ApplicationID" = a."ID"
            AND src.rn = 1
            AND a."ProcessNumber" IS NULL;
        END
        $bf$;
        """;

    /// <summary>
    /// Host-start heal for SQL Server and PostgreSQL (ModuleUpdater may be skipped when ModuleInfo is current).
    /// </summary>
    public static void ApplyIfMissing(string connectionString, bool backfill = true)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
            using var connection = new NpgsqlConnection(cleaned);
            connection.Open();
            Execute(connection, EnsureColumnsPostgres);
            if (backfill)
            {
                Execute(connection, BackfillProgressFromDescriptionPostgres);
                Execute(connection, BackfillApplicationFromProgressPostgres);
            }

            return;
        }

        using (var connection = new SqlConnection(DatabaseProviderDetector.StripEfCoreProvider(connectionString)))
        {
            connection.Open();
            Execute(connection, EnsureColumnsSqlServer);
            if (backfill)
            {
                Execute(connection, BackfillProgressFromDescriptionSqlServer);
                Execute(connection, BackfillApplicationFromProgressSqlServer);
            }
        }
    }

    private static void Execute(System.Data.Common.DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
