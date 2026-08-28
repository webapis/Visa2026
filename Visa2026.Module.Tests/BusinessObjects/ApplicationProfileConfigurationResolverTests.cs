using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileConfigurationResolverTests
{
    [Fact]
    public void GetProgressRoute_PrefersApplicationProfileOverType()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var app = new ApplicationProfileInstance { ApplicationType = type, ApplicationProfile = profile };

        Assert.Equal(ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ApplicationProfileConfigurationResolver.GetProgressRoute(app));
    }

    [Fact]
    public void GetProgressRoute_CreationRouteWinsOverProfile()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            CreationProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

        Assert.Equal(ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            ApplicationProfileConfigurationResolver.GetProgressRoute(app));
    }

    [Fact]
    public void ShowVisaType_UsesProfileRequireFlag()
    {
        var profile = new ApplicationProfile { RequireVisaType = true };
        var type = new ApplicationType { ShowVisaType = false };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile, ApplicationType = type };

        Assert.True(ApplicationProfileConfigurationResolver.ShowVisaType(app));
    }

    [Fact]
    public void ShowEntryCheckPoint_UsesProfileRequireFlag()
    {
        var profile = new ApplicationProfile { RequireEntryCheckPoint = true };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.ShowEntryCheckPoint(app));
    }

    [Fact]
    public void ShowVisaType_FallsBackToTypeWhenProfileMissing()
    {
        var type = new ApplicationType { ShowVisaType = true };
        var app = new ApplicationProfileInstance { ApplicationType = type };

        Assert.True(ApplicationProfileConfigurationResolver.ShowVisaType(app));
    }

    [Fact]
    public void ShowRegistrations_UsesProfileActionFamily()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
        };
        var type = new ApplicationType { ShowRegistrations = false };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile, ApplicationType = type };

        Assert.True(ApplicationProfileConfigurationResolver.ShowRegistrations(app));
    }

    [Fact]
    public void HasMigrationSlaConfigured_UsesProfileDays()
    {
        var profile = new ApplicationProfile { MigrationSlaDays = 21 };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.Equal(21, ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays(app));
        Assert.True(ApplicationProfileConfigurationResolver.HasMigrationSlaConfigured(app));
    }

    [Fact]
    public void HasMigrationSlaConfigured_ZeroWhenNoProfileDays()
    {
        var app = new ApplicationProfileInstance { ApplicationType = new ApplicationType() };

        Assert.Equal(0, ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays(app));
        Assert.False(ApplicationProfileConfigurationResolver.HasMigrationSlaConfigured(app));
    }

    [Fact]
    public void GetMinistrySlaMaxDays_UsesProfileDays()
    {
        var profile = new ApplicationProfile { MinistrySlaDays = 14 };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.Equal(14, ApplicationProfileConfigurationResolver.GetMinistrySlaMaxDays(app));
        Assert.True(ApplicationProfileConfigurationResolver.HasMinistrySlaConfigured(app));
    }

    [Fact]
    public void GetMinistrySlaMaxDays_ZeroWhenNoProfile()
    {
        var app = new ApplicationProfileInstance();

        Assert.Equal(0, ApplicationProfileConfigurationResolver.GetMinistrySlaMaxDays(app));
    }

    [Fact]
    public void CanIssueVisa_UsesProfileProduceFlag()
    {
        var profile = new ApplicationProfile { ProduceVisa = true };
        var type = new ApplicationType { CanIssueVisa = false };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile, ApplicationType = type };

        Assert.True(ApplicationProfileConfigurationResolver.CanIssueVisa(app));
        Assert.True(ApplicationTypeCapabilities.CanIssueVisa(app));
    }

    [Fact]
    public void CanIssueInvitation_FallsBackToTypeWhenProfileMissing()
    {
        var type = new ApplicationType { CanIssueInvitation = true };
        var app = new ApplicationProfileInstance { ApplicationType = type };

        Assert.True(ApplicationProfileConfigurationResolver.CanIssueInvitation(app));
    }

    [Fact]
    public void CanBeIssuingApplicationProfileInstanceForVisa_ProfileInvitationOnly()
    {
        var profile = new ApplicationProfile { ProduceInvitation = true, ProduceVisa = false };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.CanBeIssuingApplicationProfileInstanceForVisa(app));
        Assert.False(ApplicationProfileConfigurationResolver.CanIssueVisa(app));
        Assert.False(ApplicationProfileConfigurationResolver.ShowIssuedVisas(app));
    }

    [Fact]
    public void ShowRejections_UsesProduceRejection_NotPersonRejectionItem()
    {
        var profile = new ApplicationProfile { ProduceRejection = true, RequirePersonRejectionItem = false };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.ShowRejections(app));
        Assert.False(ApplicationProfileConfigurationResolver.RequirePersonRejectionItem(app));
        Assert.True(ApplicationProfileConfigurationResolver.CanIssueRejection(app));
    }

    [Fact]
    public void RequirePersonTravelHistory_IsFalseForBusinessTripProfiles()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.BusinessTrip,
            RequirePersonTravelHistory = true,
        };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.False(ApplicationProfileConfigurationResolver.RequirePersonTravelHistory(app));
    }

    [Fact]
    public void GetMinistryLegCount_UsesEmbeddedProfileLegs()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ApprovalLegs = new ObservableCollection<ApplicationProfileApprovalLeg>
            {
                new() { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new() { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() },
            },
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationType = new ApplicationType
            {
                ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
                MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly,
            },
        };

        Assert.Equal(2, ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(app));
    }

    [Fact]
    public void RequiresProjectContract_UsesProfileRequireProjectOnViaMinistriesRoute()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            RequireProject = true,
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationType = new ApplicationType { ShowProjectContract = false },
        };

        Assert.True(ApplicationProfileInstanceProgressProfileResolver.RequiresProjectContract(app));
    }

    [Fact]
    public void RequireBorderZoneWhenProducingInvitationOrVisa_InvitationOrVisaForcesTrue()
    {
        Assert.True(ApplicationProfileConfigurationResolver.RequireBorderZoneWhenProducingInvitationOrVisa(
            produceInvitation: true, produceVisa: false, requireBorderZone: false));
        Assert.True(ApplicationProfileConfigurationResolver.RequireBorderZoneWhenProducingInvitationOrVisa(
            produceInvitation: false, produceVisa: true, requireBorderZone: false));
        Assert.False(ApplicationProfileConfigurationResolver.RequireBorderZoneWhenProducingInvitationOrVisa(
            produceInvitation: false, produceVisa: false, requireBorderZone: false));
        Assert.True(ApplicationProfileConfigurationResolver.RequireBorderZoneWhenProducingInvitationOrVisa(
            produceInvitation: false, produceVisa: false, requireBorderZone: true));
    }

    [Fact]
    public void ShowBorderZoneLocation_ProduceVisaShowsFieldEvenWhenRequireFlagOff()
    {
        var profile = new ApplicationProfile { ProduceVisa = true, RequireBorderZone = false };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.ShowBorderZoneLocation(app));
        Assert.False(ApplicationProfileConfigurationResolver.CanIssueBorderZone(app));
    }

    [Fact]
    public void RequireWorkPermitLocationWhenProducingWorkPermit_ForcesTrue()
    {
        Assert.True(ApplicationProfileConfigurationResolver.RequireWorkPermitLocationWhenProducingWorkPermit(
            produceWorkPermit: true, requireWorkPermitLocation: false));
        Assert.False(ApplicationProfileConfigurationResolver.RequireWorkPermitLocationWhenProducingWorkPermit(
            produceWorkPermit: false, requireWorkPermitLocation: false));
        Assert.True(ApplicationProfileConfigurationResolver.RequireWorkPermitLocationWhenProducingWorkPermit(
            produceWorkPermit: false, requireWorkPermitLocation: true));
    }

    [Fact]
    public void ShowMovementPermitLocation_ProduceWorkPermitShowsFieldEvenWhenRequireFlagOff()
    {
        var profile = new ApplicationProfile { ProduceWorkPermit = true, RequireWorkPermitLocation = false };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.ShowMovementPermitLocation(app));
        Assert.True(ApplicationProfileConfigurationResolver.ShowWorkPermittedLocations(app));
        Assert.True(ApplicationProfileConfigurationResolver.CanIssueWorkPermit(app));
    }

    [Fact]
    public void RequireProcessNumberWhenProducingIssuedDocuments_ForcesTrue()
    {
        Assert.True(ApplicationProfileConfigurationResolver.RequireProcessNumberWhenProducingIssuedDocuments(
            produceInvitation: true, produceWorkPermit: false, produceVisa: false, requireProcessNumber: false));
        Assert.True(ApplicationProfileConfigurationResolver.RequireProcessNumberWhenProducingIssuedDocuments(
            produceInvitation: false, produceWorkPermit: true, produceVisa: false, requireProcessNumber: false));
        Assert.True(ApplicationProfileConfigurationResolver.RequireProcessNumberWhenProducingIssuedDocuments(
            produceInvitation: false, produceWorkPermit: false, produceVisa: true, requireProcessNumber: false));
        Assert.False(ApplicationProfileConfigurationResolver.RequireProcessNumberWhenProducingIssuedDocuments(
            produceInvitation: false, produceWorkPermit: false, produceVisa: false, requireProcessNumber: false));
        Assert.True(ApplicationProfileConfigurationResolver.RequireProcessNumberWhenProducingIssuedDocuments(
            produceInvitation: false, produceWorkPermit: false, produceVisa: false, requireProcessNumber: true));
    }

    [Fact]
    public void ShowProcessNumber_ProduceVisaShowsFieldEvenWhenRequireFlagOff()
    {
        var profile = new ApplicationProfile { ProduceVisa = true, RequireProcessNumber = false };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.ShowProcessNumber(app));
        Assert.True(ApplicationProfileConfigurationResolver.ProcessNumberUseLocked(
            profile.ProduceInvitation, profile.ProduceWorkPermit, profile.ProduceVisa));
    }

    [Fact]
    public void ShowProcessNumber_OtherTemplatesCanOptIn()
    {
        var profile = new ApplicationProfile { ProduceBorderZone = true, RequireProcessNumber = true };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.ShowProcessNumber(app));
        Assert.False(ApplicationProfileConfigurationResolver.ProcessNumberUseLocked(
            profile.ProduceInvitation, profile.ProduceWorkPermit, profile.ProduceVisa));
    }
}
