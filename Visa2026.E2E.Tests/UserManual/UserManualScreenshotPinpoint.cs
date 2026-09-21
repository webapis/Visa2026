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
        const int pad = 8;
        byte[] bytes = File.ReadAllBytes(imagePath);
        using var input = new MemoryStream(bytes);
        using var original = new Bitmap(input);
        using var bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(original, 0, 0, original.Width, original.Height);

            int x = (int)Math.Max(0, Math.Floor(box.X - pad));
            int y = (int)Math.Max(0, Math.Floor(box.Y - pad));
            int w = (int)Math.Min(bitmap.Width - x, Math.Ceiling(box.Width + pad * 2));
            int h = (int)Math.Min(bitmap.Height - y, Math.Ceiling(box.Height + pad * 2));
            if (w > 0 && h > 0)
            {
                var rect = new Rectangle(x, y, w, h);
                using var fillBrush = new SolidBrush(Color.FromArgb(56, 255, 193, 7));
                using var borderPen = new Pen(Color.FromArgb(255, 230, 126, 0), 4f);
                graphics.FillRectangle(fillBrush, rect);
                graphics.DrawRectangle(borderPen, rect);
            }

            float tipX = (float)(box.X + box.Width * 0.7);
            float tipY = (float)(box.Y + box.Height * 0.55);
            DrawMouseCursor(graphics, tipX, tipY);
        }

        string tempPath = imagePath + ".pinpoint.tmp.png";
        bitmap.Save(tempPath, ImageFormat.Png);
        File.Copy(tempPath, imagePath, overwrite: true);
        File.Delete(tempPath);
    }

    private static void DrawMouseCursor(Graphics graphics, float tipX, float tipY)
    {
        PointF[] outline =
        [
            new(tipX, tipY),
            new(tipX + 1.5f, tipY + 18f),
            new(tipX + 6.5f, tipY + 13.5f),
            new(tipX + 12f, tipY + 24f),
            new(tipX + 15.5f, tipY + 22.5f),
            new(tipX + 9.5f, tipY + 12f),
            new(tipX + 17f, tipY + 12f),
        ];

        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(Color.FromArgb(255, 30, 30, 30), 1.6f)
        {
            LineJoin = LineJoin.Round,
            EndCap = LineCap.Round,
        };
        graphics.FillPolygon(fill, outline);
        graphics.DrawPolygon(border, outline);
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
