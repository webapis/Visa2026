#nullable enable

using System.Collections.Generic;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Sanaw exports prepend the on-screen row number ahead of the sheet's person columns.
/// </summary>
public sealed class ReportDashboardWorkPermitSanawExcelTests
{
    [Fact]
    public void Cells_prefixes_row_number_to_sanaw_sheet_columns()
    {
        var row = new ReportDashboardPreviewRow
        {
            SanawCells = new List<string>
            {
                "Durdyýew",
                "Ali",
                "01.01.1990\nAşgabat",
                "Erkek",
            },
        };

        var cells = ReportDashboardWorkPermitSanawExcel.Cells(row, 4);

        Assert.Equal(["4", "Durdyýew", "Ali", "01.01.1990\nAşgabat", "Erkek"], cells);
    }

    [Fact]
    public void Cells_still_returns_row_number_when_sanaw_cells_are_null()
    {
        var row = new ReportDashboardPreviewRow { SanawCells = null! };

        var cells = ReportDashboardWorkPermitSanawExcel.Cells(row, 1);

        Assert.Equal(["1"], cells);
    }

    [Fact]
    public void ValuesFor_sanaw_uses_sanaw_cells_not_maglumat_layout()
    {
        var row = new ReportDashboardPreviewRow
        {
            Name = "should-not-appear",
            SanawCells = ["Last", "First"],
        };

        var values = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            ReportDashboardCategory.WorkPermit,
            ReportDashboardCatalog.WorkPermitSanawKey,
            row,
            rowNumber: 2,
            headerCount: 16,
            includeChartColumn: false);

        Assert.Equal(["2", "Last", "First"], values);
        Assert.Equal("Daşary ýurt raýatlarynyň sanawy", ReportDashboardWorkPermitSanawExcel.TitleText);
    }
}
