#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Azure chat <c>image_url</c> data URLs. Skips 1x1 Analyze placeholders.</summary>
internal static class ScanVisionImageDataUrl
{
    internal static bool TryGetDataUrl(ScanPageImage? page, out string dataUrl)
    {
        dataUrl = string.Empty;
        if (page?.PngBytes is not { Length: > 32 })
            return false;
        if (page.WidthPx <= 1 && page.HeightPx <= 1)
            return false;

        var mime = string.IsNullOrWhiteSpace(page.MimeType)
            ? GuessMime(page.PngBytes)
            : page.MimeType.Trim();
        dataUrl = $"data:{mime};base64,{Convert.ToBase64String(page.PngBytes)}";
        return true;
    }

    internal static string GuessMime(byte[] content)
    {
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
            return "image/jpeg";
        return "image/png";
    }
}