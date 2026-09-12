#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.ExcelReports;

public class ExcelReportPassportChangeSanawTests
{
    [Fact]
    public async Task Generate_fills_previous_and_new_passport_tables()
    {
        var template = CreateStackedTemplate(secondTableHasLoop: false);
        using var output = new MemoryStream();
        await new ExcelReportGenerator(new UnusedExtractor()).GenerateAsync(
            template,
            new ApplicationProfileInstance(),
            output,
            CreatePeople());

        output.Position = 0;
        using var workbook = new XLWorkbook(output);
        var sheet = workbook.Worksheet(1);

        Assert.Equal("Aydogan", sheet.Cell(4, 2).GetString());
        Assert.Equal("U000OLD", sheet.Cell(4, 3).GetString());
        Assert.Equal("Ali", sheet.Cell(5, 2).GetString());
        Assert.Equal("U000OLD2", sheet.Cell(5, 3).GetString());
        Assert.Contains("Taze", sheet.Cell(6, 1).GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Aydogan", sheet.Cell(8, 2).GetString());
        Assert.Equal("U111NEW", sheet.Cell(8, 3).GetString());
        Assert.Equal("Ali", sheet.Cell(9, 2).GetString());
        Assert.Equal("U111NEW2", sheet.Cell(9, 3).GetString());
        Assert.DoesNotContain("{{", sheet.Cell(4, 3).GetString());
        Assert.DoesNotContain("{{", sheet.Cell(8, 3).GetString());
    }

    [Fact]
    public async Task Generate_expands_two_loop_markers_without_deleting_titles()
    {
        var template = CreateStackedTemplate(secondTableHasLoop: true);
        using var output = new MemoryStream();
        await new ExcelReportGenerator(new UnusedExtractor()).GenerateAsync(
            template,
            new ApplicationProfileInstance(),
            output,
            CreatePeople());

        output.Position = 0;
        using var workbook = new XLWorkbook(output);
        var sheet = workbook.Worksheet(1);

        Assert.Contains("Kicirak", sheet.Cell(2, 1).GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Taze", sheet.Cell(6, 1).GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("U000OLD", sheet.Cell(4, 3).GetString());
        Assert.Equal("U111NEW", sheet.Cell(8, 3).GetString());
    }

    [Fact]
    public void Sanawy_row_dictionary_includes_previous_passport()
    {
        var item = CreatePeople()[0];
        var row = UserReportMergeDataHelper.BuildSanawyRowDictionary(item, 1);

        Assert.Equal("U111NEW", row["Passport_Number"]);
        Assert.Equal("U000OLD", row["PreviousPassport_Number"]);
        Assert.Equal("U000OLD", row["PRPN"]);
        Assert.Equal("U111NEW", row["PPN"]);
    }

    private static List<ApplicationRosterMergeLine> CreatePeople() =>
    [
        Line("Aydogan", "Mehmet", "U111NEW", "U000OLD"),
        Line("Ali", "Can", "U111NEW2", "U000OLD2"),
    ];

    private static ApplicationRosterMergeLine Line(
        string lastName,
        string firstName,
        string currentNumber,
        string previousNumber) =>
        new()
        {
            Person = new Person
            {
                LastName = lastName,
                FirstName = firstName,
            },
            CurrentPassport = new Passport { PassportNumber = currentNumber },
            PreviousPassport = new Passport { PassportNumber = previousNumber },
        };

    private static UserReportTemplate CreateStackedTemplate(bool secondTableHasLoop)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sanaw");
        sheet.Cell(1, 1).Value = "Dasary yurt rayatynyn sanawy";
        sheet.Cell(2, 1).Value = "Kicirak pasportyn maglumatlary";
        sheet.Cell(3, 2).Value = "Familiyasy";
        sheet.Cell(3, 3).Value = "Pasport";
        sheet.Cell(4, 1).Value = "{{#ds.rows}}";
        sheet.Cell(4, 2).Value = "{{.Person_LastName}}";
        sheet.Cell(4, 3).Value = "{{.PPN}}";
        sheet.Cell(5, 1).Value = "Taze pasportyn maglumatlary";
        sheet.Cell(6, 2).Value = "Familiyasy";
        sheet.Cell(6, 3).Value = "Pasport";
        sheet.Cell(7, 1).Value = secondTableHasLoop ? "{{#ds.rows}}" : string.Empty;
        sheet.Cell(7, 2).Value = "{{.Person_LastName}}";
        sheet.Cell(7, 3).Value = "{{.PPN}}";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new UserReportTemplate
        {
            ExcelMergeMode = ExcelMergeMode.ItemList,
            TemplateOutputFormat = TemplateOutputFormat.Excel,
            TemplateFile = new FileData
            {
                FileName = "dasary-yurt-rayatynyn-sanawy.xlsx",
                Content = stream.ToArray(),
            },
        };
    }

    private sealed class UnusedExtractor : IExcelTemplatePlaceholderExtractor
    {
        public Task<IList<string>> ExtractPlaceholdersAsync(Stream xlsxStream) =>
            Task.FromResult<IList<string>>(Array.Empty<string>());
    }
}