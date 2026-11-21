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

    [Fact]
    public void CanApplyCaseDefault_follows_RequireBorderZone_flag()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                RequireBorderZone = false,
                ProduceBorderZone = false,
            },
        };

        Assert.False(CaseBorderZoneLocationCopy.CanApplyCaseDefault(application));
        application.ApplicationProfile.RequireBorderZone = true;
        Assert.True(CaseBorderZoneLocationCopy.CanApplyCaseDefault(application));
    }
    [Fact]
    public void ShowInLinkReview_visa_path_matches_Case_RequireBorderZone_without_person_bz_item()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceVisa = true,
                ProduceBorderZone = false,
                RequireBorderZone = true,
                RequirePersonBorderZoneItem = false,
            },
        };

        Assert.True(ApplicationProfileConfigurationResolver.ShowCaseRequestedBorderZone(application));
        Assert.True(CaseVisaBorderZoneLocationCopy.ShowInLinkReview(application));
        Assert.False(CaseBorderZoneLocationCopy.ShowInLinkReview(application));

        application.ApplicationProfile.RequireBorderZone = false;
        Assert.False(CaseVisaBorderZoneLocationCopy.ShowInLinkReview(application));
    }

    [Fact]
    public void ShowInLinkReview_bz_permit_path_uses_Case_flag_not_person_tile()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceBorderZone = true,
                RequireBorderZone = true,
                RequirePersonBorderZoneItem = false,
            },
        };

        Assert.True(CaseBorderZoneLocationCopy.ShowInLinkReview(application));
        Assert.False(CaseVisaBorderZoneLocationCopy.ShowInLinkReview(application));
    }
}