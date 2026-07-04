using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacySqlGuard
{
    /// <summary>
    /// Legacy VISA2015 is read-only. Fail fast when SQL auth is configured but password env is missing.
    /// </summary>
    public static void EnsureLegacyReadCredentials(string legacyConnectionString)
    {
        if (string.IsNullOrWhiteSpace(legacyConnectionString))
            throw new InvalidOperationException("Legacy connection string is empty.");

        if (legacyConnectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase))
            return;

        var usesSqlAuth = legacyConnectionString.Contains("User Id=", StringComparison.OrdinalIgnoreCase)
            || legacyConnectionString.Contains("UserID=", StringComparison.OrdinalIgnoreCase);
        if (!usesSqlAuth)
            return;

        var password = Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD");
        if (!string.IsNullOrWhiteSpace(password))
            return;

        throw new InvalidOperationException(
            "Legacy VISA2015 SQL login requires VISA2014_SQL_PASSWORD (read-only source). " +
            "Set the Windows user environment variable or run: $env:VISA2014_SQL_PASSWORD='...' " +
            "before --import-visa2014 / --import-visa2014-files. " +
            "Writes go to Visa2026 via OData only — VISA2015 is never modified.");
    }

    public static async Task EnsureLegacyConnectionAsync(string legacyConnectionString, CancellationToken cancellationToken = default)
    {
        EnsureLegacyReadCredentials(legacyConnectionString);

        await using var connection = new SqlConnection(legacyConnectionString);
        await connection.OpenAsync(cancellationToken);
    }

    public static string DescribeLegacyConnection(string legacyConnectionString, string? legacyDatabase = null)
    {
        var builder = new SqlConnectionStringBuilder(legacyConnectionString);
        var database = legacyDatabase ?? builder.InitialCatalog ?? "VISA2015";
        return $"Server={builder.DataSource};Database={database};Auth={(string.IsNullOrWhiteSpace(builder.UserID) ? "Windows" : "SQL")}";
    }
}
