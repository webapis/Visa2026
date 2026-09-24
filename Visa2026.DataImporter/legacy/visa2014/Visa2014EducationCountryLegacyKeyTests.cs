using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Guards ISO3 prefix extraction for Education Country.mgCode and name fallbacks
/// used when legacy dbo.Country.mgCode is blank.
/// </summary>
public sealed class Visa2014EducationCountryLegacyKeyTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("GBR", "GBR")]
    [InlineData("  GBR  ", "GBR")]
    [InlineData("GBR-WELIKOBRITANIYA", "GBR")]
    [InlineData("TUR-TURKIYE", "TUR")]
    [InlineData("-SUFFIXONLY", "-SUFFIXONLY")]
    public void NormalizeLegacyCountryMgCode_strips_iso3_prefix(string? raw, string? expected)
    {
        Assert.Equal(expected, Visa2014EducationTransform.NormalizeLegacyCountryMgCode(raw));
    }

    [Fact]
    public void ResolveEducationCountryLegacyKey_prefers_normalized_mgCode()
    {
        Assert.Equal(
            "GBR",
            Visa2014EducationTransform.ResolveEducationCountryLegacyKey(
                "GBR-WELIKOBRITANIYA",
                "United Kingdom",
                "Great Britain"));
    }

    [Fact]
    public void ResolveEducationCountryLegacyKey_falls_back_to_NameOfCountry_when_mgCode_blank()
    {
        Assert.Equal(
            "Türkiye",
            Visa2014EducationTransform.ResolveEducationCountryLegacyKey(
                "  ",
                "Türkiye",
                "Turkey"));
    }

    [Fact]
    public void ResolveEducationCountryLegacyKey_falls_back_to_NameOfCountryL_when_both_primary_blank()
    {
        Assert.Equal(
            "Turkey",
            Visa2014EducationTransform.ResolveEducationCountryLegacyKey(
                null,
                null,
                "Turkey"));
    }

    [Fact]
    public void ResolveEducationCountryLegacyKey_returns_null_when_all_blank()
    {
        Assert.Null(Visa2014EducationTransform.ResolveEducationCountryLegacyKey(null, "  ", ""));
    }
}
