using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public sealed class ApprovalLegProfileMinistryHelperTests
{
    [Fact]
    public void GetMinistryShortNameForLeg_UsesSnapshotWhenPresent()
    {
        var app = new Application
        {
            ApprovalLegSnapshots =
            [
                new ApplicationApprovalLegSnapshot { Sequence = 1, MinistryShortName = "Energetika" }
            ],
            ApprovalLegProfile = new ApprovalLegProfile
            {
                MinistryLegs =
                [
                    new ApprovalLegProfileMinistryLeg
                    {
                        Sequence = 1,
                        ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Other" }
                    }
                ]
            }
        };

        Assert.Equal("Energetika", ApprovalLegProfileMinistryHelper.GetMinistryShortNameForLeg(app, 1));
    }

    [Fact]
    public void GetMinistryShortNameForLeg_FallsBackToLiveProfileWhenSnapshotMissing()
    {
        var app = new Application
        {
            ApprovalLegSnapshots = new ObservableCollection<ApplicationApprovalLegSnapshot>(),
            ApprovalLegProfile = new ApprovalLegProfile
            {
                MinistryLegs =
                [
                    new ApprovalLegProfileMinistryLeg
                    {
                        Sequence = 1,
                        ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Energetika" }
                    }
                ]
            }
        };

        Assert.Equal("Energetika", ApprovalLegProfileMinistryHelper.GetMinistryShortNameForLeg(app, 1));
    }

    [Fact]
    public void GetMinistryShortNameForProgressStep_ApprovedState_UsesLegFromStateCode()
    {
        var app = new Application
        {
            ApprovalLegProfile = new ApprovalLegProfile
            {
                MinistryLegs =
                [
                    new ApprovalLegProfileMinistryLeg
                    {
                        Sequence = 1,
                        ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Energetika" }
                    }
                ]
            }
        };

        var name = ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
            app,
            stateCode: "1_REVIEW_APPROVED",
            locationCode: "AT_THE_MINISTERY_1");

        Assert.Equal("Energetika", name);
    }

    [Fact]
    public void GetMinistryShortNameForLeg_ReturnsNullWhenNoSnapshotOrProfile()
    {
        var app = new Application
        {
            ApprovalLegSnapshots = new ObservableCollection<ApplicationApprovalLegSnapshot>()
        };

        Assert.Null(ApprovalLegProfileMinistryHelper.GetMinistryShortNameForLeg(app, 1));
    }
}