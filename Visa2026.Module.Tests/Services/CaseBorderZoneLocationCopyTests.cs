using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseBorderZoneLocationCopyTests
{
    [Fact]
    public void IsEnabled_requires_person_bz_item_and_effective_border_zone()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceBorderZone = true,
                RequireBorderZone = true,
                RequirePersonBorderZoneItem = true,
            },
        };

        Assert.True(CaseBorderZoneLocationCopy.IsEnabled(application));

        application.ApplicationProfile.RequirePersonBorderZoneItem = false;
        Assert.False(CaseBorderZoneLocationCopy.IsEnabled(application));

        application.ApplicationProfile.RequirePersonBorderZoneItem = true;
        application.ApplicationProfile.ProduceBorderZone = false;
        application.ApplicationProfile.RequireBorderZone = false;
        application.ApplicationProfile.ProduceInvitation = false;
        application.ApplicationProfile.ProduceVisa = false;
        Assert.False(CaseBorderZoneLocationCopy.IsEnabled(application));
    }

    [Fact]
    public void ShowInLinkReview_follows_dual_gate()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceBorderZone = true,
                RequireBorderZone = true,
                RequirePersonBorderZoneItem = true,
            },
        };

        Assert.True(CaseBorderZoneLocationCopy.ShowInLinkReview(application));

        application.ApplicationProfile.RequirePersonBorderZoneItem = false;
        Assert.False(CaseBorderZoneLocationCopy.ShowInLinkReview(application));
    }

    [Fact]
    public void Matches_and_OfferTick_compare_trimmed_locations()
    {
        var item = new BorderZoneItem { BorderZoneLocation = "Ahal" };
        Assert.True(CaseBorderZoneLocationCopy.Matches(item, "Ahal"));
        Assert.False(CaseBorderZoneLocationCopy.OfferTick("Ahal", item));
        Assert.True(CaseBorderZoneLocationCopy.OfferTick("Mary", item));
        Assert.False(CaseBorderZoneLocationCopy.OfferTick("  ", item));
        Assert.False(CaseBorderZoneLocationCopy.OfferTick("Mary", null));
    }

    [Fact]
    public void IsComplete_treats_yok_sentinel_as_complete()
    {
        Assert.True(CaseBorderZoneLocationCopy.IsComplete("Ýok"));
        Assert.True(CaseBorderZoneLocationCopy.IsComplete("Ahal"));
        Assert.False(CaseBorderZoneLocationCopy.IsComplete(null));
        Assert.False(CaseBorderZoneLocationCopy.IsComplete("  "));
    }

    [Fact]
    public void ResolveCaseLocations_prefers_instance_then_profile_default()
    {
        var application = new ApplicationProfileInstance
        {
            BorderZoneLocation = "  Ahal  ",
            ApplicationProfile = new ApplicationProfile { DefaultBorderZoneLocation = "Mary" },
        };
        Assert.Equal("Ahal", CaseBorderZoneLocationCopy.ResolveCaseLocations(application));

        application.BorderZoneLocation = null;
        Assert.Equal("Mary", CaseBorderZoneLocationCopy.ResolveCaseLocations(application));

        application.ApplicationProfile.DefaultBorderZoneLocation = " ";
        Assert.Null(CaseBorderZoneLocationCopy.ResolveCaseLocations(application));
    }

    [Fact]
    public void OfferTick_on_application_respects_gate()
    {
        var item = new BorderZoneItem { BorderZoneLocation = "Ahal" };
        var application = new ApplicationProfileInstance
        {
            BorderZoneLocation = "Mary",
            ApplicationProfile = new ApplicationProfile
            {
                ProduceBorderZone = true,
                RequireBorderZone = true,
                RequirePersonBorderZoneItem = false,
            },
        };

        Assert.False(CaseBorderZoneLocationCopy.OfferTick(application, item));

        application.ApplicationProfile.RequirePersonBorderZoneItem = true;
        Assert.True(CaseBorderZoneLocationCopy.OfferTick(application, item));
    }
}