namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent rename Visas.InvitationItemID → IssuingInvitationItemID (Postgres + SQL Server).
/// Uses pg_catalog (not information_schema) so quoted mixed-case names are reliable.
/// </summary>
public static class VisaIssuingInvitationItemSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $migrate$
        DECLARE
          r RECORD;
          has_old boolean;
          has_new boolean;
        BEGIN
          IF to_regclass('public."Visas"') IS NULL THEN
            RETURN;
          END IF;

          SELECT EXISTS (
            SELECT 1
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public' AND c.relname = 'Visas'
              AND a.attnum > 0 AND NOT a.attisdropped
              AND a.attname = 'InvitationItemID'
          ) INTO has_old;

          SELECT EXISTS (
            SELECT 1
            FROM pg_attribute a
            JOIN pg_class c ON c.oid = a.attrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public' AND c.relname = 'Visas'
              AND a.attnum > 0 AND NOT a.attisdropped
              AND a.attname = 'IssuingInvitationItemID'
          ) INTO has_new;

          IF has_old AND NOT has_new THEN
            ALTER TABLE public."Visas" RENAME COLUMN "InvitationItemID" TO "IssuingInvitationItemID";
            has_new := true;
            has_old := false;
          END IF;

          IF NOT has_new THEN
            ALTER TABLE public."Visas" ADD COLUMN "IssuingInvitationItemID" uuid NULL;
          END IF;

          -- If both somehow exist, prefer keeping Issuing* and drop the old empty/legacy name after copying nulls only when new is all null.
          IF has_old AND has_new THEN
            UPDATE public."Visas" v
            SET "IssuingInvitationItemID" = v."InvitationItemID"
            WHERE v."IssuingInvitationItemID" IS NULL
              AND v."InvitationItemID" IS NOT NULL;
            ALTER TABLE public."Visas" DROP COLUMN "InvitationItemID";
          END IF;

          FOR r IN
            SELECT c.conname
            FROM pg_constraint c
            JOIN pg_class rel ON rel.oid = c.conrelid
            JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
            WHERE nsp.nspname = 'public'
              AND rel.relname = 'Visas'
              AND c.contype = 'f'
              AND c.conname ILIKE '%InvitationItemID%'
              AND c.conname NOT ILIKE '%IssuingInvitationItemID%'
          LOOP
            EXECUTE format(
              'ALTER TABLE public."Visas" RENAME CONSTRAINT %I TO %I',
              r.conname,
              replace(r.conname, 'InvitationItemID', 'IssuingInvitationItemID'));
          END LOOP;

          FOR r IN
            SELECT i.relname AS indexname
            FROM pg_index x
            JOIN pg_class i ON i.oid = x.indexrelid
            JOIN pg_class t ON t.oid = x.indrelid
            JOIN pg_namespace nsp ON nsp.oid = t.relnamespace
            WHERE nsp.nspname = 'public'
              AND t.relname = 'Visas'
              AND NOT x.indisprimary
              AND i.relname ILIKE '%InvitationItemID%'
              AND i.relname NOT ILIKE '%IssuingInvitationItemID%'
          LOOP
            EXECUTE format(
              'ALTER INDEX public.%I RENAME TO %I',
              r.indexname,
              replace(r.indexname, 'InvitationItemID', 'IssuingInvitationItemID'));
          END LOOP;
        END
        $migrate$;
        """;

    internal const string EnsureSchemaSqlServer = """
        IF OBJECT_ID(N'dbo.Visas', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.Visas', N'InvitationItemID') IS NOT NULL
               AND COL_LENGTH(N'dbo.Visas', N'IssuingInvitationItemID') IS NULL
                EXEC sp_rename N'dbo.Visas.InvitationItemID', N'IssuingInvitationItemID', N'COLUMN';

            IF COL_LENGTH(N'dbo.Visas', N'IssuingInvitationItemID') IS NULL
                ALTER TABLE dbo.Visas ADD IssuingInvitationItemID uniqueidentifier NULL;

            IF COL_LENGTH(N'dbo.Visas', N'InvitationItemID') IS NOT NULL
               AND COL_LENGTH(N'dbo.Visas', N'IssuingInvitationItemID') IS NOT NULL
            BEGIN
                EXEC(N'UPDATE dbo.Visas SET IssuingInvitationItemID = InvitationItemID WHERE IssuingInvitationItemID IS NULL AND InvitationItemID IS NOT NULL');
                ALTER TABLE dbo.Visas DROP COLUMN InvitationItemID;
            END
        END
        """;
}