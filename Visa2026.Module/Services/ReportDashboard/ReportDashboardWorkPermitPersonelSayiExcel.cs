using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Personel sayi company sheet. Toplam is an Excel formula. Blank inactive when zero.
/// </summary>
public static class ReportDashboardWorkPermitPersonelSayiExcel
{
    public const string SheetName = "Personel sayi";
    public const string HeaderName = "DashboardHeader";
    public const string SampleName = "DashboardSample";

    public static readonly string[] Headers =
    [
        "Projeler",
        "Aktiv Personel sayi",
        "Aktiv olmayan personel sayi",
        "Toplam sayı",
    ];

    public static byte[] BuildStarter()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);
        for (var column = 0; column < Headers.Length; column++)
            sheet.Cell(1, column + 1).Value = Headers[column];

        var header = sheet.Range(1, 1, 1, Headers.Length);
        header.Style.Font.FontName = "Times New Roman";
        header.Style.Font.FontSize = 11;
        header.Style.Font.Bold = true;
        header.Style.Alignment.WrapText = true;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
        header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Row(1).Height = 36;

        var sample = sheet.Range(2, 1, 2, Headers.Length);
        sample.Style.Font.FontName = "Times New Roman";
        sample.Style.Font.FontSize = 11;
        sample.Style.Alignment.WrapText = true;
        sample.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sample.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sample.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sample.Cell(1, 4).FormulaA1 = "=B2+IF(C2=\"\",0,C2)";

        double[] widths = [22, 18, 22, 14];
        for (var column = 0; column < widths.Length; column++)
            sheet.Column(column + 1).Width = widths[column];

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, 2, Headers.Length).SetAutoFilter();
        workbook.DefinedNames.Add(HeaderName, header);
        workbook.DefinedNames.Add(SampleName, sample);
        return Save(workbook);
    }

    public static byte[] Fill(byte[]? template, IReadOnlyList<(string Department, int Active, int Inactive)> rows)
    {
        var source = template != null && HasRanges(template) ? template : BuildStarter();
        using var workbook = new XLWorkbook(new MemoryStream(source));
        if (!TryRanges(workbook, out var header, out var sample) || header == null || sample == null)
            return BuildStarter();

        var sheet = sample.Worksheet;
        var sampleRow = sample.RangeAddress.FirstAddress.RowNumber;
        var last = sheet.LastRowUsed()?.RowNumber() ?? sampleRow;
        if (last > sampleRow)
            sheet.Rows(sampleRow + 1, last).Delete();

        if (rows.Count == 0)
        {
            ClearRow(sheet, sampleRow, Headers.Length);
            return Save(workbook);
        }

        for (var index = 1; index < rows.Count; index++)
            sheet.Row(sampleRow).CopyTo(sheet.Row(sampleRow + index));

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = sampleRow + index;
            var row = rows[index];
            sheet.Cell(rowNumber, 1).Value = row.Department;
            sheet.Cell(rowNumber, 2).Value = row.Active;
            if (row.Inactive == 0)
                sheet.Cell(rowNumber, 3).Value = string.Empty;
            else
                sheet.Cell(rowNumber, 3).Value = row.Inactive;
            sheet.Cell(rowNumber, 4).FormulaA1 = $"=B{rowNumber}+IF(C{rowNumber}=\"\",0,C{rowNumber})";
        }

        var totalRow = sampleRow + rows.Count;
        sheet.Cell(totalRow, 1).Value = "Toplam";
        sheet.Cell(totalRow, 4).FormulaA1 = $"=SUM(D{sampleRow}:D{totalRow - 1})";
        sheet.Cell(totalRow, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF00");
        sheet.Cell(totalRow, 4).Style.Font.Bold = true;

        var table = sheet.Range(header.RangeAddress.FirstAddress.RowNumber, 1, totalRow, Headers.Length);
        table.Style.Font.FontName = "Times New Roman";
        table.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        table.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        table.Style.Alignment.WrapText = true;
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        var headerRange = sheet.Range(
            header.RangeAddress.FirstAddress.RowNumber, 1,
            header.RangeAddress.FirstAddress.RowNumber, Headers.Length);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9D9D9");
        headerRange.Style.Font.Bold = true;
        sheet.Range(header.RangeAddress.FirstAddress.RowNumber, 1, totalRow - 1, Headers.Length).SetAutoFilter();
        return Save(workbook);
    }

    private static bool HasRanges(byte[] content)
    {
        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(content));
            return TryRanges(workbook, out _, out _);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryRanges(XLWorkbook workbook, out IXLRange? header, out IXLRange? sample)
    {
        header = workbook.DefinedNames
            .FirstOrDefault(n => string.Equals(n.Name, HeaderName, StringComparison.OrdinalIgnoreCase))
            ?.Ranges.FirstOrDefault();
        sample = workbook.DefinedNames
            .FirstOrDefault(n => string.Equals(n.Name, SampleName, StringComparison.OrdinalIgnoreCase))
            ?.Ranges.FirstOrDefault();
        return header != null && sample != null;
    }

    private static void ClearRow(IXLWorksheet sheet, int rowNumber, int columnCount)
    {
        for (var column = 1; column <= columnCount; column++)
            sheet.Cell(rowNumber, column).Value = string.Empty;
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}