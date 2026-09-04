using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.Person.PreviousWorkplacesInTurkmenistan"/>.
/// Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).
/// </summary>
public static class PersonPreviousWorkplacesInTurkmenistanSchemaSql
{
    internal const string EnsureColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.People', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.People', N'PreviousWorkplacesInTurkmenistan') IS NULL
            ALTER TABLE dbo.People ADD PreviousWorkplacesInTurkmenistan nvarchar(500) NULL;
        """;

    internal const string EnsureColumnsPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."People"') IS NULL THEN
            RETURN;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'People' AND column_name = 'PreviousWorkplacesInTurkmenistan')
          THEN
            ALTER TABLE "People" ADD COLUMN "PreviousWorkplacesInTurkmenistan" character varying(500) NULL;
          END IF;
        END $$;
        """;

    /// <summary>Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).</summary>
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
            command.CommandText = EnsureColumnsPostgres;
            command.ExecuteNonQuery();
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = EnsureColumnsSqlServer;
        sqlCommand.ExecuteNonQuery();
    }
}