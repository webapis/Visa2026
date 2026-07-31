using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class PersonSearchTextNormalizerTests
{
    [Fact]
    public void SqlFoldMaps_HaveEqualLength()
    {
        Assert.Equal(
            PersonSearchTextNormalizer.SqlFoldFrom.Length,
            PersonSearchTextNormalizer.SqlFoldTo.Length);
    }

    [Theory]
    [InlineData("G\u00fcl", "gul")]
    [InlineData("\u00dcmit", "umit")]
    [InlineData("\u0130stanbul", "istanbul")]
    [InlineData("a\u011f\u0131r", "agir")]
    [InlineData("GUL", "gul")]
    public void Fold_StripsDiacriticsAndLowercases(string input, string expected)
    {
        Assert.Equal(expected, PersonSearchTextNormalizer.Fold(input));
    }

    [Fact]
    public void PersonSearchTokens_FoldsBeforeSplit()
    {
        var tokens = ReportDashboardCatalog.PersonSearchTokens("G\u00fcl  akku");
        Assert.Equal(new[] { "gul", "akku" }, tokens);
    }
}