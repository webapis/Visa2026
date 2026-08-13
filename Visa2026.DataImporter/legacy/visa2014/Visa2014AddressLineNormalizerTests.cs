using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014AddressLineNormalizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StripRegionAndCityPrefixes_Blank_ReturnsEmpty(string? line)
    {
        Assert.Equal(
            string.Empty,
            Visa2014AddressLineNormalizer.StripRegionAndCityPrefixes(line, "Ahal welaýaty", "Änew şäheri"));
    }

    [Fact]
    public void StripRegionAndCityPrefixes_RemovesRegionAndCityLabels()
    {
        var result = Visa2014AddressLineNormalizer.StripRegionAndCityPrefixes(
            "Ahal welaýaty, Änew şäheri, 5-nji köçe 12",
            "Ahal welaýaty",
            "Änew şäheri");

        Assert.DoesNotContain("welaýaty", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("şäheri", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("köçe", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizeMatchKey_FoldsTurkmenDiacritics()
    {
        Assert.Equal(
            Visa2014AddressLineNormalizer.NormalizeMatchKey("Şäher"),
            Visa2014AddressLineNormalizer.NormalizeMatchKey("Saher"));
        Assert.Equal(string.Empty, Visa2014AddressLineNormalizer.NormalizeMatchKey("  "));
    }

    [Fact]
    public void BuildCityScopedCatalogKey_RequiresBothParts()
    {
        Assert.Equal(string.Empty, Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey(null, "site"));
        Assert.Equal(string.Empty, Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey("Mary", null));
        Assert.Equal(string.Empty, Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey(" ", "site"));

        var key = Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey("Mary", "Çalyk üýji");
        Assert.Contains("|", key);
        Assert.StartsWith(Visa2014AddressLineNormalizer.NormalizeMatchKey("Mary"), key);
    }

    [Fact]
    public void BuildLodgingDedupeKey_EmptyWithoutCityOrScalar()
    {
        Assert.Equal(string.Empty, Visa2014AddressLineNormalizer.BuildLodgingDedupeKey(null, "üýji 1"));
        Assert.Equal(string.Empty, Visa2014AddressLineNormalizer.BuildLodgingDedupeKey("Mary", "   "));
    }

    [Fact]
    public void BuildLodgingDedupeKey_PrefersTrailingUyjiSegment()
    {
        var key = Visa2014AddressLineNormalizer.BuildLodgingDedupeKey(
            "Mary",
            "gündogar tarapynda ýerleşýän, Çalyk energiýa üýji");

        Assert.False(string.IsNullOrEmpty(key));
        Assert.Contains("|", key);
        // Typo folds + compact alphanumeric should keep uyji/energy signal.
        Assert.Contains("uyj", key.Split('|')[1], StringComparison.Ordinal);
    }

    [Fact]
    public void NormalizeHotelCatalogName_Blank_ReturnsEmpty()
    {
        Assert.Equal(
            string.Empty,
            Visa2014AddressLineNormalizer.NormalizeHotelCatalogName(" ", "Ahal welaýaty", "Änew şäheri"));
    }

    [Fact]
    public void NormalizeHospitalCatalogName_DelegatesToHotelNormalizer()
    {
        var hotel = Visa2014AddressLineNormalizer.NormalizeHotelCatalogName(
            "Ahal welaýaty, Änew şäheri, Merkezi hassahana",
            "Ahal welaýaty",
            "Änew şäheri");
        var hospital = Visa2014AddressLineNormalizer.NormalizeHospitalCatalogName(
            "Ahal welaýaty, Änew şäheri, Merkezi hassahana",
            "Ahal welaýaty",
            "Änew şäheri");

        Assert.Equal(hotel, hospital);
        Assert.DoesNotContain("welaýaty", hospital, StringComparison.OrdinalIgnoreCase);
    }
}
