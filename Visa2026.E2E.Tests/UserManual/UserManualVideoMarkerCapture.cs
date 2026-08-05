using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Visa2026.E2E.Tests.UserManual;

/// <summary>
/// Elapsed-time markers for doc-anchored guide video trim. Written beside milestone PNGs
/// under <c>recordings/screenshots/{runId}/video-markers.json</c>.
/// </summary>
internal static class UserManualVideoMarkerCapture
{
    private static readonly object Sync = new();
    private static DateTimeOffset? _recordingStartUtc;
    private static string? _sourceVideoPath;
    private static readonly Dictionary<string, double> Markers = new(StringComparer.Ordinal);

    internal static bool Enabled =>
        !IsFalsy(Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOTS"));

    internal static bool VideoRecordingEnabled =>
        Enabled && IsTruthy(Environment.GetEnvironmentVariable("VISA2026_E2E_VIDEO_RECORDING"));

    internal static void SetRecordingStart(DateTimeOffset? startUtc = null)
    {
        lock (Sync)
        {
            _recordingStartUtc = startUtc ?? DateTimeOffset.UtcNow;
        }
    }

    internal static void SetSourceVideoPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        lock (Sync)
        {
            _sourceVideoPath = path;
        }
    }

    internal static void Mark(string captureKey)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(captureKey))
            return;

        var start = ResolveRecordingStart();
        if (start == null)
            return;

        double elapsed = Math.Max(0, (DateTimeOffset.UtcNow - start.Value).TotalSeconds);
        lock (Sync)
        {
            Markers[captureKey] = elapsed;
        }

        FlushToDisk();
    }

    internal static void FlushToDisk(bool final = false)
    {
        string? outputPath = ResolveMarkerFilePath();
        if (string.IsNullOrEmpty(outputPath))
            return;

        Dictionary<string, double> snapshot;
        DateTimeOffset? started;
        string? sourceVideo;
        lock (Sync)
        {
            snapshot = new Dictionary<string, double>(Markers, StringComparer.Ordinal);
            started = _recordingStartUtc ?? ResolveRecordingStart();
            sourceVideo = _sourceVideoPath;
        }

        if (snapshot.Count == 0 && !final)
            return;

        string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
            ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["version"] = 1,
            ["mediaE2eRunId"] = runId,
            ["recordingStartedAt"] = started?.ToString("o"),
            ["sourceVideoPath"] = sourceVideo,
            ["markers"] = snapshot,
        };

        try
        {
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, json);
            if (final)
                Console.WriteLine($"[UserManual] Video markers: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserManual] Video marker write failed: {ex.Message}");
        }
    }

    private static DateTimeOffset? ResolveRecordingStart()
    {
        if (_recordingStartUtc != null)
            return _recordingStartUtc;

        string? env = Environment.GetEnvironmentVariable("VISA2026_E2E_VIDEO_RECORDING_START");
        if (DateTimeOffset.TryParse(env, out DateTimeOffset parsed))
            return parsed;

        return null;
    }

    private static string? ResolveMarkerFilePath()
    {
        try
        {
            string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
                ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string dir = Path.GetFullPath(Path.Combine(
                Environment.CurrentDirectory,
                @"..\..\..\recordings\screenshots",
                runId));
            return Path.Combine(dir, "video-markers.json");
        }
        catch
        {
            return null;
        }
    }

    private static bool IsFalsy(string? value) =>
        string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "no", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
}
