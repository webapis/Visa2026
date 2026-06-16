using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ProjectContractMinistryLegForeignKeySyncTests
{
    [Fact]
    public void SyncForeignKeys_CopiesMinistryId_AndDefersParentIdWithoutObjectSpace()
    {
        var ministry = new ApprovingMinistry { NameTm = "Test ministry" };
        var leg = new ProjectContractMinistryLeg
        {
            ProjectContract = new ProjectContract { NameTm = "Test contract" },
            ApprovingMinistry = ministry,
            Sequence = 1,
            MaxDaysInReview = 10,
        };

        leg.SyncForeignKeys();

        Assert.Equal(Guid.Empty, leg.ProjectContractId);
        Assert.Equal(ministry.ID, leg.ApprovingMinistryId);
    }

    [Fact]
    public void SyncForeignKeys_ClearsParentIdWhenParentIsNewInObjectSpace()
    {
        var contract = new ProjectContract { NameTm = "Test contract" };
        var leg = new ProjectContractMinistryLeg
        {
            ProjectContract = contract,
            Sequence = 1,
            ApprovingMinistry = new ApprovingMinistry { NameTm = "Ministry" },
            ProjectContractId = contract.ID,
        };

        leg.SyncForeignKeys();

        Assert.Equal(Guid.Empty, leg.ProjectContractId);
    }

    [Fact]
    public void WireMinistryLegs_SetsParentNavigation()
    {
        var contract = new ProjectContract { NameTm = "Test contract" };
        var leg = new ProjectContractMinistryLeg
        {
            Sequence = 1,
            MaxDaysInReview = 10,
            ApprovingMinistry = new ApprovingMinistry { NameTm = "Ministry" },
        };
        contract.MinistryLegs.Add(leg);

        ProjectContractMinistryHelper.WireMinistryLegs(contract);

        Assert.Same(contract, leg.ProjectContract);
        Assert.Equal(Guid.Empty, leg.ProjectContractId);
    }

    [Fact]
    public void WouldOrphanLegForeignKey_NewParentNotInBatch_ReturnsTrue()
    {
        var parentId = Guid.NewGuid();

        Assert.True(ProjectContractMinistryHelper.WouldOrphanLegForeignKey(
            isParentNewObject: true,
            parentId,
            contractIdsInCommitBatch: []));

        Assert.False(ProjectContractMinistryHelper.WouldOrphanLegForeignKey(
            isParentNewObject: true,
            parentId,
            contractIdsInCommitBatch: [parentId]));

        Assert.False(ProjectContractMinistryHelper.WouldOrphanLegForeignKey(
            isParentNewObject: false,
            parentId,
            contractIdsInCommitBatch: []));
    }
}
