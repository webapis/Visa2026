using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Inserts a roster change summary above the header on person-row roster Excel sheets.
/// Always shown when applied: as-of range, totals, data-change names, date/filter-change names, export line.
/// </summary>
public static class ReportDashboardRosterExcelPreamble
{
    private const int PreambleRows = 6;
    private static readonly XLColor AddedGreen = XLColor.FromHtml("#067647");
    private static readonly XLColor RemovedRed = XLColor.FromHtml("#B42318");

    public static byte[] Apply(byte[] workbookBytes, ReportDashboardRosterDiff diff, Guid exportId)
    {
        if (workbookBytes == null || workbookBytes.Length == 0)
            return workbookBytes;

        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(workbookBytes));
            if (!TryFindHeader(workbook, out var sheet, out var headerRow, out var columnCount))
                return workbookBytes;

            if (sheet.AutoFilter?.IsEnabled == true)
                sheet.AutoFilter.Clear();

            sheet.Row(headerRow).InsertRowsAbove(PreambleRows);

            var r = headerRow;
            WriteMergedLine(sheet, r, columnCount, FormatAsOfLine(diff), bold: true, color: null, minHeight: 18);
            WriteMergedLine(sheet, r + 1, columnCount, FormatTotalsLine(diff), bold: true, color: null, minHeight: 18);
            WriteMergedLine(sheet, r + 2, columnCount, FormatBucketLine("Data changes — Added", diff.DataAdded, "No people added."), bold: true, color: AddedGreen, minHeight: 18);
            WriteMergedLine(sheet, r + 3, columnCount, FormatBucketLine("Data changes — Removed", diff.DataRemoved, "No people removed."), bold: true, color: RemovedRed, minHeight: 18);
            WriteMergedLine(sheet, r + 4, columnCount, FormatBucketLine("Date / filter changes — Added", diff.DateFilterAdded, "No people added."), bold: true, color: AddedGreen, minHeight: 18);
            WriteMergedLine(sheet, r + 5, columnCount, FormatBucketLine("Date / filter changes — Removed", diff.DateFilterRemoved, "No people removed."), bold: true, color: RemovedRed, minHeight: 18);

            // Export id on the last preamble row (append to date-removed line is crowded — use row after insert? keep 6 rows: put export in as-of line)
            // Re-write row 0 to include export short id.
            WriteMergedLine(
                sheet,
                r,
                columnCount,
                FormatAsOfLine(diff) + "  |  " + FormatExportFragment(diff, exportId),
                bold: true,
                color: null,
                minHeight: 18);

            var newHeaderRow = headerRow + PreambleRows;
            sheet.SheetView.FreezeRows(newHeaderRow);
            var lastCol = Math.Max(1, columnCount);
            var lastRow = Math.Max(newHeaderRow + 1, sheet.LastRowUsed()?.RowNumber() ?? (newHeaderRow + 1));
            sheet.Range(newHeaderRow, 1, lastRow, lastCol).SetAutoFilter();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch
        {
            return workbookBytes;
        }
    }

    private static string FormatAsOfLine(ReportDashboardRosterDiff diff)
    {
        var prev = ReportDashboardRosterDiffService.FormatAsOfDate(diff.PreviousAsOfDate);
        var curr = ReportDashboardRosterDiffService.FormatAsOfDate(diff.CurrentAsOfDate);
        if (string.IsNullOrEmpty(prev))
        {
            return string.IsNullOrEmpty(curr)
                ? "Roster change log"
                : "Roster as of " + curr + " (first Excel baseline)";
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "Roster as of {0} → as of {1}",
            prev,
            string.IsNullOrEmpty(curr) ? "—" : curr);
    }

    private static string FormatTotalsLine(ReportDashboardRosterDiff diff)
    {
        var prevLabel = diff.HasPrevious
            ? diff.PreviousTotalCount.ToString(CultureInfo.InvariantCulture)
            : "—";
        return string.Format(
            CultureInfo.InvariantCulture,
            "Previous total: {0}    Current total: {1}",
            prevLabel,
            diff.CurrentTotalCount);
    }

    private static string FormatBucketLine(
        string label,
        IReadOnlyList<ReportDashboardRosterMember> people,
        string emptyText)
    {
        if (people == null || people.Count == 0)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}: 0 — {1}",
                label,
                emptyText);
        }

        var names = string.Join("; ", people.Select(FormatPerson));
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}: {1} — {2}",
            label,
            people.Count,
            names);
    }

    private static string FormatPerson(ReportDashboardRosterMember person)
    {
        var name = string.IsNullOrWhiteSpace(person.Name) ? "(unnamed)" : person.Name.Trim();
        if (string.IsNullOrWhiteSpace(person.PassportOrKey))
            return name;
        return name + " (" + person.PassportOrKey.Trim() + ")";
    }

    private static string FormatExportFragment(ReportDashboardRosterDiff diff, Guid exportId)
    {
        var shortId = ReportDashboardRosterDiffService.FormatExportShortId(exportId);
        var when = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        return "Export: " + shortId + " · " + when;
    }

    private static bool TryFindHeader(
        XLWorkbook workbook,
        out IXLWorksheet sheet,
        out int headerRow,
        out int columnCount)
    {
        sheet = workbook.Worksheets.First();
        headerRow = 1;
        columnCount = 1;

        var header = Range(workbook, ReportDashboardExcelTemplateWorkbook.HeaderName);
        if (header != null)
        {
            sheet = header.Worksheet;
            headerRow = header.RangeAddress.FirstAddress.RowNumber;
            columnCount = Math.Max(1, header.ColumnCount());
            return true;
        }

        var used = sheet.RangeUsed();
        if (used == null)
            return false;

        headerRow = used.RangeAddress.FirstAddress.RowNumber;
        columnCount = Math.Max(1, used.ColumnCount());
        return true;
    }

    private static void WriteMergedLine(
        IXLWorksheet sheet,
        int row,
        int columnCount,
        string text,
        bool bold,
        XLColor? color,
        double minHeight)
    {
        var cell = sheet.Cell(row, 1);
        cell.Value = text;
        if (columnCount > 1)
            sheet.Range(row, 1, row, columnCount).Merge();

        var range = sheet.Range(row, 1, row, columnCount);
        range.Style.Font.FontName = ReportDashboardExcelTemplateWorkbook.CompanyFontName;
        range.Style.Font.FontSize = 10;
        range.Style.Font.Bold = bold;
        if (color != null)
            range.Style.Font.FontColor = color;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;

        var lines = Math.Max(1, (int)Math.Ceiling(text.Length / 90.0));
        sheet.Row(row).Height = Math.Max(minHeight, 14 * lines);
    }

    private static IXLRange? Range(XLWorkbook workbook, string name)
    {
        var defined = workbook.DefinedNames
            .FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase)
                || n.Name.EndsWith("!" + name, StringComparison.OrdinalIgnoreCase));
        return defined?.Ranges.FirstOrDefault();
    }
}
