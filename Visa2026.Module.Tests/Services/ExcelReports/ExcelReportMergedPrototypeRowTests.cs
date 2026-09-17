#nullable enable

using ClosedXML.Excel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.ExcelReports;

public class ExcelReportMergedPrototypeRowTests
{
    [Fact]
    public async Task Generate_expands_rows_when_title_merge_crosses_data_row()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sanaw");
        sheet.Range("A1:C3").Merge();
        sheet.Cell("A1").Value = "Title";
        sheet.Cell("A4").Value = "{{#ds.rows}}{{.PLN}}";
        sheet.Cell("B4").Value = "{{.PFNM}}";
        sheet.Range("A4:A5").Merge();
        using var input = new MemoryStream();
        workbook.SaveAs(input);

        var template = new UserReportTemplate
        {
            ExcelMergeMode = ExcelMergeMode.ItemList,
            TemplateOutputFormat = TemplateOutputFormat.Excel,
            TemplateFile = new FileData
            {
                FileName = "scan-sanaw.xlsx",
                Content = input.ToArray(),
            },
        };

        using var output = new MemoryStream();
        await new ExcelReportGenerator(new UnusedExtractor()).GenerateAsync(
            template,
            new ApplicationProfileInstance(),
            output,
            [
                Line("Alkan", "Cafer"),
                Line("Erol", "Hilmi"),
                Line("Demirci", "Omer"),
            ]);

        output.Position = 0;
        using var filled = new XLWorkbook(output);
        var ws = filled.Worksheet(1);
        Assert.Contains("Alkan", ws.Cell("A4").GetString(), StringComparison.Ordinal);
        Assert.Contains("Erol", ws.Cell("A5").GetString(), StringComparison.Ordinal);
        Assert.Contains("Demirci", ws.Cell("A6").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_injects_loop_when_prototype_cells_are_merged()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sanaw");
        sheet.Range("A1:C3").Merge();
        sheet.Cell("A1").Value = "Title";
        sheet.Cell("A4").Value = "{{.PLN}}";
        sheet.Cell("B4").Value = "{{.PFNM}}";
        sheet.Range("A4:A5").Merge();
        using var input = new MemoryStream();
        workbook.SaveAs(input);

        var template = new UserReportTemplate
        {
            ExcelMergeMode = ExcelMergeMode.ItemList,
            TemplateOutputFormat = TemplateOutputFormat.Excel,
            TemplateFile = new FileData
            {
                FileName = "scan-sanaw.xlsx",
                Content = input.ToArray(),
            },
        };

        using var output = new MemoryStream();
        await new ExcelReportGenerator(new UnusedExtractor()).GenerateAsync(
            template,
            new ApplicationProfileInstance(),
            output,
            [
                Line("Alkan", "Cafer"),
                Line("Erol", "Hilmi"),
                Line("Demirci", "Omer"),
            ]);

        output.Position = 0;
        using var filled = new XLWorkbook(output);
        var ws = filled.Worksheet(1);
        Assert.Contains("Alkan", ws.Cell("A4").GetString(), StringComparison.Ordinal);
        Assert.Contains("Erol", ws.Cell("A5").GetString(), StringComparison.Ordinal);
        Assert.Contains("Demirci", ws.Cell("A6").GetString(), StringComparison.Ordinal);
    }

    private static ApplicationRosterMergeLine Line(string last, string first) =>
        new()
        {
            Person = new Person
            {
                LastName = last,
                FirstName = first,
            },
        };

    private sealed class UnusedExtractor : IExcelTemplatePlaceholderExtractor
    {
        public Task<IList<string>> ExtractPlaceholdersAsync(Stream xlsxStream) =>
            Task.FromResult<IList<string>>(Array.Empty<string>());
    }
}