using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014LegacyInvitationItemCancellationResolverTests
{
    private static readonly ApplicationTypeVisibilityCatalog Visibility = ApplicationTypeVisibilityCatalog.Load();

    [Fact]
    public void ResolveIsCancelled_ApplicationResultOne_IsNotCancelEvidence()
    {
        var index = Visa2014LegacyInvitationItemCancellationIndex.FromLegacyOidsForTests([]);
        var legacyOid = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Result = 1 is ApplicationResultEnum.Rejection → Rejection BO, not InvitationItem.IsCancelled
        Assert.False(Visa2014LegacyInvitationItemCancellationIndex.ResolveIsCancelled(1, legacyOid, index));
    }

    [Fact]
    public void ResolveIsCancelled_NoEvidence_ReturnsFalse()
    {
        var index = Visa2014LegacyInvitationItemCancellationIndex.FromLegacyOidsForTests([]);
        var legacyOid = Guid.Parse("22222222-2222-2222-2222-222222222222");

        Assert.False(Visa2014LegacyInvitationItemCancellationIndex.ResolveIsCancelled(null, legacyOid, index));
        Assert.False(Visa2014LegacyInvitationItemCancellationIndex.ResolveIsCancelled(0, legacyOid, index));
    }

    [Fact]
    public void ResolveIsCancelled_IndexContainsOid_ReturnsTrue()
    {
        var legacyOid = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var index = Visa2014LegacyInvitationItemCancellationIndex.FromLegacyOidsForTests([legacyOid]);

        Assert.True(Visa2014LegacyInvitationItemCancellationIndex.ResolveIsCancelled(null, legacyOid, index));
    }

    [Fact]
    public void ResolveDocumentCancellation_CancelInvitation_SetsInvitationItemFlag()
    {
        var flags = Visa2014ApplicationItemCancelledFlagsMapper.ResolveDocumentCancellation(
            "App_Cancel_Inv",
            Visibility,
            legacyCancelled: true);

        Assert.True(flags.InvitationItem);
        Assert.False(flags.WorkPermitItem);
        Assert.False(flags.Visa);
    }
}
