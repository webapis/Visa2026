#nullable enable

using Spire.Pdf;

namespace Visa2026.Module.Services.TemplateScan;

public static class ScanPdfMetadataReader
{
    public static int ReadPageCount(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0)
            return 0;

        using var document = new PdfDocument();
        document.LoadFromBytes(content);
        return document.Pages.Count;
    }
}
