using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Visa2026.E2E.Tests.UserManual;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>
/// Milestone screenshots for user-manual media — same labels/paths as EasyTest capture.
/// Enabled unless <c>VISA2026_E2E_SCREENSHOTS=false</c>.
/// </summary>
internal static class PlaywrightScreenshotCapture
{
    private static readonly Lazy<string?> OutputDirectory = new(ResolveOutputDirectory);

    internal static bool Enabled => !IsFalsy(Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOTS"));

    internal static Task CaptureAsync(IPage page, string label) =>
        CaptureAsync(page, label, pinpointTarget: null);

    internal static async Task CaptureAsync(IPage page, string label, ILocator? pinpointTarget)
    {
        if (!Enabled)
            return;

        string? dir = OutputDirectory.Value;
        if (string.IsNullOrEmpty(dir))
            return;

        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        string safe = string.Join("_", (label ?? "shot").Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(dir, $"{safe}-{stamp}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = false });
        await UserManualScreenshotPinpoint.TryApplyAsync(label, path, pinpointTarget);
        Console.WriteLine($"[Playwright] Screenshot: {path}");
        UserManualVideoMarkerCapture.Mark(label);
    }

    private static string? ResolveOutputDirectory()
    {
        try
        {
            string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
                ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string dir = Path.GetFullPath(Path.Combine(
                Environment.CurrentDirectory,
                @"..\..\..\recordings\screenshots",
                runId));
            Directory.CreateDirectory(dir);
            Console.WriteLine($"[Playwright] Screenshot directory: {dir}");
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
