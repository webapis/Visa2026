using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Visa2026.Module.Services.ExcelReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.ExcelReports;

public sealed class ExcelTemplatePlaceholderExtractorTests
{
    [Fact]
    public async Task ExtractPlaceholdersAsync_CollectsDistinctTokensAcrossSheets()
    {
        using var stream = CreateWorkbook(wb =>
        {
            var sheet1 = wb.Worksheets.Add("Sheet1");
            sheet1.Cell(1, 1).Value = "Hello {{Application.Number}} and {{Person_FullName}}";
            sheet1.Cell(2, 1).Value = "Again {{Application.Number}}";

            var sheet2 = wb.Worksheets.Add("Sheet2");
            sheet2.Cell(1, 1).Value = "{{.rows.Person_FullName}} {{#ds.rows}}";
            sheet2.Cell(1, 2).Value = string.Empty;
            sheet2.Cell(2, 1).Value = "no tokens here";
        });

        var extractor = new ExcelTemplatePlaceholderExtractor();
        var placeholders = await extractor.ExtractPlaceholdersAsync(stream);

        Assert.Equal(4, placeholders.Count);
        Assert.Contains("Application.Number", placeholders);
        Assert.Contains("Person_FullName", placeholders);
        Assert.Contains(".rows.Person_FullName", placeholders);
        Assert.Contains("#ds.rows", placeholders);
    }

    [Fact]
    public async Task ExtractPlaceholdersAsync_ResetsSeekableStream()
    {
        using var stream = CreateWorkbook(wb =>
        {
            wb.Worksheets.Add("Data").Cell(1, 1).Value = "{{RowNo}}";
        });

        // Consume past start so seek reset is required.
        stream.Position = stream.Length / 2;

        var extractor = new ExcelTemplatePlaceholderExtractor();
        var placeholders = await extractor.ExtractPlaceholdersAsync(stream);

        Assert.Equal(new[] { "RowNo" }, placeholders.ToArray());
    }

    [Fact]
    public async Task ExtractPlaceholdersAsync_EmptyWorkbook_ReturnsEmpty()
    {
        using var stream = CreateWorkbook(wb =>
        {
            wb.Worksheets.Add("Empty").Cell(1, 1).Value = "plain text";
        });

        var extractor = new ExcelTemplatePlaceholderExtractor();
        var placeholders = await extractor.ExtractPlaceholdersAsync(stream);

        Assert.Empty(placeholders);
    }

    private static MemoryStream CreateWorkbook(System.Action<XLWorkbook> configure)
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            configure(workbook);
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }
}
