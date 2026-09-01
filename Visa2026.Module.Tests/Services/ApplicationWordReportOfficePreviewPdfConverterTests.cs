using ClosedXML.Excel;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWordReportOfficePreviewPdfConverterTests
{
    [Fact]
    public void LooksLikeOpenXmlExcel_detects_xlsx_zip_parts()
    {
        var xlsx = CreateMinimalXlsx();

        Assert.True(ApplicationWordReportOfficePreviewPdfConverter.LooksLikeOpenXmlExcel(xlsx));
        Assert.False(ApplicationWordReportOfficePreviewPdfConverter.LooksLikeOpenXmlWord(xlsx));
    }

    [Fact]
    public void TryConvertToPdf_uses_spreadsheet_path_when_xlsx_is_named_docx()
    {
        var xlsx = CreateMinimalXlsx();
        var converter = new ApplicationWordReportOfficePreviewPdfConverter();

        var fromWrongName = converter.TryConvertToPdf(xlsx, "report_20260321.docx");
        var fromXlsxName = converter.TryConvertToPdf(xlsx, "Sanaw_clk_09.xlsx");

        Assert.NotNull(fromWrongName);
        Assert.NotNull(fromXlsxName);
        Assert.True(fromWrongName!.Length > 200, "Excel-as-docx must still produce a spreadsheet PDF, not a blank Word page.");
        Assert.Equal(fromXlsxName!.Length, fromWrongName.Length);
    }

    [Fact]
    public void TryConvertToPdf_word_openxml_emits_pdf()
    {
        var docx = Visa2026.Module.Tests.TemplateScan.ScanOfficeYellowExtractorTests.CreateWordFixture("Çalyk Enerji");
        var pdf = new ApplicationWordReportOfficePreviewPdfConverter().TryConvertToPdf(docx, "borcnama.docx");

        Assert.NotNull(pdf);
        Assert.True(pdf!.Length > 200);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    private static byte[] CreateMinimalXlsx()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet");
        sheet.Cell("A1").Value = "Familiyasy";
        sheet.Cell("A2").Value = "Amanow";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}