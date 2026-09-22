using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

public sealed class LookupLocalizationDisplayTests
{
    [Fact]
    public void UsesLocalizedDisplay_Null_ReturnsFalse()
    {
        Assert.False(LookupLocalizationDisplay.UsesLocalizedDisplay(null));
    }

    [Fact]
    public void UsesLocalizedDisplay_GlobalLookupAndApplicationType_True()
    {
        Assert.True(LookupLocalizationDisplay.UsesLocalizedDisplay(typeof(Country)));
        Assert.True(LookupLocalizationDisplay.UsesLocalizedDisplay(typeof(MaritalStatus)));
        Assert.True(LookupLocalizationDisplay.UsesLocalizedDisplay(typeof(ApplicationType)));
    }

    [Fact]
    public void UsesLocalizedDisplay_NonLookupTypes_False()
    {
        Assert.False(LookupLocalizationDisplay.UsesLocalizedDisplay(typeof(Person)));
        Assert.False(LookupLocalizationDisplay.UsesLocalizedDisplay(typeof(string)));
        Assert.False(LookupLocalizationDisplay.UsesLocalizedDisplay(typeof(GlobalLookupCatalogBase)));
    }
}
