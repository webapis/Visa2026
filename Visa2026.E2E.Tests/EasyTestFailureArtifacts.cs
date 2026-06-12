using System;
using System.IO;
using System.Text;
using DevExpress.EasyTest.Framework;
using OpenQA.Selenium;

namespace Visa2026.E2E.Tests;

/// <summary>Captures browser state when an EasyTest scenario fails (PNG + URL sidecar).</summary>
internal static class EasyTestFailureArtifacts
{
    internal static string ScreenshotDirectory =>
        Path.Combine(AppContext.BaseDirectory, "easytest-failure-screenshots");

    internal static string? CaptureScreenshot(IApplicationContext appContext, string testLabel)
    {
        if (EasyTestBlazorNavigationHelper.TryGetWebDriver(appContext) is not ITakesScreenshot screenshotDriver)
            return null;

        try
        {
            Directory.CreateDirectory(ScreenshotDirectory);

            string safeName = SanitizeFileName(testLabel);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            string baseName = $"{safeName}-{timestamp}";
            string pngPath = Path.Combine(ScreenshotDirectory, $"{baseName}.png");

            byte[] pngBytes = screenshotDriver.GetScreenshot().AsByteArray;
            File.WriteAllBytes(pngPath, pngBytes);

            string url = EasyTestBlazorNavigationHelper.GetCurrentUrl(appContext);
            string sidecarPath = Path.Combine(ScreenshotDirectory, $"{baseName}.txt");
            File.WriteAllText(
                sidecarPath,
                $"Test: {testLabel}{Environment.NewLine}URL: {url}{Environment.NewLine}CapturedUtc: {DateTime.UtcNow:O}{Environment.NewLine}",
                Encoding.UTF8);

            return pngPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EasyTest] Failure screenshot capture failed: {ex.Message}");
            return null;
        }
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "easytest-failure";

        var builder = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_');
        }

        return builder.ToString();
    }
}
