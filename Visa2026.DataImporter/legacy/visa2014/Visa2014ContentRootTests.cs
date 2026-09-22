using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014ContentRootTests
{
    [Theory]
    [InlineData("Encrypt=False", "Encrypt=Optional")]
    [InlineData("Encrypt=True", "Encrypt=Mandatory")]
    [InlineData("Encrypt=No", "Encrypt=Optional")]
    [InlineData("Encrypt=Yes", "Encrypt=Mandatory")]
    [InlineData("encrypt=false", "Encrypt=Optional")]
    public void NormalizeEncryptKeywords_RewritesLegacyEncryptValues(string inputFragment, string expectedFragment)
    {
        var input = $"Server=localhost;Database=VISA2015;{inputFragment};TrustServerCertificate=True";
        var normalized = Visa2014ContentRoot.NormalizeEncryptKeywords(input);

        Assert.Contains(expectedFragment, normalized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Encrypt=False", normalized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Encrypt=True", normalized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Encrypt=No", normalized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Encrypt=Yes", normalized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizeEncryptKeywords_Blank_ReturnsBlank()
    {
        Assert.Equal("", Visa2014ContentRoot.NormalizeEncryptKeywords(""));
        Assert.Equal("   ", Visa2014ContentRoot.NormalizeEncryptKeywords("   "));
    }

    [Fact]
    public void ApplySqlPasswordFromEnvironment_InjectsWhenUserIdWithoutPassword()
    {
        var previous = Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", "secret-from-env");
            var cs = "Server=localhost;Database=VISA2015;User Id=ReadOnlyUser;TrustServerCertificate=True";

            var applied = Visa2014ContentRoot.ApplySqlPasswordFromEnvironment(cs);

            Assert.Contains("Password=secret-from-env", applied, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", previous);
        }
    }

    [Fact]
    public void ApplySqlPasswordFromEnvironment_DoesNotOverrideExistingPassword()
    {
        var previous = Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", "should-not-appear");
            var cs = "Server=localhost;User Id=u;Password=already-set";

            var applied = Visa2014ContentRoot.ApplySqlPasswordFromEnvironment(cs);

            Assert.Equal(cs, applied);
            Assert.DoesNotContain("should-not-appear", applied, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", previous);
        }
    }

    [Fact]
    public void ResolveConnectionString_OverrideWinsOverEnvAndDefault()
    {
        var previousConn = Environment.GetEnvironmentVariable("VISA2014_SQL_CONNECTION");
        var previousPwd = Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD");
        try
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_CONNECTION", "Server=env-host;Database=ENV;");
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", null);

            var resolved = Visa2014ContentRoot.ResolveConnectionString(
                "Server=override;Database=OVR;Encrypt=False",
                sourceDefaultConnection: "Server=default;Database=DEF;");

            Assert.Contains("Server=override", resolved, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Encrypt=Optional", resolved, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("env-host", resolved, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VISA2014_SQL_CONNECTION", previousConn);
            Environment.SetEnvironmentVariable("VISA2014_SQL_PASSWORD", previousPwd);
        }
    }

    [Fact]
    public void PathHelpers_CombineUnderLegacyRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "visa2026-content-root-test");

        Assert.Equal(
            Path.Combine(root, "legacy", "visa2014"),
            Visa2014ContentRoot.LegacyRoot(root));
        Assert.Equal(
            Path.Combine(root, "legacy", "visa2014", "import-logs"),
            Visa2014ContentRoot.ImportLogsDirectory(root));
        Assert.Equal(
            Path.Combine(root, "legacy", "visa2014", "field-maps", "Person.yaml"),
            Visa2014ContentRoot.FieldMapPath(root, "Person"));
        Assert.Equal(
            Path.Combine(root, "legacy", "visa2014", "preview-export", "Visa-preview.xlsx"),
            Visa2014ContentRoot.DefaultPreviewOutputPath(root, "Visa"));
        Assert.Null(Visa2014ContentRoot.LookupTranslationsPath(null));
        Assert.EndsWith(
            Path.Combine("docs", "VISA2014_MIGRATION", "lookup-translations.yaml"),
            Visa2014ContentRoot.LookupTranslationsPath("/sol")!,
            StringComparison.Ordinal);
    }
}
