#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanManualPlacementTests
{
    [Fact]
    public void Word_click_adds_a_row_scoped_mark_for_the_clicked_word()
    {
        var bytes = WordTable("Ozer", "S36133641, 15.11.2023");
        var result = ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("S36133641", 0, 0),
            Array.Empty<ScanDetectedField>());

        var added = Assert.IsType<ScanDetectedField>(result.Added);
        Assert.Null(result.SelectFieldId);
        Assert.True(added.PlacedManually);
        Assert.False(added.IsLocked);
        Assert.Null(added.ProposedToken);
        Assert.Equal(ScanFieldScope.Row, added.Scope);
        Assert.Equal("S36133641", added.LabelText);
        var span = Assert.IsType<DocumentRegion.WordSpan>(added.SourceRegion);
        Assert.True(span.Length >= "S36133641".Length);
    }

    [Fact]
    public void Word_click_on_an_existing_span_selects_that_field()
    {
        var bytes = WordTable("Ozer", "S36133641");
        var first = ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("S36133641", 0, 0),
            Array.Empty<ScanDetectedField>());
        var again = ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("S36133641", 0, 0),
            [first.Added!]);

        Assert.Null(again.Added);
        Assert.Equal(first.Added!.FieldId, again.SelectFieldId);
    }

    [Fact]
    public void Excel_second_occurrence_is_the_right_cell()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("A1").Value = "Raýatlygy";
            ws.Cell("B1").Value = "Raýatlygy";
            ws.Cell("A2").Value = "TUR";
            ws.Cell("B2").Value = "TKM";
            wb.SaveAs(ms);
        }

        var left = ScanManualPlacement.Resolve(
            ms.ToArray(),
            ScanSourceKind.Excel,
            new ScanManualPlacement.Request("TUR", 0, 0),
            Array.Empty<ScanDetectedField>());
        var cell = Assert.IsType<DocumentRegion.ExcelCell>(left.Added!.SourceRegion);
        Assert.Equal("A2", cell.CellReference);
        Assert.Equal(ScanFieldScope.Row, left.Added.Scope);
    }

    [Fact]
    public void Word_line_around_the_second_digit_does_not_use_the_first()
    {
        const string text = "sanawdaky 1 (bir) sany. 1 (bir) ay mohlet bilen.";
        var bytes = WordBody(text);
        var result = ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("1", 0, 0, "1 (bir) ay mohlet", 42, 61),
            Array.Empty<ScanDetectedField>());

        var added = Assert.IsType<ScanDetectedField>(result.Added);
        var span = Assert.IsType<DocumentRegion.WordSpan>(added.SourceRegion);
        var first = text.IndexOf('1');
        var second = text.IndexOf('1', first + 1);
        Assert.Equal(second, span.Start);
        Assert.Equal("1", added.LabelText);
        Assert.Equal(42, added.PinLeft);
        Assert.Equal(61, added.PinTop);
    }

    [Fact]
    public void Word_line_around_the_first_digit_stays_on_the_first()
    {
        const string text = "sanawdaky 1 (bir) sany. 1 (bir) ay mohlet bilen.";
        var bytes = WordBody(text);
        var result = ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request("1", 0, 0, "sanawdaky 1 (bir) sany"),
            Array.Empty<ScanDetectedField>());

        var span = Assert.IsType<DocumentRegion.WordSpan>(result.Added!.SourceRegion);
        Assert.Equal(text.IndexOf('1'), span.Start);
    }

    [Fact]
    public void Word_selected_phrase_covers_that_phrase()
    {
        const string text = "sanawdaky 1 (bir) sany. 1 (bir) ay mohlet bilen.";
        const phrase = "1 (bir) ay mohlet";
        var bytes = WordBody(text);
        var result = ScanManualPlacement.Resolve(
            bytes,
            ScanSourceKind.Word,
            new ScanManualPlacement.Request(phrase, 0, 0, phrase, 12, 40),
            Array.Empty<ScanDetectedField>());

        var added = Assert.IsType<ScanDetectedField>(result.Added);
        var span = Assert.IsType<DocumentRegion.WordSpan>(added.SourceRegion);
        Assert.Equal(text.IndexOf(phrase, StringComparison.Ordinal), span.Start);
        Assert.Equal(phrase.Length, span.Length);
        Assert.Equal(phrase, added.LabelText);
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

    private static byte[] WordTable(string name, string passport)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var table = new Table(
                new TableRow(
                    new TableCell(new Paragraph(new Run(new Text("Familiýasy")))),
                    new TableCell(new Paragraph(new Run(new Text("Pasport"))))),
                new TableRow(
                    new TableCell(new Paragraph(new Run(new Text(name)))),
                    new TableCell(new Paragraph(new Run(new Text(passport))))));
            main.Document = new Document(new Body(table));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}
