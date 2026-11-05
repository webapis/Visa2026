using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseVisaBorderZoneLocationCopyTests
{
    [Fact]
    public void IsEnabled_requires_produce_visa_without_border_zone_produce()
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

        application.ApplicationProfile.ProduceBorderZone = false;
        application.ApplicationProfile.ProduceVisa = false;
        Assert.False(CaseVisaBorderZoneLocationCopy.IsEnabled(application));
    }

    [Fact]
    public void ShouldFillFromCase_treats_yok_as_blank_when_case_has_zone()
    {
        Assert.True(CaseVisaBorderZoneLocationCopy.ShouldFillFromCase(null, "Serhetabat etraby"));
        Assert.True(CaseVisaBorderZoneLocationCopy.ShouldFillFromCase("Ýok", "Serhetabat etraby"));
        Assert.False(CaseVisaBorderZoneLocationCopy.ShouldFillFromCase("Ahal", "Serhetabat etraby"));
        Assert.False(CaseVisaBorderZoneLocationCopy.ShouldFillFromCase("Ýok", "Ýok"));
        Assert.False(CaseVisaBorderZoneLocationCopy.ShouldFillFromCase(null, null));
    }

    [Fact]
    public void ResolveDisplaySummary_prefers_case_when_visa_is_yok()
    {
        Assert.Equal(
            "Serhetabat etraby",
            CaseVisaBorderZoneLocationCopy.ResolveDisplaySummary("Ýok", "Serhetabat etraby"));
        Assert.Equal(
            "Ahal",
            CaseVisaBorderZoneLocationCopy.ResolveDisplaySummary("Ahal", "Serhetabat etraby"));
    }

    [Fact]
    public void Matches_and_OfferTick_compare_trimmed_locations()
    {
        var visa = new Visa { BorderZoneLocation = "Ahal" };
        Assert.True(CaseVisaBorderZoneLocationCopy.Matches(visa, "Ahal"));
        Assert.False(CaseVisaBorderZoneLocationCopy.OfferTick("Ahal", visa));
        Assert.True(CaseVisaBorderZoneLocationCopy.OfferTick("Mary", visa));
        Assert.False(CaseVisaBorderZoneLocationCopy.OfferTick("  ", visa));
    }

    [Fact]
    public void IsComplete_treats_yok_as_set_on_case()
    {
        Assert.True(CaseVisaBorderZoneLocationCopy.IsComplete("Ýok"));
        Assert.False(CaseVisaBorderZoneLocationCopy.IsComplete(null));
        Assert.False(CaseVisaBorderZoneLocationCopy.IsComplete(" "));
    }
}