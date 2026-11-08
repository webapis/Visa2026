#nullable enable

using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanOfficeEditorSheetTests
{
    [Theory]
    [InlineData(1, "A")]
    [InlineData(26, "Z")]
    [InlineData(27, "AA")]
    [InlineData(28, "AB")]
    [InlineData(52, "AZ")]
    [InlineData(53, "BA")]
    public void ColumnName_matches_Excel_letters(int column, string expected)
    {
        Assert.Equal(expected, ScanOfficeEditorSheet.ColumnName(column));
    }

    [Fact]
    public void Read_returns_null_for_null_or_tiny_bytes()
    {
        Assert.Null(ScanOfficeEditorSheet.Read(null));
        Assert.Null(ScanOfficeEditorSheet.Read(Array.Empty<byte>()));
        Assert.Null(ScanOfficeEditorSheet.Read(new byte[32]));
    }

    [Fact]
    public void Read_empty_sheet_returns_empty_cell_grid()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            wb.AddWorksheet("Empty");
            wb.SaveAs(ms);
        }

        var model = ScanOfficeEditorSheet.Read(ms.ToArray());
        Assert.NotNull(model);
        Assert.Equal("Empty", model!.SheetName);
        Assert.Empty(model.Cells);
        Assert.Equal(1, model.MinRow);
        Assert.Equal(1, model.MaxRow);
    }

    [Fact]
    public void Read_includes_trimmed_text_and_yellow_flag_for_used_range()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("B2").Value = "  Familiýasy  ";
            ws.Cell("C2").Value = "Ady";
            ws.Cell("C2").Style.Fill.BackgroundColor = XLColor.Yellow;
            ws.Cell("B3").Value = "Erol";
            wb.SaveAs(ms);
        }

        var model = ScanOfficeEditorSheet.Read(ms.ToArray());
        Assert.NotNull(model);
        Assert.Equal(2, model!.MinRow);
        Assert.Equal(3, model.MaxRow);
        Assert.Equal(2, model.MinColumn);
        Assert.Equal(3, model.MaxColumn);

        var header = Assert.Single(model.Cells, c => c.Reference == "B2");
        Assert.Equal("Familiýasy", header.Text);
        Assert.False(header.Yellow);

        var yellow = Assert.Single(model.Cells, c => c.Reference == "C2");
        Assert.Equal("Ady", yellow.Text);
        Assert.True(yellow.Yellow);

        // Used range is dense: empty B3/C3? Wait B3 has Erol, C3 empty inside range
        Assert.Contains(model.Cells, c => c.Reference == "B3" && c.Text == "Erol" && !c.Yellow);
        Assert.Contains(model.Cells, c => c.Reference == "C3" && c.Text == string.Empty && !c.Yellow);
        Assert.Equal(4, model.Cells.Count);
    }

    [Fact]
    public void Read_does_not_mark_non_yellow_fills()
    {
        using var ms = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            ws.Cell("A1").Value = "plain";
            ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Cell("B1").Value = "greenish";
            ws.Cell("B1").Style.Fill.BackgroundColor = XLColor.FromArgb(200, 255, 200);
            wb.SaveAs(ms);
        }

        var model = ScanOfficeEditorSheet.Read(ms.ToArray());
        Assert.NotNull(model);
        Assert.All(model!.Cells, c => Assert.False(c.Yellow));
    }
}
