using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent create for WorkPermitLocationGrant / Item / Document tables (Postgres + SQL Server).
/// </summary>
public static class WorkPermitLocationGrantSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."WorkPermitLocationGrants"') IS NULL
             AND to_regclass('public."ApplicationProfileInstances"') IS NOT NULL THEN
            CREATE TABLE "WorkPermitLocationGrants" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NULL DEFAULT 0,
                "OptimisticLockField" integer NULL DEFAULT 0,
                "GrantNumber" character varying(50) NOT NULL,
                "IssuedDate" timestamp without time zone NOT NULL,
                "ApplicationProfileInstanceID" uuid NOT NULL,
                "SourceWorkPermitID" uuid NULL,
                CONSTRAINT "PK_WorkPermitLocationGrants" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_WorkPermitLocationGrants_ApplicationProfileInstances_ApplicationProfileInstanceID"
                    FOREIGN KEY ("ApplicationProfileInstanceID") REFERENCES "ApplicationProfileInstances" ("ID") ON DELETE NO ACTION,
                CONSTRAINT "FK_WorkPermitLocationGrants_WorkPermits_SourceWorkPermitID"
                    FOREIGN KEY ("SourceWorkPermitID") REFERENCES "WorkPermits" ("ID") ON DELETE SET NULL
            );
            CREATE INDEX "IX_WorkPermitLocationGrants_ApplicationProfileInstanceID"
                ON "WorkPermitLocationGrants" ("ApplicationProfileInstanceID");
          END IF;

          IF to_regclass('public."WorkPermitLocationGrantItems"') IS NULL
             AND to_regclass('public."WorkPermitLocationGrants"') IS NOT NULL THEN
            CREATE TABLE "WorkPermitLocationGrantItems" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NULL DEFAULT 0,
                "OptimisticLockField" integer NULL DEFAULT 0,
                "GrantID" uuid NOT NULL,
                "PersonID" uuid NOT NULL,
                "PassportID" uuid NOT NULL,
                "GrantedLocations" character varying(500) NULL,
                "SourceWorkPermitItemID" uuid NULL,
                CONSTRAINT "PK_WorkPermitLocationGrantItems" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_WorkPermitLocationGrantItems_WorkPermitLocationGrants_GrantID"
                    FOREIGN KEY ("GrantID") REFERENCES "WorkPermitLocationGrants" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_WorkPermitLocationGrantItems_People_PersonID"
                    FOREIGN KEY ("PersonID") REFERENCES "People" ("ID") ON DELETE RESTRICT,
                CONSTRAINT "FK_WorkPermitLocationGrantItems_Passports_PassportID"
                    FOREIGN KEY ("PassportID") REFERENCES "Passports" ("ID") ON DELETE RESTRICT,
                CONSTRAINT "FK_WorkPermitLocationGrantItems_WorkPermitItems_SourceWorkPermitItemID"
                    FOREIGN KEY ("SourceWorkPermitItemID") REFERENCES "WorkPermitItems" ("ID") ON DELETE SET NULL
            );
            CREATE INDEX "IX_WorkPermitLocationGrantItems_GrantID"
                ON "WorkPermitLocationGrantItems" ("GrantID");
          END IF;

          IF to_regclass('public."WorkPermitLocationGrantDocuments"') IS NULL
             AND to_regclass('public."WorkPermitLocationGrants"') IS NOT NULL THEN
            CREATE TABLE "WorkPermitLocationGrantDocuments" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NULL DEFAULT 0,
                "OptimisticLockField" integer NULL DEFAULT 0,
                "GrantID" uuid NOT NULL,
                "FileID" uuid NULL,
                "Description" character varying(255) NULL,
                CONSTRAINT "PK_WorkPermitLocationGrantDocuments" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_WorkPermitLocationGrantDocuments_WorkPermitLocationGrants_GrantID"
                    FOREIGN KEY ("GrantID") REFERENCES "WorkPermitLocationGrants" ("ID") ON DELETE CASCADE
            );
            CREATE INDEX "IX_WorkPermitLocationGrantDocuments_GrantID"
                ON "WorkPermitLocationGrantDocuments" ("GrantID");
          END IF;
        END $$;
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
        }
    }
}