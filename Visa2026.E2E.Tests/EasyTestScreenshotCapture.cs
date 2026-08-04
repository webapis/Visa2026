using System;
using System.IO;
using DevExpress.EasyTest.Framework;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Milestone browser screenshots when <c>VISA2026_E2E_SCREENSHOTS=true</c>.
/// Files land under <c>Visa2026.E2E.Tests/recordings/screenshots/{runId}/</c> (gitignored with recordings/).
/// </summary>
internal static class EasyTestScreenshotCapture
{
    private static readonly Lazy<string?> OutputDirectory = new(ResolveOutputDirectory);

    internal static bool Enabled =>
        IsTruthy(Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOTS"));

    internal static void Capture(IApplicationContext appContext, string label)
    {
        if (!Enabled)
            return;

        string? dir = OutputDirectory.Value;
        if (string.IsNullOrEmpty(dir))
            return;

        EasyTestBlazorNavigationHelper.TryCaptureScreenshot(appContext, dir, label);
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

    private static bool IsTruthy(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
}