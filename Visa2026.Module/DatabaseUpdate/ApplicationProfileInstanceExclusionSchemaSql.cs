using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for Seretmezlik (roster exclusion letters on an Application Profile instance).
/// </summary>
public static class ApplicationProfileInstanceExclusionSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileInstances"') IS NULL
             OR to_regclass('public."People"') IS NULL THEN
            RETURN;
          END IF;

          IF to_regclass('public."ApplicationProfileInstanceExclusions"') IS NULL THEN
            CREATE TABLE "ApplicationProfileInstanceExclusions" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationProfileInstanceId" uuid NOT NULL,
                "AppNumberPrefix" character varying(50) NULL,
                "Year" integer NOT NULL DEFAULT 0,
                "Month" integer NOT NULL DEFAULT 0,
                "SequenceNumber" character varying(50) NULL,
                "LetterNumber" character varying(100) NOT NULL DEFAULT '',
                "LetterDate" timestamp without time zone NOT NULL,
                "AddresseeKind" integer NOT NULL DEFAULT 0,
                "AddresseeLeg" integer NULL,
                "AddresseeName" character varying(500) NOT NULL DEFAULT '',
                "Salutation" character varying(300) NULL,
                "ReferenceMinistryName" character varying(300) NULL,
                "ReferenceLetterDate" timestamp without time zone NULL,
                "ReferenceLetterNumber" character varying(100) NULL,
                "OriginalRosterCount" integer NOT NULL DEFAULT 0,
                "Subject" character varying(700) NULL,
                "CreatedOnUtc" timestamp without time zone NULL,
                "CreatedByUserName" character varying(255) NULL,
                CONSTRAINT "PK_ApplicationProfileInstanceExclusions" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationProfileInstanceExclusions_ApplicationProfileInstances_ApplicationProfileInstanceId"
                    FOREIGN KEY ("ApplicationProfileInstanceId") REFERENCES "ApplicationProfileInstances" ("ID") ON DELETE CASCADE
            );
          END IF;

          CREATE INDEX IF NOT EXISTS "IX_ApplicationProfileInstanceExclusions_ApplicationProfileInstanceId"
            ON "ApplicationProfileInstanceExclusions" ("ApplicationProfileInstanceId");

          IF to_regclass('public."ApplicationProfileInstanceExclusionPeople"') IS NULL THEN
            CREATE TABLE "ApplicationProfileInstanceExclusionPeople" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ExclusionId" uuid NOT NULL,
                "PersonId" uuid NOT NULL,
                "Sequence" integer NOT NULL DEFAULT 0,
                "FullName" character varying(300) NOT NULL DEFAULT '',
                "PassportNumber" character varying(100) NULL,
                CONSTRAINT "PK_ApplicationProfileInstanceExclusionPeople" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationProfileInstanceExclusionPeople_ApplicationProfileInstanceExclusions_ExclusionId"
                    FOREIGN KEY ("ExclusionId") REFERENCES "ApplicationProfileInstanceExclusions" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_ApplicationProfileInstanceExclusionPeople_People_PersonId"
                    FOREIGN KEY ("PersonId") REFERENCES "People" ("ID") ON DELETE RESTRICT
            );
          END IF;

          CREATE INDEX IF NOT EXISTS "IX_ApplicationProfileInstanceExclusionPeople_ExclusionId"
            ON "ApplicationProfileInstanceExclusionPeople" ("ExclusionId");
          CREATE INDEX IF NOT EXISTS "IX_ApplicationProfileInstanceExclusionPeople_PersonId"
            ON "ApplicationProfileInstanceExclusionPeople" ("PersonId");

          IF to_regclass('public."ApplicationProfileInstanceExclusionTemplates"') IS NULL
             AND to_regclass('public."FileData"') IS NOT NULL THEN
            CREATE TABLE "ApplicationProfileInstanceExclusionTemplates" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "Kind" integer NOT NULL DEFAULT 0,
                "TemplateFileID" uuid NULL,
                "UpdatedOnUtc" timestamp without time zone NULL,
                "UpdatedByUserName" character varying(255) NULL,
                CONSTRAINT "PK_ApplicationProfileInstanceExclusionTemplates" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationProfileInstanceExclusionTemplates_FileData_TemplateFileID"
                    FOREIGN KEY ("TemplateFileID") REFERENCES "FileData" ("ID") ON DELETE NO ACTION
            );
          END IF;

          IF to_regclass('public."ApplicationProfileInstanceExclusionTemplates"') IS NOT NULL THEN
            CREATE INDEX IF NOT EXISTS "IX_ApplicationProfileInstanceExclusionTemplates_TemplateFileID"
              ON "ApplicationProfileInstanceExclusionTemplates" ("TemplateFileID");
          END IF;
        END $$;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)
            || !DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        using var connection = new NpgsqlConnection(DatabaseProviderDetector.StripEfCoreProvider(connectionString));
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = EnsureSchemaPostgres;
        command.ExecuteNonQuery();
    }
}
