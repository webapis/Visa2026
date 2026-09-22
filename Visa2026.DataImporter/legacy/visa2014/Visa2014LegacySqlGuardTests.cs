using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014LegacySqlGuardTests
{
    [Fact]
    public void EnsureLegacyReadCredentials_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(""));
    }

    [Fact]
    public void EnsureLegacyReadCredentials_PasswordEmbedded_DoesNotThrow()
    {
        Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(
            "Server=10.0.0.1;Database=VISA2015;User Id=ro;Password=secret;");
    }

    [Fact]
    public void EnsureLegacyReadCredentials_WindowsAuth_DoesNotThrow()
    {
        Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(
            "Server=10.0.0.1;Database=VISA2015;Integrated Security=true;");
    }

    [Fact]
    public void EnsureLegacyReadCredentials_SqlAuthWithoutPasswordEnv_Throws()
    {
        var previous = Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", null);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(
                    "Server=10.0.0.1;Database=VISA2015;User Id=ro;"));

            Assert.Contains("VISA2014_SQL_PASSWORD", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", previous);
        }
    }

    [Fact]
    public void EnsureLegacyReadCredentials_SqlAuthWithPasswordEnv_DoesNotThrow()
    {
        var previous = Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", "from-env");

            Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(
                "Server=10.0.0.1;Database=VISA2015;User Id=ro;");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", previous);
        }
    }

    [Fact]
    public void DescribeLegacyConnection_MasksAsServerDatabaseAuth_WithoutPassword()
    {
        var description = Visa2014LegacySqlGuard.DescribeLegacyConnection(
            "Server=10.100.128.15;Database=VISA2015;User Id=reader;Password=super-secret;",
            legacyDatabase: null);

        Assert.Equal("Server=10.100.128.15;Database=VISA2015;Auth=SQL", description);
        Assert.DoesNotContain("super-secret", description);
        Assert.DoesNotContain("Password", description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DescribeLegacyConnection_WindowsAuth_WhenNoUserId()
    {
        var description = Visa2014LegacySqlGuard.DescribeLegacyConnection(
            "Data Source=localhost;Initial Catalog=VISA2015;Integrated Security=true;",
            legacyDatabase: null);

        Assert.Equal("Server=localhost;Database=VISA2015;Auth=Windows", description);
    }
}
