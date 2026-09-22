using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014WorkPermitLocationBitMatrixTests
{
    [Theory]
    [InlineData("AsgabatSeheri", "Aşgabat şäheri")]
    [InlineData("MaryEtraby", "Mary etraby")]
    [InlineData("Dasoguz", "Daşoguz")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void LabelHeuristic_FromColumnName_MapsKnownSuffixes(string? column, string? expected)
    {
        Assert.Equal(expected, Visa2014WorkPermitLocationLabelHeuristic.FromColumnName(column!));
    }

    [Fact]
    public void BuildWorkPermittedLocations_UsesCatalogThenHeuristicFallback()
    {
        var bitColumns = new[] { "MappedBit", "AsgabatSeheri", "UnknownBit", "OffBit" };
        var row = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["MappedBit"] = "1",
            ["AsgabatSeheri"] = "True",
            ["UnknownBit"] = "true",
            ["OffBit"] = "0",
        };
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal)
        {
            ["WorkPermittedLocationName"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "WorkPermittedLocationName",
                TargetMatchProperty = "NameTm",
                UnmappedPolicy = "flag",
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["MappedBit"] = "Catalog Label",
                },
            },
        };
        var unmapped = new List<string>();

        var result = Visa2014WorkPermitLocationBitMatrix.BuildWorkPermittedLocations(
            row,
            bitColumns,
            catalogs,
            unmapped);

        // Catalog hit, city-suffix heuristic, then bare-column heuristic (not unmapped).
        Assert.Equal("Catalog Label, Aşgabat şäheri, UnknownBit", result);
        Assert.Empty(unmapped);
    }

    [Fact]
    public void BuildWorkPermittedLocations_BlankColumnNameWithBitSet_CollectsUnmapped()
    {
        var row = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["   "] = "1",
        };
        var unmapped = new List<string>();

        var result = Visa2014WorkPermitLocationBitMatrix.BuildWorkPermittedLocations(
            row,
            ["   "],
            new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal),
            unmapped);

        Assert.Equal("", result);
        Assert.Contains("WorkPermittedLocationName:   ", unmapped);
    }

    [Fact]
    public void BuildWorkPermittedLocations_NullRowOrNoBits_ReturnsEmpty()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal);
        Assert.Equal(
            "",
            Visa2014WorkPermitLocationBitMatrix.BuildWorkPermittedLocations(
                null,
                ["Any"],
                catalogs,
                unmappedCollector: null));

        Assert.Equal(
            "",
            Visa2014WorkPermitLocationBitMatrix.BuildWorkPermittedLocations(
                new Dictionary<string, string?> { ["Any"] = "0" },
                ["Any"],
                catalogs,
                unmappedCollector: null));
    }

    [Fact]
    public void LoadLocationRows_EmptyOidList_ReturnsEmptyWithoutSql()
    {
        var map = Visa2014WorkPermitLocationBitMatrix.LoadLocationRows(
            "Server=unused",
            Array.Empty<Guid>(),
            verbose: false);

        Assert.Empty(map);
    }
}
