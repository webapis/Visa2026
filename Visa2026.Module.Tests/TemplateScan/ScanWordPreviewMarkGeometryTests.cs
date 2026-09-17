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
    public void TryMap_places_later_table_cell_to_the_right()
    {
        var bytes = TableRow("Familiyasy", "Ady", "TUR");
        var marks = new[]
        {
            Mark("surname", "body/0", 0, 11, "Familiyasy"),
            Mark("given", "body/1", 0, 3, "Ady"),
            Mark("nat", "body/2", 0, 3, "TUR"),
        };

        var layout = ScanWordPreviewMarkGeometry.TryMap(bytes, marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["given"].Left > layout.Boxes["surname"].Left);
        Assert.True(layout.Boxes["nat"].Left > layout.Boxes["given"].Left);
        Assert.InRange(Math.Abs(layout.Boxes["nat"].Top - layout.Boxes["surname"].Top), 0, 2);
        Assert.Contains("nat", layout.TableMarkIds!);
    }

    [Fact]
    public void TryMap_places_next_table_row_below()
    {
        var bytes = TableGrid(
            new[] { "A1", "B1" },
            new[] { "A2", "B2" });
        var marks = new[]
        {
            Mark("a1", "body/0", 0, 2, "A1"),
            Mark("b1", "body/1", 0, 2, "B1"),
            Mark("a2", "body/2", 0, 2, "A2"),
            Mark("b2", "body/3", 0, 2, "B2"),
        };

        var layout = ScanWordPreviewMarkGeometry.TryMap(bytes, marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["a2"].Top > layout.Boxes["a1"].Top);
        Assert.True(layout.Boxes["b1"].Left > layout.Boxes["a1"].Left);
        Assert.True(layout.Boxes["b2"].Left > layout.Boxes["a2"].Left);
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

    private static byte[] TableRow(params string[] cells) => TableGrid(cells);

    private static byte[] TableGrid(params string[][] rows)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var colCount = rows.Length == 0 ? 1 : rows.Max(static r => r.Length);
            var grid = new TableGrid();
            for (var i = 0; i < colCount; i++)
                grid.AppendChild(new GridColumn { Width = "1440" });

            var table = new Table(grid);
            foreach (var rowCells in rows)
            {
                var row = new TableRow();
                foreach (var text in rowCells)
                {
                    row.AppendChild(new TableCell(new Paragraph(new Run(new Text(text)
                    {
                        Space = SpaceProcessingModeValues.Preserve,
                    }))));
                }

                table.AppendChild(row);
            }

            main.Document = new Document(new Body(table));
            main.Document.Save();
        }

        return stream.ToArray();
    }

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