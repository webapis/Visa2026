using System;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops retired <c>ApplicationMigrationSlaProfiles</c> and <c>ApplicationTypes.MigrationSlaProfileID</c>.
/// SLA days live on <c>ApplicationProfiles.MigrationSlaDays</c>.
/// </summary>
public static class ApplicationMigrationSlaProfileDropSchemaSql
{
    internal const string DropPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationMigrationSlaProfiles"') IS NOT NULL
             AND to_regclass('public."ApplicationProfiles"') IS NOT NULL
             AND EXISTS (
               SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'public'
                 AND table_name = 'ApplicationTypes'
                 AND column_name = 'MigrationSlaProfileID')
          THEN
            UPDATE "ApplicationProfiles" ap
            SET "MigrationSlaDays" = sla."MaxDaysInReview"
            FROM "ApplicationTypes" t
            JOIN "ApplicationMigrationSlaProfiles" sla ON sla."ID" = t."MigrationSlaProfileID"
            WHERE ap."MigrationSlaDays" = 0
              AND sla."MaxDaysInReview" > 0
              AND (
                (NULLIF(BTRIM(COALESCE(ap."SelectionCode", '')), '') IS NOT NULL
                 AND ap."SelectionCode" = t."SelectionCode")
                OR (NULLIF(BTRIM(COALESCE(ap."Code", '')), '') IS NOT NULL
                    AND ap."Code" = t."Name")
              );
          END IF;

          IF to_regclass('public."ApplicationTypes"') IS NOT NULL THEN
            ALTER TABLE "ApplicationTypes" DROP COLUMN IF EXISTS "MigrationSlaProfileID" CASCADE;
            ALTER TABLE "ApplicationTypes" DROP COLUMN IF EXISTS "MigrationSlaProfileId" CASCADE;
          END IF;

          DROP TABLE IF EXISTS "ApplicationMigrationSlaProfiles" CASCADE;
        END $$;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = DropPostgres;
        command.ExecuteNonQuery();
    }
}