using System;
using System.IO;
using System.Reflection;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent PostgreSQL views for Application workspace read models.
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
        ExecuteEmbedded(connection, PersonViewResource);
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
        command.ExecuteNonQuery();
    }
}
