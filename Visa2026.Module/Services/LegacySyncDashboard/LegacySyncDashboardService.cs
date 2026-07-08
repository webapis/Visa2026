using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Visa2026.Module.Services.LegacySyncDashboard;

public sealed class LegacySyncDashboardService : ILegacySyncDashboardService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly LegacySyncDashboardOptions options;
    private readonly IConfiguration configuration;

    public LegacySyncDashboardService(
        IOptions<LegacySyncDashboardOptions> options,
        IConfiguration configuration)
    {
        this.options = options.Value;
        this.configuration = configuration;
    }

    public LegacySyncDashboardRefreshResult RefreshSnapshot() =>
        new LegacySyncDashboardSnapshotRefresher(this.options, configuration).Refresh(GetSnapshot);

    public LegacySyncDashboardSnapshot GetSnapshot()
    {
        if (!options.Enabled)
        {
            return new LegacySyncDashboardSnapshot
            {
                Enabled = false,
                ErrorMessage = "Legacy sync dashboard is disabled in configuration.",
            };
        }

        if (string.IsNullOrWhiteSpace(options.SyncHostRoot))
        {
            return new LegacySyncDashboardSnapshot
            {
                Enabled = false,
                ErrorMessage = "LegacySyncDashboard:SyncHostRoot is not configured.",
            };
        }

        if (!Directory.Exists(options.SyncHostRoot))
        {
            return new LegacySyncDashboardSnapshot
            {
                Enabled = true,
                ErrorMessage = $"Sync host root not found: {options.SyncHostRoot}",
            };
        }

        var dashboardPath = Path.Combine(options.SyncHostRoot, "sync-dashboard.json");
        if (!File.Exists(dashboardPath))
        {
            return new LegacySyncDashboardSnapshot
            {
                Enabled = true,
                ErrorMessage = $"Dashboard file not found: {dashboardPath}. Run Export-OnPremSyncDashboard.ps1 or start a sync run.",
            };
        }

        try
        {
            var json = File.ReadAllText(dashboardPath);
            var file = JsonSerializer.Deserialize<DashboardFileModel>(json, JsonOptions);
            if (file == null)
            {
                return new LegacySyncDashboardSnapshot
                {
                    Enabled = true,
                    ErrorMessage = "Dashboard JSON is empty or invalid.",
                };
            }

            var runStatus = file.RunStatus;
            var waves = runStatus?.Waves?.Select(MapWave).ToList()
                ?? new List<LegacySyncWaveRowDto>();

            return new LegacySyncDashboardSnapshot
            {
                Enabled = true,
                GeneratedUtc = ParseUtc(file.GeneratedUtc),
                LegacySource = file.LegacySource,
                LegacyServer = file.LegacyServer,
                LegacyDatabase = file.LegacyDatabase,
                TargetServer = file.TargetServer,
                TargetDatabase = file.TargetDatabase,
                WatermarkUtc = ParseUtc(file.WatermarkUtc),
                OverallStatus = runStatus?.OverallStatus,
                CurrentWave = runStatus?.CurrentWave,
                WaveSummary = MapWaveSummary(file.WaveSummary),
                Entities = file.Entities?.Select(MapEntity).ToList() ?? new List<LegacySyncEntityRowDto>(),
                Waves = waves,
            };
        }
        catch (Exception ex)
        {
            return new LegacySyncDashboardSnapshot
            {
                Enabled = true,
                ErrorMessage = $"Failed to read dashboard: {ex.Message}",
            };
        }
    }

    public LegacySyncDashboardFileContent GetDashboardHtmlFile() =>
        ReadDashboardFile(LegacySyncDashboardPaths.DashboardHtmlFileName, "text/html; charset=utf-8");

    public LegacySyncDashboardFileContent GetDashboardJsonFile() =>
        ReadDashboardFile(LegacySyncDashboardPaths.DashboardJsonFileName, "application/json; charset=utf-8");

    private LegacySyncDashboardFileContent ReadDashboardFile(string fileName, string contentType)
    {
        if (!options.Enabled)
            return LegacySyncDashboardFileContent.Disabled("Legacy sync dashboard is disabled in configuration.");

        if (string.IsNullOrWhiteSpace(options.SyncHostRoot))
            return LegacySyncDashboardFileContent.Disabled("LegacySyncDashboard:SyncHostRoot is not configured.");

        if (!Directory.Exists(options.SyncHostRoot))
            return LegacySyncDashboardFileContent.NotFound($"Sync host root not found: {options.SyncHostRoot}");

        var path = Path.Combine(options.SyncHostRoot, fileName);
        if (!File.Exists(path))
        {
            return LegacySyncDashboardFileContent.NotFound(
                $"Dashboard file not found: {path}. Run Export-OnPremSyncDashboard.ps1 or complete a sync run.");
        }

        try
        {
            var content = File.ReadAllText(path);
            return LegacySyncDashboardFileContent.Ok(content, contentType);
        }
        catch (Exception ex)
        {
            return new LegacySyncDashboardFileContent
            {
                Success = false,
                StatusCode = 500,
                ErrorMessage = $"Failed to read {fileName}: {ex.Message}",
            };
        }
    }

    private static LegacySyncWaveRowDto MapWave(WaveFileModel wave) =>
        new()
        {
            Name = wave.Name ?? "",
            Status = wave.Status ?? "",
            StartedUtc = ParseUtc(wave.StartedUtc),
            CompletedUtc = ParseUtc(wave.CompletedUtc),
            ExitCode = wave.ExitCode,
            Inserted = wave.Inserted,
            Updated = wave.Updated,
            SoftDeleted = wave.SoftDeleted,
            Failed = wave.Failed,
        };

    private static LegacySyncEntityRowDto MapEntity(EntityFileModel row) =>
        new()
        {
            Kind = row.Kind ?? "",
            BO = row.BO ?? "",
            Legacy = row.Legacy,
            Migrated = row.Migrated,
            NotCompleted = row.NotCompleted,
            IdMap = row.IdMap,
            DuplicateGroups = row.DuplicateGroups,
            DuplicateExtraRows = row.DuplicateExtraRows,
            SyncState = row.SyncState ?? "",
            Note = row.Note ?? "",
        };

    private static LegacySyncWaveSummaryDto? MapWaveSummary(WaveSummaryFileModel? summary) =>
        summary == null
            ? null
            : new LegacySyncWaveSummaryDto
            {
                Pending = summary.Pending,
                Running = summary.Running,
                Completed = summary.Completed,
                Failed = summary.Failed,
            };

    private static DateTime? ParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToUniversalTime()
            : null;
    }

    private sealed class DashboardFileModel
    {
        public string? GeneratedUtc { get; set; }

        public string? LegacySource { get; set; }

        public string? LegacyServer { get; set; }

        public string? LegacyDatabase { get; set; }

        public string? TargetServer { get; set; }

        public string? TargetDatabase { get; set; }

        public string? WatermarkUtc { get; set; }

        public RunStatusFileModel? RunStatus { get; set; }

        public WaveSummaryFileModel? WaveSummary { get; set; }

        public List<EntityFileModel>? Entities { get; set; }
    }

    private sealed class RunStatusFileModel
    {
        public string? OverallStatus { get; set; }

        public string? CurrentWave { get; set; }

        public List<WaveFileModel>? Waves { get; set; }
    }

    private sealed class WaveSummaryFileModel
    {
        public int Pending { get; set; }

        public int Running { get; set; }

        public int Completed { get; set; }

        public int Failed { get; set; }
    }

    private sealed class EntityFileModel
    {
        public string? Kind { get; set; }

        public string? BO { get; set; }

        public int? Legacy { get; set; }

        public int? Migrated { get; set; }

        public int? NotCompleted { get; set; }

        public int? IdMap { get; set; }

        public int? DuplicateGroups { get; set; }

        public int? DuplicateExtraRows { get; set; }

        public string? SyncState { get; set; }

        public string? Note { get; set; }
    }

    private sealed class WaveFileModel
    {
        public string? Name { get; set; }

        public string? Status { get; set; }

        public string? StartedUtc { get; set; }

        public string? CompletedUtc { get; set; }

        public int? ExitCode { get; set; }

        public int? Inserted { get; set; }

        public int? Updated { get; set; }

        public int? SoftDeleted { get; set; }

        public int? Failed { get; set; }
    }
}
