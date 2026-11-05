using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseVisaBorderZoneLocationCopyTests
{
    [Fact]
    public void IsEnabled_visa_only_with_effective_require_border_zone()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceVisa = true,
                ProduceBorderZone = false,
                RequireBorderZone = true,
            },
        };
        Assert.True(CaseVisaBorderZoneLocationCopy.IsEnabled(application));

        application.ApplicationProfile.ProduceBorderZone = true;
        Assert.False(CaseVisaBorderZoneLocationCopy.IsEnabled(application));
    }

    [Fact]
    public void ShouldFillFromCase_treats_yok_as_fillable_when_case_has_zone()
    {
        Assert.True(CaseVisaBorderZoneLocationCopy.ShouldFillFromCase("Ýok", "Serhetabat"));
        Assert.True(CaseVisaBorderZoneLocationCopy.ShouldFillFromCase(null, "Serhetabat"));
        Assert.False(CaseVisaBorderZoneLocationCopy.ShouldFillFromCase("Serhetabat", "Farap"));
    }

    [Fact]
    public void ResolveDisplaySummary_prefers_case_when_visa_is_yok()
    {
        Assert.Equal("Serhetabat", CaseVisaBorderZoneLocationCopy.ResolveDisplaySummary("Ýok", "Serhetabat"));
        Assert.Equal("Serhetabat", CaseVisaBorderZoneLocationCopy.ResolveDisplaySummary("Serhetabat", "Farap"));
    }

    [Fact]
    public void Matches_and_OfferTick_compare_trimmed_locations()
    {
        Assert.True(CaseVisaBorderZoneLocationCopy.Matches("Ahal", "Ahal"));
        Assert.False(CaseVisaBorderZoneLocationCopy.OfferTick("Ahal", "Ahal"));
        Assert.True(CaseVisaBorderZoneLocationCopy.OfferTick("Mary", "Ahal"));
        Assert.False(CaseVisaBorderZoneLocationCopy.OfferTick("  ", "Ahal"));
    }
}