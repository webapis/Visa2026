using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for <see cref="BusinessObjects.EmployeeSearchColumnSettings"/>.
/// </summary>
public static class EmployeeSearchColumnSettingsSchemaSql
{
    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."EmployeeSearchColumnSettings"') IS NULL THEN
            CREATE TABLE "EmployeeSearchColumnSettings" (
              "ID" uuid NOT NULL CONSTRAINT "PK_EmployeeSearchColumnSettings" PRIMARY KEY,
              "ColumnsJson" text NOT NULL DEFAULT '[]',
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
          END IF;

          UPDATE "EmployeeSearchColumnSettings" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
          UPDATE "EmployeeSearchColumnSettings" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;
          UPDATE "EmployeeSearchColumnSettings" SET "ColumnsJson" = '[]' WHERE "ColumnsJson" IS NULL;

          ALTER TABLE "EmployeeSearchColumnSettings"
            ALTER COLUMN "GCRecord" SET DEFAULT 0,
            ALTER COLUMN "GCRecord" SET NOT NULL,
            ALTER COLUMN "OptimisticLockField" SET DEFAULT 0,
            ALTER COLUMN "OptimisticLockField" SET NOT NULL,
            ALTER COLUMN "ColumnsJson" SET DEFAULT '[]',
            ALTER COLUMN "ColumnsJson" SET NOT NULL;
        END $$;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)
            || !DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        if (!PostgresRelationExists.All(connectionString, "People"))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = EnsureTablePostgres;
        command.ExecuteNonQuery();
    }
}