namespace Visa2026.Module.Services.LegacySyncDashboard;

public sealed class LegacySyncDashboardOptions
{
    public const string SectionName = "LegacySyncDashboard";

    public bool Enabled { get; set; }

    /// <summary>Sync host root on prod (e.g. C:\visa2026-sync). Reads sync-dashboard.json and sync-run-status.json.</summary>
    public string SyncHostRoot { get; set; } = "";

    public int PollIntervalSeconds { get; set; } = 30;
}
