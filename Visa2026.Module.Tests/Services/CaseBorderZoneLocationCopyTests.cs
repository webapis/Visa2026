using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseBorderZoneLocationCopyTests
{
    [Fact]
    public void IsEnabled_requires_produce_bz_person_bz_and_effective_require()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceBorderZone = true,
                RequirePersonBorderZoneItem = true,
                RequireBorderZone = true,
            },
        };

        Assert.True(CaseBorderZoneLocationCopy.IsEnabled(application));

        application.ApplicationProfile.RequirePersonBorderZoneItem = false;
        Assert.False(CaseBorderZoneLocationCopy.IsEnabled(application));
    }

    [Fact]
    public void Matches_and_OfferTick_compare_trimmed_locations()
    {
        Assert.True(CaseBorderZoneLocationCopy.Matches("Ahal", "Ahal"));
        Assert.False(CaseBorderZoneLocationCopy.OfferTick("Ahal", "Ahal"));
        Assert.True(CaseBorderZoneLocationCopy.OfferTick("Mary", "Ahal"));
        Assert.False(CaseBorderZoneLocationCopy.OfferTick("  ", "Ahal"));
        Assert.True(CaseBorderZoneLocationCopy.OfferTick("Mary", null));
    }

    [Fact]
    public void ResolveCaseLocations_prefers_instance_then_profile_default()
    {
        var application = new ApplicationProfileInstance
        {
            BorderZoneLocation = "  Serhetabat  ",
            ApplicationProfile = new ApplicationProfile { DefaultBorderZoneLocation = "Farap" },
        };
        Assert.Equal("Serhetabat", CaseBorderZoneLocationCopy.ResolveCaseLocations(application));

        application.BorderZoneLocation = null;
        Assert.Equal("Farap", CaseBorderZoneLocationCopy.ResolveCaseLocations(application));
    }

    [Fact]
    public void OfferTick_on_application_respects_gate()
    {
        var application = new ApplicationProfileInstance
        {
            BorderZoneLocation = "Mary",
            ApplicationProfile = new ApplicationProfile
            {
                ProduceBorderZone = true,
                RequirePersonBorderZoneItem = false,
                RequireBorderZone = true,
            },
        };

        Assert.False(CaseBorderZoneLocationCopy.IsEnabled(application));

        application.ApplicationProfile.RequirePersonBorderZoneItem = true;
        Assert.True(CaseBorderZoneLocationCopy.IsEnabled(application));
        Assert.True(CaseBorderZoneLocationCopy.OfferTick("Mary", "Ahal"));
    }
}