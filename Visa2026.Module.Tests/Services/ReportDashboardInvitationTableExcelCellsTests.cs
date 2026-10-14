using Visa2026.Module.Localization;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Active Invitation and Invitation Process Excel cells must stay aligned with the preview table.
/// </summary>
public sealed class ReportDashboardInvitationTableExcelCellsTests
{
    private static ReportDashboardPreviewRow ReadyRow() => new()
    {
        Name = "Ada Lovelace",
        Project = "Site A",
        ColumnA = "Engineer",
        ColumnB = "Stelara",
        ColumnC = "1/055",
        ColumnD = "11.01.2026",
        ColumnE = "INV-2026-0053",
        ColumnF = "22.08.2026",
        ColumnH = "Written",
        ColumnI = "10.03.2026\n№ 40/26",
        ColumnBadge = "Validity extended",
        ExpiresBadge = "Expiring",
        MinistryCells = ["01.02.2026\n№ 7", "05.02.2026\n№ 8"],
        Status = "chart"
    };

    private static ReportDashboardPreviewRow ProcessRow() => new()
    {
        Name = "Ada Lovelace",
        Project = "Site A",
        ColumnA = "Engineer",
        ColumnB = "Stelara",
        ColumnC = "1/055",
        ColumnD = "11.01.2026",
        ColumnE = "Being Prepared",
        ColumnI = "10.03.2026\n№ 40/26",
        ColumnBadge = "Validity extended",
        MinistryCells = ["01.02.2026\n№ 7", "05.02.2026\n№ 8"],
        Status = "chart"
    };

    [Fact]
    public void Cells_ActiveInvitation_JoinsAppFieldsMigrationBadgeAndExpiryPill()
    {
        var cells = ReportDashboardInvitationTableExcel.Cells(
            ReadyRow(), includeChartColumn: false, processTable: false);

        Assert.Equal("Ada Lovelace", cells[0]);
        Assert.Equal("Site A", cells[1]);
        Assert.Equal("Engineer", cells[2]);
        Assert.Equal("Stelara", cells[3]);
        Assert.Equal("1/055\n11.01.2026", cells[4]);
        Assert.Equal("01.02.2026\n№ 7", cells[5]);
        Assert.Equal("05.02.2026\n№ 8", cells[6]);
        Assert.Equal(
            "10.03.2026\n№ 40/26\n" + ReportDashboardLocalization.Status("Validity extended"),
            cells[7]);
        Assert.Equal("INV-2026-0053", cells[8]);
        Assert.Equal(
            "22.08.2026\n" + ReportDashboardLocalization.Status("Expiring"),
            cells[9]);
        Assert.Equal("Written", cells[10]);
        Assert.DoesNotContain("chart", cells);
    }

    [Fact]
    public void Cells_ProcessTable_UsesStatusInsteadOfInvitationExpiryColumns()
    {
        var cells = ReportDashboardInvitationTableExcel.Cells(
            ProcessRow(), includeChartColumn: false, processTable: true);

        Assert.Equal(9, cells.Count);
        Assert.Equal("Ada Lovelace", cells[0]);
        Assert.Equal("1/055\n11.01.2026", cells[4]);
        Assert.Equal("01.02.2026\n№ 7", cells[5]);
        Assert.Equal("05.02.2026\n№ 8", cells[6]);
        Assert.Equal(
            "10.03.2026\n№ 40/26\n" + ReportDashboardLocalization.Status("Validity extended"),
            cells[7]);
        Assert.Equal(ReportDashboardLocalization.Status("Being Prepared"), cells[8]);
        Assert.DoesNotContain("INV-2026-0053", cells);
        Assert.DoesNotContain("22.08.2026", cells);
        Assert.DoesNotContain("Written", cells);
    }

    [Fact]
    public void Cells_MigrationOnlyBadge_WhenMigrationBlank()
    {
        var row = new ReportDashboardPreviewRow
        {
            Name = "Ada",
            Project = "Site",
            ColumnI = "",
            ColumnBadge = "Validity extended",
            MinistryCells = []
        };
        var cells = ReportDashboardInvitationTableExcel.Cells(row, includeChartColumn: false, processTable: false);
        Assert.Equal(ReportDashboardLocalization.Status("Validity extended"), cells[5]);
        Assert.DoesNotContain("\n", cells[5]);
    }

    [Fact]
    public void Cells_IncludeChartColumn_AppendsStatusAtEnd()
    {
        var cells = ReportDashboardInvitationTableExcel.Cells(
            ReadyRow(), includeChartColumn: true, processTable: false);
        Assert.Equal("chart", cells[^1]);
    }
}
