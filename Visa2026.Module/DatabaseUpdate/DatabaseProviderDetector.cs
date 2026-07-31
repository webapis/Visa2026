using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// PostgreSQL connection-string detection and EF Core Npgsql wiring.
/// Visa2026 application databases are PostgreSQL only (legacy VISA2015 SQL Server is import-only).
/// </summary>
public static class DatabaseProviderDetector
{
    public const string EfCoreProviderPostgres = "Postgres";
    public const string EfCoreProviderPostgreSql = "PostgreSQL";
    public const string EfCoreProviderSqlServer = "SqlServer";

    public static bool IsPostgreSql(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        if (ContainsProvider(connectionString, EfCoreProviderPostgres)
            || ContainsProvider(connectionString, EfCoreProviderPostgreSql))
            return true;

        // Native Npgsql connection string (Host=...;Database=...) without Server=/Data Source=.
        if (Regex.IsMatch(connectionString, @"(?i)(^|;)\s*Host\s*=")
            && !Regex.IsMatch(connectionString, @"(?i)(^|;)\s*Initial Catalog\s*=")
            && !Regex.IsMatch(connectionString, @"(?i)(^|;)\s*Data Source\s*=")
            && !Regex.IsMatch(connectionString, @"(?i)(^|;)\s*Server\s*="))
            return true;

        return false;
    }

    public static bool IsPostgreSql(IObjectSpace objectSpace)
    {
        if (objectSpace is EFCoreObjectSpace efCore
            && efCore.DbContext?.Database?.ProviderName is { } providerName)
        {
            return providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>Always false for configured Visa2026 app DBs (Postgres-only). Kept for transitional call sites.</summary>
    public static bool IsSqlServer(string? connectionString) => !IsPostgreSql(connectionString);

    /// <summary>Always false when ObjectSpace is on Npgsql. Kept for transitional call sites.</summary>
    public static bool IsSqlServer(IObjectSpace objectSpace) => !IsPostgreSql(objectSpace);

    /// <summary>Removes XAF EFCoreProvider= token so Npgsql builders accept the string.</summary>
    public static string StripEfCoreProvider(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        return Regex.Replace(
                connectionString,
                @"(?i)(^|;)\s*EFCoreProvider\s*=\s*[^;]*",
                m => m.Value.StartsWith(';') ? ";" : string.Empty)
            .Trim()
            .TrimStart(';')
            .TrimEnd(';')
            .Replace(";;", ";");
    }

    public static void ConfigureEfCore(
        DbContextOptionsBuilder options,
        string connectionString,
        int commandTimeoutSeconds = 180)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!IsPostgreSql(connectionString))
        {
            throw new InvalidOperationException(
                "Visa2026 supports PostgreSQL only. Set DefaultConnection to an Npgsql connection string " +
                "(Host=...;Database=...;Username=...;Password=...;EFCoreProvider=Postgres). " +
                "SQL Server / LocalDB is not supported. Legacy VISA2015 remains SQL Server for import only.");
        }

        // XAF / legacy seeders often use DateTime.Kind=Unspecified; Npgsql 6+ rejects that by default.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var cleaned = StripEfCoreProvider(connectionString);
        options.UseNpgsql(cleaned, npgsql =>
        {
            npgsql.CommandTimeout(commandTimeoutSeconds);
        });
    }

    private static bool ContainsProvider(string connectionString, string provider) =>
        Regex.IsMatch(
            connectionString,
            $@"(?i)(^|;)\s*EFCoreProvider\s*=\s*{Regex.Escape(provider)}\s*(;|$)");
}