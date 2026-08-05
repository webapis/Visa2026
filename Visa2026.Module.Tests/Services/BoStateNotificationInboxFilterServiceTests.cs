using Visa2026.Module.Services.StateNotifications;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class BoStateNotificationInboxFilterServiceTests
{
    [Fact]
    public void TakePendingCriticalOnly_IsOneShot()
    {
        var service = new BoStateNotificationInboxFilterService();

        Assert.False(service.TakePendingCriticalOnly());

        service.SetPendingCriticalOnly();
        Assert.True(service.TakePendingCriticalOnly());
        Assert.False(service.TakePendingCriticalOnly());
    }

    [Fact]
    public void SetPendingCriticalOnly_False_ClearsPendingFlag()
    {
        var service = new BoStateNotificationInboxFilterService();
        service.SetPendingCriticalOnly(true);
        service.SetPendingCriticalOnly(false);

        Assert.False(service.TakePendingCriticalOnly());
    }
}
