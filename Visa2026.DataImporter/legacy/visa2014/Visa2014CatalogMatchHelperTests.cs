using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014CatalogMatchHelperTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("  Aşgabat  ", "asgabat")]
    [InlineData("ÄÇŽŇÖŞÜÝ", "acznosuy")]
    [InlineData("äçžňöşüý", "acznosuy")]
    [InlineData("Café", "cafe")]
    public void NormalizeKey_FoldsTurkmenAndDiacritics(string? raw, string expected)
    {
        Assert.Equal(expected, Visa2014CatalogMatchHelper.NormalizeKey(raw));
    }

    [Fact]
    public void NormalizeKey_IsCaseInsensitiveAfterFold()
    {
        Assert.Equal(
            Visa2014CatalogMatchHelper.NormalizeKey("Şäher"),
            Visa2014CatalogMatchHelper.NormalizeKey("şäher"));
    }

    [Theory]
    [InlineData(null, "x", false)]
    [InlineData("x", null, false)]
    [InlineData("", "a", false)]
    [InlineData("  ", "a", false)]
    [InlineData("Aşgabat", "asgabat", true)]
    [InlineData("Ýok", "yok", true)]
    [InlineData("Turkmenistan", "türkmenistan", true)]
    public void KeysEqual_UsesNormalizedComparison(string? left, string? right, bool expected)
    {
        Assert.Equal(expected, Visa2014CatalogMatchHelper.KeysEqual(left, right));
    }

    [Fact]
    public void KeysEqual_RejectsWhitespaceOnlyPairs()
    {
        Assert.False(Visa2014CatalogMatchHelper.KeysEqual("   ", "   "));
    }
}
