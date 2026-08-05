using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Visa2026.E2E.Tests.UserManual;

/// <summary>
/// Click-target highlight for officer manual screenshots (Guidde/Scribe-style pinpoint, in-repo).
/// Bounding boxes are recorded in <c>pinpoints.json</c> and burned into the PNG after capture.
/// </summary>
internal static class UserManualScreenshotPinpoint
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, PinpointRect> Captures = new(StringComparer.Ordinal);

    internal static bool Enabled =>
        !IsFalsy(Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOTS"))
        && !IsFalsy(Environment.GetEnvironmentVariable("VISA2026_E2E_PINPOINTS"));

    internal static async Task TryApplyAsync(string captureKey, string imagePath, ILocator? target)
    {
        if (!Enabled || target == null || string.IsNullOrWhiteSpace(captureKey))
            return;

        try
        {
            LocatorBoundingBoxResult? box = await target.BoundingBoxAsync(new LocatorBoundingBoxOptions { Timeout = 8_000 });
            if (box == null || box.Width <= 0 || box.Height <= 0)
            {
                Console.WriteLine($"[UserManual] Pinpoint skipped for '{captureKey}' — target not visible.");
                return;
            }

            var rect = new PinpointRect(box.X, box.Y, box.Width, box.Height);
            lock (Sync)
            {
                Captures[captureKey] = rect;
            }

            BurnIntoImage(imagePath, rect);
            Console.WriteLine($"[UserManual] Pinpoint applied: {captureKey} ({rect.X:F0},{rect.Y:F0} {rect.Width:F0}x{rect.Height:F0})");
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"[UserManual] Pinpoint skipped for '{captureKey}' — target not visible.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserManual] Pinpoint burn failed for '{captureKey}': {ex.Message}");
        }

        FlushToDisk();
    }

    internal static void FlushToDisk()
    {
        string? outputPath = ResolvePinpointFilePath();
        if (string.IsNullOrEmpty(outputPath))
            return;

        Dictionary<string, PinpointRect> snapshot;
        lock (Sync)
        {
            snapshot = new Dictionary<string, PinpointRect>(Captures, StringComparer.Ordinal);
        }

        if (snapshot.Count == 0)
            return;

        string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
            ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["version"] = 1,
            ["mediaE2eRunId"] = runId,
            ["captures"] = snapshot,
        };

        try
        {
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserManual] Pinpoint manifest write failed: {ex.Message}");
        }
    }

    internal static void BurnIntoImage(string imagePath, PinpointRect box)
    {
        const int pad = 6;
        using var bitmap = new Bitmap(imagePath);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        int x = (int)Math.Max(0, Math.Floor(box.X - pad));
        int y = (int)Math.Max(0, Math.Floor(box.Y - pad));
        int w = (int)Math.Min(bitmap.Width - x, Math.Ceiling(box.Width + pad * 2));
        int h = (int)Math.Min(bitmap.Height - y, Math.Ceiling(box.Height + pad * 2));
        if (w <= 0 || h <= 0)
            return;

        var rect = new Rectangle(x, y, w, h);

        using var fillBrush = new SolidBrush(Color.FromArgb(48, 255, 193, 7));
        using var borderPen = new Pen(Color.FromArgb(255, 230, 126, 0), 3.5f);
        graphics.FillRectangle(fillBrush, rect);
        graphics.DrawRectangle(borderPen, rect);

        float pointerX = (float)(box.X + box.Width / 2);
        float pointerY = (float)(box.Y + box.Height);
        float radius = 9f;
        using var pointerBrush = new SolidBrush(Color.FromArgb(255, 230, 126, 0));
        using var pointerBorder = new Pen(Color.FromArgb(255, 180, 90, 0), 2f);
        graphics.FillEllipse(pointerBrush, pointerX - radius, pointerY - radius / 2, radius * 2, radius);
        graphics.DrawEllipse(pointerBorder, pointerX - radius, pointerY - radius / 2, radius * 2, radius);

        bitmap.Save(imagePath, ImageFormat.Png);
    }

    private static string? ResolvePinpointFilePath()
    {
        try
        {
            string runId = Environment.GetEnvironmentVariable("VISA2026_E2E_SCREENSHOT_RUN")
                ?? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string dir = Path.GetFullPath(Path.Combine(
                Environment.CurrentDirectory,
                @"..\..\..\recordings\screenshots",
                runId));
            return Path.Combine(dir, "pinpoints.json");
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

    internal sealed record PinpointRect(double X, double Y, double Width, double Height);
}
