using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public sealed class DatabaseProviderDetectorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsPostgreSql_Blank_ReturnsFalse(string? connectionString)
    {
        Assert.False(DatabaseProviderDetector.IsPostgreSql(connectionString));
    }

    [Theory]
    [InlineData("Host=localhost;Database=visa2026;Username=u;Password=p;EFCoreProvider=Postgres")]
    [InlineData("Host=db;Database=x;EFCoreProvider=PostgreSQL")]
    [InlineData("Host=10.0.0.1;Port=5432;Database=visa2026_demo;Username=visa;Password=secret")]
    public void IsPostgreSql_RecognizesProviderTokenOrNativeHostForm(string connectionString)
    {
        Assert.True(DatabaseProviderDetector.IsPostgreSql(connectionString));
        Assert.False(DatabaseProviderDetector.IsSqlServer(connectionString));
    }

    [Theory]
    [InlineData("Server=localhost;Database=VISA2015;Trusted_Connection=True")]
    [InlineData("Data Source=.\\SQLEXPRESS;Initial Catalog=VISA2015;Integrated Security=True")]
    [InlineData("Host=localhost;Initial Catalog=VISA2015;Username=sa;Password=x")] // Host but SQL catalog key
    [InlineData("Host=localhost;Data Source=legacy;Database=x")]
    [InlineData("Host=localhost;Server=sql;Database=x")]
    public void IsPostgreSql_RejectsSqlServerShapedStrings(string connectionString)
    {
        Assert.False(DatabaseProviderDetector.IsPostgreSql(connectionString));
        Assert.True(DatabaseProviderDetector.IsSqlServer(connectionString));
    }

    [Fact]
    public void StripEfCoreProvider_RemovesTokenAndCollapsesSeparators()
    {
        const string input = "Host=h;Database=d;EFCoreProvider=Postgres;Username=u";
        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(input);

        Assert.DoesNotContain("EFCoreProvider", cleaned, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Host=h", cleaned, StringComparison.Ordinal);
        Assert.Contains("Database=d", cleaned, StringComparison.Ordinal);
        Assert.Contains("Username=u", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain(";;", cleaned, StringComparison.Ordinal);
    }

    [Fact]
    public void StripEfCoreProvider_Blank_ReturnsInput()
    {
        Assert.Equal("  ", DatabaseProviderDetector.StripEfCoreProvider("  "));
        Assert.Null(DatabaseProviderDetector.StripEfCoreProvider(null!));
    }

    [Fact]
    public void ConfigureEfCore_NonPostgres_Throws()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseProviderDetector.ConfigureEfCore(
                options,
                "Server=localhost;Database=VISA2015;Trusted_Connection=True"));

        Assert.Contains("PostgreSQL only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
