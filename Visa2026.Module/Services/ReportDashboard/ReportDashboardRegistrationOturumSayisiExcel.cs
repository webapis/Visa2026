using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Oturum sayısı company sheet. Toplam is an Excel formula. Blank when zero.
/// NOT = Region-AddressType-count fragments.
/// </summary>
public static class ReportDashboardRegistrationOturumSayisiExcel
{
    public const string SheetName = "Oturum sayısı";
    public const string HeaderName = "DashboardHeader";
    public const string SampleName = "DashboardSample";

    public static readonly string[] Headers =
    [
        "Projeler",
        "Balkan kampda yasayanlar",
        "Balkan adresde yasayanlar",
        "Aşgabat",
        "Ahal",
        "Lebap",
        "Mary adres",
        "Mary - kamp şatlık-kamp",
        "Toplam",
        "NOT",
    ];

    public sealed record Row(
        string Department,
        int BalkanKamp,
        int BalkanAdres,
        int Asgabat,
        int AsgabatKamp,
        int AsgabatAdres,
        int Ahal,
        int AhalKamp,
        int AhalAdres,
        int Lebap,
        int LebapKamp,
        int LebapAdres,
        int MaryAdres,
        int MaryKamp,
        string Notes = "");

    public static string BuildNotes(Row row)
    {
        var parts = new List<string>();
        void Add(string label, int count)
        {
            if (count > 0)
                parts.Add(label + "-" + count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        Add("kamp", row.BalkanKamp);
        Add("adres", row.BalkanAdres);
        Add("Ag-kamp", row.AsgabatKamp);
        Add("Ag-adres", row.AsgabatAdres);
        Add("AH-kamp", row.AhalKamp);
        Add("AH-adres", row.AhalAdres);
        Add("LB-kamp", row.LebapKamp);
        Add("LB-adres", row.LebapAdres);
        Add("MR-kamp", row.MaryKamp);
        Add("MR-adres", row.MaryAdres);
        return string.Join(", ", parts);
    }

    public static string FormatCount(int value) =>
        value == 0 ? string.Empty : value.ToString(System.Globalization.CultureInfo.InvariantCulture);

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
        sheet.Row(1).Height = 48;

        var sample = sheet.Range(2, 1, 2, Headers.Length);
        sample.Style.Font.FontName = "Times New Roman";
        sample.Style.Font.FontSize = 11;
        sample.Style.Alignment.WrapText = true;
        sample.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sample.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sample.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sample.Cell(1, 9).FormulaA1 = "=IF(B2=\"\",0,B2)+IF(C2=\"\",0,C2)+IF(D2=\"\",0,D2)+IF(E2=\"\",0,E2)+IF(F2=\"\",0,F2)+IF(G2=\"\",0,G2)+IF(H2=\"\",0,H2)";

        double[] widths = [14, 12, 12, 10, 10, 10, 12, 14, 10, 28];
        for (var column = 0; column < widths.Length; column++)
            sheet.Column(column + 1).Width = widths[column];

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, 2, Headers.Length).SetAutoFilter();
        workbook.DefinedNames.Add(HeaderName, header);
        workbook.DefinedNames.Add(SampleName, sample);
        return Save(workbook);
    }

    public static byte[] Fill(byte[]? template, IReadOnlyList<Row> rows)
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
            WriteCount(sheet, rowNumber, 2, row.BalkanKamp);
            WriteCount(sheet, rowNumber, 3, row.BalkanAdres);
            WriteCount(sheet, rowNumber, 4, row.Asgabat);
            WriteCount(sheet, rowNumber, 5, row.Ahal);
            WriteCount(sheet, rowNumber, 6, row.Lebap);
            WriteCount(sheet, rowNumber, 7, row.MaryAdres);
            WriteCount(sheet, rowNumber, 8, row.MaryKamp);
            sheet.Cell(rowNumber, 9).FormulaA1 =
                $"=IF(B{rowNumber}=\"\",0,B{rowNumber})+IF(C{rowNumber}=\"\",0,C{rowNumber})+IF(D{rowNumber}=\"\",0,D{rowNumber})+IF(E{rowNumber}=\"\",0,E{rowNumber})+IF(F{rowNumber}=\"\",0,F{rowNumber})+IF(G{rowNumber}=\"\",0,G{rowNumber})+IF(H{rowNumber}=\"\",0,H{rowNumber})";
            sheet.Cell(rowNumber, 10).Value = string.IsNullOrWhiteSpace(row.Notes) ? BuildNotes(row) : row.Notes;
        }

        var totalRow = sampleRow + rows.Count;
        sheet.Cell(totalRow, 1).Value = "Toplam";
        sheet.Cell(totalRow, 9).FormulaA1 = $"=SUM(I{sampleRow}:I{totalRow - 1})";
        sheet.Cell(totalRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFF00");
        sheet.Cell(totalRow, 9).Style.Font.Bold = true;

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

    private static void WriteCount(IXLWorksheet sheet, int rowNumber, int column, int value)
    {
        if (value == 0)
            sheet.Cell(rowNumber, column).Value = string.Empty;
        else
            sheet.Cell(rowNumber, column).Value = value;
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