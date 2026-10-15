using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Company Excel template ValuesFor must route Expired / Cancelled / Used to the matching cell builders.
/// </summary>
public sealed class ReportDashboardExcelTemplateWorkbookLifecycleValuesTests
{
    private static ReportDashboardPreviewRow Row() => new()
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
        ExpiresBadge = "Expired",
        MinistryCells = ["01.02.2026\n№ 7"],
        VisaNumber = "V-1001",
        VisaIssuedDate = "01.04.2026",
        VisaPlace = "Ashgabat",
        VisaStartDate = "02.04.2026",
        VisaEndDate = "01.04.2027",
        Status = "chart",
    };

    [Fact]
    public void ValuesFor_Expired_UsesActiveInvitationCells()
    {
        var expected = ReportDashboardInvitationTableExcel.Cells(
            Row(), includeChartColumn: false, processTable: false);
        var actual = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            "expired-invitation", Row(), headerCount: expected.Count, includeChartColumn: false);

        Assert.Equal(expected, actual);
        Assert.DoesNotContain("V-1001", actual);
    }

    [Fact]
    public void ValuesFor_Cancelled_UsesCancelledCells()
    {
        var expected = ReportDashboardInvitationTableExcel.CancelledCells(Row());
        var actual = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            "cancelled-invitation", Row(), headerCount: expected.Count, includeChartColumn: true);

        Assert.Equal(expected, actual);
        Assert.DoesNotContain("chart", actual);
        Assert.DoesNotContain("V-1001", actual);
    }

    [Theory]
    [InlineData("used")]
    [InlineData("used-by-period-category-type")]
    public void ValuesFor_Used_UsesUsedCells(string subReport)
    {
        var expected = ReportDashboardInvitationTableExcel.UsedCells(Row(), includeChartColumn: true);
        var actual = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            subReport, Row(), headerCount: expected.Count, includeChartColumn: true);

        Assert.Equal(expected, actual);
        Assert.Equal("V-1001", actual[^6]);
        Assert.Equal("chart", actual[^1]);
    }
}
