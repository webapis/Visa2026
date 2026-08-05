using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>
/// Always-on failure artifacts for Playwright E2E (independent of milestone screenshot setting).
/// Writes PNG + HTML + step note under <c>recordings/screenshots/{runId}/failures/</c>.
/// </summary>
internal static class PlaywrightFailureCapture
{
    private static bool _artifactsWritten;
    private static string? _pendingStep;
    private static Exception? _pendingException;

    internal static void ResetForTest()
    {
        _artifactsWritten = false;
        _pendingStep = null;
        _pendingException = null;
    }

    internal static void RegisterFailure(string stepName, Exception exception)
    {
        _pendingStep = stepName;
        _pendingException = exception;
    }

    /// <summary>Final safety net — call before closing the browser page on failed tests.</summary>
    internal static async Task CaptureBeforeExitAsync(IPage? page)
    {
        if (page == null || _pendingException == null || _artifactsWritten)
            return;

        await CaptureAsync(page, $"{_pendingStep ?? "test"}-before-exit", _pendingException,
            detail: "Safety-net capture before fixture dispose / browser close");
    }

    internal static async Task CaptureAsync(
        IPage page,
        string stepName,
        Exception? exception = null,
        string? detail = null)
    {
        if (exception != null)
            RegisterFailure(stepName, exception);

        try
        {
            string dir = ResolveFailuresDirectory();
            Directory.CreateDirectory(dir);

            string safeStep = SanitizeFileName(stepName);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            string baseName = $"{safeStep}-{stamp}";
            string pngPath = Path.Combine(dir, $"{baseName}.png");
            string htmlPath = Path.Combine(dir, $"{baseName}.html");
            string notePath = Path.Combine(dir, $"{baseName}.txt");

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = pngPath,
                FullPage = true,
            });

            await File.WriteAllTextAsync(htmlPath, await page.ContentAsync(), Encoding.UTF8);

            var note = new StringBuilder();
            note.AppendLine($"step: {stepName}");
            note.AppendLine($"url: {page.Url}");
            note.AppendLine($"capturedAtUtc: {DateTime.UtcNow:O}");
            note.AppendLine($"png: {pngPath}");
            note.AppendLine($"html: {htmlPath}");
            if (!string.IsNullOrWhiteSpace(detail))
                note.AppendLine($"detail: {detail}");
            if (exception != null)
            {
                note.AppendLine($"exception: {exception.GetType().Name}");
                note.AppendLine(exception.Message);
                note.AppendLine(exception.StackTrace);
            }

            await File.WriteAllTextAsync(notePath, note.ToString(), Encoding.UTF8);
            _artifactsWritten = true;

            Console.WriteLine($"[Playwright] FAILURE capture PNG: {pngPath}");
            Console.WriteLine($"[Playwright] FAILURE capture HTML: {htmlPath}");
            Console.WriteLine($"[Playwright] FAILURE capture note: {notePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Playwright] FAILURE capture failed for '{stepName}': {ex.Message}");
        }
    }

    internal static string ResolveFailuresDirectory()
    {
        string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
            ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return Path.GetFullPath(Path.Combine(
            Environment.CurrentDirectory,
            @"..\..\..\recordings\screenshots",
            runId,
            "failures"));
    }

    private static string SanitizeFileName(string value) =>
        string.Join("_", (value ?? "failure").Split(Path.GetInvalidFileNameChars()));
}
