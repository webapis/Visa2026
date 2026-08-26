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
    public void HasMigrationSlaConfigured_UsesProfileDaysBeforeTypeProfile()
    {
        var type = new ApplicationType
        {
            MigrationSlaProfile = new ApplicationMigrationSlaProfile { MaxDaysInReview = 7 },
        };
        var profile = new ApplicationProfile { MigrationSlaDays = 21 };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile, ApplicationType = type };

        Assert.Equal(21, ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays(app));
        Assert.True(ApplicationProfileConfigurationResolver.HasMigrationSlaConfigured(app));
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
}
