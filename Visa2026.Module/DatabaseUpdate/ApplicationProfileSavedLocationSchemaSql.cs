using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationProfileSavedLocation"/>.
/// </summary>
public static class ApplicationProfileSavedLocationSchemaSql
{
    public const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfiles"') IS NULL THEN
            RETURN;
          END IF;

          IF to_regclass('public."ApplicationProfileSavedLocations"') IS NULL THEN
            CREATE TABLE "ApplicationProfileSavedLocations" (
              "ID" uuid NOT NULL,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0,
              "ApplicationProfileId" uuid NOT NULL,
              "Field" integer NOT NULL DEFAULT 1,
              "TextKey" character varying(450) NOT NULL DEFAULT '',
              "Text" character varying(100) NULL,
              "UseCount" integer NOT NULL DEFAULT 1,
              "UsedAtUtc" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '0001-01-01 00:00:00',
              "Source" integer NOT NULL DEFAULT 0,
              CONSTRAINT "PK_ApplicationProfileSavedLocations" PRIMARY KEY ("ID"),
              CONSTRAINT "FK_ApplicationProfileSavedLocations_ApplicationProfiles_ApplicationProfileId"
                  FOREIGN KEY ("ApplicationProfileId") REFERENCES "ApplicationProfiles" ("ID") ON DELETE CASCADE
            );
          END IF;

          IF to_regclass('public."ApplicationProfileSavedLocations"') IS NULL THEN
            RETURN;
          END IF;

          UPDATE "ApplicationProfileSavedLocations" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
          UPDATE "ApplicationProfileSavedLocations" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;

          ALTER TABLE "ApplicationProfileSavedLocations"
            ALTER COLUMN "GCRecord" SET DEFAULT 0,
            ALTER COLUMN "GCRecord" SET NOT NULL,
            ALTER COLUMN "OptimisticLockField" SET DEFAULT 0,
            ALTER COLUMN "OptimisticLockField" SET NOT NULL;

          ALTER TABLE "ApplicationProfileSavedLocations"
            ADD COLUMN IF NOT EXISTS "Source" integer NOT NULL DEFAULT 0;

          WITH ranked AS (
            SELECT "ID",
                   ROW_NUMBER() OVER (
                     PARTITION BY "Field", "TextKey"
                     ORDER BY "UseCount" DESC, "UsedAtUtc" DESC, "ID"
                   ) AS rn
            FROM "ApplicationProfileSavedLocations"
            WHERE "GCRecord" = 0
          )
          UPDATE "ApplicationProfileSavedLocations" s
          SET "GCRecord" = 1
          FROM ranked r
          WHERE s."ID" = r."ID" AND r.rn > 1;

          DROP INDEX IF EXISTS "IX_ApplicationProfileSavedLocations_Field_Text";
          CREATE UNIQUE INDEX "IX_ApplicationProfileSavedLocations_Field_Text"
            ON "ApplicationProfileSavedLocations" ("Field", "TextKey")
            WHERE "GCRecord" = 0;
        END $$;
        """;

    internal const string EnsureTableSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NULL
            RETURN;

        IF OBJECT_ID(N'dbo.ApplicationProfileSavedLocations', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfileSavedLocations (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfileSavedLocations PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfileSavedLocations_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfileSavedLocations_OLF DEFAULT (0),
                ApplicationProfileId uniqueidentifier NOT NULL,
                Field int NOT NULL CONSTRAINT DF_ApplicationProfileSavedLocations_Field DEFAULT (1),
                TextKey nvarchar(450) NOT NULL CONSTRAINT DF_ApplicationProfileSavedLocations_TextKey DEFAULT (N''),
                Text nvarchar(100) NULL,
                UseCount int NOT NULL CONSTRAINT DF_ApplicationProfileSavedLocations_UseCount DEFAULT (1),
                UsedAtUtc datetime2 NOT NULL CONSTRAINT DF_ApplicationProfileSavedLocations_UsedAt DEFAULT ('0001-01-01'),
                Source int NOT NULL CONSTRAINT DF_ApplicationProfileSavedLocations_Source DEFAULT (0),
                CONSTRAINT FK_ApplicationProfileSavedLocations_ApplicationProfiles_ApplicationProfileId
                    FOREIGN KEY (ApplicationProfileId) REFERENCES dbo.ApplicationProfiles(ID) ON DELETE CASCADE
            );
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileSavedLocations', N'U') IS NULL
            RETURN;

        UPDATE dbo.ApplicationProfileSavedLocations SET GCRecord = 0 WHERE GCRecord IS NULL;
        UPDATE dbo.ApplicationProfileSavedLocations SET OptimisticLockField = 0 WHERE OptimisticLockField IS NULL;

        IF COL_LENGTH(N'dbo.ApplicationProfileSavedLocations', N'Source') IS NULL
            ALTER TABLE dbo.ApplicationProfileSavedLocations
                ADD Source int NOT NULL CONSTRAINT DF_ApplicationProfileSavedLocations_Source DEFAULT (0);

        ;WITH ranked AS (
            SELECT ID,
                   ROW_NUMBER() OVER (
                     PARTITION BY Field, TextKey
                     ORDER BY UseCount DESC, UsedAtUtc DESC, ID
                   ) AS rn
            FROM dbo.ApplicationProfileSavedLocations
            WHERE GCRecord = 0
        )
        UPDATE s SET GCRecord = 1
        FROM dbo.ApplicationProfileSavedLocations s
        INNER JOIN ranked r ON s.ID = r.ID
        WHERE r.rn > 1;

        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationProfileSavedLocations_Field_Text'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileSavedLocations'))
            DROP INDEX IX_ApplicationProfileSavedLocations_Field_Text
                ON dbo.ApplicationProfileSavedLocations;

        CREATE UNIQUE INDEX IX_ApplicationProfileSavedLocations_Field_Text
            ON dbo.ApplicationProfileSavedLocations (Field, TextKey)
            WHERE [GCRecord] = 0;
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
            using var command = connection.CreateCommand();
            command.CommandText = EnsureSchemaPostgres;
            command.ExecuteNonQuery();
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = EnsureTableSqlServer;
        sqlCommand.ExecuteNonQuery();
    }
}