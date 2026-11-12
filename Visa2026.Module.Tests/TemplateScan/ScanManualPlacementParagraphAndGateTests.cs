#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// Review editor needs ParagraphText for the clicked span; empty clicks must not invent marks.
/// </summary>
public class ScanManualPlacementParagraphAndGateTests
{
    [Fact]
    public void Resolve_empty_or_whitespace_text_adds_nothing()
    {
        var bytes = WordBody("S36133641");
        Assert.Null(ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("  ", 0, 0),
            Array.Empty<ScanDetectedField>()).Added);
        Assert.Null(ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("", 0, 0),
            Array.Empty<ScanDetectedField>()).Added);
    }

    [Fact]
    public void Resolve_short_or_null_bytes_adds_nothing()
    {
        Assert.Null(ScanManualPlacement.Resolve(
            null,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("S36133641", 0, 0),
            Array.Empty<ScanDetectedField>()).Added);
        Assert.Null(ScanManualPlacement.Resolve(
            new byte[32],
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("S36133641", 0, 0),
            Array.Empty<ScanDetectedField>()).Added);
    }

    [Fact]
    public void ParagraphText_returns_openxml_paragraph_for_matching_span()
    {
        const string text = "sanawdaky 1 (bir) sany.";
        var bytes = WordBody(text);
        var placed = ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("1", 0, 0),
            Array.Empty<ScanDetectedField>());
        var span = Assert.IsType<DocumentRegion.WordSpan>(placed.Added!.SourceRegion);

        Assert.Equal(text, ScanManualPlacement.ParagraphText(bytes, span));
    }

    [Fact]
    public void ParagraphText_null_for_non_word_region_or_tiny_package()
    {
        Assert.Null(ScanManualPlacement.ParagraphText(null, new DocumentRegion.WordSpan("body/0", 0, 1)));
        Assert.Null(ScanManualPlacement.ParagraphText(
            new byte[8],
            new DocumentRegion.WordSpan("body/0", 0, 1)));
        Assert.Null(ScanManualPlacement.ParagraphText(
            WordBody("abc"),
            new DocumentRegion.ExcelCell("Sheet1", "A1")));
    }

    private static byte[] WordBody(string paragraph)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text(paragraph)))));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}
