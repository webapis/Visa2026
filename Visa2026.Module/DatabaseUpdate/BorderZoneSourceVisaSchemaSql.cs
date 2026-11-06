using System;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// BorderZone: SourceVisa FK + drop obsolete ValidityDurationID (Postgres).
/// </summary>
public static class BorderZoneSourceVisaSchemaSql
{
    internal const string EnsureSourceVisaIdPostgres =
        """ALTER TABLE "BorderZones" ADD COLUMN IF NOT EXISTS "SourceVisaID" uuid NULL;""";

    internal const string EnsureSourceVisaFkPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."BorderZones"') IS NULL OR to_regclass('public."Visas"') IS NULL THEN
            RETURN;
          END IF;
          IF NOT EXISTS (
            SELECT 1 FROM pg_constraint WHERE conname = 'FK_BorderZones_Visas_SourceVisaID'
          ) THEN
            ALTER TABLE "BorderZones"
              ADD CONSTRAINT "FK_BorderZones_Visas_SourceVisaID"
              FOREIGN KEY ("SourceVisaID") REFERENCES "Visas" ("ID") ON DELETE SET NULL;
          END IF;
          IF NOT EXISTS (
            SELECT 1 FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relname = 'IX_BorderZones_SourceVisaID' AND n.nspname = 'public'
          ) THEN
            CREATE INDEX "IX_BorderZones_SourceVisaID" ON "BorderZones" ("SourceVisaID");
          END IF;
        END $$;
        """;

    internal const string DropValidityDurationPostgres =
        """ALTER TABLE "BorderZones" DROP COLUMN IF EXISTS "ValidityDurationID";""";

    internal static readonly string[] EnsureColumnsPostgresStatements =
    [
        EnsureSourceVisaIdPostgres,
        EnsureSourceVisaFkPostgres,
        DropValidityDurationPostgres,
    ];

    /// <summary>Host-start heal when ModuleUpdater is skipped.</summary>
    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();
        foreach (var sql in EnsureColumnsPostgresStatements)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}