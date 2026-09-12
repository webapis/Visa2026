using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.ExcelReports;

public class ExcelReportSignatoryFooterTests
{
    [Fact]
    public void HeaderDictionary_includes_signatory_short_codes()
    {
        var application = CreateApplication();
        var data = UserReportMergeDataHelper.BuildApplicationHeaderDictionary(application);

        Assert.Equal("Mudir", data["Application_CompanyHead_PositionTm"]);
        Assert.Equal("Mehmet Cirak", data["Application_CompanyHead_FullName"]);
        Assert.Equal("Mudir", data["ACPOS"]);
        Assert.Equal("Mehmet Cirak", data["ACFNM"]);
        Assert.Equal("Mehmet Cirak", data["CHFN"]);
        Assert.True(data.ContainsKey("AFNUM"));
        Assert.True(data.ContainsKey("MSRV"));
        Assert.True(data.ContainsKey("TPCNT"));
        Assert.True(data.ContainsKey("TPCTX"));
        Assert.True(data.ContainsKey("CVCNT"));
        Assert.True(data.ContainsKey("CVCTX"));
        Assert.True(data.ContainsKey("CWCNT"));
        Assert.True(data.ContainsKey("CWCTX"));
        Assert.True(data.ContainsKey("CancelVisaCount"));
        Assert.True(data.ContainsKey("CancelVisaCountText"));
        Assert.True(data.ContainsKey("CancelWPCount"));
        Assert.True(data.ContainsKey("CancelWPCountText"));
    }

    [Fact]
    public async Task Generate_fills_footer_ds_and_dot_tokens_after_row_copy()
    {
        var template = CreateItemListTemplate(
            dsPosition: "{{ds.ACPOS}}",
            dotName: "{{.ACFNM}}");

        using var output = new MemoryStream();
        await new ExcelReportGenerator(new UnusedExtractor()).GenerateAsync(
            template,
            CreateApplication(),
            output,
            [
                new ApplicationRosterMergeLine(),
                new ApplicationRosterMergeLine(),
            ]);

        output.Position = 0;
        using var workbook = new XLWorkbook(output);
        var sheet = workbook.Worksheet(1);

        Assert.Equal("Mudir", sheet.Cell(6, 2).GetString());
        Assert.Equal("Mehmet Cirak", sheet.Cell(6, 9).GetString());
        Assert.DoesNotContain("{{", sheet.Cell(6, 2).GetString());
        Assert.DoesNotContain("{{", sheet.Cell(6, 9).GetString());
    }

    private static ApplicationProfileInstance CreateApplication() =>
        new()
        {
            OrganizationSignatory = new AuthorizedSignatory
            {
                FullName = "Mehmet Cirak",
                PositionTitleTm = "Mudir",
            },
        };

    private static UserReportTemplate CreateItemListTemplate(string dsPosition, string dotName)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sanaw");
        sheet.Cell(1, 2).Value = "Title";
        sheet.Cell(2, 2).Value = "Familiyasy";
        sheet.Cell(3, 1).Value = "{{#ds.rows}}";
        sheet.Cell(3, 2).Value = "{{.Person_LastName}}";
        sheet.Cell(4, 1).Value = "{{/ds.rows}}";
        sheet.Cell(6, 2).Value = dsPosition;
        sheet.Cell(6, 9).Value = dotName;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new UserReportTemplate
        {
            ExcelMergeMode = ExcelMergeMode.ItemList,
            TemplateOutputFormat = TemplateOutputFormat.Excel,
            TemplateFile = new FileData
            {
                FileName = "sanaw-hasaba-almak.xlsx",
                Content = stream.ToArray(),
            },
        };
    }

    private sealed class UnusedExtractor : IExcelTemplatePlaceholderExtractor
    {
        public Task<IList<string>> ExtractPlaceholdersAsync(Stream xlsxStream) =>
            Task.FromResult<IList<string>>(System.Array.Empty<string>());
    }
}