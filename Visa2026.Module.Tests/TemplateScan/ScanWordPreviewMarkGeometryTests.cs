#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanWordPreviewMarkGeometryTests
{
    [Fact]
    public void TryMap_places_later_body_paragraph_below_earlier()
    {
        var bytes = Letter("12.02.2026", "Mary welayaty", "Mary etrabyndan");
        var marks = new[]
        {
            Mark("date", "body/0", 0, 10, "12.02.2026"),
            Mark("region", "body/1", 0, 14, "Mary welayaty"),
            Mark("city", "body/2", 0, 15, "Mary etrabyndan"),
        };

        var layout = ScanWordPreviewMarkGeometry.TryMap(bytes, marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["region"].Top > layout.Boxes["date"].Top);
        Assert.True(layout.Boxes["city"].Top > layout.Boxes["region"].Top);
        Assert.InRange(layout.Boxes["city"].Top, 1, 90);
        Assert.True(layout.Boxes["city"].Width > 2);
    }

    [Fact]
    public void TryMap_places_later_span_to_the_right_in_same_paragraph()
    {
        var bytes = Letter("Mary welayaty Mary etrabyndan");
        var marks = new[]
        {
            Mark("region", "body/0", 0, 14, "Mary welayaty"),
            Mark("city", "body/0", 15, 15, "Mary etrabyndan"),
        };

        var layout = ScanWordPreviewMarkGeometry.TryMap(bytes, marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["city"].Left > layout.Boxes["region"].Left);
    }

    [Fact]
    public void TryMap_places_header_above_body()
    {
        var bytes = LetterWithHeader("12.02.2026", "Mary etrabyndan");
        var marks = new[]
        {
            Mark("hdr", "header0/0", 0, 10, "12.02.2026"),
            Mark("city", "body/0", 0, 15, "Mary etrabyndan"),
        };

        var layout = ScanWordPreviewMarkGeometry.TryMap(bytes, marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["city"].Top > layout.Boxes["hdr"].Top);
    }

    [Fact]
    public void TryMap_returns_null_for_excel_marks()
    {
        var marks = new[]
        {
            new ScanReviewOrderedField(
                1,
                "n",
                "1",
                "{{.PFN}}",
                new DocumentRegion.ExcelCell("Sanaw", "A1"),
                0,
                false),
        };

        Assert.Null(ScanWordPreviewMarkGeometry.TryMap(Letter("x"), marks));
    }

    private static ScanReviewOrderedField Mark(string id, string address, int start, int length, string label) =>
        new(
            1,
            id,
            label,
            "{{ds.BTFRG}}",
            new DocumentRegion.WordSpan(address, start, length),
            0,
            false,
            OverlayId: id);

    private static byte[] Letter(params string[] paragraphs)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            foreach (var text in paragraphs)
                body.AppendChild(new Paragraph(new Run(new Text(text))));
            main.Document = new Document(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] LetterWithHeader(string headerText, string bodyText)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var headerPart = main.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(
                new Paragraph(new Run(new Text(headerText) { Space = SpaceProcessingModeValues.Preserve })));
            var body = new Body(
                new Paragraph(new Run(new Text(bodyText))),
                new SectionProperties(
                    new HeaderReference
                    {
                        Id = main.GetIdOfPart(headerPart),
                        Type = HeaderFooterValues.Default,
                    }));
            main.Document = new Document(body);
            main.Document.Save();
        }

        return stream.ToArray();
    }
}