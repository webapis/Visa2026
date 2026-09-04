using System;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Greenfield host start: Configure() ObjectSpace gates run before CheckCompatibility.
/// </summary>
public static class PostgresRelationExists
{
    public static bool All(string? connectionString, params string[] tableNames)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || tableNames == null || tableNames.Length == 0)
            return false;

        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return true;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();
        foreach (var name in tableNames)
        {
            if (!Exists(connection, name))
                return false;
        }

        return true;
    }

    public static bool IsUndefinedTable(Exception ex) => HasSqlState(ex, "42P01");

    public static bool IsUndefinedColumn(Exception ex) => HasSqlState(ex, "42703");

    private static bool HasSqlState(Exception ex, string sqlState)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            if (current is PostgresException pg && pg.SqlState == sqlState)
                return true;
        }

        return false;
    }

    internal static bool Exists(NpgsqlConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT to_regclass(@qualified) IS NOT NULL;
            """;
        command.Parameters.AddWithValue("qualified", "public.\"" + tableName + "\"");
        var result = command.ExecuteScalar();
        return result is true || (result is bool b && b);
    }
}