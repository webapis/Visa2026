using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public sealed class ApprovalLegProfileMinistryHelperTests
{
    [Fact]
    public void GetMinistryShortNameForLeg_UsesSnapshotWhenPresent()
    {
        var app = new ApplicationProfileInstance
        {
            ApprovalLegSnapshots =
            [
                new ApplicationProfileInstanceApprovalLegSnapshot { Sequence = 1, MinistryShortName = "Energetika" }
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
        var app = new ApplicationProfileInstance
        {
            ApprovalLegSnapshots = new ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>(),
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
        var app = new ApplicationProfileInstance
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
        var app = new ApplicationProfileInstance
        {
            ApprovalLegSnapshots = new ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>()
        };

        Assert.Null(ApprovalLegProfileMinistryHelper.GetMinistryShortNameForLeg(app, 1));
    }

    [Fact]
    public void SyncForeignKeys_copies_parent_id_while_parent_is_new()
    {
        var parentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ministryId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var parent = new ApprovalLegProfile { ID = parentId };
        var ministry = new ApprovingMinistry { ID = ministryId };
        var leg = new ApprovalLegProfileMinistryLeg
        {
            ApprovalLegProfile = parent,
            ApprovingMinistry = ministry,
        };

        leg.SyncForeignKeys();

        Assert.Equal(parentId, leg.ApprovalLegProfileId);
        Assert.Equal(ministryId, leg.ApprovingMinistryId);
    }
}