using System;
using System.IO;
using DevExpress.EasyTest.Framework;
using Visa2026.E2E.Tests.UserManual;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Milestone browser screenshots for user-manual media — <b>enabled by default</b>.
/// Opt out: <c>VISA2026_E2E_SCREENSHOTS=false</c> (or <c>0</c> / <c>no</c> / <c>off</c>).
/// Files land under <c>Visa2026.E2E.Tests/recordings/screenshots/{runId}/</c> (gitignored with recordings/).
/// </summary>
internal static class EasyTestScreenshotCapture
{
    private static readonly Lazy<string?> OutputDirectory = new(ResolveOutputDirectory);

    /// <summary>True unless explicitly disabled via env.</summary>
    internal static bool Enabled =>
        !IsFalsy(Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOTS"));

    internal static void Capture(IApplicationContext appContext, string label)
    {
        if (!Enabled)
            return;

        string? dir = OutputDirectory.Value;
        if (string.IsNullOrEmpty(dir))
            return;

        EasyTestBlazorNavigationHelper.TryCaptureScreenshot(appContext, dir, label);
        UserManualVideoMarkerCapture.Mark(label);
    }

    private static string? ResolveOutputDirectory()
    {
        try
        {
            string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
                ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            // testhost cwd is bin/EasyTest/net8.0 — walk up to project recordings/
            string dir = Path.GetFullPath(Path.Combine(
                Environment.CurrentDirectory,
                @"..\..\..\recordings\screenshots",
                runId));
            Directory.CreateDirectory(dir);
            Console.WriteLine($"[EasyTest] Screenshot directory: {dir}");
            return dir;
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
}