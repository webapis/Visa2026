using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL DDL for <see cref="BusinessObjects.ReportDashboardRosterSnapshot"/>.
/// EF/XAF maps the entity to the plural table name <c>ReportDashboardRosterSnapshots</c>.
/// </summary>
public static class ReportDashboardRosterSnapshotSchemaSql
{
    internal const string EnsureTablePostgres = """
        DO $$
        BEGIN
          -- Earlier heal used the singular name; EF queries the plural (BoStateSnapshots pattern).
          IF to_regclass('public."ReportDashboardRosterSnapshots"') IS NULL
             AND to_regclass('public."ReportDashboardRosterSnapshot"') IS NOT NULL THEN
            ALTER TABLE "ReportDashboardRosterSnapshot" RENAME TO "ReportDashboardRosterSnapshots";
            IF to_regclass('public."PK_ReportDashboardRosterSnapshot"') IS NOT NULL THEN
              ALTER TABLE "ReportDashboardRosterSnapshots"
                RENAME CONSTRAINT "PK_ReportDashboardRosterSnapshot" TO "PK_ReportDashboardRosterSnapshots";
            END IF;
            IF to_regclass('public."IX_ReportDashboardRosterSnapshot_Lookup"') IS NOT NULL THEN
              ALTER INDEX "IX_ReportDashboardRosterSnapshot_Lookup"
                RENAME TO "IX_ReportDashboardRosterSnapshots_Lookup";
            END IF;
          END IF;

          IF to_regclass('public."ReportDashboardRosterSnapshots"') IS NULL THEN
            CREATE TABLE "ReportDashboardRosterSnapshots" (
              "ID" uuid NOT NULL CONSTRAINT "PK_ReportDashboardRosterSnapshots" PRIMARY KEY,
              "CreatedOnUtc" timestamp with time zone NOT NULL DEFAULT now(),
              "AsOfDate" timestamp without time zone NULL,
              "CreatedByUserName" character varying(128) NULL,
              "Category" integer NOT NULL DEFAULT 0,
              "SubReportKey" character varying(128) NOT NULL DEFAULT '',
              "FilterFingerprint" character varying(512) NOT NULL DEFAULT '',
              "TotalCount" integer NOT NULL DEFAULT 0,
              "ExportId" uuid NULL,
              "FileName" character varying(260) NULL,
              "MembersJson" text NOT NULL DEFAULT '[]',
              "LastChangeJson" text NULL,
              "GCRecord" integer NOT NULL DEFAULT 0,
              "OptimisticLockField" integer NOT NULL DEFAULT 0
            );
            CREATE INDEX "IX_ReportDashboardRosterSnapshots_Lookup"
              ON "ReportDashboardRosterSnapshots" ("Category", "SubReportKey", "FilterFingerprint", "CreatedOnUtc" DESC);
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ReportDashboardRosterSnapshots'
              AND column_name = 'AsOfDate'
          ) THEN
            ALTER TABLE "ReportDashboardRosterSnapshots" ADD COLUMN "AsOfDate" timestamp without time zone NULL;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ReportDashboardRosterSnapshots'
              AND column_name = 'ExportId'
          ) THEN
            ALTER TABLE "ReportDashboardRosterSnapshots" ADD COLUMN "ExportId" uuid NULL;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ReportDashboardRosterSnapshots'
              AND column_name = 'FileName'
          ) THEN
            ALTER TABLE "ReportDashboardRosterSnapshots" ADD COLUMN "FileName" character varying(260) NULL;
          END IF;

          UPDATE "ReportDashboardRosterSnapshots" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
          UPDATE "ReportDashboardRosterSnapshots" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;
          UPDATE "ReportDashboardRosterSnapshots" SET "MembersJson" = '[]' WHERE "MembersJson" IS NULL;
          UPDATE "ReportDashboardRosterSnapshots" SET "SubReportKey" = '' WHERE "SubReportKey" IS NULL;
          UPDATE "ReportDashboardRosterSnapshots" SET "FilterFingerprint" = '' WHERE "FilterFingerprint" IS NULL;
          UPDATE "ReportDashboardRosterSnapshots" SET "ExportId" = gen_random_uuid() WHERE "ExportId" IS NULL;
          UPDATE "ReportDashboardRosterSnapshots" SET "AsOfDate" = ("CreatedOnUtc" AT TIME ZONE 'UTC')::date WHERE "AsOfDate" IS NULL;
          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ReportDashboardRosterSnapshots'
              AND column_name = 'LastChangeJson'
          ) THEN
            ALTER TABLE "ReportDashboardRosterSnapshots" ADD COLUMN "LastChangeJson" text NULL;
          END IF;

          ALTER TABLE "ReportDashboardRosterSnapshots"
            ALTER COLUMN "GCRecord" SET DEFAULT 0,
            ALTER COLUMN "GCRecord" SET NOT NULL,
            ALTER COLUMN "OptimisticLockField" SET DEFAULT 0,
            ALTER COLUMN "OptimisticLockField" SET NOT NULL,
            ALTER COLUMN "MembersJson" SET DEFAULT '[]',
            ALTER COLUMN "MembersJson" SET NOT NULL,
            ALTER COLUMN "SubReportKey" SET DEFAULT '',
            ALTER COLUMN "SubReportKey" SET NOT NULL,
            ALTER COLUMN "FilterFingerprint" SET DEFAULT '',
            ALTER COLUMN "FilterFingerprint" SET NOT NULL,
            ALTER COLUMN "TotalCount" SET DEFAULT 0,
            ALTER COLUMN "TotalCount" SET NOT NULL,
            ALTER COLUMN "Category" SET DEFAULT 0,
            ALTER COLUMN "Category" SET NOT NULL;
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

    /// <summary>
    /// Create/rename the snapshot table on first use when startup heal was skipped.
    /// </summary>
    public static void EnsureFromObjectSpace(IObjectSpace objectSpace)
    {
        if (!DatabaseProviderDetector.IsPostgreSql(objectSpace))
            return;
        if (objectSpace is not EFCoreObjectSpace efOs || efOs.DbContext is null)
            return;

        efOs.DbContext.Database.ExecuteSqlRaw(EnsureTablePostgres);
    }
}
