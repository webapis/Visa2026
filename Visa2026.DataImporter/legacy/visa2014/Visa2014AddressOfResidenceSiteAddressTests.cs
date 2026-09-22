using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014AddressOfResidenceSiteAddressTests
{
    private static IReadOnlyDictionary<string, Visa2014LookupCatalog> RegionCatalog(params (string legacy, string target)[] pairs) =>
        new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal)
        {
            ["Region"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "Region",
                TargetMatchProperty = "Code",
                UnmappedPolicy = "block_row",
                LegacyToTarget = pairs.ToDictionary(p => p.legacy, p => p.target, StringComparer.Ordinal),
            },
        };

    [Fact]
    public void TryBuildLodgingSiteAddress_ResolvesRegionCityAndStripsPrefixes()
    {
        var catalogs = RegionCatalog(("AS", "AS"));

        var ok = Visa2014AddressOfResidenceTransform.TryBuildLodgingSiteAddress(
            addressLine: "Aşgabat şäheri Köpetdag etraby 12-nji jaý",
            regionMgCode: "AS",
            regionName: null,
            cityMgCode: "AS57",
            cityName: null,
            catalogs,
            out var fullAddress,
            out var regionNameTm,
            out var cityNameTm,
            out var unmappedReason);

        Assert.True(ok);
        Assert.Null(unmappedReason);
        Assert.Equal("Aşgabat şäheri", regionNameTm);
        Assert.Equal("Köpetdag etraby", cityNameTm);
        Assert.False(string.IsNullOrWhiteSpace(fullAddress));
        Assert.DoesNotContain("Aşgabat şäheri", fullAddress, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryBuildLodgingSiteAddress_UnknownRegion_Fails()
    {
        var catalogs = RegionCatalog(("AS", "AS"));

        var ok = Visa2014AddressOfResidenceTransform.TryBuildLodgingSiteAddress(
            addressLine: "Street 1",
            regionMgCode: "ZZ",
            regionName: null,
            cityMgCode: "AS57",
            cityName: null,
            catalogs,
            out _,
            out _,
            out _,
            out var unmappedReason);

        Assert.False(ok);
        Assert.StartsWith("Region:", unmappedReason);
    }

    [Fact]
    public void TryBuildLodgingSiteAddress_EmptyAddress_Fails()
    {
        var catalogs = RegionCatalog(("AS", "AS"));

        var ok = Visa2014AddressOfResidenceTransform.TryBuildLodgingSiteAddress(
            addressLine: "   ",
            regionMgCode: "AS",
            regionName: null,
            cityMgCode: "AS57",
            cityName: null,
            catalogs,
            out _,
            out _,
            out _,
            out var unmappedReason);

        Assert.False(ok);
        Assert.Equal("empty after strip", unmappedReason);
    }

    [Fact]
    public void TryBuildHotelSiteAddress_UsesHotelNameNormalizer()
    {
        var catalogs = RegionCatalog(("AS", "AS"));

        var ok = Visa2014AddressOfResidenceTransform.TryBuildHotelSiteAddress(
            addressLine: "Aşgabat şäheri Köpetdag etraby Grand Hotel",
            regionMgCode: "AS",
            regionName: null,
            cityMgCode: "AS57",
            cityName: null,
            catalogs,
            out var hotelName,
            out var regionNameTm,
            out var cityNameTm,
            out var unmappedReason);

        Assert.True(ok);
        Assert.Null(unmappedReason);
        Assert.Equal("Aşgabat şäheri", regionNameTm);
        Assert.Equal("Köpetdag etraby", cityNameTm);
        Assert.Contains("Grand Hotel", hotelName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryBuildHospitalSiteAddress_UsesHospitalNameNormalizer()
    {
        var catalogs = RegionCatalog(("AS", "AS"));

        var ok = Visa2014AddressOfResidenceTransform.TryBuildHospitalSiteAddress(
            addressLine: "Aşgabat şäheri Köpetdag etraby Merkezi hassahanasy",
            regionMgCode: "AS",
            regionName: null,
            cityMgCode: "AS57",
            cityName: null,
            catalogs,
            out var hospitalName,
            out _,
            out _,
            out var unmappedReason);

        Assert.True(ok);
        Assert.Null(unmappedReason);
        Assert.False(string.IsNullOrWhiteSpace(hospitalName));
    }

    [Fact]
    public void TryBuildOtherSiteAddress_DelegatesToLodgingBuilder()
    {
        var catalogs = RegionCatalog(("AS", "AS"));

        var ok = Visa2014AddressOfResidenceTransform.TryBuildOtherSiteAddress(
            addressLine: "Aşgabat şäheri Köpetdag etraby Other site 5",
            regionMgCode: "AS",
            regionName: null,
            cityMgCode: "AS57",
            cityName: null,
            catalogs,
            out var fullAddress,
            out _,
            out _,
            out var unmappedReason);

        Assert.True(ok);
        Assert.Null(unmappedReason);
        Assert.Contains("Other site 5", fullAddress, StringComparison.OrdinalIgnoreCase);
    }
}
