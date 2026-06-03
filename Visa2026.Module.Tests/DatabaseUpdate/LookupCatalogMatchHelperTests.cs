using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class LookupCatalogMatchHelperTests
{
    [Theory]
    [InlineData("Aýal", "Ayal")]
    [InlineData("Öýlenen", "Oylenen")]
    [InlineData("Erkek", "erkek")]
    [InlineData("Sallah", "SALLAH")]
    public void KeysEqual_matches_normalized_turkmen_titles(string left, string right) =>
        Assert.True(LookupCatalogMatchHelper.KeysEqual(left, right));

    [Fact]
    public void KeysEqual_distinguishes_different_catalog_keys() =>
        Assert.False(LookupCatalogMatchHelper.KeysEqual("Aýal", "Erkek"));
}
