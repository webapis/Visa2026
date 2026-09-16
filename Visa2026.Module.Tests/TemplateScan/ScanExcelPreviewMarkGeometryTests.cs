#nullable enable

using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanExcelPreviewMarkGeometryTests
{
    [Fact]
    public void TryMap_places_later_column_to_the_right_of_earlier()
    {
        var bytes = BuildSanaw();
        var marks = new[]
        {
            Mark("n", "A4", "1"),
            Mark("name", "B4", "Erol"),
            Mark("addr", "L4", "Garabogaz"),
        };

        var layout = ScanExcelPreviewMarkGeometry.TryMap(bytes, marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["n"].Left < layout.Boxes["name"].Left);
        Assert.True(layout.Boxes["name"].Left < layout.Boxes["addr"].Left);
        Assert.True(layout.Boxes["addr"].Left > 50, "Wide sanaw address column should sit on the right half.");
        Assert.InRange(layout.Boxes["n"].Top, 0, 99);
        Assert.True(layout.Aspect > 0);
    }

    [Fact]
    public void TryMap_places_data_row_below_title()
    {
        var bytes = BuildSanaw();
        var marks = new[]
        {
            Mark("title", "A1", "Sanaw"),
            Mark("cell", "B4", "Erol"),
        };

        var layout = ScanExcelPreviewMarkGeometry.TryMap(bytes, marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["cell"].Top > layout.Boxes["title"].Top);
        Assert.True(layout.Boxes["cell"].Top > 20);
    }

    [Fact]
    public void TryMap_uses_merged_range_width()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("A1").Value = "Title";
            ws.Range("B2:D2").Merge();
            ws.Cell("B2").Value = "TUR, Tatlısu";
            ws.Cell("B2").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var marks = new[] { Mark("compound", "B2", "TUR, Tatlısu") };
        var layout = ScanExcelPreviewMarkGeometry.TryMap(ms.ToArray(), marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["compound"].Width > layout.Boxes["compound"].Height);
        Assert.True(layout.Boxes["compound"].Width > 20);
    }

    [Fact]
    public void TryMap_returns_null_for_word_marks()
    {
        var marks = new[]
        {
            new ScanReviewOrderedField(
                1,
                "w1",
                "Name",
                "{{ds.PFN}}",
                new DocumentRegion.WordSpan("body/0", 0, 4),
                0,
                false),
        };

        Assert.Null(ScanExcelPreviewMarkGeometry.TryMap(BuildSanaw(), marks));
    }

    [Fact]
    public void TryMap_slices_shared_birth_cell_into_compound_part_boxes()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("D4").Value = "Doglan senesi we ýeri";
            ws.Cell("D5").Value = "05.04.1989, TUR, Fatih";
            ws.Cell("D5").Style.Fill.BackgroundColor = XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var cell = new DocumentRegion.ExcelCell("Sanaw", "D5");
        var marks = new[]
        {
            new ScanReviewOrderedField(4, "mark6", "05.04.1989", "{{.PDBT}}", cell, 0, false, "4.1", "mark6:1", 1, cell),
            new ScanReviewOrderedField(4, "mark6", "TUR", "{{.PCBT}}", cell, 0, false, "4.2", "mark6:2", 2, cell),
            new ScanReviewOrderedField(4, "mark6", "Fatih", "{{.PBPL}}", cell, 0, false, "4.3", "mark6:3", 3, cell),
        };

        var layout = ScanExcelPreviewMarkGeometry.TryMap(ms.ToArray(), marks);
        Assert.NotNull(layout);
        Assert.True(layout!.Boxes["mark6:1"].Left < layout.Boxes["mark6:2"].Left);
        Assert.True(layout.Boxes["mark6:2"].Left < layout.Boxes["mark6:3"].Left);
        Assert.True(layout.Boxes["mark6:1"].Width < 40);
    }

    private static ScanReviewOrderedField Mark(string id, string cell, string label) =>
        new(
            1,
            id,
            label,
            "{{.PFN}}",
            new DocumentRegion.ExcelCell("Sanaw", cell),
            0,
            false,
            OverlayId: id);

    private static byte[] BuildSanaw()
    {
        using var ms = new MemoryStream();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sanaw");
        ws.Cell("A1").Value = "Sanaw";
        ws.Cell("A3").Value = "№";
        ws.Cell("B3").Value = "Familiýasy";
        ws.Cell("L3").Value = "Salgysy";
        ws.Cell("A4").Value = "1";
        ws.Cell("B4").Value = "Erol";
        ws.Cell("L4").Value = "Garabogaz";
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
