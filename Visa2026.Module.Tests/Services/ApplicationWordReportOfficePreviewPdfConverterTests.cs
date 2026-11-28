using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplater;
using Visa2026.Module.Services.UserReports;
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
    public void AlignDownloadFileName_renames_docx_label_when_bytes_are_xlsx()
    {
        var xlsx = CreateMinimalXlsx();

        var name = ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName(
            "Dasary_yurt_rayatlarynyn_sanawy_cakylyk.docx",
            xlsx);

        Assert.Equal("Dasary_yurt_rayatlarynyn_sanawy_cakylyk.xlsx", name);
    }

    [Fact]
    public void AlignDownloadFileName_keeps_docx_for_word_bytes()
    {
        var docx = Visa2026.Module.Tests.TemplateScan.ScanOfficeYellowExtractorTests.CreateWordFixture("Çalyk");

        var name = ApplicationWordReportOfficePreviewPdfConverter.AlignDownloadFileName(
            "yuztutma.docx",
            docx);

        Assert.Equal("yuztutma.docx", name);
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

    [Fact]
    public void TryConvertManyToMergedPdf_joins_two_word_files()
    {
        var first = Visa2026.Module.Tests.TemplateScan.ScanOfficeYellowExtractorTests.CreateWordFixture("Birinji");
        var second = Visa2026.Module.Tests.TemplateScan.ScanOfficeYellowExtractorTests.CreateWordFixture("Ikinji");
        var converter = new ApplicationWordReportOfficePreviewPdfConverter();

        var merged = converter.TryConvertManyToMergedPdf(
        [
            (first, "one.docx"),
            (second, "two.docx"),
        ]);

        Assert.NotNull(merged);
        Assert.True(merged!.Length > 200);
        Assert.Equal((byte)'%', merged[0]);
        Assert.Equal((byte)'P', merged[1]);
        Assert.Equal((byte)'D', merged[2]);
        Assert.Equal((byte)'F', merged[3]);
    }

    [Fact]
    public void RemoveInjectedSheet_drops_evaluation_warning_and_keeps_the_sanaw()
    {
        using var workbook = new XLWorkbook();
        var sanaw = workbook.AddWorksheet("Sanaw");
        sanaw.Cell("A1").Value = "Ferdi";
        sanaw.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sanaw.PageSetup.FitToPages(1, 0);
        var warning = workbook.AddWorksheet("Evaluation Warning");
        warning.Cell("A5").Value = "For evaluation purposes only";
        workbook.AddWorksheet("Evaluation Warning (1)");
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var cleaned = ExcelPreviewEvaluationNotice.RemoveInjectedSheet(stream.ToArray());

        using var again = new XLWorkbook(new MemoryStream(cleaned));
        var only = Assert.Single(again.Worksheets);
        Assert.Equal("Sanaw", only.Name);
        Assert.Equal("Ferdi", only.Cell("A1").GetString());
        Assert.Equal(XLPageOrientation.Landscape, only.PageSetup.PageOrientation);
        Assert.Equal(1, only.PageSetup.PagesWide);
    }

    [Fact]
    public void Preview_keeps_bold_on_the_opening_words()
    {
        var docx = CreateBoldOpeningDocx();
        using var input = new MemoryStream(docx, writable: false);
        var template = DocxTemplateFactory.Open(input);
        template.BindModel("ds", new Dictionary<string, object> { ["Urgency_NameTm"] = "Gyssagly tertipde!" });
        using var merged = new MemoryStream();
        template.Save(merged);
        merged.Position = 0;

        using var check = WordprocessingDocument.Open(merged, false);
        var boldText = string.Concat(check.MainDocumentPart!.Document.Body!
            .Descendants<Run>()
            .Where(run => run.RunProperties?.GetFirstChild<Bold>() != null)
            .Select(run => run.InnerText));
        Assert.Contains("Türkmenistanyň", boldText);
        Assert.Contains("Energetika", boldText);

        var pdf = new ApplicationWordReportOfficePreviewPdfConverter().TryConvertToPdf(merged.ToArray(), "yuztutma.docx");
        Assert.NotNull(pdf);
        var pdfText = System.Text.Encoding.Latin1.GetString(pdf!);
        Assert.Contains("Bold", pdfText, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CreateBoldOpeningDocx()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            main.Document = new Document(body);
            var paragraph = new Paragraph();
            paragraph.AppendChild(RunOf("{{ds.Urgency_NameTm}}", bold: false));
            paragraph.AppendChild(RunOf("Türkmenistanyň", bold: true));
            paragraph.AppendChild(RunOf(" ", bold: false));
            paragraph.AppendChild(RunOf("Energetika ", bold: true));
            paragraph.AppendChild(RunOf("ministrligi", bold: false));
            body.AppendChild(paragraph);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static Run RunOf(string text, bool bold)
    {
        var props = new RunProperties(
            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
            new FontSize { Val = "32" },
            new FontSizeComplexScript { Val = "32" });
        if (bold)
        {
            props.AppendChild(new Bold());
            props.AppendChild(new BoldComplexScript());
        }

        return new Run(props, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
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