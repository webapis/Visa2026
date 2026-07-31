using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.ImportHistory;

public sealed class ImportReimportRunSummary
{
    public string RunId { get; init; } = "";
    public string Profile { get; init; } = "";
    public string OverallStatus { get; init; } = "";
    public string? StartedUtc { get; init; }
    public string? CompletedUtc { get; init; }
    public int? ElapsedSeconds { get; init; }
    public int WavesCompleted { get; init; }
    public int WavesFailed { get; init; }
    public int WavesPending { get; init; }
    public bool? FileWavesIncluded { get; init; }
}

public sealed class ImportReimportBoCountRow
{
    public string BO { get; init; } = "";
    public string? Table { get; init; }
    public int? Left { get; init; }
    public int? Right { get; init; }
    public int? Delta { get; init; }
    public double? AbsPct { get; init; }
    public bool Anomaly { get; init; }
}

public sealed class ImportReimportWaveRow
{
    public string Wave { get; init; } = "";
    public string LeftStatus { get; init; } = "";
    public string RightStatus { get; init; } = "";
    public int? LeftFailed { get; init; }
    public int? RightFailed { get; init; }
    public int? LeftExit { get; init; }
    public int? RightExit { get; init; }
    public bool Regressed { get; init; }
}

public sealed class ImportReimportFileWaveRow
{
    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public string LeftStatus { get; init; } = "";
    public string RightStatus { get; init; } = "";
    public int? LeftExit { get; init; }
    public int? RightExit { get; init; }
    public string LeftPosted { get; init; } = "";
    public string RightPosted { get; init; } = "";
    public bool Regressed { get; init; }
}

public sealed class ImportReimportFilePresenceRow
{
    public string Metric { get; init; } = "";
    public int? LeftParent { get; init; }
    public int? RightParent { get; init; }
    public int? LeftPresent { get; init; }
    public int? RightPresent { get; init; }
    public int? DeltaPresent { get; init; }
    public double? AbsPct { get; init; }
    public bool Anomaly { get; init; }
    public string Notes { get; init; } = "";
}

public sealed class ImportReimportCompareResult
{
    public string LeftRunId { get; init; } = "";
    public string RightRunId { get; init; } = "";
    public IReadOnlyList<ImportReimportBoCountRow> BoRows { get; init; } = Array.Empty<ImportReimportBoCountRow>();
    public IReadOnlyList<ImportReimportWaveRow> WaveRows { get; init; } = Array.Empty<ImportReimportWaveRow>();
    public IReadOnlyList<ImportReimportFileWaveRow> FileWaveRows { get; init; } = Array.Empty<ImportReimportFileWaveRow>();
    public IReadOnlyList<ImportReimportFilePresenceRow> FilePresenceRows { get; init; } = Array.Empty<ImportReimportFilePresenceRow>();
    public IReadOnlyList<string> Anomalies { get; init; } = Array.Empty<string>();
    public bool FileWavesIncludedLeft { get; init; }
    public bool FileWavesIncludedRight { get; init; }
    public string? FileWavesNoteLeft { get; init; }
    public string? FileWavesNoteRight { get; init; }
    public int AbsoluteCountThreshold { get; init; }
    public double RelativePercentThreshold { get; init; }
}

public interface IImportReimportHistoryReader
{
    string? ResolvedRootPath { get; }
    bool IsAvailable { get; }
    string? UnavailableReason { get; }
    IReadOnlyList<ImportReimportRunSummary> ListRuns();
    ImportReimportCompareResult? Compare(
        string leftRunId,
        string rightRunId,
        int absoluteCountThreshold = 20,
        double relativePercentThreshold = 1.0);
}
