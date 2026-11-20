using System;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ExcelPreviewEvaluationNoticeEdgeTests
{
    [Fact]
    public void RemoveInjectedSheet_null_returns_empty()
    {
        var cleaned = ExcelPreviewEvaluationNotice.RemoveInjectedSheet(null!);

        Assert.Empty(cleaned);
    }

    [Fact]
    public void RemoveInjectedSheet_empty_returns_empty()
    {
        var cleaned = ExcelPreviewEvaluationNotice.RemoveInjectedSheet(Array.Empty<byte>());

        Assert.Empty(cleaned);
    }

    [Fact]
    public void RemoveInjectedSheet_leaves_bytes_when_only_evaluation_sheets_exist()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Evaluation Warning").Cell("A1").Value = "For evaluation purposes only";
        workbook.AddWorksheet("Evaluation Warning (1)").Cell("A1").Value = "Also evaluation";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var original = stream.ToArray();

        var cleaned = ExcelPreviewEvaluationNotice.RemoveInjectedSheet(original);

        Assert.Equal(original, cleaned);
        using var again = new XLWorkbook(new MemoryStream(cleaned));
        Assert.Equal(2, again.Worksheets.Count);
        Assert.All(again.Worksheets, sheet => Assert.StartsWith("Evaluation Warning", sheet.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RemoveInjectedSheet_no_warning_sheet_returns_original_workbook_content()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Sanaw").Cell("A1").Value = "Familiyasy";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var original = stream.ToArray();

        var cleaned = ExcelPreviewEvaluationNotice.RemoveInjectedSheet(original);

        using var again = new XLWorkbook(new MemoryStream(cleaned));
        Assert.Equal("Sanaw", Assert.Single(again.Worksheets).Name);
        Assert.Equal("Familiyasy", again.Worksheet(1).Cell("A1").GetString());
    }

    [Fact]
    public void RemoveInjectedSheet_corrupt_bytes_returns_original()
    {
        var corrupt = Encoding.UTF8.GetBytes("not-an-xlsx");

        var cleaned = ExcelPreviewEvaluationNotice.RemoveInjectedSheet(corrupt);

        Assert.Equal(corrupt, cleaned);
    }

    [Fact]
    public void RemoveInjectedSheet_is_case_insensitive_on_warning_name()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Officer").Cell("A1").Value = "Keep";
        workbook.AddWorksheet("evaluation warning").Cell("A1").Value = "stamp";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var cleaned = ExcelPreviewEvaluationNotice.RemoveInjectedSheet(stream.ToArray());

        using var again = new XLWorkbook(new MemoryStream(cleaned));
        var only = Assert.Single(again.Worksheets);
        Assert.Equal("Officer", only.Name);
        Assert.DoesNotContain(
            again.Worksheets,
            sheet => sheet.Name.Contains("evaluation", StringComparison.OrdinalIgnoreCase));
    }
}
