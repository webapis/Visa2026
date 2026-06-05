namespace Visa2026.Blazor.Server.Services;

public enum DocumentCopyPreviewKind
{
    None,
    Pdf,
    Image
}

public static class DocumentCopyPreviewFormats
{
    public static DocumentCopyPreviewKind GetKind(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty)?.ToLowerInvariant();
        return ext switch
        {
            ".pdf" => DocumentCopyPreviewKind.Pdf,
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".tif" or ".tiff" or ".webp" =>
                DocumentCopyPreviewKind.Image,
            _ => DocumentCopyPreviewKind.None
        };
    }

    public static bool IsPreviewable(string? fileName) =>
        GetKind(fileName) != DocumentCopyPreviewKind.None;
}

public static class DocumentFileContentTypes
{
    public static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tif" or ".tiff" => "image/tiff",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
