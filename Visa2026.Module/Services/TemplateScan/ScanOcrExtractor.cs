#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using Spire.Pdf;
using Spire.Pdf.Texts;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanOcrExtractor
{
    ScanOcrResult Extract(ScanOcrRequest request);
}

/// <summary>
/// Local text extract: PDF via Spire; Word/Excel via OpenXML/ClosedXML.
/// Raster images intentionally return no lines — vision reads PNG page bytes.
/// </summary>
public sealed class ScanOcrExtractor : IScanOcrExtractor
{
    public ScanOcrResult Extract(ScanOcrRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Input.SourceKind switch
        {
            ScanSourceKind.Pdf => ExtractFromPdf(request),
            ScanSourceKind.Word => ExtractFromWord(request.OriginalContent),
            ScanSourceKind.Excel => ExtractFromExcel(request.OriginalContent),
            ScanSourceKind.Image => EmptyResult(),
            _ => EmptyResult(),
        };
    }

    private static ScanOcrResult ExtractFromWord(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var lines = new List<ScanOcrLine>();
        var totalChars = 0;
        foreach (var addressed in WordTemplateAddressing.EnumerateParagraphs(document))
        {
            var text = WordTemplateAddressing.GetParagraphText(addressed.Paragraph)?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;
            totalChars += text.Length;
            lines.Add(new ScanOcrLine { PageIndex = 0, Text = text, Confidence = 1.0 });
        }

        return new ScanOcrResult
        {
            Lines = lines,
            TextConfidence = lines.Count == 0 ? 0 : 1.0,
        };
    }

    private static ScanOcrResult ExtractFromExcel(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var workbook = new XLWorkbook(stream);
        var lines = new List<ScanOcrLine>();
        foreach (var sheet in workbook.Worksheets)
        {
            foreach (var cell in sheet.CellsUsed())
            {
                var text = cell.GetFormattedString()?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    text = cell.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                lines.Add(new ScanOcrLine
                {
                    PageIndex = 0,
                    Text = $"{sheet.Name}!{cell.Address}: {text}",
                    Confidence = 1.0,
                });
            }
        }

        return new ScanOcrResult
        {
            Lines = lines,
            TextConfidence = lines.Count == 0 ? 0 : 1.0,
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