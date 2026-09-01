#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanYellowMarkContextBuilderTests
{
    [Fact]
    public void Word_marks_yellow_span_and_keeps_printed_label()
    {
        var bytes = CreateWordWithLabelAndYellow("Wekil ady: ", "Nepesowa Tumar Aşyrowna");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var yellow = Assert.Single(yellows);
        var drafts = new[]
        {
            new ScanDetectedFieldDraft
            {
                FieldId = "n1",
                Box = ScanBoundingBox.FullPage,
                PageIndex = 0,
                LabelText = yellow.Text,
                ProposedToken = "{{ds.RPFN}}",
                SourceRegion = yellow.Region,
            },
        };

        var map = ScanYellowMarkContextBuilder.Build(bytes, ScanSourceKind.Word, drafts);
        var context = Assert.Single(map).Value;

        Assert.Contains("<<<Nepesowa Tumar Aşyrowna>>>", context.SurroundingSnippet, StringComparison.Ordinal);
        Assert.Contains("Wekil ady", context.PrintedLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Null(context.SheetName);
    }

    [Fact]
    public void Word_uses_previous_paragraph_when_yellow_is_on_its_own_line()
    {
        var bytes = ScanOfficeYellowExtractorTests.CreateWordWithCaptionThenYellow(
            "we Kärhananyň wiza işleri boýunça ygtyýarly wekili:",
            "Nepesowa Tumar Aşyrowna");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var yellow = Assert.Single(yellows);
        var drafts = new[]
        {
            new ScanDetectedFieldDraft
            {
                FieldId = "n2",
                Box = ScanBoundingBox.FullPage,
                PageIndex = 0,
                LabelText = yellow.Text,
                ProposedToken = "{{.PFN}}",
                SourceRegion = yellow.Region,
            },
        };

        var map = ScanYellowMarkContextBuilder.Build(bytes, ScanSourceKind.Word, drafts);
        var context = Assert.Single(map).Value;

        Assert.Contains("wekili", context.PrintedLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Word_captures_parenthetical_caption_under_the_yellow_line()
    {
        var bytes = ScanOfficeYellowExtractorTests.CreateWordWithYellowThenCaption(
            "pasporty: ",
            "U37109249, T.C. ASKABAT BE, 19.02.2024",
            "(pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var yellow = Assert.Single(yellows);
        var drafts = new[]
        {
            new ScanDetectedFieldDraft
            {
                FieldId = "p1",
                Box = ScanBoundingBox.FullPage,
                PageIndex = 0,
                LabelText = yellow.Text,
                SourceRegion = yellow.Region,
            },
        };

        var map = ScanYellowMarkContextBuilder.Build(bytes, ScanSourceKind.Word, drafts);
        var context = Assert.Single(map).Value;

        Assert.Contains("pasporty", context.PrintedLabel, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(context.FollowingCaption);
        Assert.Contains("mohleti", context.FollowingCaption, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("belgisi", context.FollowingCaption, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Excel_includes_sheet_name_and_header_row()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("A4").Value = "Familiýasy";
            ws.Cell("B4").Value = "Ady";
            ws.Cell("A5").Value = "Erol";
            ws.Cell("A5").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var yellow = Assert.Single(yellows);
        var drafts = new[]
        {
            new ScanDetectedFieldDraft
            {
                FieldId = "e1",
                Box = ScanBoundingBox.FullPage,
                PageIndex = 0,
                LabelText = yellow.Text,
                ColumnHeader = "Familiýasy",
                SourceRegion = yellow.Region,
            },
        };

        var map = ScanYellowMarkContextBuilder.Build(bytes, ScanSourceKind.Excel, drafts);
        var context = Assert.Single(map).Value;

        Assert.Equal("Sanaw", context.SheetName);
        Assert.Contains("A: Familiýasy", context.HeaderRow, StringComparison.Ordinal);
        Assert.Contains("B: Ady", context.HeaderRow, StringComparison.Ordinal);
        Assert.Equal("Familiýasy", context.PrintedLabel);
    }

    [Fact]
    public void MarkAndTrim_centers_on_yellow_when_paragraph_is_long()
    {
        var prefix = new string('x', 200);
        var suffix = new string('y', 200);
        var marked = ScanYellowMarkContextBuilder.MarkAndTrim(prefix + "NAME" + suffix, 200, 4);

        Assert.Contains("<<<NAME>>>", marked, StringComparison.Ordinal);
        Assert.True(marked.Length < prefix.Length + suffix.Length);
        Assert.StartsWith("…", marked, StringComparison.Ordinal);
        Assert.EndsWith("…", marked, StringComparison.Ordinal);
    }

    private static byte[] CreateWordWithLabelAndYellow(string label, string yellow)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var paragraph = new Paragraph(
                new Run(new Text(label) { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new RunProperties(new Highlight { Val = HighlightColorValues.Yellow }),
                    new Text(yellow)));
            main.Document = new Document(new Body(paragraph));
            main.Document.Save();
        }

        return stream.ToArray();
    }
}
