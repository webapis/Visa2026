#nullable enable

using DocumentFormat.OpenXml.Packaging;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Paragraph-relative boxes for Word Review marks, as percentages of the printed page
/// (header band, then body, then footer). pdf.js uses these when sample-text match misses
/// or would land on the wrong duplicate (two dates, a lone 2).
/// </summary>
public static class ScanWordPreviewMarkGeometry
{
    public static ScanExcelPreviewMarkGeometry.Layout? TryMap(
        byte[]? wordBytes,
        IReadOnlyList<ScanReviewOrderedField> marks)
    {
        if (wordBytes is not { Length: > 64 } || marks == null || marks.Count == 0)
            return null;

        var wordMarks = new List<(ScanReviewOrderedField Mark, string Address, int Start, int Length)>();
        foreach (var mark in marks)
        {
            switch (mark.DisplayRegion)
            {
                case DocumentRegion.WordSpan span when !string.IsNullOrWhiteSpace(span.ParagraphAddress):
                    wordMarks.Add((mark, span.ParagraphAddress, Math.Max(0, span.Start), Math.Max(1, span.Length)));
                    break;
                case DocumentRegion.WordDrawing drawing when !string.IsNullOrWhiteSpace(drawing.ParagraphAddress):
                    wordMarks.Add((mark, drawing.ParagraphAddress, Math.Max(0, drawing.TextInsertOffset), 4));
                    break;
            }
        }

        if (wordMarks.Count == 0)
            return null;

        try
        {
            using var stream = new MemoryStream(wordBytes, writable: false);
            using var document = WordprocessingDocument.Open(stream, false);
            var paragraphs = WordTemplateAddressing.EnumerateParagraphs(document);
            if (paragraphs.Count == 0)
                return null;

            var byAddress = paragraphs.ToDictionary(static p => p.Address, StringComparer.OrdinalIgnoreCase);
            var bands = BuildBands(paragraphs);
            if (bands.Total <= 0.01)
                return null;

            var boxes = new Dictionary<string, ScanExcelPreviewMarkBox>(StringComparer.Ordinal);
            foreach (var (mark, address, start, length) in wordMarks)
            {
                if (!byAddress.TryGetValue(address, out var para) || !bands.Top.TryGetValue(address, out var paraTop))
                    continue;

                var text = WordTemplateAddressing.GetParagraphText(para.Paragraph);
                var textLen = Math.Max(text.Length, 1);
                var paraHeight = bands.Height[address];
                var clampedStart = Math.Min(start, textLen - 1);
                var clampedLen = Math.Min(length, textLen - clampedStart);
                var lineChars = 72;
                var lineCount = Math.Max(1, (textLen + lineChars - 1) / lineChars);
                var lineH = paraHeight / lineCount;
                var line = clampedStart / lineChars;
                var col = clampedStart % lineChars;
                var spanLines = Math.Max(1, (clampedLen + lineChars - 1) / lineChars);

                boxes[mark.DisplayId] = new ScanExcelPreviewMarkBox(
                    Left: 8d + 84d * col / lineChars,
                    Top: 100d * (paraTop + line * lineH) / bands.Total,
                    Width: Math.Max(4d, 84d * Math.Min(clampedLen, lineChars) / lineChars),
                    Height: Math.Max(1.4d, 100d * spanLines * lineH / bands.Total));
            }

            if (boxes.Count == 0)
                return null;

            return new ScanExcelPreviewMarkGeometry.Layout(1.414, boxes);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private readonly record struct Bands(
        double Total,
        Dictionary<string, double> Top,
        Dictionary<string, double> Height);

    private static Bands BuildBands(IReadOnlyList<WordParagraphAddress> paragraphs)
    {
        var headers = paragraphs.Where(static p => p.Part == WordPart.Header).ToList();
        var body = paragraphs.Where(static p => p.Part == WordPart.Body).ToList();
        var footers = paragraphs.Where(static p => p.Part == WordPart.Footer).ToList();

        var headerShare = headers.Count == 0 ? 0d : 0.10;
        var footerShare = footers.Count == 0 ? 0d : 0.08;
        var bodyShare = Math.Max(0.55, 1d - headerShare - footerShare);

        var top = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var height = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        Place(headers, 0, headerShare, top, height);
        Place(body, headerShare, bodyShare, top, height);
        Place(footers, headerShare + bodyShare, footerShare, top, height);

        return new Bands(1d, top, height);
    }

    private static void Place(
        IReadOnlyList<WordParagraphAddress> items,
        double bandTop,
        double bandHeight,
        Dictionary<string, double> top,
        Dictionary<string, double> height)
    {
        if (items.Count == 0 || bandHeight <= 0)
            return;

        var weights = new double[items.Count];
        var sum = 0d;
        for (var i = 0; i < items.Count; i++)
        {
            var len = WordTemplateAddressing.GetParagraphText(items[i].Paragraph).Length;
            weights[i] = Math.Max(len, 12);
            sum += weights[i];
        }

        var cursor = bandTop;
        for (var i = 0; i < items.Count; i++)
        {
            var h = bandHeight * weights[i] / sum;
            top[items[i].Address] = cursor;
            height[items[i].Address] = h;
            cursor += h;
        }
    }
}