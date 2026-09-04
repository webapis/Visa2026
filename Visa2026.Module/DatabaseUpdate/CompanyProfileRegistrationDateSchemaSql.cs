using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.CompanyProfile.RegistrationDate"/>.
/// Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).
/// </summary>
public static class CompanyProfileRegistrationDateSchemaSql
{
    internal const string EnsureColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.CompanyProfiles', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.CompanyProfiles', N'RegistrationDate') IS NULL
            ALTER TABLE dbo.CompanyProfiles ADD RegistrationDate datetime2 NULL;
        """;

    internal const string EnsureColumnsPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."CompanyProfiles"') IS NULL THEN
            RETURN;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'CompanyProfiles' AND column_name = 'RegistrationDate')
          THEN
            ALTER TABLE "CompanyProfiles" ADD COLUMN "RegistrationDate" timestamp without time zone NULL;
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