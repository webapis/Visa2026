using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for picker shelves when ModuleUpdater is skipped.
/// Must run before any EF query that maps <c>ApplicationProfile.PickerShelfId</c>.
/// </summary>
/// <remarks>
/// <c>GCRecord</c> / <c>OptimisticLockField</c> must be NOT NULL DEFAULT 0 (same as other
/// BaseObject tables). XAF omits them on insert; a nullable column stores NULL and
/// Npgsql then throws InvalidCastException ("Column 'GCRecord' is null") on RETURNING.
/// </remarks>
public static class ApplicationProfilePickerShelfSchemaSql
{
    public const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfilePickerShelves"') IS NULL THEN
            CREATE TABLE "ApplicationProfilePickerShelves" (
              "ID" uuid NOT NULL CONSTRAINT "PK_ApplicationProfilePickerShelves" PRIMARY KEY,
              "Code" character varying(64) NULL,
              "Name" character varying(100) NULL,
              "SortOrder" integer NOT NULL DEFAULT 0,
              "IsSystem" boolean NOT NULL DEFAULT FALSE,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
          END IF;

          IF to_regclass('public."ApplicationProfilePickerShelves"') IS NOT NULL THEN
            -- Heal tables created with nullable GCRecord (Npgsql cannot read null as int).
            UPDATE "ApplicationProfilePickerShelves" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
            UPDATE "ApplicationProfilePickerShelves" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;

            ALTER TABLE "ApplicationProfilePickerShelves"
              ALTER COLUMN "GCRecord" SET DEFAULT 0,
              ALTER COLUMN "GCRecord" SET NOT NULL,
              ALTER COLUMN "OptimisticLockField" SET DEFAULT 0,
              ALTER COLUMN "OptimisticLockField" SET NOT NULL;

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ApplicationProfilePickerShelves_Code"
              ON "ApplicationProfilePickerShelves" ("Code")
              WHERE "Code" IS NOT NULL AND "Code" <> '' AND "GCRecord" IS NULL;
            CREATE INDEX IF NOT EXISTS "IX_ApplicationProfilePickerShelves_SortOrder"
              ON "ApplicationProfilePickerShelves" ("SortOrder");
          END IF;

          IF to_regclass('public."ApplicationProfiles"') IS NOT NULL THEN
            ALTER TABLE "ApplicationProfiles"
              ADD COLUMN IF NOT EXISTS "PickerShelfId" uuid NULL;
            CREATE INDEX IF NOT EXISTS "IX_ApplicationProfiles_PickerShelfId"
              ON "ApplicationProfiles" ("PickerShelfId");
            IF NOT EXISTS (
              SELECT 1 FROM pg_constraint
              WHERE conname = 'FK_ApplicationProfiles_PickerShelfId'
            ) THEN
              ALTER TABLE "ApplicationProfiles"
                ADD CONSTRAINT "FK_ApplicationProfiles_PickerShelfId"
                FOREIGN KEY ("PickerShelfId")
                REFERENCES "ApplicationProfilePickerShelves" ("ID")
                ON DELETE SET NULL;
            END IF;
          END IF;
        END $$;
        """;

    /// <summary>Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).</summary>
    public static void ApplyIfMissing(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || !DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        if (!PostgresRelationExists.All(connectionString, "ApplicationProfiles"))
            return;

        using var connection = new NpgsqlConnection(DatabaseProviderDetector.StripEfCoreProvider(connectionString));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = EnsureSchemaPostgres;
        command.ExecuteNonQuery();
    }
}