#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Tests.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanYellowSubstitutionBinderTests
{
    [Fact]
    public void Bind_does_not_steal_a_yellow_by_matching_sample_text()
    {
        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture("19.01.2026", "19.01.2026");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word).ToList();
        Assert.Equal(2, yellows.Count);

        var second = Assert.IsType<DocumentRegion.WordSpan>(yellows[1].Region);
        var stale = second with { Start = 999, Length = 1 };
        var fields = new[]
        {
            Field("{{.ADAT}}", stale, yellows[0].Text),
        };

        var substitutions = ScanYellowSubstitutionBinder.Bind(
            fields,
            bytes,
            ScanSourceKind.Word,
            TemplateSourceFormat.Docx);

        var written = Assert.Single(substitutions);
        var region = Assert.IsType<DocumentRegion.WordSpan>(written.Region);
        Assert.Equal(second.ParagraphAddress, region.ParagraphAddress);
        Assert.Equal(second.Start, region.Start);
        Assert.NotEqual(
            ((DocumentRegion.WordSpan)yellows[0].Region).ParagraphAddress + "|0",
            region.ParagraphAddress + "|" + region.Start.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains("ADAT", written.Token, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bind_splits_a_compound_token_across_sibling_yellows_in_the_same_paragraph()
    {
        var bytes = CreateWordOneParagraphYellows("kop gezeklik", "Yerkin Didem", "U3655957");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word).ToList();
        Assert.Equal(3, yellows.Count);

        var fields = new[]
        {
            Field("{{.AVCAT}}, {{.PFN}}, {{.PPN}}", yellows[0].Region, yellows[0].Text),
        };

        var substitutions = ScanYellowSubstitutionBinder.Bind(
            fields,
            bytes,
            ScanSourceKind.Word,
            TemplateSourceFormat.Docx);

        Assert.Equal(3, substitutions.Count);
        Assert.Contains("AVCAT", substitutions[0].Token, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PFN", substitutions[1].Token, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PPN", substitutions[2].Token, StringComparison.OrdinalIgnoreCase);
        Assert.All(substitutions, static s =>
            Assert.Equal(1, TemplateTokenSyntax.GetShortCodes(s.Token).Count));
        Assert.Equal(yellows[0].Region, substitutions[0].Region);
        Assert.Equal(yellows[1].Region, substitutions[1].Region);
        Assert.Equal(yellows[2].Region, substitutions[2].Region);
    }

    [Fact]
    public void Bind_keeps_a_compound_when_the_paragraph_has_one_yellow()
    {
        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture("kop gezeklik Yerkin Didem U3655957");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var span = Assert.Single(yellows);

        var substitutions = ScanYellowSubstitutionBinder.Bind(
            new[] { Field("{{.AVCAT}}, {{.PFN}}, {{.PPN}}", span.Region, span.Text) },
            bytes,
            ScanSourceKind.Word,
            TemplateSourceFormat.Docx);

        var written = Assert.Single(substitutions);
        Assert.Equal(3, TemplateTokenSyntax.GetShortCodes(written.Token).Count);
        Assert.Equal(span.Region, written.Region);
    }

    [Fact]
    public void Bind_writes_image_token_on_live_drawing()
    {
        var bytes = YellowWordWithPortrait();
        var drawing = Assert.Single(ScanOfficePictureExtractor.Extract(bytes));

        var substitutions = ScanYellowSubstitutionBinder.Bind(
            new[] { Field("{{IMAGE:PPH}}", drawing, "Person photo") },
            bytes,
            ScanSourceKind.Word,
            TemplateSourceFormat.Docx);

        var written = Assert.Single(substitutions);
        Assert.IsType<DocumentRegion.WordDrawing>(written.Region);
        Assert.Contains("IMAGE:PPH", written.Token, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(drawing, written.Region);
    }

    [Fact]
    public void Bind_image_wordspan_uses_live_drawing_not_text_yellow()
    {
        var bytes = YellowWordWithPortrait();
        var drawing = Assert.Single(ScanOfficePictureExtractor.Extract(bytes));

        var substitutions = ScanYellowSubstitutionBinder.Bind(
            new[] { Field("{{IMAGE:PPH}}", new DocumentRegion.WordSpan("p10", 0, 12), "{{IMAGE:PPH}}") },
            bytes,
            ScanSourceKind.Word,
            TemplateSourceFormat.Docx);

        var written = Assert.Single(substitutions);
        var region = Assert.IsType<DocumentRegion.WordDrawing>(written.Region);
        Assert.Equal(drawing.ParagraphAddress, region.ParagraphAddress);
        Assert.Equal(drawing.DrawingIndex, region.DrawingIndex);
    }

    private static readonly long PortraitWidthEmu = 35L * WordInlinePictureLocator.EmuPerMillimetre;
    private static readonly long PortraitHeightEmu = 45L * WordInlinePictureLocator.EmuPerMillimetre;

    private static byte[] YellowWordWithPortrait() =>
        TemplateConvertFixtures.AppendInlinePicture(
            ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434"),
            PortraitWidthEmu,
            PortraitHeightEmu);

    private static ScanDetectedField Field(string token, DocumentRegion? region, string label) =>
        new()
        {
            FieldId = Guid.NewGuid().ToString("N"),
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = label,
            ProposedToken = token,
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = region,
            IsLocked = true,
        };

    private static byte[] CreateWordOneParagraphYellows(params string[] yellowPhrases)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var runs = new List<OpenXmlElement>();
            for (var i = 0; i < yellowPhrases.Length; i++)
            {
                if (i > 0)
                {
                    runs.Add(new Run(new Text(", ")
                    {
                        Space = SpaceProcessingModeValues.Preserve,
                    }));
                }

                runs.Add(new Run(
                    new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                    new Text(yellowPhrases[i])));
            }

            main.Document = new Document(new Body(new Paragraph(runs)));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}