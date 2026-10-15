using Visa2026.Module.Localization;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Used / Cancelled Invitation Excel cells must stay aligned with the preview table layouts.
/// </summary>
public sealed class ReportDashboardInvitationUsedCancelledExcelCellsTests
{
    private static ReportDashboardPreviewRow LifecycleRow() => new()
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
        ExpiresBadge = "Expired",
        MinistryCells = ["01.02.2026\n№ 7", "05.02.2026\n№ 8"],
        VisaNumber = "V-1001",
        VisaIssuedDate = "01.04.2026",
        VisaPlace = "Ashgabat",
        VisaStartDate = "02.04.2026",
        VisaEndDate = "01.04.2027",
        Status = "chart",
    };

    [Fact]
    public void UsedCells_AppendVisaFieldsAfterActiveInvitationCore()
    {
        var cells = ReportDashboardInvitationTableExcel.UsedCells(
            LifecycleRow(), includeChartColumn: false);

        Assert.Equal("Ada Lovelace", cells[0]);
        Assert.Equal("INV-2026-0053", cells[8]);
        Assert.Equal(
            "22.08.2026\n" + ReportDashboardLocalization.Status("Expired"),
            cells[9]);
        Assert.Equal("Written", cells[10]);
        Assert.Equal("V-1001", cells[^5]);
        Assert.Equal("01.04.2026", cells[^4]);
        Assert.Equal("Ashgabat", cells[^3]);
        Assert.Equal("02.04.2026", cells[^2]);
        Assert.Equal("01.04.2027", cells[^1]);
        Assert.DoesNotContain("chart", cells);
    }

    [Fact]
    public void UsedCells_IncludeChartColumn_AppendsStatusAfterVisaFields()
    {
        var cells = ReportDashboardInvitationTableExcel.UsedCells(
            LifecycleRow(), includeChartColumn: true);

        Assert.Equal("chart", cells[^1]);
        Assert.Equal("01.04.2027", cells[^2]);
        Assert.Equal("V-1001", cells[^6]);
    }

    [Fact]
    public void CancelledCells_UsesCompactMigrationLayout()
    {
        var cells = ReportDashboardInvitationTableExcel.CancelledCells(LifecycleRow());

        Assert.Equal(
            [
                "Ada Lovelace",
                "INV-2026-0053",
                "Engineer",
                "22.08.2026\n" + ReportDashboardLocalization.Status("Expired"),
                "Written",
                "1/055",
                "11.01.2026",
                "10.03.2026\n№ 40/26",
            ],
            cells);
        Assert.DoesNotContain("Site A", cells);
        Assert.DoesNotContain("V-1001", cells);
        Assert.DoesNotContain("01.02.2026\n№ 7", cells);
    }

    [Fact]
    public void CancelledCells_NullMigrationBecomesEmpty()
    {
        var row = new ReportDashboardPreviewRow
        {
            Name = "Ada",
            ColumnE = "INV-1",
            ColumnA = "01.01.2026",
            ColumnF = "01.02.2026",
            ExpiresBadge = "Expired",
            ColumnH = "Written",
            ColumnC = "9/001",
            ColumnD = "02.01.2026",
            ColumnI = null,
        };

        var cells = ReportDashboardInvitationTableExcel.CancelledCells(row);
        Assert.Equal(8, cells.Count);
        Assert.Equal(string.Empty, cells[^1]);
    }
}
