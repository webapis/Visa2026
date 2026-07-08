namespace Visa2026.Module.Services.LegacySyncDashboard;

public sealed class LegacySyncDashboardSnapshot
{
    public bool Enabled { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime? GeneratedUtc { get; init; }

    public string? LegacySource { get; init; }

    public string? LegacyServer { get; init; }

    public string? LegacyDatabase { get; init; }

    public string? TargetServer { get; init; }

    public string? TargetDatabase { get; init; }

    public DateTime? WatermarkUtc { get; init; }

    public string? OverallStatus { get; init; }

    public string? CurrentWave { get; init; }

    public LegacySyncWaveSummaryDto? WaveSummary { get; init; }

    public IReadOnlyList<LegacySyncEntityRowDto> Entities { get; init; } = Array.Empty<LegacySyncEntityRowDto>();

    public IReadOnlyList<LegacySyncWaveRowDto> Waves { get; init; } = Array.Empty<LegacySyncWaveRowDto>();
}

public sealed class LegacySyncWaveSummaryDto
{
    public int Pending { get; init; }

    public int Running { get; init; }

    public int Completed { get; init; }

    public int Failed { get; init; }
}

public sealed class LegacySyncEntityRowDto
{
    public string Kind { get; init; } = "";

    public string BO { get; init; } = "";

    public int? Legacy { get; init; }

    public int? Migrated { get; init; }

    public int? NotCompleted { get; init; }

    public int? IdMap { get; init; }

    public string SyncState { get; init; } = "";

    public string Note { get; init; } = "";
}

public sealed class LegacySyncWaveRowDto
{
    public string Name { get; init; } = "";

    public string Status { get; init; } = "";

    public DateTime? StartedUtc { get; init; }

    public DateTime? CompletedUtc { get; init; }

    public int? ExitCode { get; init; }

    public int? Inserted { get; init; }

    public int? Updated { get; init; }

    public int? SoftDeleted { get; init; }

    public int? Failed { get; init; }
}