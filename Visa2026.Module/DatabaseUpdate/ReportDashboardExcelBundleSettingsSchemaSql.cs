using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for <see cref="BusinessObjects.ReportDashboardExcelBundleSettings"/>.
/// </summary>
public static class ReportDashboardExcelBundleSettingsSchemaSql
{
    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ReportDashboardExcelBundleSettings"') IS NULL THEN
            CREATE TABLE "ReportDashboardExcelBundleSettings" (
              "ID" uuid NOT NULL CONSTRAINT "PK_ReportDashboardExcelBundleSettings" PRIMARY KEY,
              "OrderJson" text NOT NULL DEFAULT '[]',
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
          END IF;

          UPDATE "ReportDashboardExcelBundleSettings" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
          UPDATE "ReportDashboardExcelBundleSettings" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;
          UPDATE "ReportDashboardExcelBundleSettings" SET "OrderJson" = '[]' WHERE "OrderJson" IS NULL;

          ALTER TABLE "ReportDashboardExcelBundleSettings"
            ALTER COLUMN "GCRecord" SET DEFAULT 0,
            ALTER COLUMN "GCRecord" SET NOT NULL,
            ALTER COLUMN "OptimisticLockField" SET DEFAULT 0,
            ALTER COLUMN "OptimisticLockField" SET NOT NULL,
            ALTER COLUMN "OrderJson" SET DEFAULT '[]',
            ALTER COLUMN "OrderJson" SET NOT NULL;
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