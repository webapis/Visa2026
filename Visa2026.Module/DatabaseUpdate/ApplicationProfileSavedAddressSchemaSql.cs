using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationProfileSavedAddress"/>.
/// </summary>
/// <remarks>
/// <c>GCRecord</c> / <c>OptimisticLockField</c> must be NOT NULL DEFAULT 0.
/// Unique key is Field + TextKey only — suggestions are shared across all applications
/// (not per profile / project).
/// </remarks>
public static class ApplicationProfileSavedAddressSchemaSql
{
    public const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfiles"') IS NULL THEN
            RETURN;
          END IF;

          IF to_regclass('public."ApplicationProfileSavedAddresses"') IS NULL THEN
            CREATE TABLE "ApplicationProfileSavedAddresses" (
              "ID" uuid NOT NULL,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0,
              "ApplicationProfileId" uuid NOT NULL,
              "ProjectContractId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
              "Field" integer NOT NULL DEFAULT 1,
              "TextKey" character varying(450) NOT NULL DEFAULT '',
              "Text" text NULL,
              "UseCount" integer NOT NULL DEFAULT 1,
              "UsedAtUtc" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '0001-01-01 00:00:00',
              CONSTRAINT "PK_ApplicationProfileSavedAddresses" PRIMARY KEY ("ID"),
              CONSTRAINT "FK_ApplicationProfileSavedAddresses_ApplicationProfiles_ApplicationProfileId"
                  FOREIGN KEY ("ApplicationProfileId") REFERENCES "ApplicationProfiles" ("ID") ON DELETE CASCADE
            );
          END IF;

          IF to_regclass('public."ApplicationProfileSavedAddresses"') IS NULL THEN
            RETURN;
          END IF;

          UPDATE "ApplicationProfileSavedAddresses" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
          UPDATE "ApplicationProfileSavedAddresses" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;

          ALTER TABLE "ApplicationProfileSavedAddresses"
            ALTER COLUMN "GCRecord" SET DEFAULT 0,
            ALTER COLUMN "GCRecord" SET NOT NULL,
            ALTER COLUMN "OptimisticLockField" SET DEFAULT 0,
            ALTER COLUMN "OptimisticLockField" SET NOT NULL;

          -- Collapse legacy profile/project duplicates before Field+TextKey unique index.
          WITH ranked AS (
            SELECT "ID",
                   ROW_NUMBER() OVER (
                     PARTITION BY "Field", "TextKey"
                     ORDER BY "UseCount" DESC, "UsedAtUtc" DESC, "ID"
                   ) AS rn
            FROM "ApplicationProfileSavedAddresses"
            WHERE "GCRecord" = 0
          )
          UPDATE "ApplicationProfileSavedAddresses" s
          SET "GCRecord" = 1
          FROM ranked r
          WHERE s."ID" = r."ID" AND r.rn > 1;

          DROP INDEX IF EXISTS "IX_ApplicationProfileSavedAddresses_Profile_Project_Field_Text";
          DROP INDEX IF EXISTS "IX_ApplicationProfileSavedAddresses_Field_Text";
          CREATE UNIQUE INDEX "IX_ApplicationProfileSavedAddresses_Field_Text"
            ON "ApplicationProfileSavedAddresses" ("Field", "TextKey")
            WHERE "GCRecord" = 0;
        END $$;
        """;

    internal const string EnsureTableSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NULL
            RETURN;

        IF OBJECT_ID(N'dbo.ApplicationProfileSavedAddresses', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfileSavedAddresses (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfileSavedAddresses PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfileSavedAddresses_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfileSavedAddresses_OLF DEFAULT (0),
                ApplicationProfileId uniqueidentifier NOT NULL,
                ProjectContractId uniqueidentifier NOT NULL CONSTRAINT DF_ApplicationProfileSavedAddresses_Project DEFAULT ('00000000-0000-0000-0000-000000000000'),
                Field int NOT NULL CONSTRAINT DF_ApplicationProfileSavedAddresses_Field DEFAULT (1),
                TextKey nvarchar(450) NOT NULL CONSTRAINT DF_ApplicationProfileSavedAddresses_TextKey DEFAULT (N''),
                Text nvarchar(max) NULL,
                UseCount int NOT NULL CONSTRAINT DF_ApplicationProfileSavedAddresses_UseCount DEFAULT (1),
                UsedAtUtc datetime2 NOT NULL CONSTRAINT DF_ApplicationProfileSavedAddresses_UsedAt DEFAULT ('0001-01-01'),
                CONSTRAINT FK_ApplicationProfileSavedAddresses_ApplicationProfiles_ApplicationProfileId
                    FOREIGN KEY (ApplicationProfileId) REFERENCES dbo.ApplicationProfiles(ID) ON DELETE CASCADE
            );
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileSavedAddresses', N'U') IS NULL
            RETURN;

        UPDATE dbo.ApplicationProfileSavedAddresses SET GCRecord = 0 WHERE GCRecord IS NULL;
        UPDATE dbo.ApplicationProfileSavedAddresses SET OptimisticLockField = 0 WHERE OptimisticLockField IS NULL;

        IF EXISTS (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.ApplicationProfileSavedAddresses')
              AND name = N'GCRecord' AND is_nullable = 1)
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM sys.default_constraints dc
                JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ApplicationProfileSavedAddresses')
                  AND c.name = N'GCRecord')
                ALTER TABLE dbo.ApplicationProfileSavedAddresses
                    ADD CONSTRAINT DF_ApplicationProfileSavedAddresses_GCRecord DEFAULT (0) FOR GCRecord;
            ALTER TABLE dbo.ApplicationProfileSavedAddresses ALTER COLUMN GCRecord int NOT NULL;
        END;

        IF EXISTS (
            SELECT 1 FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.ApplicationProfileSavedAddresses')
              AND name = N'OptimisticLockField' AND is_nullable = 1)
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM sys.default_constraints dc
                JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ApplicationProfileSavedAddresses')
                  AND c.name = N'OptimisticLockField')
                ALTER TABLE dbo.ApplicationProfileSavedAddresses
                    ADD CONSTRAINT DF_ApplicationProfileSavedAddresses_OLF DEFAULT (0) FOR OptimisticLockField;
            ALTER TABLE dbo.ApplicationProfileSavedAddresses ALTER COLUMN OptimisticLockField int NOT NULL;
        END;

        ;WITH ranked AS (
            SELECT ID,
                   ROW_NUMBER() OVER (
                     PARTITION BY Field, TextKey
                     ORDER BY UseCount DESC, UsedAtUtc DESC, ID
                   ) AS rn
            FROM dbo.ApplicationProfileSavedAddresses
            WHERE GCRecord = 0
        )
        UPDATE s SET GCRecord = 1
        FROM dbo.ApplicationProfileSavedAddresses s
        INNER JOIN ranked r ON s.ID = r.ID
        WHERE r.rn > 1;

        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationProfileSavedAddresses_Profile_Project_Field_Text'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileSavedAddresses'))
            DROP INDEX IX_ApplicationProfileSavedAddresses_Profile_Project_Field_Text
                ON dbo.ApplicationProfileSavedAddresses;

        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_ApplicationProfileSavedAddresses_Field_Text'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileSavedAddresses'))
            DROP INDEX IX_ApplicationProfileSavedAddresses_Field_Text
                ON dbo.ApplicationProfileSavedAddresses;

        CREATE UNIQUE INDEX IX_ApplicationProfileSavedAddresses_Field_Text
            ON dbo.ApplicationProfileSavedAddresses (Field, TextKey)
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