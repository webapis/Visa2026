using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Visa2026.Module.Services.ImportHistory;

namespace Visa2026.Blazor.Server.Services.ImportHistory;

public sealed class ImportReimportHistoryReader : IImportReimportHistoryReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string? _rootPath;
    private readonly string? _unavailableReason;

    public ImportReimportHistoryReader(
        IOptions<ImportHistoryOptions> options,
        IConfiguration configuration)
    {
        _rootPath = ResolveRootPath(options.Value, configuration, out _unavailableReason);
    }

    public string? ResolvedRootPath => _rootPath;
    public bool IsAvailable => !string.IsNullOrWhiteSpace(_rootPath) && Directory.Exists(_rootPath);
    public string? UnavailableReason => IsAvailable ? null : (_unavailableReason ?? "Import history folder is not available.");

    public IReadOnlyList<ImportReimportRunSummary> ListRuns()
    {
        if (!IsAvailable || _rootPath == null)
            return Array.Empty<ImportReimportRunSummary>();

        var runsDir = Path.Combine(_rootPath, "runs");
        if (!Directory.Exists(runsDir))
            return Array.Empty<ImportReimportRunSummary>();

        var list = new List<ImportReimportRunSummary>();
        foreach (var dir in Directory.EnumerateDirectories(runsDir).OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var runId = Path.GetFileName(dir);
            var metaPath = Path.Combine(dir, "meta.json");
            if (!File.Exists(metaPath))
            {
                list.Add(new ImportReimportRunSummary { RunId = runId, OverallStatus = "Unknown" });
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(metaPath));
                var root = doc.RootElement;
                var wave = root.TryGetProperty("WaveSummary", out var ws) ? ws : default;
                bool? fileIncluded = null;
                if (root.TryGetProperty("FileWavesIncluded", out var fi) &&
                    (fi.ValueKind == JsonValueKind.True || fi.ValueKind == JsonValueKind.False))
                    fileIncluded = fi.GetBoolean();

                list.Add(new ImportReimportRunSummary
                {
                    RunId = runId,
                    Profile = GetString(root, "Profile"),
                    OverallStatus = GetString(root, "OverallStatus"),
                    StartedUtc = GetStringOrNull(root, "StartedUtc"),
                    CompletedUtc = GetStringOrNull(root, "CompletedUtc"),
                    ElapsedSeconds = GetIntOrNull(root, "ElapsedSeconds"),
                    WavesCompleted = GetInt(wave, "Completed"),
                    WavesFailed = GetInt(wave, "Failed"),
                    WavesPending = GetInt(wave, "Pending"),
                    FileWavesIncluded = fileIncluded,
                });
            }
            catch
            {
                list.Add(new ImportReimportRunSummary { RunId = runId, OverallStatus = "Corrupt" });
            }
        }

        return list;
    }

    public ImportReimportCompareResult? Compare(
        string leftRunId,
        string rightRunId,
        int absoluteCountThreshold = 20,
        double relativePercentThreshold = 1.0)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(leftRunId) || string.IsNullOrWhiteSpace(rightRunId))
            return null;

        var leftCounts = LoadDbCounts(leftRunId);
        var rightCounts = LoadDbCounts(rightRunId);
        var anomalies = new List<string>();
        var boRows = new List<ImportReimportBoCountRow>();

        var allBos = leftCounts.Keys.Union(rightCounts.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        foreach (var bo in allBos)
        {
            leftCounts.TryGetValue(bo, out var left);
            rightCounts.TryGetValue(bo, out var right);
            int? delta = left.HasValue && right.HasValue ? right - left : null;
            double? absPct = null;
            if (delta.HasValue && left.HasValue && left.Value != 0)
                absPct = Math.Round(100.0 * Math.Abs(delta.Value) / left.Value, 2);
            else if (delta.HasValue && left == 0 && right > 0)
                absPct = 100.0;

            var anomaly = false;
            if (delta.HasValue)
            {
                var absHit = Math.Abs(delta.Value) >= absoluteCountThreshold;
                var pctHit = absPct.HasValue && absPct.Value >= relativePercentThreshold;
                anomaly = (absHit && pctHit)
                    || (left == 0 && right >= absoluteCountThreshold)
                    || (right == 0 && left >= absoluteCountThreshold);
            }

            boRows.Add(new ImportReimportBoCountRow
            {
                BO = bo,
                Left = left,
                Right = right,
                Delta = delta,
                AbsPct = absPct,
                Anomaly = anomaly,
            });
            if (anomaly)
                anomalies.Add($"DbCount {bo} delta={delta} ({absPct}%)");
        }

        var leftWaves = LoadWaves(leftRunId);
        var rightWaves = LoadWaves(rightRunId);
        var waveRows = new List<ImportReimportWaveRow>();
        var waveNames = leftWaves.Keys.Union(rightWaves.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        foreach (var name in waveNames)
        {
            leftWaves.TryGetValue(name, out var lw);
            rightWaves.TryGetValue(name, out var rw);
            var regressed = false;
            if (lw != null && rw != null)
            {
                if (lw.Status == "Completed" && rw.Status == "Failed")
                    regressed = true;
                var lf = lw.Failed ?? 0;
                var rf = rw.Failed ?? 0;
                if (rf > lf)
                    regressed = true;
            }

            waveRows.Add(new ImportReimportWaveRow
            {
                Wave = name,
                LeftStatus = lw?.Status ?? "",
                RightStatus = rw?.Status ?? "",
                LeftFailed = lw?.Failed,
                RightFailed = rw?.Failed,
                LeftExit = lw?.ExitCode,
                RightExit = rw?.ExitCode,
                Regressed = regressed,
            });
            if (regressed)
                anomalies.Add($"Wave {name} regressed ({lw?.Status}/fail={lw?.Failed} -> {rw?.Status}/fail={rw?.Failed})");
        }

        var leftFiles = LoadFileWaves(leftRunId);
        var rightFiles = LoadFileWaves(rightRunId);
        var fileWaveRows = new List<ImportReimportFileWaveRow>();
        var fileKeys = leftFiles.Steps.Keys.Union(rightFiles.Steps.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        foreach (var key in fileKeys)
        {
            leftFiles.Steps.TryGetValue(key, out var ls);
            rightFiles.Steps.TryGetValue(key, out var rs);
            var regressed = ls != null && rs != null
                && string.Equals(ls.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                && string.Equals(rs.Status, "Failed", StringComparison.OrdinalIgnoreCase);
            fileWaveRows.Add(new ImportReimportFileWaveRow
            {
                Key = key,
                Name = ls?.Name ?? rs?.Name ?? key,
                LeftStatus = ls?.Status ?? "",
                RightStatus = rs?.Status ?? "",
                LeftExit = ls?.ExitCode,
                RightExit = rs?.ExitCode,
                LeftPosted = ls?.Posted ?? "",
                RightPosted = rs?.Posted ?? "",
                Regressed = regressed,
            });
            if (regressed)
                anomalies.Add($"File wave {key} regressed ({ls?.Status} -> {rs?.Status})");
        }

        var leftPresence = LoadFilePresence(leftRunId);
        var rightPresence = LoadFilePresence(rightRunId);
        var presenceRows = new List<ImportReimportFilePresenceRow>();
        var metrics = leftPresence.Keys.Union(rightPresence.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
        foreach (var metric in metrics)
        {
            leftPresence.TryGetValue(metric, out var lp);
            rightPresence.TryGetValue(metric, out var rp);
            var leftPresent = lp?.PresentCount;
            var rightPresent = rp?.PresentCount;
            int? delta = leftPresent.HasValue && rightPresent.HasValue ? rightPresent - leftPresent : null;
            double? absPct = null;
            if (delta.HasValue && leftPresent.HasValue && leftPresent.Value != 0)
                absPct = Math.Round(100.0 * Math.Abs(delta.Value) / leftPresent.Value, 2);
            else if (delta.HasValue && leftPresent == 0 && rightPresent > 0)
                absPct = 100.0;

            var anomaly = false;
            if (delta.HasValue)
            {
                var absHit = Math.Abs(delta.Value) >= absoluteCountThreshold;
                var pctHit = absPct.HasValue && absPct.Value >= relativePercentThreshold;
                anomaly = (absHit && pctHit)
                    || (leftPresent == 0 && rightPresent >= absoluteCountThreshold)
                    || (rightPresent == 0 && leftPresent >= absoluteCountThreshold);
            }

            presenceRows.Add(new ImportReimportFilePresenceRow
            {
                Metric = metric,
                LeftParent = lp?.ParentCount,
                RightParent = rp?.ParentCount,
                LeftPresent = leftPresent,
                RightPresent = rightPresent,
                DeltaPresent = delta,
                AbsPct = absPct,
                Anomaly = anomaly,
                Notes = lp?.Notes ?? rp?.Notes ?? "",
            });
            if (anomaly)
                anomalies.Add($"File presence {metric} delta={delta} ({absPct}%)");
        }

        return new ImportReimportCompareResult
        {
            LeftRunId = leftRunId,
            RightRunId = rightRunId,
            BoRows = boRows,
            WaveRows = waveRows,
            FileWaveRows = fileWaveRows,
            FilePresenceRows = presenceRows,
            Anomalies = anomalies,
            FileWavesIncludedLeft = leftFiles.Included,
            FileWavesIncludedRight = rightFiles.Included,
            FileWavesNoteLeft = leftFiles.Note,
            FileWavesNoteRight = rightFiles.Note,
            AbsoluteCountThreshold = absoluteCountThreshold,
            RelativePercentThreshold = relativePercentThreshold,
        };
    }

    private Dictionary<string, int?> LoadDbCounts(string runId)
    {
        var map = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(_rootPath!, "runs", runId, "db-counts.json");
        if (!File.Exists(path))
            return map;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("Counts", out var counts) || counts.ValueKind != JsonValueKind.Array)
                return map;

            foreach (var row in EnumerateCountRows(counts))
            {
                var bo = GetString(row, "BO");
                if (string.IsNullOrWhiteSpace(bo))
                    continue;
                map[bo] = GetIntOrNull(row, "Count");
            }
        }
        catch
        {
            // ignore corrupt archive
        }

        return map;
    }

    private Dictionary<string, WaveSnap> LoadWaves(string runId)
    {
        var map = new Dictionary<string, WaveSnap>(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(_rootPath!, "runs", runId, "run-status.json");
        if (!File.Exists(path))
            return map;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("Waves", out var waves) || waves.ValueKind != JsonValueKind.Array)
                return map;

            foreach (var w in waves.EnumerateArray())
            {
                var name = GetString(w, "Name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                map[name] = new WaveSnap(
                    GetString(w, "Status"),
                    GetIntOrNull(w, "Failed"),
                    GetIntOrNull(w, "ExitCode"));
            }
        }
        catch
        {
            // ignore
        }

        return map;
    }

    private FileWavesArchive LoadFileWaves(string runId)
    {
        var result = new FileWavesArchive();
        var path = Path.Combine(_rootPath!, "runs", runId, "file-waves.json");
        if (!File.Exists(path))
        {
            result.Note = "file-waves.json missing (older archive or file waves not run).";
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.TryGetProperty("Included", out var inc) &&
                (inc.ValueKind == JsonValueKind.True || inc.ValueKind == JsonValueKind.False))
                result.Included = inc.GetBoolean();
            result.Note = GetStringOrNull(root, "Note");

            if (root.TryGetProperty("Steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in steps.EnumerateArray())
                {
                    var key = GetString(s, "Key");
                    if (string.IsNullOrWhiteSpace(key))
                        key = GetString(s, "Name");
                    if (string.IsNullOrWhiteSpace(key))
                        continue;
                    result.Steps[key] = new FileStepSnap(
                        GetString(s, "Name"),
                        GetString(s, "Status"),
                        GetIntOrNull(s, "ExitCode"),
                        GetStringOrNull(s, "Posted") ?? GetStringOrNull(s, "PostedLine") ?? "");
                }
            }
        }
        catch
        {
            result.Note = "file-waves.json corrupt.";
        }

        return result;
    }

    private Dictionary<string, PresenceSnap> LoadFilePresence(string runId)
    {
        var map = new Dictionary<string, PresenceSnap>(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(_rootPath!, "runs", runId, "file-presence.json");
        if (!File.Exists(path))
            return map;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("Metrics", out var metrics) || metrics.ValueKind != JsonValueKind.Array)
                return map;

            foreach (var m in metrics.EnumerateArray())
            {
                var name = GetString(m, "Metric");
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                map[name] = new PresenceSnap(
                    GetIntOrNull(m, "ParentCount"),
                    GetIntOrNull(m, "PresentCount"),
                    GetStringOrNull(m, "Notes") ?? "");
            }
        }
        catch
        {
            // ignore
        }

        return map;
    }

    /// <summary>
    /// PowerShell ConvertTo-Json often nests Counts as [[{BO,Count},...]] — unwrap to objects.
    /// </summary>
    private static IEnumerable<JsonElement> EnumerateCountRows(JsonElement counts)
    {
        foreach (var row in counts.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object)
            {
                yield return row;
                continue;
            }

            if (row.ValueKind == JsonValueKind.Array)
            {
                foreach (var nested in EnumerateCountRows(row))
                    yield return nested;
            }
        }
    }

    private static string? ResolveRootPath(
        ImportHistoryOptions options,
        IConfiguration configuration,
        out string? reason)
    {
        if (!string.IsNullOrWhiteSpace(options.RootPath))
        {
            reason = Directory.Exists(options.RootPath)
                ? null
                : $"ImportHistory:RootPath does not exist: {options.RootPath}";
            return options.RootPath;
        }

        var slot = configuration["DeploymentEnvironment:Slot"];
        var fallback = slot switch
        {
            "Demo" => @"C:\visa2026-sync-demo\history",
            "Staging" => @"C:\visa2026-sync-staging\history",
            "Production" => @"C:\visa2026-sync\history",
            _ => null,
        };

        if (fallback != null)
        {
            reason = Directory.Exists(fallback)
                ? null
                : $"Default history path for slot '{slot}' does not exist: {fallback}";
            return fallback;
        }

        reason = "ImportHistory:RootPath is not configured, and DeploymentEnvironment:Slot is not Demo/Staging/Production.";
        return null;
    }

    private static string GetString(JsonElement el, string name) =>
        el.ValueKind != JsonValueKind.Undefined && el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? ""
            : "";

    private static string? GetStringOrNull(JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Undefined || !el.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null)
            return null;
        return p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
    }

    private static int GetInt(JsonElement el, string name) => GetIntOrNull(el, name) ?? 0;

    private static int? GetIntOrNull(JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Undefined || !el.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null)
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i))
            return i;
        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var s))
            return s;
        return null;
    }

    private sealed record WaveSnap(string Status, int? Failed, int? ExitCode);
    private sealed record FileStepSnap(string Name, string Status, int? ExitCode, string Posted);
    private sealed record PresenceSnap(int? ParentCount, int? PresentCount, string Notes);

    private sealed class FileWavesArchive
    {
        public bool Included { get; set; }
        public string? Note { get; set; }
        public Dictionary<string, FileStepSnap> Steps { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
