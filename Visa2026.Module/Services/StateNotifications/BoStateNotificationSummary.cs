namespace Visa2026.Module.Services.StateNotifications;

/// <summary>Open notification counts for header badge and dashboards (prototype / live evaluation).
/// </summary>
public sealed class BoStateNotificationSummary
{
    public int OpenCriticalCount { get; init; }
    public int OpenWarningCount { get; init; }
    public int OpenInfoCount { get; init; }
    public int OpenTotalCount { get; init; }

    public int OpenMissingDataCount { get; init; }
}
