using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class LookupCatalogMatchHelperTests
{
    [Fact]
    public void ToLocalizationKey_LongProjectContractTitle_FitsMaxLength()
    {
        const string title =
            "GT-15 — (4 ylalaşyk: türkmenenergo > energetika > gurluşyk > energetika)";

        var key = LookupCatalogMatchHelper.ToLocalizationKey(title);

        Assert.True(key.Length <= LookupCatalogMatchHelper.LocalizationKeyMaxLength);
        Assert.Contains('_', key);
    }

    [Fact]
    public void ToLocalizationKey_ShortKey_Unchanged()
    {
        Assert.Equal("gt-15-yl2-te-en", LookupCatalogMatchHelper.ToLocalizationKey("GT-15-YL2-TE-EN"));
    }
}
