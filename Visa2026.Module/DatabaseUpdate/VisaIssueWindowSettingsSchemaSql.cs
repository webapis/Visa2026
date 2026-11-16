using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for <see cref="BusinessObjects.VisaIssueWindowSettings"/>.
/// </summary>
/// <remarks>
/// No EF migrations: when ModuleInfo is already current, XAF skips the schema pass and a
/// newly added table is never created. Host-start <see cref="ApplyIfMissing"/> heals that.
/// <c>GCRecord</c> / <c>OptimisticLockField</c> must be NOT NULL DEFAULT 0.
/// </remarks>
public static class VisaIssueWindowSettingsSchemaSql
{
    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."VisaIssueWindowSettings"') IS NULL THEN
            CREATE TABLE "VisaIssueWindowSettings" (
              "ID" uuid NOT NULL CONSTRAINT "PK_VisaIssueWindowSettings" PRIMARY KEY,
              "WindowDays" integer NOT NULL DEFAULT 45,
              "WarningDays" integer NOT NULL DEFAULT 5,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
          END IF;

          UPDATE "VisaIssueWindowSettings" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
          UPDATE "VisaIssueWindowSettings" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;

          ALTER TABLE "VisaIssueWindowSettings"
            ALTER COLUMN "GCRecord" SET DEFAULT 0,
            ALTER COLUMN "GCRecord" SET NOT NULL,
            ALTER COLUMN "OptimisticLockField" SET DEFAULT 0,
            ALTER COLUMN "OptimisticLockField" SET NOT NULL;

          IF NOT EXISTS (
              SELECT 1 FROM "VisaIssueWindowSettings" WHERE COALESCE("GCRecord", 0) = 0
          ) THEN
            INSERT INTO "VisaIssueWindowSettings" (
              "ID", "WindowDays", "WarningDays", "GCRecord", "OptimisticLockField"
            ) VALUES (
              gen_random_uuid(), 45, 5, 0, 0
            );
          END IF;
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
