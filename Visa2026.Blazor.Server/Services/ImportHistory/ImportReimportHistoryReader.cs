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
        if (!IsAvailable || _rootPath == null)
            return null;
        if (string.IsNullOrWhiteSpace(leftRunId) || string.IsNullOrWhiteSpace(rightRunId))
            return null;

        var leftCounts = LoadDbCounts(leftRunId);
        var rightCounts = LoadDbCounts(rightRunId);
        var leftWaves = LoadWaves(leftRunId);
        var rightWaves = LoadWaves(rightRunId);

        var anomalies = new List<string>();
        var allBos = leftCounts.Keys.Union(rightCounts.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var boRows = new List<ImportReimportBoCountRow>();
        foreach (var bo in allBos)
        {
            leftCounts.TryGetValue(bo, out var left);
            rightCounts.TryGetValue(bo, out var right);
            int? delta = left.HasValue && right.HasValue ? right.Value - left.Value : null;
            double? pct = null;
            if (delta.HasValue && left.HasValue && left.Value > 0)
                pct = Math.Round(100.0 * Math.Abs(delta.Value) / left.Value, 2);
            else if (delta.HasValue && left == 0 && right > 0)
                pct = 100.0;

            var isAnomaly = false;
            if (delta.HasValue)
            {
                var absHit = Math.Abs(delta.Value) >= absoluteCountThreshold;
                var pctHit = pct.HasValue && pct.Value >= relativePercentThreshold;
                isAnomaly = (absHit && pctHit)
                    || (left == 0 && right >= absoluteCountThreshold)
                    || (right == 0 && left >= absoluteCountThreshold);
            }

            if (isAnomaly)
                anomalies.Add($"DbCount {bo} delta={delta} ({pct}%)");

            boRows.Add(new ImportReimportBoCountRow
            {
                BO = bo,
                Left = left,
                Right = right,
                Delta = delta,
                AbsPct = pct,
                Anomaly = isAnomaly,
            });
        }

        var waveNames = leftWaves.Keys.Union(rightWaves.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var waveRows = new List<ImportReimportWaveRow>();
        foreach (var name in waveNames)
        {
            leftWaves.TryGetValue(name, out var lw);
            rightWaves.TryGetValue(name, out var rw);
            var regressed = false;
            if (lw != null && rw != null)
            {
                if (string.Equals(lw.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(rw.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                    regressed = true;
                var lf = lw.Failed ?? 0;
                var rf = rw.Failed ?? 0;
                if (rf > lf)
                    regressed = true;
            }

            if (regressed)
                anomalies.Add($"Wave {name} regressed");

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
        }

        return new ImportReimportCompareResult
        {
            LeftRunId = leftRunId,
            RightRunId = rightRunId,
            BoRows = boRows,
            WaveRows = waveRows,
            Anomalies = anomalies,
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

            foreach (var row in counts.EnumerateArray())
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

    private static string? ResolveRootPath(
        ImportHistoryOptions options,
        IConfiguration configuration,
        out string? reason)
    {
        reason = null;
        if (!string.IsNullOrWhiteSpace(options.RootPath))
            return options.RootPath.Trim();

        var slot = configuration["DeploymentEnvironment:Slot"]
            ?? configuration["DeploymentEnvironment:Profile"]
            ?? "";
        var mapped = slot.Trim().ToLowerInvariant() switch
        {
            "demo" => @"C:\visa2026-sync-demo\history",
            "staging" => @"C:\visa2026-sync-staging\history",
            "production" => @"C:\visa2026-sync\history",
            _ => null,
        };

        if (mapped != null)
            return mapped;

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

    private static int GetInt(JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Undefined || !el.TryGetProperty(name, out var p))
            return 0;
        return p.TryGetInt32(out var v) ? v : 0;
    }

    private static int? GetIntOrNull(JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Undefined || !el.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null)
            return null;
        return p.TryGetInt32(out var v) ? v : null;
    }

    private sealed record WaveSnap(string Status, int? Failed, int? ExitCode);
}
