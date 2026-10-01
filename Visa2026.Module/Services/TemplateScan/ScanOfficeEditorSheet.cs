#nullable enable

using ClosedXML.Excel;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// First worksheet of an uploaded workbook, laid out for the Review editor.
/// Cell text matches <see cref="ScanManualPlacement"/> so a click maps to that cell.
/// </summary>
public static class ScanOfficeEditorSheet
{
    public sealed record Cell(string Reference, int Row, int Column, string Text, bool Yellow);

    public sealed record Model(
        string SheetName,
        int MinRow,
        int MaxRow,
        int MinColumn,
        int MaxColumn,
        IReadOnlyList<Cell> Cells);

    public const int MaxRows = 200;

    public const int MaxColumns = 40;

    public static Model? Read(byte[]? bytes)
    {
        if (bytes is not { Length: > 64 })
            return null;

        using var stream = new MemoryStream(bytes, writable: false);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
            return null;

        var used = sheet.CellsUsed().ToList();
        if (used.Count == 0)
        {
            return new Model(sheet.Name, 1, 1, 1, 1, Array.Empty<Cell>());
        }

        var minRow = used.Min(static c => c.Address.RowNumber);
        var maxRow = Math.Min(used.Max(static c => c.Address.RowNumber), minRow + MaxRows - 1);
        var minColumn = used.Min(static c => c.Address.ColumnNumber);
        var maxColumn = Math.Min(used.Max(static c => c.Address.ColumnNumber), minColumn + MaxColumns - 1);
        var cells = new List<Cell>();
        for (var row = minRow; row <= maxRow; row++)
        {
            for (var column = minColumn; column <= maxColumn; column++)
            {
                var cell = sheet.Cell(row, column);
                cells.Add(new Cell(
                    cell.Address.ToStringRelative(),
                    row,
                    column,
                    ScanExcelWorkbookHelper.ReadCellText(cell).Trim(),
                    IsYellow(cell)));
            }
        }

        return new Model(sheet.Name, minRow, maxRow, minColumn, maxColumn, cells);
    }

    public static string ColumnName(int column)
    {
        var name = string.Empty;
        var value = column;
        while (value > 0)
        {
            value--;
            name = (char)('A' + (value % 26)) + name;
            value /= 26;
        }

        return name;
    }

    private static bool IsYellow(IXLCell cell)
    {
        var fill = cell.Style.Fill;
        if (fill.PatternType is XLFillPatternValues.None or XLFillPatternValues.Gray125)
            return false;

        try
        {
            var color = fill.BackgroundColor;
            if (color.ColorType != XLColorType.Color)
                return false;

            var rgb = color.Color;
            if (rgb.R < 180 || rgb.G < 160)
                return false;

            var chroma = (rgb.R + rgb.G) / 2.0 - rgb.B;
            return chroma >= 35 && rgb.B <= 210;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
