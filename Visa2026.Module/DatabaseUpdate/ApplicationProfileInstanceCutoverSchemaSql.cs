using System;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Big-bang §13 cutover: rename Applications* → ApplicationProfileInstances* (same Guids),
/// rename child FK columns, drop leftover old tables.
/// </summary>
public static class ApplicationProfileInstanceCutoverSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $$
        DECLARE
          apps_count bigint;
          inst_count bigint;
        BEGIN
          -- Static SQL against "Applications" is planned even when the IF is false (42P01 after rename).
          IF to_regclass('public."Applications"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstances"') IS NULL THEN
            EXECUTE 'ALTER TABLE "Applications" RENAME TO "ApplicationProfileInstances"';
          ELSIF to_regclass('public."Applications"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstances"') IS NOT NULL THEN
            EXECUTE 'SELECT COUNT(*) FROM "ApplicationProfileInstances"' INTO inst_count;
            EXECUTE 'SELECT COUNT(*) FROM "Applications"' INTO apps_count;
            IF inst_count = 0 AND apps_count > 0 THEN
              EXECUTE 'INSERT INTO "ApplicationProfileInstances" SELECT * FROM "Applications"';
            END IF;
          END IF;

          IF to_regclass('public."ApplicationProgresses"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstanceProgresses"') IS NULL THEN
            EXECUTE 'ALTER TABLE "ApplicationProgresses" RENAME TO "ApplicationProfileInstanceProgresses"';
          END IF;

          IF to_regclass('public."ApplicationPeople"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstancePeople"') IS NULL THEN
            EXECUTE 'ALTER TABLE "ApplicationPeople" RENAME TO "ApplicationProfileInstancePeople"';
          END IF;

          IF to_regclass('public."ApplicationPersonResolvedLinks"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstancePersonResolvedLinks"') IS NULL THEN
            EXECUTE 'ALTER TABLE "ApplicationPersonResolvedLinks" RENAME TO "ApplicationProfileInstancePersonResolvedLinks"';
          END IF;

          IF to_regclass('public."ApplicationApprovalLegSnapshots"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstanceApprovalLegSnapshots"') IS NULL THEN
            EXECUTE 'ALTER TABLE "ApplicationApprovalLegSnapshots" RENAME TO "ApplicationProfileInstanceApprovalLegSnapshots"';
          END IF;
        END $$;
        """;

    internal const string RenameChildFkColumnsPostgres = """
        DO $$
        DECLARE
          r record;
        BEGIN
          FOR r IN
            SELECT * FROM (VALUES
              ('ApplicationProfileInstanceProgresses','ApplicationID','ApplicationProfileInstanceID'),
              ('ApplicationProfileInstanceProgresses','ApplicationId','ApplicationProfileInstanceId'),
              ('ApplicationProfileInstancePeople','ApplicationId','ApplicationProfileInstanceId'),
              ('ApplicationProfileInstancePersonResolvedLinks','ApplicationPersonId','ApplicationProfileInstancePersonId'),
              ('ApplicationProfileInstanceApprovalLegSnapshots','ApplicationId','ApplicationProfileInstanceId'),
              ('Invitations','ApplicationID','ApplicationProfileInstanceID'),
              ('WorkPermits','ApplicationID','ApplicationProfileInstanceID'),
              ('Rejections','ApplicationID','ApplicationProfileInstanceID'),
              ('BorderZones','ApplicationID','ApplicationProfileInstanceID'),
              ('WordReportGenerationBatches','ApplicationID','ApplicationProfileInstanceID'),
              ('ApplicationItems','ApplicationID','ApplicationProfileInstanceID'),
              ('ApplicationItems','ApplicationId','ApplicationProfileInstanceID'),
              ('VisaExtensionStatuses','ApplicationID','ApplicationProfileInstanceID'),
              ('VisaExtensionTrackings','ApplicationID','ApplicationProfileInstanceID'),
              ('VisaCancellationStatuses','ApplicationID','ApplicationProfileInstanceID'),
              ('VisaCancelExtStatuses','ApplicationID','ApplicationProfileInstanceID'),
              ('VisaTransferStatuses','ApplicationID','ApplicationProfileInstanceID'),
              ('WorkPermitExtensionStatuses','ApplicationID','ApplicationProfileInstanceID'),
              ('WorkPermitExtensionTrackings','ApplicationID','ApplicationProfileInstanceID'),
              ('Visas','IssuingApplicationID','IssuingApplicationProfileInstanceID'),
              -- Deprecated ApplicationType lookup: column follows the renamed progress BO.
              ('ApplicationTypes','ApplicationProgressRoute','ApplicationProfileInstanceProgressRoute'),
              ('ApplicationProfiles','CancelApplications','CancelApplicationProfileInstances'),
              -- Rename keeps the imported legacy PIA ids; the additive heal would leave them behind in the old column.
              ('Visas','LegacyPersonInApplicationOid','LegacyPersonInApplicationProfileInstanceOid')
            ) AS t(tbl, oldc, newc)
          LOOP
            IF to_regclass(format('public.%I', r.tbl)) IS NOT NULL
               AND EXISTS (
                 SELECT 1
                 FROM pg_attribute a
                 JOIN pg_class c ON c.oid = a.attrelid
                 JOIN pg_namespace n ON n.oid = c.relnamespace
                 WHERE n.nspname = 'public'
                   AND c.relname = r.tbl
                   AND a.attname = r.oldc
                   AND a.attnum > 0
                   AND NOT a.attisdropped)
               AND NOT EXISTS (
                 SELECT 1
                 FROM pg_attribute a
                 JOIN pg_class c ON c.oid = a.attrelid
                 JOIN pg_namespace n ON n.oid = c.relnamespace
                 WHERE n.nspname = 'public'
                   AND c.relname = r.tbl
                   AND a.attname = r.newc
                   AND a.attnum > 0
                   AND NOT a.attisdropped)
            THEN
              EXECUTE format('ALTER TABLE %I RENAME COLUMN %I TO %I', r.tbl, r.oldc, r.newc);
            END IF;
          END LOOP;
        END $$;
        """;

    internal const string DropOldTablesPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."Applications"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstances"') IS NOT NULL THEN
            EXECUTE 'DROP TABLE IF EXISTS "Applications" CASCADE';
          END IF;
          IF to_regclass('public."ApplicationProgresses"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstanceProgresses"') IS NOT NULL THEN
            EXECUTE 'DROP TABLE IF EXISTS "ApplicationProgresses" CASCADE';
          END IF;
          IF to_regclass('public."ApplicationPeople"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstancePeople"') IS NOT NULL THEN
            EXECUTE 'DROP TABLE IF EXISTS "ApplicationPeople" CASCADE';
          END IF;
          IF to_regclass('public."ApplicationPersonResolvedLinks"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstancePersonResolvedLinks"') IS NOT NULL THEN
            EXECUTE 'DROP TABLE IF EXISTS "ApplicationPersonResolvedLinks" CASCADE';
          END IF;
          IF to_regclass('public."ApplicationApprovalLegSnapshots"') IS NOT NULL
             AND to_regclass('public."ApplicationProfileInstanceApprovalLegSnapshots"') IS NOT NULL THEN
            EXECUTE 'DROP TABLE IF EXISTS "ApplicationApprovalLegSnapshots" CASCADE';
          END IF;
        END $$;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;
        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        using var conn = new NpgsqlConnection(cleaned);
        conn.Open();
        using (var cmd = new NpgsqlCommand(EnsureSchemaPostgres, conn))
            cmd.ExecuteNonQuery();
        using (var cmd = new NpgsqlCommand(RenameChildFkColumnsPostgres, conn))
            cmd.ExecuteNonQuery();
        using (var cmd = new NpgsqlCommand(DropOldTablesPostgres, conn))
            cmd.ExecuteNonQuery();
    }
}