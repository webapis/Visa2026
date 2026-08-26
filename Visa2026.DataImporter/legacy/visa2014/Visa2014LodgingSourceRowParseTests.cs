using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Covers shared Lojman/Patent/myhmanhana source-row parsing used by lodging, hotel, hospital, and other-site transforms.
/// </summary>
public sealed class Visa2014LodgingSourceRowParseTests
{
    [Fact]
    public void TryParseSourceRow_RejectsMissingOrBlankAddressLine()
    {
        Assert.False(Visa2014LodgingTransform.TryParseSourceRow(
            new Dictionary<string, string?>(StringComparer.Ordinal),
            out _));
        Assert.False(Visa2014LodgingTransform.TryParseSourceRow(
            new Dictionary<string, string?>(StringComparer.Ordinal) { ["AddressLine"] = "   " },
            out _));
    }

    [Fact]
    public void TryParseSourceRow_TrimsAddress_DefaultsUsageToOne_NullsBlankCodes()
    {
        var ok = Visa2014LodgingTransform.TryParseSourceRow(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AddressLine"] = "  Işçiler şäherçesi 12  ",
                ["RegionMgCode"] = "  ",
                ["RegionName"] = "Ahal",
                ["CityMgCode"] = " 05 ",
                ["CityName"] = "Änew",
                ["UsageCount"] = "not-a-number",
            },
            out var parsed);

        Assert.True(ok);
        Assert.Equal("Işçiler şäherçesi 12", parsed.AddressLine);
        Assert.Null(parsed.RegionMgCode);
        Assert.Equal("Ahal", parsed.RegionName);
        Assert.Equal("05", parsed.CityMgCode);
        Assert.Equal("Änew", parsed.CityName);
        Assert.Equal(1, parsed.UsageCount);
    }

    [Fact]
    public void TryParseSourceRow_ParsesPositiveUsageCount()
    {
        var ok = Visa2014LodgingTransform.TryParseSourceRow(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AddressLine"] = "Lojman A",
                ["UsageCount"] = " 7 ",
            },
            out var parsed);

        Assert.True(ok);
        Assert.Equal(7, parsed.UsageCount);
    }

    [Fact]
    public void TryParseSourceRow_IgnoresZeroOrNegativeUsageCount()
    {
        Assert.True(Visa2014LodgingTransform.TryParseSourceRow(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AddressLine"] = "Lojman B",
                ["UsageCount"] = "0",
            },
            out var zero));
        Assert.Equal(1, zero.UsageCount);

        Assert.True(Visa2014LodgingTransform.TryParseSourceRow(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AddressLine"] = "Lojman C",
                ["UsageCount"] = "-3",
            },
            out var negative));
        Assert.Equal(1, negative.UsageCount);
    }
}
