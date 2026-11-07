#nullable enable

using ClosedXML.Excel;
using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

/// <summary>
/// Green highlighter ink (header-letter people lists) must clear like yellow so Preview
/// does not keep sample fill after Create-from-yellow-marks.
/// </summary>
public class ExcelTemplateGreenFillStripTests
{
    [Theory]
    [InlineData(100, 200, 100, true)]
    [InlineData(80, 180, 90, true)]
    [InlineData(255, 255, 0, false)]  // yellow, not green
    [InlineData(120, 130, 120, false)] // not green enough
    [InlineData(50, 100, 50, false)]   // too dark / low green
    public void IsHighlighterGreenRgb_matches_office_green_not_yellow(int r, int g, int b, bool expected) =>
        Assert.Equal(expected, ExcelTemplateTokenWriter.IsHighlighterGreenRgb(r, g, b));

    [Fact]
    public void StripAllYellowFills_clears_unmapped_green_rgb_leftover()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Sanaw");
            sheet.Cell("A2").Value = "1. Hayati Uyan";
            // Highlighter green: strong G, not yellowish.
            sheet.Cell("A2").Style.Fill.BackgroundColor = XLColor.FromArgb(100, 200, 100);
            workbook.SaveAs(stream);
        }

        var cleaned = ExcelTemplateTokenWriter.StripAllYellowFills(stream.ToArray());
        using var verify = new MemoryStream(cleaned, writable: false);
        using var wb = new XLWorkbook(verify);
        Assert.Equal(XLFillPatternValues.None, wb.Worksheet("Sanaw").Cell("A2").Style.Fill.PatternType);
        Assert.Equal("1. Hayati Uyan", wb.Worksheet("Sanaw").Cell("A2").GetString());
    }
}
