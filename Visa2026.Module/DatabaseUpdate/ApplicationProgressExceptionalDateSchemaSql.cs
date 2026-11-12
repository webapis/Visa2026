using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationProfileInstanceProgress.ExceptionalDate"/>.
/// </summary>
public static class ApplicationProfileInstanceProgressExceptionalDateSchemaSql
{
    internal const string EnsureColumnPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileInstanceProgresses"') IS NULL THEN
            RETURN;
          END IF;

          ALTER TABLE "ApplicationProfileInstanceProgresses" ADD COLUMN IF NOT EXISTS "ExceptionalDate" boolean NOT NULL DEFAULT false;
        END $$;
        """;

    internal const string EnsureColumnSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'ExceptionalDate') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstanceProgresses ADD ExceptionalDate bit NOT NULL CONSTRAINT DF_ApplicationProfileInstanceProgresses_ExceptionalDate DEFAULT 0;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
            using var connection = new NpgsqlConnection(cleaned);
            connection.Open();
            Execute(connection, EnsureColumnPostgres);
            return;
        }

        using var sqlConnection = new SqlConnection(DatabaseProviderDetector.StripEfCoreProvider(connectionString));
        sqlConnection.Open();
        Execute(sqlConnection, EnsureColumnSqlServer);
    }

    private static void Execute(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
