using System;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Converts <c>ApplicationProfileInstancePeople</c> from a BaseObject roster-line table
/// to a skip-navigation join (instanceId + personId only), and retargets ResolvedLinks
/// to (ApplicationProfileInstanceId, PersonId).
/// </summary>
public static class ApplicationProfileInstancePeopleSkipNavSchemaSql
{
    internal const string HealPostgres = """
        DO $$
        DECLARE
          has_join_id boolean;
          has_person_row_id boolean;
          has_instance_id boolean;
          has_person_id boolean;
          r RECORD;
        BEGIN
          IF to_regclass('public."ApplicationProfileInstancePersonResolvedLinks"') IS NOT NULL THEN
            SELECT EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'public'
                AND table_name = 'ApplicationProfileInstancePersonResolvedLinks'
                AND column_name = 'ApplicationProfileInstanceId'
            ) INTO has_instance_id;
            SELECT EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'public'
                AND table_name = 'ApplicationProfileInstancePersonResolvedLinks'
                AND column_name = 'PersonId'
            ) INTO has_person_id;
            SELECT EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'public'
                AND table_name = 'ApplicationProfileInstancePersonResolvedLinks'
                AND column_name = 'ApplicationProfileInstancePersonId'
            ) INTO has_person_row_id;

            IF NOT has_instance_id THEN
              ALTER TABLE "ApplicationProfileInstancePersonResolvedLinks"
                ADD COLUMN "ApplicationProfileInstanceId" uuid;
            END IF;
            IF NOT has_person_id THEN
              ALTER TABLE "ApplicationProfileInstancePersonResolvedLinks"
                ADD COLUMN "PersonId" uuid;
            END IF;

            IF has_person_row_id AND to_regclass('public."ApplicationProfileInstancePeople"') IS NOT NULL THEN
              SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'ApplicationProfileInstancePeople'
                  AND column_name = 'ID'
              ) INTO has_join_id;
              IF has_join_id THEN
                UPDATE "ApplicationProfileInstancePersonResolvedLinks" rl
                SET "ApplicationProfileInstanceId" = ap."ApplicationProfileInstanceId",
                    "PersonId" = ap."PersonId"
                FROM "ApplicationProfileInstancePeople" ap
                WHERE rl."ApplicationProfileInstancePersonId" = ap."ID"
                  AND (rl."ApplicationProfileInstanceId" IS NULL OR rl."PersonId" IS NULL);
              END IF;
            END IF;

            DELETE FROM "ApplicationProfileInstancePersonResolvedLinks"
            WHERE "ApplicationProfileInstanceId" IS NULL OR "PersonId" IS NULL;

            FOR r IN
              SELECT con.conname AS conname
              FROM pg_constraint con
              JOIN pg_class rel ON rel.oid = con.conrelid
              JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
              WHERE nsp.nspname = 'public'
                AND rel.relname = 'ApplicationProfileInstancePersonResolvedLinks'
                AND pg_get_constraintdef(con.oid) LIKE '%ApplicationProfileInstancePersonId%'
            LOOP
              EXECUTE format(
                'ALTER TABLE "ApplicationProfileInstancePersonResolvedLinks" DROP CONSTRAINT IF EXISTS %I',
                r.conname);
            END LOOP;

            FOR r IN
              SELECT idx.relname AS conname
              FROM pg_index i
              JOIN pg_class idx ON idx.oid = i.indexrelid
              JOIN pg_class tbl ON tbl.oid = i.indrelid
              JOIN pg_namespace nsp ON nsp.oid = tbl.relnamespace
              JOIN pg_attribute a ON a.attrelid = tbl.oid AND a.attnum = ANY (i.indkey)
              WHERE nsp.nspname = 'public'
                AND tbl.relname = 'ApplicationProfileInstancePersonResolvedLinks'
                AND a.attname = 'ApplicationProfileInstancePersonId'
                AND NOT i.indisprimary
            LOOP
              EXECUTE format('DROP INDEX IF EXISTS public.%I', r.conname);
            END LOOP;

            DROP INDEX IF EXISTS "IX_ApplicationProfileInstancePersonResolvedLinks_PersonRow_Kind";

            IF has_person_row_id THEN
              -- Views (vw_rd_*, workspace) still join the old roster-line FK; CASCADE drops them.
              -- Startup recreates views after this heal (ApplicationWorkspacePostgresViewsSql /
              -- ReportDashboardPostgresViewsHealSql).
              ALTER TABLE "ApplicationProfileInstancePersonResolvedLinks"
                DROP COLUMN IF EXISTS "ApplicationProfileInstancePersonId" CASCADE;
            END IF;

            ALTER TABLE "ApplicationProfileInstancePersonResolvedLinks"
              ALTER COLUMN "ApplicationProfileInstanceId" SET NOT NULL;
            ALTER TABLE "ApplicationProfileInstancePersonResolvedLinks"
              ALTER COLUMN "PersonId" SET NOT NULL;

            IF NOT EXISTS (
              SELECT 1 FROM pg_constraint
              WHERE conname = 'FK_ApplicationProfileInstancePersonResolvedLinks_ApplicationProfileInstances_ApplicationProfileInstanceId'
            ) THEN
              ALTER TABLE "ApplicationProfileInstancePersonResolvedLinks"
                ADD CONSTRAINT "FK_ApplicationProfileInstancePersonResolvedLinks_ApplicationProfileInstances_ApplicationProfileInstanceId"
                FOREIGN KEY ("ApplicationProfileInstanceId")
                REFERENCES "ApplicationProfileInstances" ("ID") ON DELETE CASCADE;
            END IF;

            IF NOT EXISTS (
              SELECT 1 FROM pg_constraint
              WHERE conname = 'FK_ApplicationProfileInstancePersonResolvedLinks_People_PersonId'
            ) THEN
              ALTER TABLE "ApplicationProfileInstancePersonResolvedLinks"
                ADD CONSTRAINT "FK_ApplicationProfileInstancePersonResolvedLinks_People_PersonId"
                FOREIGN KEY ("PersonId")
                REFERENCES "People" ("ID") ON DELETE RESTRICT;
            END IF;

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ApplicationProfileInstancePersonResolvedLinks_Instance_Person_Kind"
              ON "ApplicationProfileInstancePersonResolvedLinks"
              ("ApplicationProfileInstanceId", "PersonId", "LinkKind");
          END IF;

          IF to_regclass('public."ApplicationProfileInstancePeople"') IS NOT NULL THEN
            SELECT EXISTS (
              SELECT 1 FROM information_schema.columns
              WHERE table_schema = 'public'
                AND table_name = 'ApplicationProfileInstancePeople'
                AND column_name = 'ID'
            ) INTO has_join_id;

            IF has_join_id THEN
              DROP TABLE IF EXISTS "ApplicationProfileInstancePeople_skipnav";
              CREATE TABLE "ApplicationProfileInstancePeople_skipnav" (
                "ApplicationProfileInstanceId" uuid NOT NULL,
                "PersonId" uuid NOT NULL,
                CONSTRAINT "PK_ApplicationProfileInstancePeople" PRIMARY KEY
                  ("ApplicationProfileInstanceId", "PersonId"),
                CONSTRAINT "FK_ApplicationProfileInstancePeople_ApplicationProfileInstances_ApplicationProfileInstanceId"
                  FOREIGN KEY ("ApplicationProfileInstanceId")
                  REFERENCES "ApplicationProfileInstances" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_ApplicationProfileInstancePeople_People_PersonId"
                  FOREIGN KEY ("PersonId")
                  REFERENCES "People" ("ID") ON DELETE RESTRICT
              );
              INSERT INTO "ApplicationProfileInstancePeople_skipnav"
                ("ApplicationProfileInstanceId", "PersonId")
              SELECT DISTINCT "ApplicationProfileInstanceId", "PersonId"
              FROM "ApplicationProfileInstancePeople"
              WHERE COALESCE("GCRecord", 0) = 0
                AND "ApplicationProfileInstanceId" IS NOT NULL
                AND "PersonId" IS NOT NULL
              ON CONFLICT DO NOTHING;

              DROP TABLE "ApplicationProfileInstancePeople" CASCADE;
              ALTER TABLE "ApplicationProfileInstancePeople_skipnav"
                RENAME TO "ApplicationProfileInstancePeople";
            END IF;
          END IF;
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
        command.CommandText = HealPostgres;
        command.ExecuteNonQuery();
    }
}
