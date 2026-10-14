using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ReportDashboardExcelTemplateWorkbookTests
{
    [Fact]
    public void BuildStarter_DefinesHeaderAndSampleNamedRanges()
    {
        var headers = new[] { "Name", "Project", "Status" };
        var bytes = ReportDashboardExcelTemplateWorkbook.BuildStarter("Active Invitation", headers);

        Assert.True(ReportDashboardExcelTemplateWorkbook.MatchesColumns(bytes, headers.Length));
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Contains(
            workbook.DefinedNames,
            n => string.Equals(n.Name, ReportDashboardExcelTemplateWorkbook.HeaderName, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            workbook.DefinedNames,
            n => string.Equals(n.Name, ReportDashboardExcelTemplateWorkbook.SampleName, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Active Invitation", workbook.Worksheet(1).Cell(1, 1).GetString());
        Assert.Equal("Name", workbook.Worksheet(1).Cell(2, 1).GetString());
    }

    [Fact]
    public void MatchesColumns_RejectsWrongWidthCorruptAndEmpty()
    {
        var starter = ReportDashboardExcelTemplateWorkbook.BuildStarter("Title", ["A", "B", "C"]);
        Assert.False(ReportDashboardExcelTemplateWorkbook.MatchesColumns(starter, expectedColumns: 2));
        Assert.False(ReportDashboardExcelTemplateWorkbook.MatchesColumns(starter, expectedColumns: 4));
        Assert.False(ReportDashboardExcelTemplateWorkbook.MatchesColumns([1, 2, 3], expectedColumns: 3));
        Assert.False(ReportDashboardExcelTemplateWorkbook.MatchesColumns([], expectedColumns: 1));
    }

    [Fact]
    public void ValuesFor_RoutesInvitationReadyAndProcessLayouts()
    {
        var row = new ReportDashboardPreviewRow
        {
            Name = "Ada",
            Project = "Site A",
            ColumnA = "Engineer",
            ColumnB = "Co",
            ColumnC = "1/001",
            ColumnD = "01.01.2026",
            ColumnE = "Being Prepared",
            ColumnF = "31.12.2026",
            ColumnH = "INV-1",
            ColumnI = "15.03.2026",
            ColumnBadge = "Validity extended",
            ExpiresBadge = "Valid",
            MinistryCells = ["01.02.2026\n№ 1"],
            Status = "chart-bucket"
        };

        var ready = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            "ready-by-project", row, headerCount: 12, includeChartColumn: false);
        Assert.Equal("Ada", ready[0]);
        Assert.Contains(
            ready,
            cell => cell.Contains("15.03.2026", StringComparison.Ordinal)
                && cell.Contains(ReportDashboardLocalization.Status("Validity extended"), StringComparison.Ordinal));
        Assert.Contains("INV-1", ready);

        var process = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            "in-process", row, headerCount: 10, includeChartColumn: false);
        Assert.Equal("Ada", process[0]);
        Assert.DoesNotContain("INV-1", process);
        Assert.Equal(ReportDashboardLocalization.Status("Being Prepared"), process[^1]);

        var generic = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            "by-project", row, headerCount: 5, includeChartColumn: false);
        Assert.Equal(5, generic.Count);
        Assert.Equal("Ada", generic[0]);
        Assert.Equal("Site A", generic[1]);
    }

    [Fact]
    public void Export_FallsBackToStarterWhenTemplateColumnsMismatch()
    {
        var headers = new[] { "Name", "Project", "A", "B", "Status" };
        var wrongWidth = ReportDashboardExcelTemplateWorkbook.BuildStarter("Wrong", ["Only", "Two"]);
        var rows = new List<ReportDashboardPreviewRow>
        {
            new()
            {
                Name = "Row1",
                Project = "P1",
                ColumnA = "a",
                ColumnB = "b",
                Status = "Open"
            }
        };

        var exported = ReportDashboardExcelTemplateWorkbook.Export(
            wrongWidth, "Fallback Title", headers, "by-project", rows, includeChartColumn: false);

        Assert.True(ReportDashboardExcelTemplateWorkbook.MatchesColumns(exported, headers.Length));
        using var workbook = new XLWorkbook(new MemoryStream(exported));
        Assert.Equal("Fallback Title", workbook.Worksheet(1).Cell(1, 1).GetString());
        Assert.Equal("Row1", workbook.Worksheet(1).Cell(3, 1).GetString());
    }

    [Fact]
    public void Export_WithMatchingTemplate_WritesDataOntoSampleRow()
    {
        var headers = new[] { "Name", "Project", "A", "B", "Status" };
        var template = ReportDashboardExcelTemplateWorkbook.BuildStarter("Kept Title", headers);
        var rows = new List<ReportDashboardPreviewRow>
        {
            new() { Name = "One", Project = "P", ColumnA = "x", ColumnB = "y", Status = "Open" },
            new() { Name = "Two", Project = "Q", ColumnA = "u", ColumnB = "v", Status = "Open" }
        };

        var exported = ReportDashboardExcelTemplateWorkbook.Export(
            template, "Ignored When Matched", headers, "by-project", rows, includeChartColumn: false);

        using var workbook = new XLWorkbook(new MemoryStream(exported));
        var sheet = workbook.Worksheet(1);
        Assert.Equal("Kept Title", sheet.Cell(1, 1).GetString());
        Assert.Equal("One", sheet.Cell(3, 1).GetString());
        Assert.Equal("Two", sheet.Cell(4, 1).GetString());
    }
}
