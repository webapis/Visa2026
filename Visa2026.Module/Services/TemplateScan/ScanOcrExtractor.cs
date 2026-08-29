#nullable enable

using Spire.Pdf;
using Spire.Pdf.Texts;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanOcrExtractor
{
    ScanOcrResult Extract(ScanOcrRequest request);
}

/// <summary>
/// Local OCR for S1: PDF embedded text via Spire.
/// Raster images intentionally return no lines — vision (S2) reads PNG page bytes.
/// </summary>
public sealed class ScanOcrExtractor : IScanOcrExtractor
{
    public ScanOcrResult Extract(ScanOcrRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Input.SourceKind switch
        {
            ScanSourceKind.Pdf => ExtractFromPdf(request),
            ScanSourceKind.Image => EmptyResult(),
            _ => EmptyResult(),
        };
    }

    private static ScanOcrResult ExtractFromPdf(ScanOcrRequest request)
    {
        using var document = new PdfDocument();
        document.LoadFromBytes(request.OriginalContent);

        var lines = new List<ScanOcrLine>();
        var totalChars = 0;

        foreach (var pageImage in request.Input.Pages)
        {
            if (pageImage.PageIndex < 0 || pageImage.PageIndex >= document.Pages.Count)
                continue;

            var pdfPage = document.Pages[pageImage.PageIndex];
            var extractor = new PdfTextExtractor(pdfPage);
            var text = extractor.ExtractText(new PdfTextExtractOptions())?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            totalChars += text.Length;
            foreach (var rawLine in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                lines.Add(new ScanOcrLine
                {
                    PageIndex = pageImage.PageIndex,
                    Text = rawLine,
                    Confidence = 0.85,
                });
            }
        }

        var confidence = ComputeConfidence(totalChars, lines.Count);
        return new ScanOcrResult
        {
            Lines = lines,
            TextConfidence = confidence,
        };
    }

    private static ScanOcrResult EmptyResult() =>
        new()
        {
            Lines = Array.Empty<ScanOcrLine>(),
            TextConfidence = 0,
        };

    internal static double ComputeConfidence(int totalChars, int lineCount)
    {
        if (totalChars <= 0 || lineCount <= 0)
            return 0;

        var charScore = Math.Min(1.0, totalChars / 400.0);
        var lineScore = Math.Min(1.0, lineCount / 8.0);
        return Math.Round(Math.Max(charScore, lineScore * 0.75), 3);
    }
}
