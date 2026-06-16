using System.Linq;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressProfileResolverTests
{
    [Fact]
    public void GetMinistryLegCount_UsesProjectContractLegs()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var contract = new ProjectContract
        {
            MinistryLegs =
            [
                new ProjectContractMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };
        var app = new Application { ApplicationType = type, ProjectContract = contract };

        Assert.Equal(1, ApplicationProgressProfileResolver.GetMinistryLegCount(app));
        Assert.Equal(
            MinistryReviewDepth.FirstMinistryOnly,
            ApplicationProgressProfileResolver.GetMinistryReviewDepth(app));
    }

    [Fact]
    public void GetMinistryLegCount_UsesThreeLegContract()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var contract = new ProjectContract
        {
            MinistryLegs =
            [
                new ProjectContractMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new ProjectContractMinistryLeg { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() },
                new ProjectContractMinistryLeg { Sequence = 3, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };
        var app = new Application { ApplicationType = type, ProjectContract = contract };

        Assert.Equal(3, ApplicationProgressProfileResolver.GetMinistryLegCount(app));
    }

    [Fact]
    public void GetMinistryLegCount_UsesSnapshotWhenPresent()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new Application
        {
            ApplicationType = type,
            ProjectContract = new ProjectContract(),
            ApprovalLegSnapshots =
            [
                new ApplicationApprovalLegSnapshot { Sequence = 1, MinistryShortName = "A" },
                new ApplicationApprovalLegSnapshot { Sequence = 2, MinistryShortName = "B" }
            ]
        };

        Assert.Equal(2, ApplicationProgressProfileResolver.GetMinistryLegCount(app));
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
        var contract = new ProjectContract
        {
            MinistryLegs =
            [
                new ProjectContractMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new ProjectContractMinistryLeg { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };
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
    public void WouldMinistryDepthChange_WhenContractLegCountDiffers()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new Application { ApplicationType = type };
        var oneLeg = new ProjectContract
        {
            MinistryLegs = [new ProjectContractMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() }]
        };
        var threeLeg = new ProjectContract
        {
            MinistryLegs =
            [
                new ProjectContractMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new ProjectContractMinistryLeg { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() },
                new ProjectContractMinistryLeg { Sequence = 3, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };

        Assert.True(ApplicationProgressProfileResolver.WouldMinistryDepthChange(app, oneLeg, threeLeg));
        Assert.False(ApplicationProgressProfileResolver.WouldMinistryDepthChange(app, oneLeg, oneLeg));
    }

    [Fact]
    public void IsProjectContractLocked_FalseDuringOfficePreparationOnly()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new Application
        {
            ApplicationType = type,
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.IsBeingPrepared },
                    Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtOffice }
                }
            ]
        };

        Assert.False(ApplicationProgressProfileResolver.IsProjectContractLocked(app));
    }

    [Fact]
    public void IsProjectContractLocked_TrueAfterMinistryStep()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new Application
        {
            ApplicationType = type,
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.IsBeingPrepared },
                    Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtOffice }
                },
                new ApplicationProgress
                {
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.Review1Started },
                    Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMinistry1 }
                }
            ]
        };

        Assert.True(ApplicationProgressProfileResolver.IsProjectContractLocked(app));
        Assert.True(ApplicationProgressProfileResolver.IsApplicationLockedAfterOfficePreparation(app));
    }

    [Fact]
    public void IsApplicationLockedAfterOfficePreparation_TrueAfterMinistryStep_WithoutShowProjectContract()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = false
        };
        var app = new Application
        {
            ApplicationType = type,
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.Review1Started },
                    Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMinistry1 }
                }
            ]
        };

        Assert.True(ApplicationProgressProfileResolver.IsApplicationLockedAfterOfficePreparation(app));
        Assert.False(ApplicationProgressProfileResolver.IsProjectContractLocked(app));
    }

    [Fact]
    public void IsProjectContractLocked_FalseWhenShowProjectContractDisabled()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = false
        };
        var app = new Application
        {
            ApplicationType = type,
            ProgressHistory =
            [
                new ApplicationProgress
                {
                    State = new ApplicationState { Code = ApplicationProgressStateCodes.Review1Started },
                    Location = new ApplicationLocation { Code = ApplicationProgressLocationCodes.AtMinistry1 }
                }
            ]
        };

        Assert.False(ApplicationProgressProfileResolver.IsProjectContractLocked(app));
    }

    [Fact]
    public void ApplicationLockedHeaderScalarsDiffer_IgnoresWorkflowFields()
    {
        var type = new ApplicationType
        {
            ApplicationProgressRoute = ApplicationProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var original = new Application
        {
            ApplicationType = type,
            ApplicationNumber = "1",
            VisaPeriod = new VisaPeriod { LocalizationKey = "Month1" }
        };
        var current = new Application
        {
            ApplicationType = type,
            ApplicationNumber = "1",
            VisaPeriod = new VisaPeriod { LocalizationKey = "Month6" }
        };

        Assert.False(InvokeApplicationLockedHeaderScalarsDiffer(original, current));
    }

    [Fact]
    public void ApplicationLockedHeaderScalarsDiffer_DetectsApplicationTypeChange()
    {
        var original = new Application
        {
            ApplicationType = new ApplicationType { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "A" },
            ApplicationNumber = "1"
        };
        var current = new Application
        {
            ApplicationType = new ApplicationType { ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "B" },
            ApplicationNumber = "1"
        };

        Assert.True(InvokeApplicationLockedHeaderScalarsDiffer(original, current));
    }

    private static bool InvokeApplicationLockedHeaderScalarsDiffer(Application original, Application current)
    {
        var method = typeof(ApplicationProgressProfileResolver).GetMethod(
            "ApplicationLockedHeaderScalarsDiffer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method.Invoke(null, [original, current])!;
    }
}
