using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Guards Postgres detection and EFCoreProvider stripping used by import + schema heal paths.
/// A false negative skips Postgres duplicate guards; a dirty connection string breaks Npgsql.
/// </summary>
public class DatabaseProviderDetectorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsPostgreSql_blank_is_false(string? connectionString) =>
        Assert.False(DatabaseProviderDetector.IsPostgreSql(connectionString));

    [Theory]
    [InlineData("Host=127.0.0.1;Database=visa2026_dev;Username=u;Password=p")]
    [InlineData("host=db;Database=x;Username=u;Password=p;EFCoreProvider=Postgres")]
    [InlineData("Server=ignored;Database=x;EFCoreProvider=PostgreSQL")]
    public void IsPostgreSql_accepts_host_or_efcore_provider_token(string connectionString) =>
        Assert.True(DatabaseProviderDetector.IsPostgreSql(connectionString));

    [Theory]
    [InlineData("Server=localhost\\SQLEXPRESS;Database=VISA2015;Trusted_Connection=True")]
    [InlineData("Data Source=.;Initial Catalog=VISA2015;Integrated Security=True")]
    [InlineData("Host=only-if-no-server;Server=sql;Database=x")]
    public void IsPostgreSql_rejects_sql_server_shaped_strings(string connectionString)
    {
        Assert.False(DatabaseProviderDetector.IsPostgreSql(connectionString));
        Assert.True(DatabaseProviderDetector.IsSqlServer(connectionString));
    }

    [Fact]
    public void StripEfCoreProvider_removes_token_and_collapses_separators()
    {
        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(
            "Host=127.0.0.1;Database=visa2026;EFCoreProvider=Postgres;Username=u");

        Assert.Equal("Host=127.0.0.1;Database=visa2026;Username=u", cleaned);
        Assert.DoesNotContain("EFCoreProvider", cleaned, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StripEfCoreProvider_leading_token_does_not_leave_leading_semicolon()
    {
        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(
            "EFCoreProvider=Postgres;Host=db;Database=visa2026");

        Assert.StartsWith("Host=", cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EFCoreProvider", cleaned, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StripEfCoreProvider_blank_passthrough(string? connectionString) =>
        Assert.Equal(connectionString, DatabaseProviderDetector.StripEfCoreProvider(connectionString!));

    [Fact]
    public void ConfigureEfCore_throws_for_non_postgres()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProviderDetector.ConfigureEfCore(
                options,
                "Server=localhost;Database=VISA2015;Trusted_Connection=True"));

        Assert.Contains("PostgreSQL only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
