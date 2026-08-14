using System.Linq;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressProfileResolverTests
{
    [Fact]
    public void GetMinistryLegCount_UsesApprovalLegProfileWhenShowApprovalLegProfile()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true,
            ShowApprovalLegProfile = true
        };
        var profile = new ApprovalLegProfile
        {
            MinistryLegs =
            [
                new ApprovalLegProfileMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new ApprovalLegProfileMinistryLeg { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };
        var contract = new ProjectContract();
        var app = new ApplicationProfileInstance
        {
            ApplicationType = type,
            ApprovalLegProfile = profile,
            ProjectContract = contract
        };

        Assert.Equal(2, ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(app));
    }

    [Fact]
    public void TryValidateApprovalLegProfileForProgress_BlocksSecondStepWithoutProfile()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowApprovalLegProfile = true,
            ShowProjectContract = true
        };
        var app = new ApplicationProfileInstance { ApplicationType = type };
        var progress = new ApplicationProfileInstanceProgress
        {
            ApplicationProfileInstance = app,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.Review1Approved },
        };

        Assert.False(ApplicationProfileInstanceProgressProfileResolver.TryValidateApprovalLegProfileForProgress(progress, null, out var message));
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void TryValidateApprovalLegProfileForProgress_AllowsFirstMinistryStepWhenEmbeddedProfileLegs()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 1,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Turkmenenergo" },
        });
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };
        var progress = new ApplicationProfileInstanceProgress
        {
            ApplicationProfileInstance = app,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1) },
        };

        Assert.True(ApplicationProfileInstanceProgressProfileResolver.TryValidateApprovalLegProfileForProgress(progress, null, out var message));
        Assert.True(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void GetSuggestedNextStateAfterOfficePreparation_UsesEmbeddedProfileLegs()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 1,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Turkmenenergo" },
        });
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.Equal(
            ApplicationProfileInstanceProgressLegCodes.ReviewStarted(1),
            ApplicationProfileInstanceProgressRouteHelper.GetSuggestedNextStateAfterOfficePreparation(app));
    }

    [Fact]
    public void WouldMinistryDepthChange_WhenProfileLegCountDiffers()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowApprovalLegProfile = true
        };
        var app = new ApplicationProfileInstance { ApplicationType = type };
        var oneLeg = new ApprovalLegProfile
        {
            MinistryLegs = [new ApprovalLegProfileMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() }]
        };
        var twoLeg = new ApprovalLegProfile
        {
            MinistryLegs =
            [
                new ApprovalLegProfileMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new ApprovalLegProfileMinistryLeg { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };

        Assert.True(ApplicationProfileInstanceProgressProfileResolver.WouldMinistryDepthChange(app, oneLeg, twoLeg));
        Assert.False(ApplicationProfileInstanceProgressProfileResolver.WouldMinistryDepthChange(app, oneLeg, oneLeg));
    }

    [Fact]
    public void ApplicationLockedHeaderScalarsDiffer_DetectsApprovalLegProfileChange()
    {
        var original = new ApplicationProfileInstance
        {
            ApplicationNumber = "1",
            ApprovalLegProfile = new ApprovalLegProfile { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") }
        };
        var current = new ApplicationProfileInstance
        {
            ApplicationNumber = "1",
            ApprovalLegProfile = new ApprovalLegProfile { ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") }
        };

        Assert.True(InvokeApplicationLockedHeaderScalarsDiffer(original, current));
    }

    [Fact]
    public void GetMinistryLegCount_FallsBackToApplicationType_WhenNoProfileOrSnapshot()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true,
            MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly
        };
        var app = new ApplicationProfileInstance { ApplicationType = type, ProjectContract = new ProjectContract() };

        Assert.Equal(1, ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(app));
        Assert.Equal(
            MinistryReviewDepth.FirstMinistryOnly,
            ApplicationProfileInstanceProgressProfileResolver.GetMinistryReviewDepth(app));
    }

    [Fact]
    public void GetMinistryLegCount_UsesThreeLegProfile()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowApprovalLegProfile = true
        };
        var profile = new ApprovalLegProfile
        {
            MinistryLegs =
            [
                new ApprovalLegProfileMinistryLeg { Sequence = 1, ApprovingMinistry = new ApprovingMinistry() },
                new ApprovalLegProfileMinistryLeg { Sequence = 2, ApprovingMinistry = new ApprovingMinistry() },
                new ApprovalLegProfileMinistryLeg { Sequence = 3, ApprovingMinistry = new ApprovingMinistry() }
            ]
        };
        var app = new ApplicationProfileInstance { ApplicationType = type, ApprovalLegProfile = profile };

        Assert.Equal(3, ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(app));
    }

    [Fact]
    public void GetMinistryLegCount_UsesSnapshotWhenPresent()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationType = type,
            ProjectContract = new ProjectContract(),
            ApprovalLegSnapshots =
            [
                new ApplicationProfileInstanceApprovalLegSnapshot { Sequence = 1, MinistryShortName = "A" },
                new ApplicationProfileInstanceApprovalLegSnapshot { Sequence = 2, MinistryShortName = "B" }
            ]
        };

        Assert.Equal(2, ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(app));
    }

    [Fact]
    public void GetMinistryReviewDepth_FallsBackToApplicationType_WhenNoContract()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry,
            ShowProjectContract = true
        };
        var app = new ApplicationProfileInstance { ApplicationType = type };

        Assert.Equal(
            MinistryReviewDepth.FirstAndSecondMinistry,
            ApplicationProfileInstanceProgressProfileResolver.GetMinistryReviewDepth(app));
    }

    [Fact]
    public void GetMinistryReviewDepth_IgnoresContract_WhenShowProjectContractFalse()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = MinistryReviewDepth.FirstMinistryOnly,
            ShowProjectContract = false
        };
        var contract = new ProjectContract();
        var app = new ApplicationProfileInstance { ApplicationType = type, ProjectContract = contract };

        Assert.Equal(
            MinistryReviewDepth.FirstMinistryOnly,
            ApplicationProfileInstanceProgressProfileResolver.GetMinistryReviewDepth(app));
    }

    [Fact]
    public void TryValidateProjectContractForProgress_AllowsInitialOfficePreparationWithoutContract()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new ApplicationProfileInstance { ApplicationType = type };
        var progress = new ApplicationProfileInstanceProgress
        {
            ApplicationProfileInstance = app,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared },
        };

        Assert.True(ApplicationProfileInstanceProgressProfileResolver.TryValidateProjectContractForProgress(progress, null, out _));
    }

    [Fact]
    public void TryValidateProjectContractForProgress_BlocksSecondStepWithoutContract()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new ApplicationProfileInstance { ApplicationType = type };
        var progress = new ApplicationProfileInstanceProgress
        {
            ApplicationProfileInstance = app,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.Review1Approved },
        };

        Assert.False(ApplicationProfileInstanceProgressProfileResolver.TryValidateProjectContractForProgress(progress, null, out var message));
        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void WouldMinistryDepthChange_AlwaysFalse_ForProjectContract()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new ApplicationProfileInstance { ApplicationType = type };
        var oneLeg = new ProjectContract();
        var threeLeg = new ProjectContract();

        Assert.False(ApplicationProfileInstanceProgressProfileResolver.WouldMinistryDepthChange(app, oneLeg, threeLeg));
        Assert.False(ApplicationProfileInstanceProgressProfileResolver.WouldMinistryDepthChange(app, oneLeg, oneLeg));
    }

    [Fact]
    public void IsProjectContractLocked_FalseDuringOfficePreparationOnly()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationType = type,
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared },
                }
            ]
        };

        Assert.False(ApplicationProfileInstanceProgressProfileResolver.IsProjectContractLocked(app));
    }

    [Fact]
    public void IsProjectContractLocked_TrueAfterMinistryStep()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationType = type,
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared },
                },
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.Review1Started },
                }
            ]
        };

        Assert.True(ApplicationProfileInstanceProgressProfileResolver.IsProjectContractLocked(app));
        Assert.True(ApplicationProfileInstanceProgressProfileResolver.IsApplicationLockedAfterOfficePreparation(app));
    }

    [Fact]
    public void IsApplicationLockedAfterOfficePreparation_TrueAfterMinistryStep_WithoutShowProjectContract()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = false
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationType = type,
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.Review1Started },
                }
            ]
        };

        Assert.True(ApplicationProfileInstanceProgressProfileResolver.IsApplicationLockedAfterOfficePreparation(app));
        Assert.False(ApplicationProfileInstanceProgressProfileResolver.IsProjectContractLocked(app));
    }

    [Fact]
    public void IsProjectContractLocked_FalseWhenShowProjectContractDisabled()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = false
        };
        var app = new ApplicationProfileInstance
        {
            ApplicationType = type,
            ProgressHistory =
            [
                new ApplicationProfileInstanceProgress
                {
                    State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.Review1Started },
                }
            ]
        };

        Assert.False(ApplicationProfileInstanceProgressProfileResolver.IsProjectContractLocked(app));
    }

    [Fact]
    public void ApplicationLockedHeaderScalarsDiffer_IgnoresWorkflowFields()
    {
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ShowProjectContract = true
        };
        var original = new ApplicationProfileInstance
        {
            ApplicationType = type,
            ApplicationNumber = "1",
            VisaPeriod = new VisaPeriod { LocalizationKey = "Month1" }
        };
        var current = new ApplicationProfileInstance
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
        var original = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "A" },
            ApplicationNumber = "1"
        };
        var current = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "B" },
            ApplicationNumber = "1"
        };

        Assert.True(InvokeApplicationLockedHeaderScalarsDiffer(original, current));
    }

    private static bool InvokeApplicationLockedHeaderScalarsDiffer(ApplicationProfileInstance original, ApplicationProfileInstance current)
    {
        var method = typeof(ApplicationProfileInstanceProgressProfileResolver).GetMethod(
            "ApplicationLockedHeaderScalarsDiffer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method.Invoke(null, [original, current])!;
    }
}
