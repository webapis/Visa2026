using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressProfileResolverTests
{
    [Fact]
    public void GetMinistryReviewDepth_UsesProjectContract_WhenShowProjectContract()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry,
            ShowProjectContract = true
        };
        var contract = new ProjectContract { MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly };
        var app = new Application { ApplicationType = type, ProjectContract = contract };

        Assert.Equal(
            MinistryReviewDepth.FirstMinistryOnly,
            ApplicationProgressProfileResolver.GetMinistryReviewDepth(app));
    }

    [Fact]
    public void GetMinistryReviewDepth_FallsBackToApplicationType_WhenNoContract()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry,
            ShowProjectContract = true
        };
        var app = new Application { ApplicationType = type };

        Assert.Equal(
            MinistryReviewDepth.FirstAndSecondMinistry,
            ApplicationProgressProfileResolver.GetMinistryReviewDepth(app));
    }

    [Fact]
    public void GetMinistryReviewDepth_IgnoresContract_WhenShowProjectContractFalse()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly,
            ShowProjectContract = false
        };
        var contract = new ProjectContract { MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry };
        var app = new Application { ApplicationType = type, ProjectContract = contract };

        Assert.Equal(
            MinistryReviewDepth.FirstMinistryOnly,
            ApplicationProgressProfileResolver.GetMinistryReviewDepth(app));
    }

    [Fact]
    public void TryValidateProjectContractForProgress_AllowsInitialOfficePreparationWithoutContract()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new Application { ApplicationType = type };
        var progress = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.IsBeingPrepared },
            Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtOffice }
        };

        Assert.True(ApplicationProgressProfileResolver.TryValidateProjectContractForProgress(progress, null, out _));
    }

    [Fact]
    public void TryValidateProjectContractForProgress_BlocksSecondStepWithoutContract()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new Application { ApplicationType = type };
        var progress = new ApplicationProgress
        {
            Application = app,
            State = new ApplicationState { Code = ApplicationProgressStateCodes.Review1Started },
            Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMinistry1 }
        };

        Assert.False(ApplicationProgressProfileResolver.TryValidateProjectContractForProgress(progress, null, out var message));
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void WouldMinistryDepthChange_WhenContractDepthDiffers()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true,
            MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry
        };
        var app = new Application { ApplicationType = type };
        var one = new ProjectContract { MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly };
        var two = new ProjectContract { MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry };

        Assert.True(ApplicationProgressProfileResolver.WouldMinistryDepthChange(app, one, two));
        Assert.False(ApplicationProgressProfileResolver.WouldMinistryDepthChange(app, one, one));
    }
}
