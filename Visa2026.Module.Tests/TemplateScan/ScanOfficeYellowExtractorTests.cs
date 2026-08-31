#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanOfficeYellowExtractorTests
{
    [Fact]
    public void Extract_Word_FindsYellowHighlightRuns()
    {
        var bytes = CreateWordFixture("№ 4/-434", "literal text");
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        Assert.Contains(spans, s => s.Text.Contains("4/-434", StringComparison.Ordinal));
        Assert.All(spans, s => Assert.IsType<DocumentRegion.WordSpan>(s.Region));
    }

    [Fact]
    public void Normalize_Docx_SetsOfficeSource()
    {
        var (normalizer, _, _, _) = ScanTestServiceFactory.Create();
        var bytes = CreateWordFixture("Adaty tertipde!", "body");
        var input = normalizer.Normalize(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "letter.docx",
        });
        Assert.Equal(ScanSourceKind.Word, input.SourceKind);
        Assert.NotNull(input.OfficePackageBytes);
        Assert.True(input.IsOfficeSource);
    }

    [Fact]
    public async Task BuildAsync_WordYellow_MapsLibraryTokensWithoutVision()
    {
        var (_, _, ingest, fieldPlan) = ScanTestServiceFactory.Create();
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var bytes = CreateWordFixture("№ 4/-434", "Adaty tertipde!");
        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = bytes,
            FileName = "letter.docx",
        });

        Assert.True(ingested.Suitability.CanContinue);

        var plan = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingested,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        Assert.Equal("office-yellow", plan.Source);
        Assert.True(plan.YellowHighlightCount >= 1);
        Assert.Contains(plan.Fields, f => f.ProposedToken != null
            && TemplateTokenSyntax.TryGetShortCode(f.ProposedToken, out var c)
            && c.Equals("AFNUM", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(plan.Fields, f => f.SourceRegion is DocumentRegion.WordSpan);
    }

    [Fact]
    public void Extract_Excel_FindsYellowCells()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            ws.Cell("A1").Value = "header";
            ws.Cell("B2").Value = "köp gezeklik";
            ws.Cell("B2").Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 235, 40);
            wb.SaveAs(ms);
        }

        var spans = new ScanOfficeYellowExtractor().Extract(ms.ToArray(), ScanSourceKind.Excel);
        Assert.Contains(spans, s => s.Text.Contains("köp gezeklik", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(spans, s => s.Region is DocumentRegion.ExcelCell);
    }

    [Fact]
    public void Extract_Excel_ignores_yellow_cells_on_sheets_after_the_first()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var first = wb.AddWorksheet("Sanaw");
            first.Cell("B5").Value = "Erol";
            first.Cell("B5").Style.Fill.BackgroundColor = XLColor.Yellow;

            var second = wb.AddWorksheet("Archive");
            second.Cell("A1").Value = "ignored";
            second.Cell("A1").Style.Fill.BackgroundColor = XLColor.Yellow;

            wb.SaveAs(ms);
        }

        var spans = new ScanOfficeYellowExtractor().Extract(ms.ToArray(), ScanSourceKind.Excel);
        Assert.Single(spans);
        Assert.Equal("Erol", spans[0].Text);
        Assert.Equal("Sanaw", ((DocumentRegion.ExcelCell)spans[0].Region).SheetName);
    }

    public static byte[] CreateWordFixture(params string[] yellowPhrases)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());
            var body = main.Document.Body!;

            foreach (var phrase in yellowPhrases)
            {
                var run = new Run(
                    new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                    new Text(phrase));
                body.AppendChild(new Paragraph(run));
            }

            body.AppendChild(new Paragraph(new Run(new Text("not highlighted"))));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}