using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// A Maglumat workbook. The title date is the day the file is created. Rows are the table the officer is looking at.
/// </summary>
public static class ReportDashboardWorkPermitMaglumatExcel
{
    public static byte[] Build(
        IReadOnlyList<string> headers,
        IReadOnlyList<ReportDashboardPreviewRow> rows,
        DateTime created)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("A.Maglumat");
        var columnCount = Math.Max(1, headers.Count);
        var title = sheet.Cell(1, 1);
        title.Value = Title(created);
        if (columnCount > 1)
            sheet.Range(1, 1, 1, columnCount).Merge();
        title.Style.Font.Bold = true;
        title.Style.Alignment.WrapText = true;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 32;

        for (var column = 0; column < headers.Count; column++)
        {
            var header = sheet.Cell(2, column + 1);
            header.Value = headers[column];
            header.Style.Font.Bold = true;
            header.Style.Alignment.WrapText = true;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var values = Cells(rows[rowIndex], rowIndex + 1);
            var count = Math.Min(columnCount, values.Count);
            for (var column = 0; column < count; column++)
            {
                var cell = sheet.Cell(rowIndex + 3, column + 1);
                cell.Value = values[column];
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                if (values[column].Contains('\n', StringComparison.Ordinal))
                    cell.Style.Alignment.WrapText = true;
            }
        }

        sheet.SheetView.FreezeRows(2);
        sheet.Columns(1, columnCount).AdjustToContents(2, 80);
        sheet.Column(1).Width = 6;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string Title(DateTime created)
    {
        var date = created.ToString("dd.MM.yyyy");
        return "A. " + date + " sene boýunça Çalyk Energi Sanayi we Tijaret A.Ş. Türk kärhanasynyň Türkmenistandaky şahamçasy işleýän daşary ýurt raýatlaryň sanawy barada MAGLUMAT";
    }

    public static List<string> Cells(ReportDashboardPreviewRow row, int rowNumber) =>
    [
        rowNumber.ToString(),
        row.Name ?? string.Empty,
        Join(row.ColumnA, row.ColumnB),
        Join(row.ColumnC, row.ColumnD),
        row.ColumnE ?? string.Empty,
        row.ColumnF ?? string.Empty,
        row.ColumnG ?? string.Empty,
        PermitCell(row),
        Join(row.VisaNumber, row.VisaStartDate, row.VisaEndDate),
        string.Empty
    ];

    public static List<string> CellsWithLocations(ReportDashboardPreviewRow row, int rowNumber)
    {
        var cells = Cells(row, rowNumber);
        cells.Insert(8, LocationCell(row));
        return cells;
    }

    private static string PermitCell(ReportDashboardPreviewRow row)
    {
        if (row.WorkPermits.Count == 0)
            return Join(row.ColumnH, row.WorkPermitStart, row.WorkPermitEnd);

        var blocks = new List<string>();
        foreach (var permit in row.WorkPermits)
        {
            var text = Join(permit.Number, permit.Start, permit.End);
            if (text.Length > 0)
                blocks.Add(text);
        }

        return string.Join("\n\n", blocks);
    }

    private static string LocationCell(ReportDashboardPreviewRow row)
    {
        if (row.WorkPermits.Count == 0)
            return string.Empty;

        return string.Join("\n\n", row.WorkPermits.Select(permit => permit.Locations ?? string.Empty));
    }

    private static string Join(params string?[] lines)
    {
        var kept = new List<string>();
        foreach (var line in lines)
        {
            var text = line?.Trim() ?? string.Empty;
            if (text.Length > 0)
                kept.Add(text);
        }

        return string.Join("\n", kept);
    }
}