using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// kota oranımız company sheet matched to the original workbook layout.
/// App writes date, C2/D2/E2, and ORAN (B7/C7). Other cells stay Excel formulas.
/// </summary>
public static class ReportDashboardWorkPermitKotaExcel
{
    public const string SheetName = "kota oranımız";
    public const string HeaderName = "DashboardHeader";
    public const string SampleName = "DashboardSample";

    private static readonly XLColor HeaderGrey = XLColor.FromHtml("#808080");
    private static readonly XLColor GreenRight = XLColor.FromHtml("#92D050");
    private static readonly XLColor YellowTotal = XLColor.FromHtml("#FFFF00");
    private static readonly XLColor RedRatio = XLColor.FromHtml("#FF0000");

    public static readonly string[] ScreenHeaders =
    [
        "TARİHİ",
        "LOKAL PERSONEL SAYISI",
        "YABANCI PERSONEL SAYISI",
        "İŞLEMDE OLANLAR",
        "ORAN YABANCI",
        "ORAN YEREL",
    ];

    public static byte[] BuildStarter()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);
        ApplyLayout(sheet);
        workbook.DefinedNames.Add(HeaderName, sheet.Range(1, 2, 1, 7));
        workbook.DefinedNames.Add(SampleName, sheet.Range(2, 2, 2, 7));
        return Save(workbook);
    }

    public static byte[] Fill(
        byte[] template,
        DateTime reportDate,
        int localCount,
        int foreignCount,
        int inProcessCount,
        int foreignRatio,
        int localRatio)
    {
        using var workbook = new XLWorkbook(new MemoryStream(
            template != null && template.Length > 0 ? template : BuildStarter()));
        var sheet = workbook.Worksheets.First();
        ApplyLayout(sheet);
        sheet.Cell(2, 2).Value = reportDate.Date;
        sheet.Cell(2, 2).Style.NumberFormat.Format = "dd.MM.yyyy";
        sheet.Cell(2, 3).Value = localCount;
        sheet.Cell(2, 4).Value = foreignCount;
        sheet.Cell(2, 5).Value = inProcessCount;
        sheet.Cell(7, 2).Value = foreignRatio;
        sheet.Cell(7, 3).Value = localRatio;
        return Save(workbook);
    }

    private static void ApplyLayout(IXLWorksheet sheet)
    {
        ClearNote(sheet);
        UnmergeAll(sheet);

        sheet.Cell(1, 2).Value = "TARİHİ";
        sheet.Cell(1, 3).Value = "LOKAL PERSONEL  SAYISI";
        sheet.Cell(1, 4).Value = "YABANCI PERSONEL SAYISI";
        sheet.Cell(1, 5).Value = "İŞLEMDE OLANLAR";
        sheet.Cell(1, 6).Value = "YABANCI PERSONEL SAYISI, %";
        sheet.Cell(1, 7).Value = "LOKAL SAYISI, %";
        sheet.Cell(1, 10).Value = "Vize Başvurusu Hakkı";

        if (sheet.Cell(2, 2).IsEmpty())
        {
            sheet.Cell(2, 2).Value = DateTime.Today;
            sheet.Cell(2, 2).Style.NumberFormat.Format = "dd.MM.yyyy";
        }

        if (sheet.Cell(2, 3).IsEmpty())
            sheet.Cell(2, 3).Value = 0;
        if (sheet.Cell(2, 4).IsEmpty())
            sheet.Cell(2, 4).Value = 0;
        if (sheet.Cell(2, 5).IsEmpty())
            sheet.Cell(2, 5).Value = 0;

        sheet.Cell(2, 6).FormulaA1 = "=ROUND((D2+E2)/C3*100,0)";
        sheet.Cell(2, 7).FormulaA1 = "=ROUND(C2/C3*100,0)";
        sheet.Cell(2, 9).FormulaA1 = "=ROUND(C2*B7/C7-1,0)";
        sheet.Cell(2, 10).FormulaA1 = "=ROUND(I2-D2-E2,0)";

        sheet.Cell(3, 2).Value = "Toplam";
        sheet.Cell(3, 3).FormulaA1 = "=C2+D2+E2";

        sheet.Cell(5, 2).Value = "ORAN";
        sheet.Range(5, 2, 5, 3).Merge();
        sheet.Cell(6, 2).Value = "YABANCI";
        sheet.Cell(6, 3).Value = "YEREL";
        if (sheet.Cell(7, 2).IsEmpty())
            sheet.Cell(7, 2).Value = 10;
        if (sheet.Cell(7, 3).IsEmpty())
            sheet.Cell(7, 3).Value = 90;

        sheet.Cell(5, 5).Value = "Mevcut Lokal personel";
        sheet.Range(5, 5, 6, 6).Merge();
        sheet.Cell(5, 7).Value = "Mevcut Türk personel";
        sheet.Range(5, 7, 6, 8).Merge();
        sheet.Cell(7, 5).FormulaA1 = "=C2";
        sheet.Range(7, 5, 8, 6).Merge();
        sheet.Cell(7, 7).FormulaA1 = "=D2";
        sheet.Range(7, 7, 8, 8).Merge();

        sheet.Cell(9, 2).Value = string.Empty;
        sheet.Cell(9, 3).Value = string.Empty;
        sheet.Cell(10, 2).Value = string.Empty;
        sheet.Cell(10, 3).Value = string.Empty;

        sheet.Cell(10, 5).Value = "LOKAL ORANI";
        sheet.Range(10, 5, 11, 6).Merge();
        sheet.Cell(10, 7).Value = "YABANCI ORANI";
        sheet.Range(10, 7, 11, 8).Merge();
        sheet.Cell(12, 5).Value = "%";
        sheet.Range(12, 5, 13, 5).Merge();
        sheet.Cell(12, 6).FormulaA1 = "=ROUND(C2/(C3-E2)*100,0)";
        sheet.Range(12, 6, 13, 6).Merge();
        sheet.Cell(12, 7).Value = "%";
        sheet.Range(12, 7, 13, 7).Merge();
        sheet.Cell(12, 8).FormulaA1 = "=ROUND((D2)/(C3-E2)*100,0)";
        sheet.Range(12, 8, 13, 8).Merge();

        foreach (var address in new[] { "F2", "G2", "I2", "J2", "F12", "H12" })
            sheet.Cell(address).Style.NumberFormat.Format = "0";

        StyleCompany(sheet);
    }

    private static void ClearNote(IXLWorksheet sheet)
    {
        foreach (var cell in sheet.CellsUsed())
        {
            if (cell.DataType == XLDataType.Text
                && cell.GetString().Contains("Siyah renkli", StringComparison.OrdinalIgnoreCase))
            {
                cell.Value = string.Empty;
            }
        }
    }

    private static void UnmergeAll(IXLWorksheet sheet)
    {
        var merges = sheet.MergedRanges.ToList();
        foreach (var range in merges)
            range.Unmerge();
    }

    private static void StyleCompany(IXLWorksheet sheet)
    {
        sheet.Style.Font.FontName = "Times New Roman";
        var used = sheet.Range(1, 2, 13, 10);
        used.Style.Font.FontName = "Times New Roman";
        used.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        used.Style.Alignment.WrapText = true;

        var header = sheet.Range(1, 2, 1, 7);
        header.Style.Fill.BackgroundColor = HeaderGrey;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Font.Bold = true;
        header.Style.Font.FontSize = 11;
        header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        sheet.Cell(1, 10).Style.Fill.BackgroundColor = GreenRight;
        sheet.Cell(1, 10).Style.Font.Bold = true;
        sheet.Cell(1, 10).Style.Font.FontName = "Times New Roman";
        sheet.Cell(1, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Cell(2, 10).Style.Fill.BackgroundColor = GreenRight;
        sheet.Cell(2, 10).Style.Font.Bold = true;
        sheet.Cell(2, 10).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        foreach (var column in new[] { 3, 4, 5 })
        {
            sheet.Cell(2, column).Style.Fill.BackgroundColor = XLColor.Black;
            sheet.Cell(2, column).Style.Font.FontColor = XLColor.White;
            sheet.Cell(2, column).Style.Font.Bold = true;
            sheet.Cell(2, column).Style.Font.FontName = "Times New Roman";
            sheet.Cell(2, column).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        foreach (var column in new[] { 2, 3 })
        {
            sheet.Cell(7, column).Style.Fill.BackgroundColor = XLColor.Black;
            sheet.Cell(7, column).Style.Font.FontColor = XLColor.White;
            sheet.Cell(7, column).Style.Font.Bold = true;
            sheet.Cell(7, column).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        sheet.Cell(2, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Cell(3, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Cell(3, 3).Style.Fill.BackgroundColor = YellowTotal;
        sheet.Cell(3, 3).Style.Font.Bold = true;
        sheet.Cell(3, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        sheet.Cell(2, 6).Style.Font.FontColor = XLColor.Red;
        sheet.Cell(2, 6).Style.Font.Bold = true;
        sheet.Cell(2, 7).Style.Font.FontColor = XLColor.Red;
        sheet.Cell(2, 7).Style.Font.Bold = true;

        StyleBox(sheet.Range(5, 2, 7, 3));
        StyleBox(sheet.Range(5, 5, 8, 6));
        StyleBox(sheet.Range(5, 7, 8, 8));
        StyleBox(sheet.Range(10, 5, 13, 6));
        StyleBox(sheet.Range(10, 7, 13, 8));

        sheet.Cell(7, 5).Style.Fill.BackgroundColor = XLColor.Black;
        sheet.Cell(7, 5).Style.Font.FontColor = XLColor.White;
        sheet.Cell(7, 7).Style.Fill.BackgroundColor = XLColor.Black;
        sheet.Cell(7, 7).Style.Font.FontColor = XLColor.White;

        sheet.Cell(12, 6).Style.Fill.BackgroundColor = RedRatio;
        sheet.Cell(12, 6).Style.Font.FontColor = XLColor.White;
        sheet.Cell(12, 6).Style.Font.Bold = true;
        sheet.Cell(12, 8).Style.Fill.BackgroundColor = RedRatio;
        sheet.Cell(12, 8).Style.Font.FontColor = XLColor.White;
        sheet.Cell(12, 8).Style.Font.Bold = true;

        sheet.Column(1).Width = 3;
        sheet.Column(2).Width = 12;
        sheet.Column(3).Width = 14;
        sheet.Column(4).Width = 14;
        sheet.Column(5).Width = 12;
        sheet.Column(6).Width = 12;
        sheet.Column(7).Width = 14;
        sheet.Column(8).Width = 12;
        sheet.Column(9).Width = 5;
        sheet.Column(10).Width = 18;
        sheet.Row(1).Height = 36;
        sheet.Row(2).Height = 22;
    }

    private static void StyleBox(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Font.FontName = "Times New Roman";
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}