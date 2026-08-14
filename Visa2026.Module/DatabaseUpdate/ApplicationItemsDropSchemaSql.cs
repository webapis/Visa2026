using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Hard-remove Phase B: drop legacy ApplicationItems table after FK consumers cleared.</summary>
public static class ApplicationItemsDropSchemaSql
{
    internal const string DropPostgres = """
        DO $$
        DECLARE
          r RECORD;
        BEGIN
          IF to_regclass('public."ApplicationItems"') IS NULL THEN
            RETURN;
          END IF;

          -- Drop FKs referencing ApplicationItems from other tables.
          FOR r IN
            SELECT con.conname AS conname, rel.relname AS relname
            FROM pg_constraint con
            JOIN pg_class rel ON rel.oid = con.conrelid
            JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
            WHERE nsp.nspname = 'public'
              AND con.contype = 'f'
              AND con.confrelid = 'public."ApplicationItems"'::regclass
          LOOP
            EXECUTE format('ALTER TABLE %I DROP CONSTRAINT IF EXISTS %I', r.relname, r.conname);
          END LOOP;

          DROP TABLE IF EXISTS "ApplicationItems" CASCADE;
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
        command.CommandText = DropPostgres;
        command.ExecuteNonQuery();
    }
}