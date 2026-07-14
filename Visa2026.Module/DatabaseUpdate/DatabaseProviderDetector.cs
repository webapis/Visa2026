using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Detects SQL Server vs PostgreSQL for connection-string / ObjectSpace branching.
/// Demo PostgreSQL pilot skips T-SQL ModuleUpdaters; Prod/Staging stay on SQL Server.
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

    public static bool IsSqlServer(string? connectionString) => !IsPostgreSql(connectionString);

    public static bool IsSqlServer(IObjectSpace objectSpace) => !IsPostgreSql(objectSpace);

    /// <summary>Removes XAF EFCoreProvider= token so Npgsql/SqlClient builders accept the string.</summary>
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

        if (IsPostgreSql(connectionString))
        {
            // XAF / legacy seeders often use DateTime.Kind=Unspecified; Npgsql 6+ rejects that by default.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var cleaned = StripEfCoreProvider(connectionString);
            options.UseNpgsql(cleaned, npgsql =>
            {
                npgsql.CommandTimeout(commandTimeoutSeconds);
            });
            return;
        }

        var sqlCs = StripEfCoreProvider(connectionString);
        options.UseSqlServer(sqlCs, sql =>
        {
            sql.CommandTimeout(commandTimeoutSeconds);
            sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
    }

    private static bool ContainsProvider(string connectionString, string provider) =>
        Regex.IsMatch(
            connectionString,
            $@"(?i)(^|;)\s*EFCoreProvider\s*=\s*{Regex.Escape(provider)}\s*(;|$)");
}