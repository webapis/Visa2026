using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Covers lodging/hotel/hospital/other-site catalog TransformRows: type filters, unmapped skips, and city-scoped dedupe.
/// </summary>
public sealed class Visa2014SiteCatalogTransformRowsTests
{
    private static IReadOnlyDictionary<string, Visa2014LookupCatalog> RegionCatalog(
        params (string legacy, string target)[] pairs) =>
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

    private static Visa2014LodgingSourceRow Row(
        string addressLine,
        int usage = 1,
        string regionMg = "AS",
        string cityMg = "AS57") =>
        new(
            AddressLine: addressLine,
            RegionMgCode: regionMg,
            RegionName: null,
            CityMgCode: cityMg,
            CityName: null,
            UsageCount: usage);

    [Fact]
    public void LodgingTransformRows_ImportsLodging_SkipsHotel_DedupesByCityKey()
    {
        var catalogs = RegionCatalog(("AS", "AS"));
        var rows = new[]
        {
            Row("Aşgabat şäheri Köpetdag etraby Lojman A", usage: 2),
            Row("Aşgabat şäheri Köpetdag etraby Lojman A", usage: 3),
            Row("Aşgabat şäheri Köpetdag etraby Grand Hotel myhmanhanasy", usage: 9),
            Row("Aşgabat şäheri Köpetdag etraby Merkezi hassahanasy", usage: 4),
        };

        var batch = Visa2014LodgingTransform.TransformRows(
            rows, catalogs, out var skipped, out _, out var dedupeSummary);

        Assert.Equal(2, batch.LegacyRowCount);
        Assert.Single(batch.ImportRows);
        Assert.Equal(1, batch.DedupeMergedCount);
        Assert.Single(dedupeSummary);
        Assert.Empty(skipped);
        Assert.Equal(5, batch.ImportRows[0]["UsageCount"]);
        Assert.Equal(2, batch.ImportRows[0]["_legacyVariantCount"]);
        Assert.Equal("import", batch.ImportRows[0]["_importAction"]);
        Assert.Equal("Aşgabat şäheri", batch.ImportRows[0]["Region"]);
        Assert.Equal("Köpetdag etraby", batch.ImportRows[0]["City"]);
    }

    [Fact]
    public void LodgingTransformRows_UnmappedRegion_GoesToSkipped()
    {
        var catalogs = RegionCatalog(("AS", "AS"));
        var rows = new[]
        {
            Row("Aşgabat şäheri Köpetdag etraby Lojman Z", regionMg: "ZZ"),
        };

        var batch = Visa2014LodgingTransform.TransformRows(
            rows, catalogs, out var skipped, out var unmapped, out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.StartsWith("Region:", skipped[0]["reason"]?.ToString());
        Assert.Contains(unmapped, u => Equals(u["catalog"], "Region"));
    }

    [Fact]
    public void HospitalTransformRows_OnlyHospitalLines_Dedupes()
    {
        var catalogs = RegionCatalog(("AS", "AS"));
        var rows = new[]
        {
            Row("Aşgabat şäheri Köpetdag etraby Merkezi hassahanasy", usage: 2),
            Row("Aşgabat şäheri Köpetdag etraby Merkezi hassahanasy", usage: 1),
            Row("Aşgabat şäheri Köpetdag etraby Grand Hotel myhmanhanasy", usage: 5),
            Row("Aşgabat şäheri Köpetdag etraby Lojman B", usage: 3),
        };

        var batch = Visa2014HospitalTransform.TransformRows(
            rows, catalogs, out var skipped, out _, out var dedupeSummary);

        Assert.Equal(2, batch.LegacyRowCount);
        Assert.Single(batch.ImportRows);
        Assert.Equal(1, batch.DedupeMergedCount);
        Assert.Single(dedupeSummary);
        Assert.Empty(skipped);
        Assert.Equal(3, batch.ImportRows[0]["UsageCount"]);
        Assert.Contains("hassahan", batch.ImportRows[0]["Name"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HotelTransformRows_SkipsHospital_DedupesHotel_UnmappedRegionSkipped()
    {
        var catalogs = RegionCatalog(("AS", "AS"));
        var rows = new[]
        {
            Row("Aşgabat şäheri Köpetdag etraby Grand Hotel myhmanhanasy", usage: 4),
            Row("Aşgabat şäheri Köpetdag etraby Grand Hotel myhmanhanasy", usage: 1),
            Row("Aşgabat şäheri Köpetdag etraby Merkezi hassahanasy", usage: 8),
            Row("Street without region map", usage: 2, regionMg: "ZZ"),
        };

        var batch = Visa2014HotelTransform.TransformRows(
            rows, catalogs, out var skipped, out var unmapped, out var dedupeSummary);

        // LegacyRowCount counts every non-hospital source row (including unmapped).
        Assert.Equal(3, batch.LegacyRowCount);
        Assert.Single(batch.ImportRows);
        Assert.Equal(1, batch.DedupeMergedCount);
        Assert.Single(dedupeSummary);
        Assert.Equal(5, batch.ImportRows[0]["UsageCount"]);
        Assert.Contains("Grand Hotel", batch.ImportRows[0]["Name"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Single(skipped);
        Assert.StartsWith("Region:", skipped[0]["reason"]?.ToString());
        Assert.Contains(unmapped, u => Equals(u["catalog"], "Region"));
    }

    [Fact]
    public void OtherSiteTransformRows_SkipsHotelAndLodging_ImportsPlainAddress()
    {
        var catalogs = RegionCatalog(("AS", "AS"));
        var rows = new[]
        {
            Row("Aşgabat şäheri Köpetdag etraby Other site 5", usage: 2),
            Row("Aşgabat şäheri Köpetdag etraby Other site 5", usage: 3),
            Row("Aşgabat şäheri Köpetdag etraby Lojman C", usage: 9),
            Row("Aşgabat şäheri Köpetdag etraby Grand Hotel myhmanhanasy", usage: 7),
        };

        var batch = Visa2014OtherSiteTransform.TransformRows(
            rows, catalogs, out var skipped, out _, out var dedupeSummary);

        Assert.Equal(2, batch.LegacyRowCount);
        Assert.Single(batch.ImportRows);
        Assert.Equal(1, batch.DedupeMergedCount);
        Assert.Single(dedupeSummary);
        Assert.Empty(skipped);
        Assert.Equal(5, batch.ImportRows[0]["UsageCount"]);
        Assert.Contains("Other site 5", batch.ImportRows[0]["FullAddress"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
