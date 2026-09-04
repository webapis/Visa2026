using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for Visa.IssuingApplicationProfileInstance FK on Visas.
/// Drops legacy IssuingApplicationItemID after optional one-time backfill from ApplicationItems.
/// </summary>
public static class VisaIssuingApplicationProfileInstanceSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        DO $$
        DECLARE
          r RECORD;
        BEGIN
          IF to_regclass('public."Visas"') IS NULL THEN
            RETURN;
          END IF;

          ALTER TABLE "Visas" ADD COLUMN IF NOT EXISTS "IssuingApplicationProfileInstanceID" uuid NULL;

          IF NOT EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = 'IX_Visas_IssuingApplicationProfileInstanceID') THEN
            CREATE INDEX "IX_Visas_IssuingApplicationProfileInstanceID"
                ON "Visas" ("IssuingApplicationProfileInstanceID");
          END IF;

          IF to_regclass('public."ApplicationProfileInstances"') IS NOT NULL
             AND NOT EXISTS (
               SELECT 1 FROM pg_constraint
               WHERE conname = 'FK_Visas_Applications_IssuingApplicationProfileInstanceID') THEN
            ALTER TABLE "Visas"
                ADD CONSTRAINT "FK_Visas_Applications_IssuingApplicationProfileInstanceID"
                FOREIGN KEY ("IssuingApplicationProfileInstanceID") REFERENCES "ApplicationProfileInstances" ("ID");
          END IF;

          IF to_regclass('public."ApplicationItems"') IS NOT NULL
             AND EXISTS (
               SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'public' AND table_name = 'Visas'
                 AND column_name = 'IssuingApplicationItemID') THEN
            IF EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'public' AND table_name = 'ApplicationItems'
                AND column_name = 'ApplicationProfileInstanceID') THEN
              EXECUTE $u$
                UPDATE "Visas" v
                SET "IssuingApplicationProfileInstanceID" = ai."ApplicationProfileInstanceID"
                FROM "ApplicationItems" ai
                WHERE v."IssuingApplicationItemID" = ai."ID"
                  AND v."IssuingApplicationProfileInstanceID" IS NULL
                  AND ai."ApplicationProfileInstanceID" IS NOT NULL
              $u$;
            ELSIF EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'public' AND table_name = 'ApplicationItems'
                AND column_name = 'ApplicationID') THEN
              EXECUTE $u$
                UPDATE "Visas" v
                SET "IssuingApplicationProfileInstanceID" = ai."ApplicationID"
                FROM "ApplicationItems" ai
                WHERE v."IssuingApplicationItemID" = ai."ID"
                  AND v."IssuingApplicationProfileInstanceID" IS NULL
                  AND ai."ApplicationID" IS NOT NULL
              $u$;
            END IF;
          END IF;

          IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'Visas'
              AND column_name = 'IssuingApplicationItemID') THEN
            FOR r IN
              SELECT con.conname AS conname
              FROM pg_constraint con
              JOIN pg_class rel ON rel.oid = con.conrelid
              JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
              WHERE nsp.nspname = 'public'
                AND rel.relname = 'Visas'
                AND con.contype = 'f'
                AND pg_get_constraintdef(con.oid) LIKE '%IssuingApplicationItemID%'
            LOOP
              EXECUTE format('ALTER TABLE "Visas" DROP CONSTRAINT IF EXISTS %I', r.conname);
            END LOOP;

            -- Legacy vw_rd_* / workspace views still select the column; startup view heals recreate them.
            FOR r IN
              SELECT DISTINCT
                dep_ns.nspname AS schemaname,
                dep_view.relname AS viewname,
                dep_view.relkind AS relkind
              FROM pg_depend dep
              JOIN pg_rewrite rw ON rw.oid = dep.objid
              JOIN pg_class dep_view ON dep_view.oid = rw.ev_class
              JOIN pg_namespace dep_ns ON dep_ns.oid = dep_view.relnamespace
              JOIN pg_attribute att
                ON att.attrelid = dep.refobjid
               AND att.attnum = dep.refobjsubid
              WHERE dep.refobjid = 'public."Visas"'::regclass
                AND att.attname = 'IssuingApplicationItemID'
                AND dep_view.relkind IN ('v', 'm')
            LOOP
              IF r.relkind = 'm' THEN
                EXECUTE format('DROP MATERIALIZED VIEW IF EXISTS %I.%I CASCADE', r.schemaname, r.viewname);
              ELSE
                EXECUTE format('DROP VIEW IF EXISTS %I.%I CASCADE', r.schemaname, r.viewname);
              END IF;
            END LOOP;

            ALTER TABLE "Visas" DROP COLUMN IF EXISTS "IssuingApplicationItemID";
          END IF;
        END $$;
        """;

    internal const string EnsureSchemaSqlServer = """
        IF OBJECT_ID(N'dbo.Visas', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.Visas', N'IssuingApplicationProfileInstanceID') IS NULL
            ALTER TABLE dbo.Visas ADD IssuingApplicationProfileInstanceID uniqueidentifier NULL;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_Visas_IssuingApplicationProfileInstanceID'
              AND object_id = OBJECT_ID(N'dbo.Visas'))
            CREATE INDEX IX_Visas_IssuingApplicationProfileInstanceID ON dbo.Visas (IssuingApplicationProfileInstanceID);

        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM sys.foreign_keys
               WHERE name = N'FK_Visas_Applications_IssuingApplicationProfileInstanceID')
            ALTER TABLE dbo.Visas
                ADD CONSTRAINT FK_Visas_Applications_IssuingApplicationProfileInstanceID
                FOREIGN KEY (IssuingApplicationProfileInstanceID) REFERENCES dbo.ApplicationProfileInstances(ID);

        IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.Visas', N'IssuingApplicationItemID') IS NOT NULL
        BEGIN
            UPDATE v
            SET v.IssuingApplicationProfileInstanceID = ai.ApplicationProfileInstanceID
            FROM dbo.Visas v
            INNER JOIN dbo.ApplicationItems ai ON v.IssuingApplicationItemID = ai.ID
            WHERE v.IssuingApplicationProfileInstanceID IS NULL
              AND ai.ApplicationProfileInstanceID IS NOT NULL;
        END;

        IF COL_LENGTH(N'dbo.Visas', N'IssuingApplicationItemID') IS NOT NULL
        BEGIN
            DECLARE @fk sysname;
            SELECT TOP 1 @fk = fk.name
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
            WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Visas')
              AND c.name = N'IssuingApplicationItemID';
            IF @fk IS NOT NULL
                EXEC(N'ALTER TABLE dbo.Visas DROP CONSTRAINT [' + @fk + N']');
            ALTER TABLE dbo.Visas DROP COLUMN IssuingApplicationItemID;
        END;
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
            Execute(connection, EnsureSchemaPostgres);
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        Execute(sqlConnection, EnsureSchemaSqlServer);
    }

    private static void Execute(System.Data.Common.DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}