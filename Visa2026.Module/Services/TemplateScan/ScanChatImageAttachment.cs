#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Officer PNG/JPG attached on Ask AI only. Wizard Upload still rejects images.
/// </summary>
public static class ScanChatImageAttachment
{
    public static bool TryCreate(
        byte[]? content,
        string? fileName,
        int maxBytes,
        out ScanPageImage image,
        out string error)
    {
        image = null!;
        error = string.Empty;
        if (content is not { Length: > 0 })
        {
            error = "Choose a PNG or JPG image.";
            return false;
        }

        var cap = maxBytes > 0 ? maxBytes : 8_388_608;
        if (content.Length > cap)
        {
            error = $"Image is too large. Maximum is {Math.Max(1, cap / (1024 * 1024))} MB.";
            return false;
        }

        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg"))
        {
            error = "Use a PNG or JPG image.";
            return false;
        }

        if (!ScanImageDimensionReader.TryReadImageDimensions(content, out var width, out var height))
        {
            error = "Could not read that image.";
            return false;
        }

        image = new ScanPageImage
        {
            PageIndex = 0,
            PngBytes = content,
            WidthPx = width,
            HeightPx = height,
            MimeType = extension == ".png" ? "image/png" : "image/jpeg",
        };
        return true;
    }
}