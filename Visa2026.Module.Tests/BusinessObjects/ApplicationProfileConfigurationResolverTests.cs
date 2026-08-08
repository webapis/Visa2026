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
            ApplicationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService,
        };
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
        };
        var app = new Application { ApplicationType = type, ApplicationProfile = profile };

        Assert.Equal(ApplicationProgressRouteKind.ViaMinistries,
            ApplicationProfileConfigurationResolver.GetProgressRoute(app));
    }

    [Fact]
    public void GetProgressRoute_CreationRouteWinsOverProfile()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
        };
        var app = new Application
        {
            ApplicationProfile = profile,
            CreationProgressRoute = ApplicationProgressRouteKind.DirectToMigrationService,
        };

        Assert.Equal(ApplicationProgressRouteKind.DirectToMigrationService,
            ApplicationProfileConfigurationResolver.GetProgressRoute(app));
    }

    [Fact]
    public void ShowVisaType_UsesProfileRequireFlag()
    {
        var profile = new ApplicationProfile { RequireVisaType = true };
        var type = new ApplicationType { ShowVisaType = false };
        var app = new Application { ApplicationProfile = profile, ApplicationType = type };

        Assert.True(ApplicationProfileConfigurationResolver.ShowVisaType(app));
    }

    [Fact]
    public void ShowVisaType_FallsBackToTypeWhenProfileMissing()
    {
        var type = new ApplicationType { ShowVisaType = true };
        var app = new Application { ApplicationType = type };

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
        var app = new Application { ApplicationProfile = profile, ApplicationType = type };

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
        var app = new Application { ApplicationProfile = profile, ApplicationType = type };

        Assert.Equal(21, ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays(app));
        Assert.True(ApplicationProfileConfigurationResolver.HasMigrationSlaConfigured(app));
    }

    [Fact]
    public void CanIssueVisa_UsesProfileProduceFlag()
    {
        var profile = new ApplicationProfile { ProduceVisa = true };
        var type = new ApplicationType { CanIssueVisa = false };
        var app = new Application { ApplicationProfile = profile, ApplicationType = type };

        Assert.True(ApplicationProfileConfigurationResolver.CanIssueVisa(app));
        Assert.True(ApplicationTypeCapabilities.CanIssueVisa(app));
    }

    [Fact]
    public void CanIssueInvitation_FallsBackToTypeWhenProfileMissing()
    {
        var type = new ApplicationType { CanIssueInvitation = true };
        var app = new Application { ApplicationType = type };

        Assert.True(ApplicationProfileConfigurationResolver.CanIssueInvitation(app));
    }

    [Fact]
    public void CanBeIssuingApplicationForVisa_ProfileInvitationOnly()
    {
        var profile = new ApplicationProfile { ProduceInvitation = true, ProduceVisa = false };
        var app = new Application { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.CanBeIssuingApplicationForVisa(app));
        Assert.False(ApplicationProfileConfigurationResolver.CanIssueVisa(app));
    }

    [Fact]
    public void GetMinistryLegCount_UsesEmbeddedProfileLegs()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ApprovalLegs = new ObservableCollection<ApplicationProfileApprovalLeg>
            {
                new() { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new() { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() },
            },
        };
        var app = new Application
        {
            ApplicationProfile = profile,
            ApplicationType = new ApplicationType
            {
                ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
                MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly,
            },
        };

        Assert.Equal(2, ApplicationProgressProfileResolver.GetMinistryLegCount(app));
    }

    [Fact]
    public void RequiresProjectContract_UsesProfileRequireProjectOnViaMinistriesRoute()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            RequireProject = true,
        };
        var app = new Application
        {
            ApplicationProfile = profile,
            ApplicationType = new ApplicationType { ShowProjectContract = false },
        };

        Assert.True(ApplicationProgressProfileResolver.RequiresProjectContract(app));
    }
}
