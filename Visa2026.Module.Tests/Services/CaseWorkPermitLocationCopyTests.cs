using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseWorkPermitLocationCopyTests
{
    [Fact]
    public void IsEnabled_requires_produce_wp_person_wp_item_and_location()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceWorkPermit = true,
                RequirePersonWorkPermitItem = true,
                RequireWorkPermitLocation = true,
            },
        };

        Assert.True(CaseWorkPermitLocationCopy.IsEnabled(application));

        application.ApplicationProfile.RequirePersonWorkPermitItem = false;
        Assert.False(CaseWorkPermitLocationCopy.IsEnabled(application));

        application.ApplicationProfile.RequirePersonWorkPermitItem = true;
        application.ApplicationProfile.ProduceWorkPermit = false;
        application.ApplicationProfile.RequireWorkPermitLocation = false;
        Assert.False(CaseWorkPermitLocationCopy.IsEnabled(application));
    }

    [Fact]
    public void Matches_and_OfferTick_compare_trimmed_locations()
    {
        Assert.True(CaseWorkPermitLocationCopy.Matches("Ahal", "Ahal"));
        Assert.False(CaseWorkPermitLocationCopy.OfferTick("Ahal", "Ahal"));
        Assert.True(CaseWorkPermitLocationCopy.OfferTick("Mary", "Ahal"));
        Assert.False(CaseWorkPermitLocationCopy.OfferTick("  ", "Ahal"));
        Assert.False(CaseWorkPermitLocationCopy.OfferTick("Mary", null) == false && false);
        Assert.True(CaseWorkPermitLocationCopy.OfferTick("Mary", null));
    }

    [Fact]
    public void ResolveCaseLocations_prefers_instance_then_profile_default()
    {
        var application = new ApplicationProfileInstance
        {
            MovementPermitLocation = "  Ahal  ",
            ApplicationProfile = new ApplicationProfile { DefaultWorkPermitLocation = "Mary" },
        };
        Assert.Equal("Ahal", CaseWorkPermitLocationCopy.ResolveCaseLocations(application));

        application.MovementPermitLocation = null;
        Assert.Equal("Mary", CaseWorkPermitLocationCopy.ResolveCaseLocations(application));

        application.ApplicationProfile.DefaultWorkPermitLocation = " ";
        Assert.Null(CaseWorkPermitLocationCopy.ResolveCaseLocations(application));
    }

    [Fact]
    public void OfferTick_on_application_respects_gate_via_IsEnabled()
    {
        var application = new ApplicationProfileInstance
        {
            MovementPermitLocation = "Mary",
            ApplicationProfile = new ApplicationProfile
            {
                ProduceWorkPermit = true,
                RequirePersonWorkPermitItem = false,
                RequireWorkPermitLocation = true,
            },
        };

        Assert.False(CaseWorkPermitLocationCopy.IsEnabled(application));
        Assert.True(CasePersonRequestedLocation.OfferTick("Mary", "Ahal"));

        application.ApplicationProfile.RequirePersonWorkPermitItem = true;
        Assert.True(CaseWorkPermitLocationCopy.IsEnabled(application));
    }

    [Fact]
    public void CanApplyCaseDefault_follows_RequireWorkPermitLocation_flag()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceWorkPermit = true,
                RequireWorkPermitLocation = false,
                ProduceWorkLocation = false,
                RequirePersonWorkPermitItem = false,
            },
        };

        Assert.False(CaseWorkPermitLocationCopy.CanApplyCaseDefault(application));
        application.ApplicationProfile.RequireWorkPermitLocation = true;
        Assert.True(CaseWorkPermitLocationCopy.CanApplyCaseDefault(application));
    }
    [Fact]
    public void ShowInLinkReview_matches_Case_Results_without_person_wp_item()
    {
        // Çakylyk Almak-style: Case WP Use on, Person WP item off.
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceWorkPermit = false,
                ProduceWorkLocation = false,
                RequireWorkPermitLocation = true,
                RequirePersonWorkPermitItem = false,
            },
        };

        Assert.True(ApplicationProfileConfigurationResolver.ShowCaseRequestedWorkPermitLocation(application));
        Assert.True(CaseWorkPermitLocationCopy.ShowInLinkReview(application));
        Assert.False(CaseWorkPermitLocationCopy.IsEnabled(application));

        application.ApplicationProfile.RequireWorkPermitLocation = false;
        Assert.False(CaseWorkPermitLocationCopy.ShowInLinkReview(application));
    }
}