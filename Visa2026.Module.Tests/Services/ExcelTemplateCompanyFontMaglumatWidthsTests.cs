using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation/Work Permit company workbooks use Times New Roman, Maglumat column widths,
/// Sanaw last-column SUM, and remaining-days cell colors.
/// </summary>
public class ExcelTemplateCompanyFontMaglumatWidthsTests
{
    [Fact]
    public void CompanyFontName_IsTimesNewRoman()
    {
        Assert.Equal("Times New Roman", ReportDashboardExcelTemplateWorkbook.CompanyFontName);
    }

    [Fact]
    public void MaglumatColumnWidths_SelectsByHeaderCount_WhenSecondHeaderIsFullName()
    {
        Assert.Null(ReportDashboardExcelTemplateWorkbook.MaglumatColumnWidths(null));
        Assert.Null(ReportDashboardExcelTemplateWorkbook.MaglumatColumnWidths(
            ["№", "Name", "x", "x", "x", "x", "x", "x", "x", "x", "x"]));

        var maglumat = ReportDashboardExcelTemplateWorkbook.MaglumatColumnWidths(
            ["№", "F.A.A", "a", "b", "c", "d", "e", "f", "g", "h", "i"]);
        var maglumat2 = ReportDashboardExcelTemplateWorkbook.MaglumatColumnWidths(
            ["№", "F.A.A", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j"]);
        var maglumat3 = ReportDashboardExcelTemplateWorkbook.MaglumatColumnWidths(
            ["№", "F.A.A", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k"]);

        Assert.Equal(11, maglumat!.Length);
        Assert.Equal(12, maglumat2!.Length);
        Assert.Equal(13, maglumat3!.Length);
        Assert.Equal(15.7, maglumat[0], 1);
        Assert.Equal(29.1, maglumat[1], 1);
        Assert.Equal(maglumat[0], maglumat2[0]);
        Assert.Equal(maglumat2[0], maglumat3[0]);
    }

    [Fact]
    public void BuildInvitationCompanyStarter_UsesCompanyFontAndGrayHeader()
    {
        var bytes = ReportDashboardExcelTemplateWorkbook.BuildInvitationCompanyStarter();
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        var header = sheet.Row(1);

        Assert.Equal(
            ReportDashboardExcelTemplateWorkbook.CompanyFontName,
            header.Cell(1).Style.Font.FontName);
        Assert.Equal(
            XLColor.FromHtml("#D9D9D9").Color.ToArgb(),
            header.Cell(1).Style.Fill.BackgroundColor.Color.ToArgb());
        Assert.Equal(
            ReportDashboardInvitationTableExcel.CompanyHeaders.Length,
            sheet.LastColumnUsed()!.ColumnNumber());
    }

    [Fact]
    public void BuildInvitationProcessCompanyStarter_UsesCompanyFontAndThirteenColumns()
    {
        var bytes = ReportDashboardExcelTemplateWorkbook.BuildInvitationProcessCompanyStarter();
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);

        Assert.Equal("Invitation Process", sheet.Name);
        Assert.Equal(
            ReportDashboardExcelTemplateWorkbook.CompanyFontName,
            sheet.Cell(1, 1).Style.Font.FontName);
        Assert.Equal(
            ReportDashboardInvitationTableExcel.ProcessCompanyHeaders.Length,
            sheet.LastColumnUsed()!.ColumnNumber());
        Assert.Equal("Şu anki Durumu", sheet.Cell(1, 12).GetString());
    }

    [Fact]
    public void WithCompanyFont_RewritesUsedCellsToTimesNewRoman()
    {
        var starter = ReportDashboardExcelTemplateWorkbook.BuildStarter(
            "Title",
            ["A", "B", "C"],
            sheetName: "Report",
            companyFont: false);
        var styled = ReportDashboardExcelTemplateWorkbook.WithCompanyFont(starter);

        using var workbook = new XLWorkbook(new MemoryStream(styled));
        var sheet = workbook.Worksheet(1);
        Assert.Equal(
            ReportDashboardExcelTemplateWorkbook.CompanyFontName,
            sheet.Cell(1, 1).Style.Font.FontName);
        Assert.Equal(
            ReportDashboardExcelTemplateWorkbook.CompanyFontName,
            sheet.Cell(2, 1).Style.Font.FontName);
        Assert.Equal(
            XLColor.FromHtml("#D9D9D9").Color.ToArgb(),
            sheet.Cell(2, 1).Style.Fill.BackgroundColor.Color.ToArgb());
    }

    [Fact]
    public void Export_Sanaw_WritesNumericCountAndSumFormula()
    {
        var headers = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit,
            ReportDashboardCatalog.WorkPermitSanawKey);
        var sanawCells = Enumerable.Repeat(string.Empty, headers.Length - 1).ToList();
        sanawCells[^1] = "2";

        var bytes = ReportDashboardExcelTemplateWorkbook.Export(
            template: null,
            title: ReportDashboardWorkPermitSanawExcel.TitleText,
            headers: headers,
            category: ReportDashboardCategory.WorkPermit,
            subReport: ReportDashboardCatalog.WorkPermitSanawKey,
            rows:
            [
                new ReportDashboardPreviewRow { SanawCells = sanawCells },
                new ReportDashboardPreviewRow
                {
                    SanawCells = sanawCells.Select((_, i) => i == sanawCells.Count - 1 ? "3" : string.Empty).ToList(),
                },
            ],
            includeChartColumn: false);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        var lastCol = headers.Length;

        Assert.Equal("Sanaw", sheet.Name);
        Assert.Equal(2d, sheet.Cell(3, lastCol).GetDouble());
        Assert.Equal(3d, sheet.Cell(4, lastCol).GetDouble());
        Assert.Equal("SUM(P3:P4)", sheet.Cell(5, lastCol).FormulaA1);
        Assert.True(sheet.Cell(5, lastCol).Style.Font.Bold);
        Assert.Equal(
            ReportDashboardExcelTemplateWorkbook.CompanyFontName,
            sheet.Cell(3, 1).Style.Font.FontName);
    }

    [Theory]
    [InlineData(-2, "#CC0000")]
    [InlineData(10, "#E06666")]
    [InlineData(20, "#F6B26B")]
    [InlineData(45, "#FFE599")]
    [InlineData(100, "#FFF2CC")]
    public void Export_ActiveInvitation_ColorsRemainingDaysNote(int days, string expectedBackground)
    {
        var note = "geçerli gün sayı : " + days;
        var bytes = ReportDashboardExcelTemplateWorkbook.Export(
            template: null,
            title: "Active Invitation",
            headers: ReportDashboardCatalog.EnglishTableHeaders(
                ReportDashboardCategory.Invitation, "ready-by-project"),
            category: ReportDashboardCategory.Invitation,
            subReport: "ready-by-project",
            rows:
            [
                new ReportDashboardPreviewRow
                {
                    Name = "Ada",
                    RemainingDaysNote = note,
                },
            ],
            includeChartColumn: false);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        var cell = sheet.Cell(2, ReportDashboardInvitationTableExcel.CompanyHeaders.Length);

        Assert.Equal(note, cell.GetString());
        Assert.Equal(
            XLColor.FromHtml(expectedBackground).Color.ToArgb(),
            cell.Style.Fill.BackgroundColor.Color.ToArgb());
        Assert.Equal(days < 30, cell.Style.Font.Bold);
    }

    [Fact]
    public void ApplyHeaderLabels_RewritesSanawVisaDurationCaption()
    {
        var headers = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.WorkPermit,
            ReportDashboardCatalog.WorkPermitSanawKey);
        var starter = ReportDashboardExcelTemplateWorkbook.BuildStarter(
            "Sanaw",
            headers,
            sheetName: "Sanaw",
            companyFont: true);

        var updated = ReportDashboardExcelTemplateWorkbook.ApplyHeaderLabels(starter, headers);
        using var workbook = new XLWorkbook(new MemoryStream(updated));
        var sheet = workbook.Worksheet(1);

        Assert.Equal("Möhleti we gezekligi", sheet.Cell(2, 11).GetString());
    }
}
