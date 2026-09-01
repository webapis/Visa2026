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

public class ScanOfficeLibraryTokenExtractorTests
{
    private static ApplicationProfilePlaceholderSet PlaceholderSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Fact]
    public void Cluster_JoinsCommaSeparatedLibraryTokens()
    {
        var set = PlaceholderSet();
        var text = "{{.PPN}}, {{.PPAT}}, {{.PPED}}";
        var clusters = ScanOfficeLibraryTokenExtractor.ClusterLibraryTokens(text, set);
        Assert.Single(clusters);
        Assert.Equal(text, clusters[0].Text);
        Assert.Equal(0, clusters[0].Start);
        Assert.Equal(text.Length, clusters[0].Length);
    }

    [Fact]
    public void Cluster_SkipsLoopMarkers()
    {
        var set = PlaceholderSet();
        var text = "{{#ds.rows}}{{.PFN}}{{/ds.rows}}";
        var clusters = ScanOfficeLibraryTokenExtractor.ClusterLibraryTokens(text, set);
        Assert.Single(clusters);
        Assert.Equal("{{.PFN}}", clusters[0].Text);
    }

    [Fact]
    public void Cluster_KeepsSeparateTokensWhenLiteralTextIsBetween()
    {
        var set = PlaceholderSet();
        var text = "{{.ASPN}} company {{.ACTAX}}";
        var clusters = ScanOfficeLibraryTokenExtractor.ClusterLibraryTokens(text, set);
        Assert.Equal(2, clusters.Count);
        Assert.Equal("{{.ASPN}}", clusters[0].Text);
        Assert.Equal("{{.ACTAX}}", clusters[1].Text);
    }

    [Fact]
    public void Extract_Word_FindsTokenClustersWithoutYellow()
    {
        var set = PlaceholderSet();
        var bytes = CreateWordTokenFixture("{{.ASPN}}", "{{.PPN}}, {{.PPAT}}");
        var spans = ScanOfficeLibraryTokenExtractor.Extract(bytes, ScanSourceKind.Word, set);
        Assert.Equal(2, spans.Count);
        Assert.Equal("{{.ASPN}}", spans[0].Text);
        Assert.Equal("{{.PPN}}, {{.PPAT}}", spans[1].Text);
        Assert.All(spans, s => Assert.IsType<DocumentRegion.WordSpan>(s.Region));
    }

    [Fact]
    public void Extract_Excel_FindsTokenCellsWithoutYellow()
    {
        var set = PlaceholderSet();
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            ws.Cell("B2").Value = "{{.PFN}}";
            ws.Cell("C2").Value = "{{.PPN}}, {{.PPAT}}";
            ws.Cell("A1").Value = "literal";
            wb.SaveAs(ms);
        }

        var spans = ScanOfficeLibraryTokenExtractor.Extract(ms.ToArray(), ScanSourceKind.Excel, set);
        Assert.Equal(2, spans.Count);
        Assert.Contains(spans, s => s.Text == "{{.PFN}}");
        Assert.Contains(spans, s => s.Text == "{{.PPN}}, {{.PPAT}}");
        Assert.All(spans, s => Assert.IsType<DocumentRegion.ExcelCell>(s.Region));
    }

    [Fact]
    public void BuildFromLibraryTokens_MapsExistingPlaceholders()
    {
        var set = PlaceholderSet();
        var spans = new List<ScanOfficeYellowSpan>
        {
            new()
            {
                Text = "{{.PPN}}, {{.PPAT}}",
                Region = new DocumentRegion.WordSpan("p1", 0, 22),
                PageIndex = 0,
            },
        };

        var proposal = ScanOfficeFieldPlanBuilder.BuildFromLibraryTokens(spans, set);
        Assert.Equal(ScanOfficeLibraryTokenExtractor.FieldPlanSource, proposal.Source);
        Assert.Equal(0, proposal.YellowHighlightCount);
        var field = Assert.Single(proposal.Fields);
        Assert.Equal("{{.PPN}}, {{.PPAT}}", field.ProposedToken);
        Assert.Equal(ScanFieldConfidence.High, field.Confidence);
    }

    [Fact]
    public async Task BuildAsync_TokenWord_UsesExistingTokensWhenNoYellow()
    {
        var (_, _, ingest, fieldPlan) = ScanTestServiceFactory.Create();
        var set = PlaceholderSet();
        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = CreateWordTokenFixture("{{ds.ASPN}}"),
            FileName = "letter.docx",
        });

        var plan = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingested,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        Assert.Equal(ScanOfficeLibraryTokenExtractor.FieldPlanSource, plan.Source);
        Assert.Equal(0, plan.YellowHighlightCount);
        Assert.Contains(plan.Fields, f => f.ProposedToken != null
            && TemplateTokenSyntax.TryGetShortCode(f.ProposedToken, out var c)
            && c.Equals("ASPN", StringComparison.OrdinalIgnoreCase));
    }

    public static byte[] CreateWordTokenFixture(params string[] paragraphs)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());
            var body = main.Document.Body!;
            foreach (var paragraph in paragraphs)
                body.AppendChild(new Paragraph(new Run(new Text(paragraph))));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}