using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014EducationCountryKeyTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("GBR", "GBR")]
    [InlineData("GBR-WELIKOBRITANIYA", "GBR")]
    [InlineData("TUR-Turkiye", "TUR")]
    public void NormalizeLegacyCountryMgCode_TakesIso3Prefix(string? mgCode, string? expected)
    {
        Assert.Equal(expected, Visa2014EducationTransform.NormalizeLegacyCountryMgCode(mgCode));
    }

    [Fact]
    public void ResolveEducationCountryLegacyKey_PrefersMgCodeThenNames()
    {
        Assert.Equal(
            "GBR",
            Visa2014EducationTransform.ResolveEducationCountryLegacyKey(
                "GBR-WELIKOBRITANIYA", "United Kingdom", "Beýik Britaniýa"));

        Assert.Equal(
            "United Kingdom",
            Visa2014EducationTransform.ResolveEducationCountryLegacyKey(
                null, "United Kingdom", "Beýik Britaniýa"));

        Assert.Equal(
            "Beýik Britaniýa",
            Visa2014EducationTransform.ResolveEducationCountryLegacyKey(
                "  ", null, "Beýik Britaniýa"));

        Assert.Null(
            Visa2014EducationTransform.ResolveEducationCountryLegacyKey(null, null, null));
    }
}
