#nullable enable

using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// A. Maglumat / Maglumat 2 / Maglumat 3 Excel cells must keep column order officers export from the table.
/// </summary>
public sealed class ReportDashboardWorkPermitMaglumatExcelTests
{
    [Fact]
    public void Title_embeds_creation_day_as_dd_MM_yyyy()
    {
        var title = ReportDashboardWorkPermitMaglumatExcel.Title(new DateTime(2026, 10, 17));

        Assert.StartsWith("A. 17.10.2026 sene boýunça", title, StringComparison.Ordinal);
        Assert.Contains("MAGLUMAT", title, StringComparison.Ordinal);
    }

    [Fact]
    public void Cells_joins_person_fields_and_stacks_multi_permit_blocks()
    {
        var row = new ReportDashboardPreviewRow
        {
            Name = "Ali Durdyýew",
            ColumnA = "1990",
            ColumnB = "TR",
            ColumnC = "P-1",
            ColumnD = "01.01.2030",
            ColumnE = "Ýokary",
            ColumnF = "Inžener",
            ColumnG = "Aşgabat",
            ColumnH = "legacy-wp",
            WorkPermitStart = "legacy-start",
            WorkPermitEnd = "legacy-end",
            VisaNumber = "V-9",
            VisaStartDate = "01.02.2026",
            VisaEndDate = "01.02.2027",
            WorkPermits =
            [
                new WorkPermitBundleSlot { Number = "WP-1", Start = "01.01.2026", End = "01.01.2027" },
                new WorkPermitBundleSlot { Number = "WP-2", Start = "02.02.2026", End = "02.02.2027" },
            ],
        };

        var cells = ReportDashboardWorkPermitMaglumatExcel.Cells(row, 3);

        Assert.Equal(10, cells.Count);
        Assert.Equal("3", cells[0]);
        Assert.Equal("Ali Durdyýew", cells[1]);
        Assert.Equal("1990\nTR", cells[2]);
        Assert.Equal("P-1\n01.01.2030", cells[3]);
        Assert.Equal("WP-1\n01.01.2026\n01.01.2027\n\nWP-2\n02.02.2026\n02.02.2027", cells[7]);
        Assert.Equal("V-9\n01.02.2026\n01.02.2027", cells[8]);
        Assert.Equal(string.Empty, cells[9]);
    }

    [Fact]
    public void Cells_falls_back_to_scalar_permit_when_bundle_is_empty()
    {
        var row = new ReportDashboardPreviewRow
        {
            ColumnH = "WP-ONLY",
            WorkPermitStart = "01.01.2026",
            WorkPermitEnd = "31.12.2026",
        };

        var cells = ReportDashboardWorkPermitMaglumatExcel.Cells(row, 1);

        Assert.Equal("WP-ONLY\n01.01.2026\n31.12.2026", cells[7]);
    }

    [Fact]
    public void CellsWithLocations_inserts_permit_locations_before_visa()
    {
        var row = new ReportDashboardPreviewRow
        {
            VisaNumber = "V-1",
            WorkPermits =
            [
                new WorkPermitBundleSlot
                {
                    Number = "WP-1",
                    Start = "s",
                    End = "e",
                    Locations = "Aşgabat",
                },
                new WorkPermitBundleSlot
                {
                    Number = "WP-2",
                    Locations = "Mary",
                },
            ],
        };

        var cells = ReportDashboardWorkPermitMaglumatExcel.CellsWithLocations(row, 1);

        Assert.Equal(11, cells.Count);
        Assert.Equal("Aşgabat\n\nMary", cells[8]);
        Assert.Equal("V-1", cells[9]);
    }

    [Fact]
    public void CellsWithAdditionalLocations_inserts_goşmaça_column_after_locations()
    {
        var row = new ReportDashboardPreviewRow
        {
            VisaNumber = "V-1",
            WorkPermits =
            [
                new WorkPermitBundleSlot
                {
                    Locations = "Base",
                    AdditionalLocations = "CHG-1\n10.10.2026\nExtra",
                },
            ],
        };

        var cells = ReportDashboardWorkPermitMaglumatExcel.CellsWithAdditionalLocations(row, 2);

        Assert.Equal(12, cells.Count);
        Assert.Equal("2", cells[0]);
        Assert.Equal("Base", cells[8]);
        Assert.Equal("CHG-1\n10.10.2026\nExtra", cells[9]);
        Assert.Equal("V-1", cells[10]);
        Assert.Equal(string.Empty, cells[11]);
    }

    [Theory]
    [InlineData("active-by-project", 10)]
    [InlineData("a-maglumat-2", 11)]
    [InlineData("a-maglumat-3", 12)]
    public void ValuesFor_routes_maglumat_tabs_to_matching_cell_width(string subReport, int expectedWidth)
    {
        var row = new ReportDashboardPreviewRow
        {
            Name = "X",
            WorkPermits =
            [
                new WorkPermitBundleSlot
                {
                    Number = "WP",
                    Locations = "Loc",
                    AdditionalLocations = "Add",
                },
            ],
        };

        var values = ReportDashboardExcelTemplateWorkbook.ValuesFor(
            ReportDashboardCategory.WorkPermit,
            subReport,
            row,
            rowNumber: 1,
            headerCount: expectedWidth,
            includeChartColumn: false);

        Assert.Equal(expectedWidth, values.Count);
    }
}
