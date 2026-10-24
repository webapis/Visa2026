using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// A4 print layout for Report Dashboard company Excel sheets:
/// orientation, fit all columns on one page width, open in Page Break Preview,
/// and repeat the title/header rows on every printed page.
/// </summary>
public static class ReportDashboardExcelPrintLayout
{
    /// <summary>
    /// When true, Open in Excel / Download template force A4 + orientation + fit width + Page Break Preview.
    /// Null means this report is left unchanged.
    /// </summary>
    public static bool? IsLandscape(ReportDashboardCategory category, string? subReport)
    {
        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, "active-by-project", StringComparison.OrdinalIgnoreCase))
            return false;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitMaglumat2Key, StringComparison.OrdinalIgnoreCase))
            return false;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitMaglumat3Key, StringComparison.OrdinalIgnoreCase))
            return true;

        if (category == ReportDashboardCategory.WorkPermit
            && (string.Equals(subReport, ReportDashboardCatalog.WorkPermitSanawKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(subReport, ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(subReport, ReportDashboardCatalog.WorkPermitKotaOranimizKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(subReport, ReportDashboardCatalog.WorkPermitPersonelSayiKey, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (category == ReportDashboardCategory.Registration
            && string.Equals(subReport, ReportDashboardCatalog.RegistrationOturumSayisiKey, StringComparison.OrdinalIgnoreCase))
            return true;

        if (category == ReportDashboardCategory.VisaExtension
            && (string.Equals(subReport, ReportDashboardCatalog.VisaAktivPersonellerKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(subReport, ReportDashboardCatalog.VisaUzatilanlarKey, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (category == ReportDashboardCategory.Invitation
            && (ReportDashboardCatalog.IsInvitationReadySubReport(subReport)
                || ReportDashboardCatalog.IsInvitationExpiredSubReport(subReport)
                || ReportDashboardCatalog.IsInvitationCancelledSubReport(subReport)
                || ReportDashboardCatalog.IsInvitationProcessSubReport(subReport)
                || ReportDashboardCatalog.IsInvitationUsedSubReport(subReport)))
            return true;

        return null;
    }

    public static byte[] Apply(byte[] content, ReportDashboardCategory category, string? subReport)
    {
        if (content == null || content.Length == 0)
            return content ?? Array.Empty<byte>();

        var landscape = IsLandscape(category, subReport);
        if (landscape is null)
            return content;

        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(content));
            foreach (var sheet in workbook.Worksheets)
                ApplyToWorksheet(workbook, sheet, landscape.Value);
            using var output = new MemoryStream();
            workbook.SaveAs(output);
            return output.ToArray();
        }
        catch (Exception)
        {
            return content;
        }
    }

    public static void ApplyToWorksheet(XLWorkbook workbook, IXLWorksheet sheet, bool landscape)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(sheet);

        sheet.PageSetup.PageOrientation = landscape
            ? XLPageOrientation.Landscape
            : XLPageOrientation.Portrait;
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.SheetView.View = XLSheetViewOptions.PageBreakPreview;

        var lastTitleRow = ResolvePrintTitleLastRow(workbook, sheet);
        if (lastTitleRow > 0)
            sheet.PageSetup.SetRowsToRepeatAtTop(1, lastTitleRow);
    }

    /// <summary>
    /// Column header row (and any title rows above it) so Print Titles show on every page.
    /// Prefers the DashboardHeader named range; otherwise freeze-pane split; otherwise row 1.
    /// </summary>
    public static int ResolvePrintTitleLastRow(XLWorkbook workbook, IXLWorksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(sheet);

        var defined = workbook.DefinedNames
            .FirstOrDefault(n => string.Equals(
                n.Name, ReportDashboardExcelTemplateWorkbook.HeaderName, StringComparison.OrdinalIgnoreCase));
        var range = defined?.Ranges.FirstOrDefault(r => ReferenceEquals(r.Worksheet, sheet))
            ?? defined?.Ranges.FirstOrDefault();
        if (range != null && ReferenceEquals(range.Worksheet, sheet))
            return Math.Max(1, range.RangeAddress.LastAddress.RowNumber);

        if (sheet.SheetView.SplitRow > 0)
            return sheet.SheetView.SplitRow;

        return 1;
    }

    public static void CopyPageSetup(IXLWorksheet source, IXLWorksheet dest)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(dest);

        dest.PageSetup.PageOrientation = source.PageSetup.PageOrientation;
        dest.PageSetup.PaperSize = source.PageSetup.PaperSize;
        dest.PageSetup.FitToPages(source.PageSetup.PagesWide, source.PageSetup.PagesTall);
        dest.SheetView.View = source.SheetView.View;
        if (source.PageSetup.FirstRowToRepeatAtTop > 0 && source.PageSetup.LastRowToRepeatAtTop > 0)
            dest.PageSetup.SetRowsToRepeatAtTop(
                source.PageSetup.FirstRowToRepeatAtTop,
                source.PageSetup.LastRowToRepeatAtTop);
    }
}