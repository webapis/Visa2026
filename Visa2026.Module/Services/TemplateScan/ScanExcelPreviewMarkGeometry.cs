#nullable enable

using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Used-range box for one Review mark, as percentages of the printed first sheet.</summary>
public readonly record struct ScanExcelPreviewMarkBox(double Left, double Top, double Width, double Height);

/// <summary>
/// Maps Excel yellow cells onto the first-sheet used range so Review pdf.js marks
/// can sit on the printed table instead of matching short sample text (1, TUR, …).
/// </summary>
public static class ScanExcelPreviewMarkGeometry
{
    public sealed record Layout(
        double Aspect,
        IReadOnlyDictionary<string, ScanExcelPreviewMarkBox> Boxes);

    public static Layout? TryMap(byte[]? workbookBytes, IReadOnlyList<ScanReviewOrderedField> marks)
    {
        if (workbookBytes is not { Length: > 64 } || marks == null || marks.Count == 0)
            return null;

        var excelMarks = new List<(ScanReviewOrderedField Mark, DocumentRegion.ExcelCell Cell)>();
        foreach (var mark in marks)
        {
            if (mark.DisplayRegion is DocumentRegion.ExcelCell cell
                && !string.IsNullOrWhiteSpace(cell.CellReference))
            {
                excelMarks.Add((mark, cell));
            }
        }

        if (excelMarks.Count == 0)
            return null;

        try
        {
            using var stream = new MemoryStream(workbookBytes, writable: false);
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
                return null;

            var used = sheet.RangeUsed();
            if (used == null)
                return null;

            var firstCol = used.FirstColumn().ColumnNumber();
            var lastCol = used.LastColumn().ColumnNumber();
            var firstRow = used.FirstRow().RowNumber();
            var lastRow = used.LastRow().RowNumber();

            foreach (var (_, cell) in excelMarks)
            {
                if (!TryResolveCell(sheet, cell, out var resolved))
                    continue;

                var range = resolved.MergedRange() ?? resolved.AsRange();
                firstCol = Math.Min(firstCol, range.FirstColumn().ColumnNumber());
                lastCol = Math.Max(lastCol, range.LastColumn().ColumnNumber());
                firstRow = Math.Min(firstRow, range.FirstRow().RowNumber());
                lastRow = Math.Max(lastRow, range.LastRow().RowNumber());
            }

            var colCount = lastCol - firstCol + 1;
            var rowCount = lastRow - firstRow + 1;
            if (colCount <= 0 || rowCount <= 0)
                return null;

            var colLeft = new double[colCount + 1];
            var colWidth = new double[colCount];
            var widthPts = 0d;
            for (var i = 0; i < colCount; i++)
            {
                colLeft[i] = widthPts;
                var width = ColumnWidthToPoints(sheet.Column(firstCol + i).Width);
                colWidth[i] = width;
                widthPts += width;
            }

            var rowTop = new double[rowCount + 1];
            var rowHeight = new double[rowCount];
            var heightPts = 0d;
            for (var i = 0; i < rowCount; i++)
            {
                rowTop[i] = heightPts;
                var height = Math.Max(sheet.Row(firstRow + i).Height, 1d);
                rowHeight[i] = height;
                heightPts += height;
            }

            if (widthPts <= 0.01 || heightPts <= 0.01)
                return null;

            var boxes = new Dictionary<string, ScanExcelPreviewMarkBox>(StringComparer.Ordinal);
            foreach (var (mark, cell) in excelMarks)
            {
                if (!TryResolveCell(sheet, cell, out var resolved))
                    continue;

                var range = resolved.MergedRange() ?? resolved.AsRange();
                var c0 = range.FirstColumn().ColumnNumber();
                var c1 = range.LastColumn().ColumnNumber();
                var r0 = range.FirstRow().RowNumber();
                var r1 = range.LastRow().RowNumber();
                if (c0 < firstCol || c1 > lastCol || r0 < firstRow || r1 > lastRow)
                    continue;

                var i0 = c0 - firstCol;
                var i1 = c1 - firstCol;
                var j0 = r0 - firstRow;
                var j1 = r1 - firstRow;
                var left = colLeft[i0];
                var top = rowTop[j0];
                var right = colLeft[i1] + colWidth[i1];
                var bottom = rowTop[j1] + rowHeight[j1];

                boxes[mark.DisplayId] = new ScanExcelPreviewMarkBox(
                    Left: 100d * left / widthPts,
                    Top: 100d * top / heightPts,
                    Width: Math.Max(100d * (right - left) / widthPts, 0.4),
                    Height: Math.Max(100d * (bottom - top) / heightPts, 0.4));
            }

            SplitSharedCellBoxesForCompoundParts(excelMarks, boxes);

            if (boxes.Count == 0)
                return null;

            return new Layout(heightPts / widthPts, boxes);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Compound Review parts (4.1 / 4.2 / 4.3) share one Excel cell address — slice the
    /// printed box so each part gets its own border instead of stacking one badge.
    /// </summary>
    internal static void SplitSharedCellBoxesForCompoundParts(
        IReadOnlyList<(ScanReviewOrderedField Mark, DocumentRegion.ExcelCell Cell)> excelMarks,
        Dictionary<string, ScanExcelPreviewMarkBox> boxes)
    {
        foreach (var group in excelMarks
                     .Where(static row => row.Mark.PartIndex > 0)
                     .GroupBy(
                         static row => row.Cell.SheetName + "!" + row.Cell.CellReference,
                         StringComparer.OrdinalIgnoreCase))
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

    internal static double ColumnWidthToPoints(double excelWidth)
    {
        var width = Math.Max(excelWidth, 0.05);
        var pixels = width * 7d + 5d;
        return pixels * 72d / 96d;
    }

    private static bool TryResolveCell(IXLWorksheet sheet, DocumentRegion.ExcelCell cell, out IXLCell resolved)
    {
        resolved = sheet.Cell(1, 1);
        try
        {
            resolved = sheet.Cell(cell.CellReference);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
