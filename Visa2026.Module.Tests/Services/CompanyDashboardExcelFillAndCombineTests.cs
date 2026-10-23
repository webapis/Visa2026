using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Company Excel builders for kota / Personel sayi / Oturum / çalışma / uzatımlar,
/// plus multi-report Combine — regressions here corrupt officer workbooks.
/// </summary>
public sealed class CompanyDashboardExcelFillAndCombineTests
{
    [Fact]
    public void KotaFill_WritesDateCountsAndOranCells()
    {
        var reportDate = new DateTime(2026, 10, 23);
        var bytes = ReportDashboardWorkPermitKotaExcel.Fill(
            template: Array.Empty<byte>(),
            reportDate: reportDate,
            localCount: 100,
            foreignCount: 12,
            inProcessCount: 3,
            foreignRatio: 10,
            localRatio: 90);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        Assert.Equal(ReportDashboardWorkPermitKotaExcel.SheetName, sheet.Name);
        Assert.Equal(reportDate, sheet.Cell(2, 2).GetDateTime().Date);
        Assert.Equal(100, sheet.Cell(2, 3).GetValue<int>());
        Assert.Equal(12, sheet.Cell(2, 4).GetValue<int>());
        Assert.Equal(3, sheet.Cell(2, 5).GetValue<int>());
        Assert.Equal(10, sheet.Cell(7, 2).GetValue<int>());
        Assert.Equal(90, sheet.Cell(7, 3).GetValue<int>());
        // ClosedXML FormulaA1 omits the leading '=' after round-trip.
        Assert.Equal("ROUND((D2+E2)/C3*100,0)", sheet.Cell(2, 6).FormulaA1);
    }

    [Fact]
    public void PersonelSayiFill_BlanksInactiveWhenZero_AndKeepsTotalFormula()
    {
        var bytes = ReportDashboardWorkPermitPersonelSayiExcel.Fill(
            template: null,
            rows:
            [
                ("Project A", 5, 0),
                ("Project B", 2, 4),
            ]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(ReportDashboardWorkPermitPersonelSayiExcel.SheetName);
        Assert.Equal("Project A", sheet.Cell(2, 1).GetString());
        Assert.Equal(5, sheet.Cell(2, 2).GetValue<int>());
        Assert.Equal(string.Empty, sheet.Cell(2, 3).GetString());
        Assert.Equal("B2+IF(C2=\"\",0,C2)", sheet.Cell(2, 4).FormulaA1);

        Assert.Equal("Project B", sheet.Cell(3, 1).GetString());
        Assert.Equal(2, sheet.Cell(3, 2).GetValue<int>());
        Assert.Equal(4, sheet.Cell(3, 3).GetValue<int>());
        Assert.Equal("B3+IF(C3=\"\",0,C3)", sheet.Cell(3, 4).FormulaA1);
    }

    [Fact]
    public void OturumBuildNotes_EmitsOnlyPositiveFragments_AndFormatCountBlanksZero()
    {
        var row = new ReportDashboardRegistrationOturumSayisiExcel.Row(
            Department: "Proj",
            BalkanKamp: 2,
            BalkanAdres: 0,
            Asgabat: 1,
            AsgabatKamp: 1,
            AsgabatAdres: 0,
            Ahal: 0,
            AhalKamp: 0,
            AhalAdres: 0,
            Lebap: 0,
            LebapKamp: 0,
            LebapAdres: 0,
            MaryAdres: 3,
            MaryKamp: 0);

        var notes = ReportDashboardRegistrationOturumSayisiExcel.BuildNotes(row);
        Assert.Equal("kamp-2, Ag-kamp-1, MR-adres-3", notes);
        Assert.Equal(string.Empty, ReportDashboardRegistrationOturumSayisiExcel.FormatCount(0));
        Assert.Equal("7", ReportDashboardRegistrationOturumSayisiExcel.FormatCount(7));
    }

    [Fact]
    public void CalismaCells_PrefixesRowNumber_AndPadsToHeaderWidth()
    {
        var row = new ReportDashboardPreviewRow
        {
            CalismaCells = ["Dept", "Firm", "Name"],
        };

        var cells = ReportDashboardWorkPermitCalismaExcel.Cells(row, 4);
        Assert.Equal("4", cells[0]);
        Assert.Equal("Dept", cells[1]);
        Assert.Equal(ReportDashboardWorkPermitCalismaExcel.Headers.Length, cells.Count);
        Assert.Equal(string.Empty, cells[^1]);
    }

    [Fact]
    public void UzatilanlarCells_StripsNumberMarkFromMigrationServiceCell()
    {
        var row = new ReportDashboardPreviewRow
        {
            UzatilanlarCells =
            [
                "Ada", "Görev", "Proje", "Firma", "01.01.2027", "yazı", "02.02.2026", "90 gün",
                "03.03.2026", "04.04.2026", "№ CO-9\n05.05.2026", "işlemde", "",
            ],
        };

        var cells = ReportDashboardVisaUzatilanlarExcel.Cells(row, 1);
        Assert.Equal("1", cells[0]);
        Assert.Equal("CO-9\n05.05.2026", cells[11]);
        Assert.Equal(ReportDashboardVisaUzatilanlarExcel.Headers.Length, cells.Count);
    }

    [Fact]
    public void Combine_CopiesFirstSheetFromEachPart_WithUniqueNames()
    {
        var first = ReportDashboardWorkPermitKotaExcel.BuildStarter();
        var second = ReportDashboardWorkPermitPersonelSayiExcel.BuildStarter();

        var combined = ReportDashboardExcelTemplateWorkbook.Combine(
        [
            ("kota oranımız", first),
            ("kota oranımız", second),
        ]);

        using var workbook = new XLWorkbook(new MemoryStream(combined));
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.Equal("kota oranımız", workbook.Worksheet(1).Name);
        Assert.Equal("kota oranımız 2", workbook.Worksheet(2).Name);
        Assert.Equal("TARİHİ", workbook.Worksheet(1).Cell(1, 2).GetString());
        Assert.Equal("Projeler", workbook.Worksheet(2).Cell(1, 1).GetString());
    }

    [Fact]
    public void FormatVisaDaysRemaining_UsesTodayRelativeWholeDays()
    {
        Assert.Equal(string.Empty, VwRdWorkPermitCalismaIzniListesi.FormatVisaDaysRemaining(null));
        Assert.Equal(
            string.Empty,
            VwRdWorkPermitCalismaIzniListesi.FormatVisaDaysRemaining(new DateTime(1900, 1, 1)));

        var tomorrow = DateTime.Today.AddDays(1);
        Assert.Equal("1", VwRdWorkPermitCalismaIzniListesi.FormatVisaDaysRemaining(tomorrow));

        var yesterday = DateTime.Today.AddDays(-2);
        Assert.Equal("-2", VwRdWorkPermitCalismaIzniListesi.FormatVisaDaysRemaining(yesterday));
    }
}
