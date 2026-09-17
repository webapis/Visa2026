#nullable enable

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Paragraph-relative boxes for Word Review marks, as percentages of the printed page
/// (header band, then body, then footer). Table cells use the OpenXML grid (row/column)
/// so pdf.js can snap sample text to the printed column instead of the first TUR/E hit.
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
                if (!byAddress.TryGetValue(address, out var para) || !bands.Box.TryGetValue(address, out var slot))
                    continue;

                var text = WordTemplateAddressing.GetParagraphText(para.Paragraph);
                var textLen = Math.Max(text.Length, 1);
                var clampedStart = Math.Min(start, textLen - 1);
                var clampedLen = Math.Min(length, textLen - clampedStart);

                if (slot.IsTableCell)
                {
                    var frac = textLen <= 1 ? 0d : (double)clampedStart / textLen;
                    var spanFrac = Math.Max(clampedLen / (double)textLen, 0.12);
                    boxes[mark.DisplayId] = new ScanExcelPreviewMarkBox(
                        Left: 100d * (slot.Left + slot.Width * frac),
                        Top: 100d * slot.Top / bands.Total,
                        Width: Math.Max(0.45, 100d * slot.Width * Math.Min(spanFrac, 1d - frac)),
                        Height: Math.Max(1.4, 100d * slot.Height / bands.Total));
                    continue;
                }

                var paraHeight = slot.Height;
                var lineChars = 72;
                var lineCount = Math.Max(1, (textLen + lineChars - 1) / lineChars);
                var lineH = paraHeight / lineCount;
                var line = clampedStart / lineChars;
                var col = clampedStart % lineChars;
                var spanLines = Math.Max(1, (clampedLen + lineChars - 1) / lineChars);

                boxes[mark.DisplayId] = new ScanExcelPreviewMarkBox(
                    Left: 8d + 84d * col / lineChars,
                    Top: 100d * (slot.Top + line * lineH) / bands.Total,
                    Width: Math.Max(4d, 84d * Math.Min(clampedLen, lineChars) / lineChars),
                    Height: Math.Max(1.4d, 100d * spanLines * lineH / bands.Total));
            }

            SplitSharedCellBoxesForCompoundParts(wordMarks, boxes);

            if (boxes.Count == 0)
                return null;

            var tableIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (mark, address, _, _) in wordMarks)
            {
                if (!byAddress.TryGetValue(address, out var para) || !para.IsInTable)
                    continue;
                tableIds.Add(mark.DisplayId);
            }

            return new ScanExcelPreviewMarkGeometry.Layout(1.414, boxes, tableIds);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private readonly record struct ParaSlot(double Left, double Top, double Width, double Height, bool IsTableCell);

    private readonly record struct Bands(
        double Total,
        Dictionary<string, ParaSlot> Box);

    private readonly record struct LayoutBlock(
        WordParagraphAddress? Paragraph,
        Table? Table,
        IReadOnlyList<WordParagraphAddress> TableParas,
        double Weight);

    private static Bands BuildBands(IReadOnlyList<WordParagraphAddress> paragraphs)
    {
        var headers = paragraphs.Where(static p => p.Part == WordPart.Header).ToList();
        var body = paragraphs.Where(static p => p.Part == WordPart.Body).ToList();
        var footers = paragraphs.Where(static p => p.Part == WordPart.Footer).ToList();

        var headerShare = headers.Count == 0 ? 0d : 0.10;
        var footerShare = footers.Count == 0 ? 0d : 0.08;
        var bodyShare = Math.Max(0.55, 1d - headerShare - footerShare);

        var box = new Dictionary<string, ParaSlot>(StringComparer.OrdinalIgnoreCase);
        Place(headers, 0, headerShare, box);
        Place(body, headerShare, bodyShare, box);
        Place(footers, headerShare + bodyShare, footerShare, box);
        return new Bands(1d, box);
    }

    private static void Place(
        IReadOnlyList<WordParagraphAddress> items,
        double bandTop,
        double bandHeight,
        Dictionary<string, ParaSlot> dest)
    {
        if (items.Count == 0 || bandHeight <= 0)
            return;

        var blocks = GroupBlocks(items);
        var sum = blocks.Sum(static b => b.Weight);
        if (sum <= 0)
            return;

        var cursor = bandTop;
        foreach (var block in blocks)
        {
            var h = bandHeight * block.Weight / sum;
            if (block.Table != null)
                PlaceTable(block.Table, block.TableParas, cursor, h, dest);
            else if (block.Paragraph != null)
            {
                dest[block.Paragraph.Address] = new ParaSlot(0, cursor, 1, h, false);
            }

            cursor += h;
        }
    }

    private static List<LayoutBlock> GroupBlocks(IReadOnlyList<WordParagraphAddress> items)
    {
        var blocks = new List<LayoutBlock>();
        var i = 0;
        while (i < items.Count)
        {
            var table = InnermostTable(items[i].Paragraph);
            if (table == null)
            {
                var paraWeight = Math.Max(WordTemplateAddressing.GetParagraphText(items[i].Paragraph).Length, 12);
                blocks.Add(new LayoutBlock(items[i], null, Array.Empty<WordParagraphAddress>(), paraWeight));
                i++;
                continue;
            }

            var paras = new List<WordParagraphAddress>();
            var textSum = 0;
            while (i < items.Count && ReferenceEquals(InnermostTable(items[i].Paragraph), table))
            {
                paras.Add(items[i]);
                textSum += WordTemplateAddressing.GetParagraphText(items[i].Paragraph).Length;
                i++;
            }

            var rows = Math.Max(table.Elements<TableRow>().Count(), 1);
            var tableWeight = Math.Max(rows * 40d, Math.Max(textSum, 24));
            blocks.Add(new LayoutBlock(null, table, paras, tableWeight));
        }

        return blocks;
    }

    private static void PlaceTable(
        Table table,
        IReadOnlyList<WordParagraphAddress> cellParas,
        double tableTop,
        double tableHeight,
        Dictionary<string, ParaSlot> dest)
    {
        var rows = table.Elements<TableRow>().ToList();
        var rowCount = Math.Max(rows.Count, 1);
        var rowH = tableHeight / rowCount;
        var colWidths = ColumnFractions(table, rows);

        foreach (var para in cellParas)
        {
            var cell = para.Paragraph.Ancestors<TableCell>().FirstOrDefault();
            var row = para.Paragraph.Ancestors<TableRow>().FirstOrDefault();
            if (cell == null || row == null)
            {
                dest[para.Address] = new ParaSlot(0, tableTop, 1, tableHeight, true);
                continue;
            }

            var rowIndex = Math.Max(rows.IndexOf(row), 0);
            var colIndex = ColumnIndex(row, cell);
            var span = GridSpan(cell);
            var left = Sum(colWidths, 0, colIndex);
            var width = Math.Max(Sum(colWidths, colIndex, span), 0.04);
            dest[para.Address] = new ParaSlot(left, tableTop + rowIndex * rowH, width, rowH, true);
        }
    }

    private static double[] ColumnFractions(Table table, IReadOnlyList<TableRow> rows)
    {
        var grid = table.Elements<TableGrid>().FirstOrDefault();
        var cols = grid?.Elements<GridColumn>().ToList() ?? [];
        if (cols.Count > 0)
        {
            var widths = new double[cols.Count];
            var sum = 0d;
            for (var i = 0; i < cols.Count; i++)
            {
                widths[i] = Math.Max(ParseDxa(cols[i].Width?.Value), 1);
                sum += widths[i];
            }

            if (sum > 0)
            {
                for (var i = 0; i < widths.Length; i++)
                    widths[i] /= sum;
                return widths;
            }
        }

        var count = 1;
        if (rows.Count > 0)
        {
            var inferred = 0;
            foreach (var cell in rows[0].Elements<TableCell>())
                inferred += GridSpan(cell);
            count = Math.Max(inferred, 1);
        }

        var even = new double[count];
        var part = 1d / count;
        for (var i = 0; i < count; i++)
            even[i] = part;
        return even;
    }

    private static int ColumnIndex(TableRow row, TableCell target)
    {
        var col = 0;
        foreach (var cell in row.Elements<TableCell>())
        {
            if (ReferenceEquals(cell, target))
                return col;
            col += GridSpan(cell);
        }

        return col;
    }

    private static int GridSpan(TableCell cell)
    {
        var span = cell.TableCellProperties?.GridSpan?.Val;
        return span != null && span.HasValue && span.Value > 0 ? span.Value : 1;
    }

    private static double Sum(IReadOnlyList<double> values, int start, int count)
    {
        var total = 0d;
        var end = Math.Min(values.Count, start + Math.Max(count, 0));
        for (var i = Math.Max(start, 0); i < end; i++)
            total += values[i];
        return total;
    }

    private static double ParseDxa(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 1;
        return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n)
            ? Math.Max(n, 1)
            : 1;
    }

    private static Table? InnermostTable(Paragraph paragraph) =>
        paragraph.Ancestors<Table>().FirstOrDefault();

    private static void SplitSharedCellBoxesForCompoundParts(
        IReadOnlyList<(ScanReviewOrderedField Mark, string Address, int Start, int Length)> wordMarks,
        Dictionary<string, ScanExcelPreviewMarkBox> boxes)
    {
        foreach (var group in wordMarks
                     .Where(static row => row.Mark.PartIndex > 0)
                     .GroupBy(static row => row.Address, StringComparer.OrdinalIgnoreCase))
        {
            var parts = group.OrderBy(static row => row.Mark.PartIndex).ToList();
            if (parts.Count <= 1)
                continue;
            if (!boxes.TryGetValue(parts[0].Mark.DisplayId, out var full))
                continue;

            var slice = full.Width / parts.Count;
            for (var i = 0; i < parts.Count; i++)
            {
                boxes[parts[i].Mark.DisplayId] = new ScanExcelPreviewMarkBox(
                    Left: full.Left + slice * i,
                    Top: full.Top,
                    Width: Math.Max(slice, 0.35),
                    Height: full.Height);
            }
        }
    }
}