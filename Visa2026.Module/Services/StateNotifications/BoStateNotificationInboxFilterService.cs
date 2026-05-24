namespace Visa2026.Module.Services.StateNotifications;

/// <summary>Passes inbox list filters from header badge / nav into the State notifications view.</summary>
public sealed class BoStateNotificationInboxFilterService
{
    private bool _pendingCriticalOnly;

    public void SetPendingCriticalOnly(bool criticalOnly = true) => _pendingCriticalOnly = criticalOnly;

    public bool TakePendingCriticalOnly()
    {
        if (!_pendingCriticalOnly)
            return false;

        _pendingCriticalOnly = false;
        return true;
    }
}
