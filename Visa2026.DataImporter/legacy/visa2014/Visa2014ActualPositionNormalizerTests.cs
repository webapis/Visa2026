using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014ActualPositionNormalizerTests
{
    [Theory]
    [InlineData(null, "-")]
    [InlineData("", "-")]
    [InlineData("   ", "-")]
    [InlineData("-", "-")]
    [InlineData(".", "-")]
    [InlineData("..", "-")]
    [InlineData("617-", "-")]
    [InlineData("1902 -", "-")]
    [InlineData("1 216", "-")]
    [InlineData("209550-8-1-1226", "-")]
    [InlineData("1003-", "-")]
    [InlineData("Süpervizör", "Süpervizör")]
    [InlineData(" BORUCU EKİPBAŞI ", "BORUCU EKİPBAŞI")]
    [InlineData("Field Engineer", "Field Engineer")]
    public void Normalize_CollapsesNonTitles_KeepsRealTitles(string? raw, string expected)
    {
        Assert.Equal(expected, Visa2014ActualPositionNormalizer.Normalize(raw));
    }

    [Theory]
    [InlineData("617-", true)]
    [InlineData(".", true)]
    [InlineData("-", false)]
    [InlineData("Süpervizör", false)]
    public void IsNonTitlePlaceholder_MatchesRule(string? raw, bool expected)
    {
        Assert.Equal(expected, Visa2014ActualPositionNormalizer.IsNonTitlePlaceholder(raw));
    }
}