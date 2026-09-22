using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Pure commit-batch and leg-count edges for ministry profile legs
/// (FK orphan risk when a new parent is not in the same save batch).
/// </summary>
public sealed class ApprovalLegProfileMinistryHelperOrphanAndCountTests
{
    [Fact]
    public void WouldOrphanLegForeignKey_True_WhenNewParentNotInBatch()
    {
        var parentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        Assert.True(ApprovalLegProfileMinistryHelper.WouldOrphanLegForeignKey(
            isParentNewObject: true,
            parentId,
            profileIdsInCommitBatch: Array.Empty<Guid>()));
    }

    [Fact]
    public void WouldOrphanLegForeignKey_False_WhenNewParentIsInBatch()
    {
        var parentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        Assert.False(ApprovalLegProfileMinistryHelper.WouldOrphanLegForeignKey(
            isParentNewObject: true,
            parentId,
            profileIdsInCommitBatch: new[] { parentId }));
    }

    [Fact]
    public void WouldOrphanLegForeignKey_False_WhenParentAlreadyPersisted()
    {
        var parentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        Assert.False(ApprovalLegProfileMinistryHelper.WouldOrphanLegForeignKey(
            isParentNewObject: false,
            parentId,
            profileIdsInCommitBatch: Array.Empty<Guid>()));
    }

    [Fact]
    public void WouldOrphanLegForeignKey_False_WhenParentIdEmpty()
    {
        Assert.False(ApprovalLegProfileMinistryHelper.WouldOrphanLegForeignKey(
            isParentNewObject: true,
            parentId: Guid.Empty,
            profileIdsInCommitBatch: Array.Empty<Guid>()));
    }

    [Fact]
    public void GetLegCount_CountsOnlyLegsWithApprovingMinistry()
    {
        var profile = new ApprovalLegProfile
        {
            MinistryLegs = new ObservableCollection<ApprovalLegProfileMinistryLeg>
            {
                new() { ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Energetika" } },
                new() { ApprovingMinistry = null },
                new() { ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Gurlusyk" } },
            }
        };

        Assert.Equal(2, ApprovalLegProfileMinistryHelper.GetLegCount(profile));
        Assert.True(ApprovalLegProfileMinistryHelper.HasConfiguredLegs(profile));
    }

    [Fact]
    public void GetLegCount_NullProfileOrEmptyLegs_IsZero()
    {
        Assert.Equal(0, ApprovalLegProfileMinistryHelper.GetLegCount(null));
        Assert.False(ApprovalLegProfileMinistryHelper.HasConfiguredLegs(null));

        var empty = new ApprovalLegProfile
        {
            MinistryLegs = new ObservableCollection<ApprovalLegProfileMinistryLeg>()
        };
        Assert.Equal(0, ApprovalLegProfileMinistryHelper.GetLegCount(empty));
        Assert.False(ApprovalLegProfileMinistryHelper.HasConfiguredLegs(empty));
    }
}
