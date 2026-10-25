using System.IO;
using ClosedXML.Excel;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Bundle Excel combine copies page setup from the first stamped sheet; freeze-pane
/// fallback drives Print Titles when DashboardHeader is missing.
/// </summary>
public class ReportDashboardExcelPrintLayoutCopyPageSetupTests
{
    [Fact]
    public void IsLandscape_CoversKotaOturumAndProcessLong()
    {
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitKotaOranimizKey));
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitPersonelSayiKey));
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.Registration, ReportDashboardCatalog.RegistrationOturumSayisiKey));
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.VisaExtension, ReportDashboardCatalog.VisaUzatilanlarKey));
        Assert.True(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.Invitation, "in-process-long"));
        Assert.Null(ReportDashboardExcelPrintLayout.IsLandscape(
            ReportDashboardCategory.PersonSearch, "all"));
    }

    [Fact]
    public void Apply_LeavesBytesUnchanged_WhenLayoutNotConfigured()
    {
        var original = BuildSheet(splitRow: 0, defineHeader: false);
        var stamped = ReportDashboardExcelPrintLayout.Apply(
            original, ReportDashboardCategory.PersonSearch, "all");
        Assert.Same(original, stamped);
    }

    [Fact]
    public void Apply_EmptyContent_ReturnsEmpty()
    {
        Assert.Empty(ReportDashboardExcelPrintLayout.Apply(
            System.Array.Empty<byte>(), ReportDashboardCategory.Invitation, "used"));
        Assert.Empty(ReportDashboardExcelPrintLayout.Apply(
            null!, ReportDashboardCategory.Invitation, "used"));
    }

    [Fact]
    public void ResolvePrintTitleLastRow_UsesFreezePaneWhenHeaderMissing()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Report");
        sheet.Cell(1, 1).Value = "Title";
        sheet.Cell(3, 1).Value = "Header";
        sheet.SheetView.SplitRow = 3;

        Assert.Equal(3, ReportDashboardExcelPrintLayout.ResolvePrintTitleLastRow(workbook, sheet));
    }

    [Fact]
    public void ResolvePrintTitleLastRow_DefaultsToRowOne()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Report");
        Assert.Equal(1, ReportDashboardExcelPrintLayout.ResolvePrintTitleLastRow(workbook, sheet));
    }

    [Fact]
    public void CopyPageSetup_CopiesOrientationFitAndPrintTitles()
    {
        using var workbook = new XLWorkbook();
        var source = workbook.AddWorksheet("Source");
        var dest = workbook.AddWorksheet("Dest");

        ReportDashboardExcelPrintLayout.ApplyToWorksheet(workbook, source, landscape: true);
        source.PageSetup.SetRowsToRepeatAtTop(1, 2);

        ReportDashboardExcelPrintLayout.CopyPageSetup(source, dest);

        Assert.Equal(XLPageOrientation.Landscape, dest.PageSetup.PageOrientation);
        Assert.Equal(XLPaperSize.A4Paper, dest.PageSetup.PaperSize);
        Assert.Equal(1, dest.PageSetup.PagesWide);
        Assert.Equal(0, dest.PageSetup.PagesTall);
        Assert.Equal(XLSheetViewOptions.PageBreakPreview, dest.SheetView.View);
        Assert.Equal(1, dest.PageSetup.FirstRowToRepeatAtTop);
        Assert.Equal(2, dest.PageSetup.LastRowToRepeatAtTop);
    }

    private static byte[] BuildSheet(int splitRow, bool defineHeader)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Report");
        sheet.Cell(1, 1).Value = "A";
        if (splitRow > 0)
            sheet.SheetView.SplitRow = splitRow;
        if (defineHeader)
        {
            workbook.DefinedNames.Add(
                ReportDashboardExcelTemplateWorkbook.HeaderName,
                sheet.Range(2, 1, 2, 1));
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
