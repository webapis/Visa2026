using ClosedXML.Excel;
using PdfSharpCore.Pdf.IO;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ExcelPreviewPageLayoutTests
{
    [Fact]
    public void ShouldUseLandscape_when_page_setup_is_landscape()
    {
        Assert.True(ExcelPreviewPageLayout.ShouldUseLandscape(true, usedColumnCount: 2, usedCharacterWidth: 20));
    }

    [Fact]
    public void ShouldUseLandscape_when_used_range_is_wider_than_portrait()
    {
        Assert.True(ExcelPreviewPageLayout.ShouldUseLandscape(false, usedColumnCount: 11, usedCharacterWidth: 140));
    }

    [Fact]
    public void ShouldUseLandscape_false_for_narrow_letter_sheet()
    {
        Assert.False(ExcelPreviewPageLayout.ShouldUseLandscape(false, usedColumnCount: 3, usedCharacterWidth: 40));
    }

    [Fact]
    public void StampFromContent_sets_landscape_on_wide_sanaw()
    {
        var stamped = ExcelPreviewPageLayout.StampFromContent(WideSanawXlsx());
        using var workbook = new XLWorkbook(new MemoryStream(stamped));
        Assert.Equal(XLPageOrientation.Landscape, workbook.Worksheet(1).PageSetup.PageOrientation);
    }

    [Fact]
    public void TryConvertToPdf_wide_sanaw_page_is_landscape()
    {
        var pdf = new ApplicationWordReportOfficePreviewPdfConverter()
            .TryConvertToPdf(WideSanawXlsx(), "Hasaba almak sanawy.xlsx");

        Assert.NotNull(pdf);
        using var document = PdfReader.Open(new MemoryStream(pdf!), PdfDocumentOpenMode.Import);
        var page = document.Pages[0];
        Assert.True(page.Width.Point > page.Height.Point);
    }

    private static byte[] WideSanawXlsx()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sanaw");
        var headers = new[]
        {
            "No", "Familiyasy", "Ady", "Doglan senesi", "Jynsy", "Milleti",
            "Pasport", "Berlen senesi", "Girelgi", "Hasaba alnan", "Bellik",
        };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(2, i + 1).Value = "x";
            sheet.Column(i + 1).Width = 14;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}