#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Maglumat specialty column and Sanaw family-member cells must stay aligned with catalog widths.
/// </summary>
public sealed class ReportDashboardSpecialtyFamilyExcelTests
{
    [Fact]
    public void Maglumat_Cells_places_specialty_before_position()
    {
        var row = new ReportDashboardPreviewRow
        {
            Name = "Ali",
            ColumnA = "1990",
            ColumnB = "TR",
            ColumnC = "P-1",
            ColumnD = "01.01.2030",
            ColumnE = "Ýokary",
            Specialty = "Inžener",
            ColumnF = "Ussat",
            ColumnG = "Aşgabat",
            WorkPermits =
            [
                new WorkPermitBundleSlot { Number = "WP-1", Start = "s", End = "e" },
            ],
            VisaNumber = "V-1",
        };

        var cells = ReportDashboardWorkPermitMaglumatExcel.Cells(row, 1);

        Assert.Equal(11, cells.Count);
        Assert.Equal("Inžener", cells[5]);
        Assert.Equal("Ussat", cells[6]);
        Assert.Equal("WP-1\ns\ne", cells[8]);
        Assert.Equal("V-1", cells[9]);
    }

    [Fact]
    public void Maglumat2_and_3_insert_location_columns_after_specialty_shift()
    {
        var row = new ReportDashboardPreviewRow
        {
            Specialty = "Spec",
            VisaNumber = "V-9",
            WorkPermits =
            [
                new WorkPermitBundleSlot
                {
                    Number = "WP",
                    Locations = "Base",
                    AdditionalLocations = "Extra",
                },
            ],
        };

        var withLocations = ReportDashboardWorkPermitMaglumatExcel.CellsWithLocations(row, 1);
        var withAdditional = ReportDashboardWorkPermitMaglumatExcel.CellsWithAdditionalLocations(row, 1);

        Assert.Equal(12, withLocations.Count);
        Assert.Equal("Spec", withLocations[5]);
        Assert.Equal("Base", withLocations[9]);
        Assert.Equal("V-9", withLocations[10]);

        Assert.Equal(13, withAdditional.Count);
        Assert.Equal("Base", withAdditional[9]);
        Assert.Equal("Extra", withAdditional[10]);
        Assert.Equal("V-9", withAdditional[11]);
    }

    [Theory]
    [InlineData("active-by-project", 11)]
    [InlineData("a-maglumat-2", 12)]
    [InlineData("a-maglumat-3", 13)]
    public void ValuesFor_maglumat_widths_include_specialty_column(string subReport, int expectedWidth)
    {
        var row = new ReportDashboardPreviewRow
        {
            Specialty = "S",
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
        Assert.Equal("S", values[5]);
    }

    [Fact]
    public void Sanaw_Cells_prefix_row_number_and_keep_family_member_slot()
    {
        var row = new ReportDashboardPreviewRow
        {
            SanawCells =
            [
                "Durdyýew",
                "Ali",
                "1990 Aşgabat",
                "Erkek",
                "TR",
                "P-1",
                "Ýokary",
                "Inžener",
                "Ussat",
                "salary",
                "addr",
                "foreign",
                "Plant",
                "1 Aýaly-Gül\n2 Ogly-Merdan",
                "2",
            ],
        };

        var cells = ReportDashboardWorkPermitSanawExcel.Cells(row, 4);

        Assert.Equal(16, cells.Count);
        Assert.Equal("4", cells[0]);
        Assert.Equal("Inžener", cells[8]);
        Assert.Equal("1 Aýaly-Gül\n2 Ogly-Merdan", cells[14]);
        Assert.Equal("2", cells[15]);
    }
}
