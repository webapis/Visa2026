using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.Visa.IssuingApplication"/> FK on Visas.
/// </summary>
public static class VisaIssuingApplicationSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."Visas"') IS NULL THEN
            RETURN;
          END IF;

          ALTER TABLE "Visas" ADD COLUMN IF NOT EXISTS "IssuingApplicationID" uuid NULL;

          IF NOT EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = 'IX_Visas_IssuingApplicationID') THEN
            CREATE INDEX "IX_Visas_IssuingApplicationID"
                ON "Visas" ("IssuingApplicationID");
          END IF;

          IF to_regclass('public."Applications"') IS NOT NULL
             AND NOT EXISTS (
               SELECT 1 FROM pg_constraint
               WHERE conname = 'FK_Visas_Applications_IssuingApplicationID') THEN
            ALTER TABLE "Visas"
                ADD CONSTRAINT "FK_Visas_Applications_IssuingApplicationID"
                FOREIGN KEY ("IssuingApplicationID") REFERENCES "Applications" ("ID");
          END IF;

          IF to_regclass('public."ApplicationItems"') IS NOT NULL THEN
            UPDATE "Visas" v
            SET "IssuingApplicationID" = ai."ApplicationID"
            FROM "ApplicationItems" ai
            WHERE v."IssuingApplicationItemID" = ai."ID"
              AND v."IssuingApplicationID" IS NULL
              AND ai."ApplicationID" IS NOT NULL;
          END IF;
        END $$;
        """;

    internal const string EnsureSchemaSqlServer = """
        IF OBJECT_ID(N'dbo.Visas', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.Visas', N'IssuingApplicationID') IS NULL
            ALTER TABLE dbo.Visas ADD IssuingApplicationID uniqueidentifier NULL;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_Visas_IssuingApplicationID'
              AND object_id = OBJECT_ID(N'dbo.Visas'))
            CREATE INDEX IX_Visas_IssuingApplicationID ON dbo.Visas (IssuingApplicationID);

        IF OBJECT_ID(N'dbo.Applications', N'U') IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM sys.foreign_keys
               WHERE name = N'FK_Visas_Applications_IssuingApplicationID')
            ALTER TABLE dbo.Visas
                ADD CONSTRAINT FK_Visas_Applications_IssuingApplicationID
                FOREIGN KEY (IssuingApplicationID) REFERENCES dbo.Applications(ID);

        IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NOT NULL
        BEGIN
            UPDATE v
            SET v.IssuingApplicationID = ai.ApplicationID
            FROM dbo.Visas v
            INNER JOIN dbo.ApplicationItems ai ON v.IssuingApplicationItemID = ai.ID
            WHERE v.IssuingApplicationID IS NULL
              AND ai.ApplicationID IS NOT NULL;
        END;
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
            Execute(connection, EnsureSchemaPostgres);
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        Execute(sqlConnection, EnsureSchemaSqlServer);
    }

    private static void Execute(System.Data.Common.DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
