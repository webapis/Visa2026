#nullable enable

using System.Collections.Generic;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation Excel rows must keep Department after Name/Project (or after Name on Cancelled).
/// </summary>
public sealed class ReportDashboardInvitationDepartmentExcelTests
{
    [Fact]
    public void Cells_inserts_department_after_project_on_active_and_process_tables()
    {
        var row = SampleRow();

        var active = ReportDashboardInvitationTableExcel.Cells(row, includeChartColumn: false);
        var process = ReportDashboardInvitationTableExcel.Cells(
            row, includeChartColumn: false, processTable: true);

        Assert.Equal("Ops", active[2]);
        Assert.Equal("Engineer", active[3]);
        Assert.Equal("Ops", process[2]);
        Assert.Equal(ReportDashboardLocalization.Status(row.ColumnE), process[^1]);
    }

    [Fact]
    public void CancelledCells_puts_department_immediately_after_name()
    {
        var row = SampleRow();

        var cells = ReportDashboardInvitationTableExcel.CancelledCells(row);

        Assert.Equal("Ali Durdyýew", cells[0]);
        Assert.Equal("Ops", cells[1]);
        Assert.Equal("INV-9", cells[2]);
    }

    [Fact]
    public void UsedCells_keeps_department_from_active_layout_before_visa_columns()
    {
        var row = SampleRow();
        row = new ReportDashboardPreviewRow
        {
            Name = row.Name,
            Project = row.Project,
            Department = row.Department,
            ColumnA = row.ColumnA,
            ColumnB = row.ColumnB,
            ColumnC = row.ColumnC,
            ColumnD = row.ColumnD,
            ColumnE = row.ColumnE,
            ColumnF = row.ColumnF,
            ColumnH = row.ColumnH,
            ColumnI = row.ColumnI,
            ExpiresBadge = row.ExpiresBadge,
            MinistryCells = row.MinistryCells,
            VisaNumber = "V-1",
            VisaIssuedDate = "01.02.2026",
            VisaPlace = "Ashgabat",
            VisaStartDate = "01.03.2026",
            VisaEndDate = "01.03.2027",
        };

        var cells = ReportDashboardInvitationTableExcel.UsedCells(row, includeChartColumn: false);

        Assert.Equal("Ops", cells[2]);
        Assert.Equal("V-1", cells[^5]);
        Assert.Equal("01.03.2027", cells[^1]);
    }

    private static ReportDashboardPreviewRow SampleRow() =>
        new()
        {
            Name = "Ali Durdyýew",
            Project = "Plant",
            Department = "Ops",
            ColumnA = "Engineer",
            ColumnB = "Sub A",
            ColumnC = "APP-1",
            ColumnD = "01.01.2026",
            ColumnE = "INV-9",
            ColumnF = "01.04.2026",
            ColumnH = "6 months",
            ColumnI = "MIG-1",
            ExpiresBadge = "Valid",
            MinistryCells = new List<string> { "m1", "m2", "m3" },
        };
}
