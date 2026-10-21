using System.Collections.Generic;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation Process company sheet shares Active column order through ministry/CO,
/// maps migration-service status to Turkish, and keeps Açıklama empty.
/// </summary>
public class InvitationProcessCompanyCellsTurkishStatusTests
{
    [Fact]
    public void ProcessCompanyHeaders_MatchActiveThroughMinistryThenMigrationFields()
    {
        var active = ReportDashboardInvitationTableExcel.CompanyHeaders;
        var process = ReportDashboardInvitationTableExcel.ProcessCompanyHeaders;

        Assert.Equal(12, active.Length);
        Assert.Equal(13, process.Length);
        for (var i = 0; i < 9; i++)
            Assert.Equal(active[i], process[i]);

        Assert.Equal("Yabancı Şubeye veriliş tarihi", process[9]);
        Assert.Equal("Yabancı Şube CO No ve tarihi", process[10]);
        Assert.Equal("Şu anki Durumu", process[11]);
        Assert.Equal("Açıklama", process[12]);
        Assert.DoesNotContain(process, h => h.Contains("geçerlilik", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(process, h => h.Equals("NOT", StringComparison.Ordinal));
    }

    [Fact]
    public void ProcessCells_AlignWithActive_ThroughLastApproval()
    {
        var row = SampleRow();

        var active = ReportDashboardInvitationTableExcel.ActiveCells(row, rowNumber: 4, includeChartColumn: false);
        var process = ReportDashboardInvitationTableExcel.ProcessCells(row, rowNumber: 4);

        Assert.Equal(12, active.Count);
        Assert.Equal(13, process.Count);
        for (var i = 0; i < 9; i++)
            Assert.Equal(active[i], process[i]);

        Assert.Equal("04.03.2026", process[9]);
        Assert.Equal("05.03.2026\nCO-9", process[10]);
        Assert.Equal(string.Empty, process[12]);
    }

    [Theory]
    [InlineData("MIGRASIÝA GULLUGYNDA", "Yabancıların şubesinde")]
    [InlineData("MIGRASIYA GULLUGYNDA", "Yabancıların şubesinde")]
    [InlineData(" migrasiýa gullugynda ", "Yabancıların şubesinde")]
    [InlineData("Office review", "Office review")]
    [InlineData("", "")]
    public void ProcessCells_MapsMigrationServiceStatusToTurkish(string columnE, string expectedStatus)
    {
        var row = SampleRow(columnE: columnE);
        var process = ReportDashboardInvitationTableExcel.ProcessCells(row, rowNumber: 1);
        Assert.Equal(expectedStatus, process[11]);
    }

    [Fact]
    public void ProcessCells_NormalizesOfficerDatesAndStripsNumberMarkFromCo()
    {
        var row = SampleRow(
            columnD: "Mar 01, 2026",
            columnG: "Mar 04, 2026",
            columnI: "Mar 05, 2026\n№ CO-9",
            lastApproval: "Feb 20, 2026\n№ MIN-1");

        var process = ReportDashboardInvitationTableExcel.ProcessCells(row, rowNumber: 2);

        Assert.Equal("01.03.2026", process[6]);
        Assert.Equal("20.02.2026\n№ MIN-1", process[8]);
        Assert.Equal("04.03.2026", process[9]);
        Assert.Equal("05.03.2026\nCO-9", process[10]);
    }

    [Fact]
    public void ActiveCells_PutRemainingDaysNoteInNotColumn()
    {
        var row = SampleRow(
            remainingDaysNote: "geçerli gün sayı : 12",
            columnF: "Apr 01, 2026");

        var active = ReportDashboardInvitationTableExcel.ActiveCells(row, rowNumber: 1, includeChartColumn: false);

        Assert.Equal("01.04.2026", active[10]);
        Assert.Equal("geçerli gün sayı : 12", active[11]);
    }

    [Fact]
    public void ValuesFor_RoutesProcessAndActiveCompanySheets()
    {
        var row = SampleRow(columnE: "MIGRASIÝA GULLUGYNDA");

        var process = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            ReportDashboardCategory.Invitation,
            "in-process",
            row,
            rowNumber: 1,
            headerCount: 13,
            includeChartColumn: false);
        var active = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            ReportDashboardCategory.Invitation,
            "ready-by-project",
            row,
            rowNumber: 1,
            headerCount: 12,
            includeChartColumn: false);

        Assert.Equal("Yabancıların şubesinde", process[11]);
        Assert.Equal(row.RemainingDaysNote, active[11]);
        Assert.Equal(row.Department, process[3]);
        Assert.Equal(row.Department, active[3]);
    }

    private static ReportDashboardPreviewRow SampleRow(
        string? columnE = "Office",
        string? columnD = "01.03.2026",
        string? columnF = "01.06.2026",
        string? columnG = "04.03.2026",
        string? columnI = "05.03.2026\n№ CO-9",
        string? lastApproval = "20.02.2026\nMIN-1",
        string? remainingDaysNote = "geçerli gün sayı : 40") => new()
    {
        Name = "Ada Yılmaz",
        ColumnA = "Welder",
        Department = "Construction",
        ColumnB = "Çalık",
        ColumnC = "INV-1",
        ColumnD = columnD ?? string.Empty,
        ColumnH = "90 days",
        LastApproval = lastApproval ?? string.Empty,
        ColumnG = columnG ?? string.Empty,
        ColumnI = columnI ?? string.Empty,
        ColumnE = columnE ?? string.Empty,
        ColumnF = columnF ?? string.Empty,
        RemainingDaysNote = remainingDaysNote ?? string.Empty,
    };
}
