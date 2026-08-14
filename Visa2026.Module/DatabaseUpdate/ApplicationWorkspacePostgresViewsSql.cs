using System;
using System.IO;
using System.Reflection;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL views for ApplicationProfileInstance workspace read models.
/// </summary>
public static class ApplicationWorkspacePostgresViewsSql
{
    private const string PersonViewResource =
        "Visa2026.Module.SqlViews.vw_application_workspace_person.postgres.sql";

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();

        // Configure() runs before CheckCompatibility on a greenfield DB (drop+create visa2026).
        // Skip until EF/XAF has created the skip-nav join; AddBuildStep heals views after that.
        if (!RelationExists(connection, "ApplicationProfileInstancePeople")
            || !RelationExists(connection, "People"))
            return;

        ExecuteEmbedded(connection, PersonViewResource);
    }

    private static bool RelationExists(NpgsqlConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT to_regclass(@qualified) IS NOT NULL;
            """;
        command.Parameters.AddWithValue("qualified", "public.\"" + tableName + "\"");
        var result = command.ExecuteScalar();
        return result is true || (result is bool b && b);
    }

    private static void ExecuteEmbedded(NpgsqlConnection connection, string resourceName)
    {
        var assembly = typeof(ApplicationWorkspacePostgresViewsSql).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return;

        using var reader = new StreamReader(stream);
        var sql = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(sql))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Application workspace SQL heal failed for " + resourceName + ".", ex);
        }
    }
}
