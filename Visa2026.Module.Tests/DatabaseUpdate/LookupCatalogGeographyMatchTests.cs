using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Guards geography catalog match contracts used to heal City↔Region and site↔City links.
/// </summary>
public class LookupCatalogGeographyMatchTests
{
    [Theory]
    [InlineData("Ahal welaýaty", "Ahal welayaty")]
    [InlineData("Türkmenbaşy etraby", "Turkmenbasy etraby")]
    [InlineData("Aşgabat şäheri", "Asgabat saheri")]
    public void KeysEqual_FoldsTurkmenDiacritics_ForRegionAndCityTitles(string left, string right)
    {
        Assert.True(LookupCatalogMatchHelper.KeysEqual(left, right));
    }

    [Fact]
    public void NormalizeKey_StableForCatalogIdentity()
    {
        var a = LookupCatalogMatchHelper.NormalizeKey("Balkan welaýaty");
        var b = LookupCatalogMatchHelper.NormalizeKey("Balkan welayaty");
        Assert.Equal(a, b);
        Assert.False(string.IsNullOrEmpty(a));
    }
}