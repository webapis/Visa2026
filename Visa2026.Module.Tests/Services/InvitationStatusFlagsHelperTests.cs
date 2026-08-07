using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class InvitationStatusFlagsHelperTests
{
    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, true, true, false)]
    public void AreInvitationItemFlagsExclusive_AllowsAtMostOneSetFlag(
        bool cancelled, bool changed, bool used, bool expected)
    {
        Assert.Equal(
            expected,
            InvitationStatusFlagsHelper.AreInvitationItemFlagsExclusive(cancelled, changed, used));
    }

    [Fact]
    public void NormalizeInvitationItem_PrefersCancelledWhenMultipleFlagsSet()
    {
        var item = new InvitationItem();
        item.SetItemStatusFlags(cancelled: true, changed: true, used: true);

        InvitationStatusFlagsHelper.NormalizeInvitationItem(item);

        Assert.True(item.IsCancelled);
        Assert.False(item.IsChanged);
        Assert.False(item.IsUsed);
    }

    [Fact]
    public void NormalizeInvitationItem_PrefersChangedOverUsed()
    {
        var item = new InvitationItem();
        item.SetItemStatusFlags(cancelled: false, changed: true, used: true);

        InvitationStatusFlagsHelper.NormalizeInvitationItem(item);

        Assert.False(item.IsCancelled);
        Assert.True(item.IsChanged);
        Assert.False(item.IsUsed);
    }

    [Fact]
    public void NormalizeInvitationItem_LeavesSingleFlagUntouched()
    {
        var item = new InvitationItem();
        item.SetItemStatusFlags(cancelled: false, changed: false, used: true);

        InvitationStatusFlagsHelper.NormalizeInvitationItem(item);

        Assert.False(item.IsCancelled);
        Assert.False(item.IsChanged);
        Assert.True(item.IsUsed);
    }
}
