namespace Visa2026.Module.Services.LegacySyncDashboard;

public sealed class LegacySyncDashboardOptions
{
    public const string SectionName = "LegacySyncDashboard";

    public bool Enabled { get; set; }

    /// <summary>Sync host root on prod (e.g. C:\visa2026-sync). Reads sync-dashboard.json and sync-run-status.json.</summary>
    public string SyncHostRoot { get; set; } = "";

    public string LegacySource { get; set; } = "calik-energi-onprem-prod";

    public string LegacyServer { get; set; } = "10.100.128.15";

    public string LegacyDatabase { get; set; } = "VISA2015";

    public string LegacyUser { get; set; } = "ReadOnlyUser";

    public int PollIntervalSeconds { get; set; } = 30;
}