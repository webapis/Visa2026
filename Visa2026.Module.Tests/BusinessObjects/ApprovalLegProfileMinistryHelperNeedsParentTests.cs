using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// NeedsParentInCommitBatch gates FK orphan repair when a new ApprovalLegProfile is not
/// in the same save batch as its ministry leg (nested Blazor popup commit).
/// </summary>
public sealed class ApprovalLegProfileMinistryHelperNeedsParentTests
{
    [Fact]
    public void NeedsParent_false_when_leg_has_no_parent()
    {
        var space = MultiTypeSeedObjectSpaceStub.Create();
        var leg = new ApprovalLegProfileMinistryLeg();

        Assert.False(ApprovalLegProfileMinistryHelper.NeedsParentInCommitBatch(space, leg, out var parent));
        Assert.Null(parent);
    }

    [Fact]
    public void NeedsParent_true_when_new_parent_missing_from_commit_batch()
    {
        var parentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var parent = new ApprovalLegProfile { ID = parentId };
        var leg = new ApprovalLegProfileMinistryLeg { ApprovalLegProfile = parent };

        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.MarkNew(parent);
            // Parent not in ModifiedObjects / GetObjectsToSave → orphan risk.
        });

        Assert.True(ApprovalLegProfileMinistryHelper.NeedsParentInCommitBatch(space, leg, out var resolved));
        Assert.Same(parent, resolved);
    }

    [Fact]
    public void NeedsParent_false_when_new_parent_is_in_modified_objects()
    {
        var parentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var parent = new ApprovalLegProfile { ID = parentId };
        var leg = new ApprovalLegProfileMinistryLeg { ApprovalLegProfile = parent };

        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.MarkNew(parent);
            stub.TrackModified(parent);
        });

        Assert.False(ApprovalLegProfileMinistryHelper.NeedsParentInCommitBatch(space, leg, out var resolved));
        Assert.Same(parent, resolved);
    }

    [Fact]
    public void NeedsParent_false_when_parent_already_persisted()
    {
        var parentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var parent = new ApprovalLegProfile { ID = parentId };
        var leg = new ApprovalLegProfileMinistryLeg { ApprovalLegProfile = parent };

        // Not marked new → IsNewObject false → WouldOrphan false even with empty batch.
        var space = MultiTypeSeedObjectSpaceStub.Create();

        Assert.False(ApprovalLegProfileMinistryHelper.NeedsParentInCommitBatch(space, leg, out var resolved));
        Assert.Same(parent, resolved);
    }

    [Fact]
    public void NeedsParent_false_when_new_parent_is_in_objects_to_save()
    {
        var parentId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var parent = new ApprovalLegProfile { ID = parentId };
        var leg = new ApprovalLegProfileMinistryLeg { ApprovalLegProfile = parent };

        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.MarkNew(parent);
            stub.TrackToSave(parent);
        });

        Assert.False(ApprovalLegProfileMinistryHelper.NeedsParentInCommitBatch(space, leg, out _));
    }
}
