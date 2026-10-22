using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for <see cref="BusinessObjects.WorkPermitQuotaSettings"/>.
/// </summary>
/// <remarks>
/// No EF migrations: when ModuleInfo is already current, XAF skips the schema pass and a
/// newly added table is never created. Host-start <see cref="ApplyIfMissing"/> heals that.
/// <c>GCRecord</c> / <c>OptimisticLockField</c> must be NOT NULL DEFAULT 0 (same as other
/// BaseObject tables) — nullable columns break Npgsql after Save (InvalidCastException).
/// </remarks>
public static class WorkPermitQuotaSettingsSchemaSql
{
    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."WorkPermitQuotaSettings"') IS NULL THEN
            CREATE TABLE "WorkPermitQuotaSettings" (
              "ID" uuid NOT NULL CONSTRAINT "PK_WorkPermitQuotaSettings" PRIMARY KEY,
              "LocalPersonnelCount" integer NOT NULL DEFAULT 0,
              "ForeignRatio" integer NOT NULL DEFAULT 10,
              "LocalRatio" integer NOT NULL DEFAULT 90,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
          END IF;

          -- Heal early hand-created / nullable columns (Npgsql cannot read null GCRecord as int).
          UPDATE "WorkPermitQuotaSettings" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
          UPDATE "WorkPermitQuotaSettings" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;

          ALTER TABLE "WorkPermitQuotaSettings"
            ALTER COLUMN "GCRecord" SET DEFAULT 0,
            ALTER COLUMN "GCRecord" SET NOT NULL,
            ALTER COLUMN "OptimisticLockField" SET DEFAULT 0,
            ALTER COLUMN "OptimisticLockField" SET NOT NULL;
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