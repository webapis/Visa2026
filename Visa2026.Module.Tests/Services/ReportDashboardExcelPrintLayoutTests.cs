using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using ClosedXML.Excel;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ReportDashboardExcelPrintLayoutTests
{
    [Fact]
    public void IsLandscape_portrait_for_a_maglumat_and_a_maglumat_2()
    {
        Assert.False(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.WorkPermit, "active-by-project"));
        Assert.False(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat2Key));
    }

    [Fact]
    public void IsLandscape_true_for_gosmaca_and_wide_lists()
    {
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitMaglumat3Key));
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitSanawKey));
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.Invitation, "used"));
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.VisaExtension, ReportDashboardCatalog.VisaAktivPersonellerKey));
    }

    [Fact]
    public void Apply_sets_a4_fit_width_and_page_break_preview()
    {
        var stamped = ReportDashboardExcelPrintLayout.Apply(
            SheetWithHeaderOnRow(1), ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey);

        using var workbook = new XLWorkbook(new MemoryStream(stamped));
        var sheet = workbook.Worksheet(1);
        Assert.Equal(XLPageOrientation.Landscape, sheet.PageSetup.PageOrientation);
        Assert.Equal(XLPaperSize.A4Paper, sheet.PageSetup.PaperSize);
        Assert.Equal(1, sheet.PageSetup.PagesWide);
        Assert.Equal(0, sheet.PageSetup.PagesTall);
        Assert.Equal(1, sheet.PageSetup.FirstRowToRepeatAtTop);
        Assert.Equal(1, sheet.PageSetup.LastRowToRepeatAtTop);
        Assert.Equal("pageBreakPreview", ReadSheetViewMode(stamped));
    }

    [Fact]
    public void Apply_portrait_for_a_maglumat_repeats_title_and_header()
    {
        var stamped = ReportDashboardExcelPrintLayout.Apply(
            SheetWithHeaderOnRow(2), ReportDashboardCategory.WorkPermit, "active-by-project");

        using var workbook = new XLWorkbook(new MemoryStream(stamped));
        var sheet = workbook.Worksheet(1);
        Assert.Equal(XLPageOrientation.Portrait, sheet.PageSetup.PageOrientation);
        Assert.Equal(1, sheet.PageSetup.FirstRowToRepeatAtTop);
        Assert.Equal(2, sheet.PageSetup.LastRowToRepeatAtTop);
        Assert.Equal("pageBreakPreview", ReadSheetViewMode(stamped));
    }

    private static string? ReadSheetViewMode(byte[] content)
    {
        using var zip = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);
        var entry = zip.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(entry);
        using var stream = entry!.Open();
        var xml = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return xml.Root?
            .Element(ns + "sheetViews")?
            .Element(ns + "sheetView")?
            .Attribute("view")?
            .Value;
    }

    private static byte[] SheetWithHeaderOnRow(int headerRow)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Report");
        sheet.Cell(1, 1).Value = "Title";
        sheet.Cell(headerRow, 1).Value = "Col A";
        sheet.Cell(headerRow, 2).Value = "Col B";
        workbook.DefinedNames.Add(
            ReportDashboardExcelTemplateWorkbook.HeaderName,
            sheet.Range(headerRow, 1, headerRow, 2));
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}