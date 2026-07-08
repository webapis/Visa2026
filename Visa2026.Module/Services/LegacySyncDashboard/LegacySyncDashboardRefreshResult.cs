namespace Visa2026.Module.Services.LegacySyncDashboard;

public sealed class LegacySyncDashboardRefreshResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public LegacySyncDashboardSnapshot? Snapshot { get; init; }
}