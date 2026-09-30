#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanLetterRosterTests
{
    [Fact]
    public void Extract_Word_GreenIsRoster_YellowIsHeader()
    {
        var bytes = Letter();
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);

        Assert.Contains(spans, s => s.Text.Contains("20.01.2026", StringComparison.Ordinal)
            && s.MarkKind == ScanOfficeMarkKind.Yellow);
        Assert.Contains(spans, s => s.Text.Contains("Hayati", StringComparison.Ordinal)
            && s.MarkKind == ScanOfficeMarkKind.Green);
        Assert.Contains(spans, s => s.Text.Contains("Hong", StringComparison.Ordinal)
            && s.MarkKind == ScanOfficeMarkKind.Green);
    }

    [Fact]
    public void GreenNumberedNames_MapToRosterPersonLine()
    {
        var set = PlaceholderSet();
        var bytes = Letter();
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var plan = ScanOfficeFieldPlanBuilder.Build(spans, set, bytes, ScanSourceKind.Word);

        var hayati = Assert.Single(plan.Fields, f => f.LabelText.Contains("Hayati", StringComparison.Ordinal));
        Assert.Equal(ScanFieldScope.Row, hayati.Scope);
        Assert.Contains("{{.RNUM}}", hayati.ProposedToken, StringComparison.Ordinal);
        Assert.Contains("{{.PFN}}", hayati.ProposedToken, StringComparison.Ordinal);

        var hong = Assert.Single(plan.Fields, f => f.LabelText.Contains("Hong", StringComparison.Ordinal));
        Assert.Equal(ScanFieldScope.Row, hong.Scope);
        Assert.Contains("{{.PFN}}", hong.ProposedToken, StringComparison.Ordinal);
    }

    [Fact]
    public void Collapse_KeepsFirstGreenLine_AndRemovesTheSample()
    {
        var bytes = Letter();
        var spans = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var subs = spans
            .Select(span => new TokenSubstitution(
                span.Region,
                span.MarkKind == ScanOfficeMarkKind.Green
                    ? "{{.RNUM}}. {{.PFN}}"
                    : "{{ds.ADAT}}"))
            .ToList();

        var collapsed = ScanLetterRosterCollapse.Apply(bytes, subs);

        var removed = Assert.Single(collapsed.RemovedParagraphAddresses);
        Assert.Single(collapsed.ParagraphLoops);
        Assert.DoesNotContain(
            collapsed.Substitutions,
            s => s.Region is DocumentRegion.WordSpan word && word.ParagraphAddress == removed);
        Assert.Contains(
            collapsed.Substitutions,
            s => s.Token.Contains("{{.PFN}}", StringComparison.Ordinal));
        Assert.Contains(
            collapsed.Substitutions,
            s => s.Token.Contains("{{ds.ADAT}}", StringComparison.Ordinal));
    }

    [Fact]
    public void ExpandPrototypeParagraph_RepeatsTheGreenLineForEachPerson()
    {
        var bytes = TemplateWithRosterLine();
        IReadOnlyList<IDictionary<string, object>> rows =
        [
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Person_FullName"] = "Hayati Uyan",
            },
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Person_FullName"] = "Hong Huang",
            },
        ];

        var expanded = WordScanTableRowExpander.ExpandPrototypeParagraph(bytes, rows);
        using var stream = new MemoryStream(expanded);
        using var document = WordprocessingDocument.Open(stream, false);
        var text = document.MainDocumentPart!.Document.Body!.InnerText;

        Assert.Contains("{{ds.ADAT}}", text, StringComparison.Ordinal);
        Assert.Contains("1. Hayati Uyan", text, StringComparison.Ordinal);
        Assert.Contains("2. Hong Huang", text, StringComparison.Ordinal);
        Assert.DoesNotContain("{{#ds.rows}}", text, StringComparison.Ordinal);
        Assert.DoesNotContain("{{.PFN}}", text, StringComparison.Ordinal);
    }

    private static ApplicationProfilePlaceholderSet PlaceholderSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    private static byte[] Letter() =>
        Build(
            (HighlightColorValues.Yellow, "20.01.2026"),
            (HighlightColorValues.Green, "1. Hayati Uyan"),
            (HighlightColorValues.Green, "2. Hong Huang"));

    private static byte[] TemplateWithRosterLine()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text("{{ds.ADAT}}"))),
                new Paragraph(new Run(new Text("{{#ds.rows}}{{.RNUM}}. {{.PFN}}{{/ds.rows}}")))));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] Build(params (HighlightColorValues Color, string Text)[] lines)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());
            foreach (var line in lines)
            {
                main.Document.Body!.AppendChild(new Paragraph(
                    new Run(
                        new RunProperties(new Highlight { Val = line.Color }),
                        new Text(line.Text))));
            }

            main.Document.Save();
        }

        return stream.ToArray();
    }
}
