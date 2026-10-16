using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Workbook contract: a title area, a named header row, and a named sample row.
/// Export keeps the title and header text and copies the sample row's format onto each data row.
/// </summary>
public static class ReportDashboardExcelTemplateWorkbook
{
    public const string HeaderName = "DashboardHeader";
    public const string SampleName = "DashboardSample";

    public static byte[] Export(
        byte[]? template,
        string title,
        IReadOnlyList<string> headers,
        ReportDashboardCategory category,
        string? subReport,
        IReadOnlyList<ReportDashboardPreviewRow> rows,
        bool includeChartColumn)
    {
        var source = template != null && MatchesColumns(template, headers.Count)
            ? template
            : BuildStarter(title, headers, SheetName(category, subReport));

        var values = new List<IReadOnlyList<string>>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
            values.Add(ValuesFor(category, subReport, rows[index], index + 1, headers.Count, includeChartColumn));

        var titleDate = IsWorkPermitMaglumat(category, subReport) ? DateTime.Today : (DateTime?)null;
        return Fill(source, values, titleDate);
    }

    public static byte[] BuildStarter(string title, IReadOnlyList<string> headers, string? sheetName = null)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Report" : sheetName);
        var columnCount = Math.Max(1, headers.Count);

        sheet.Cell(1, 1).Value = title;
        if (columnCount > 1)
            sheet.Range(1, 1, 1, columnCount).Merge();
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Font.FontSize = 16;
        sheet.Row(1).Style.Alignment.WrapText = true;
        sheet.Row(1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(1).Height = 36;

        for (var column = 0; column < columnCount; column++)
            sheet.Cell(2, column + 1).Value = column < headers.Count ? headers[column] : string.Empty;

        var header = sheet.Range(2, 1, 2, columnCount);
        header.Style.Font.Bold = true;
        header.Style.Alignment.WrapText = true;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        var sample = sheet.Range(3, 1, 3, columnCount);
        sample.Style.Alignment.WrapText = true;
        sample.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

        sheet.SheetView.FreezeRows(2);
        sheet.Columns(1, columnCount).Width = 18;
        sheet.Row(2).Height = 30;
        sheet.Row(3).Height = 36;

        workbook.DefinedNames.Add(HeaderName, header);
        workbook.DefinedNames.Add(SampleName, sample);

        return Save(workbook);
    }

    public static bool MatchesColumns(byte[] content, int expectedColumns)
    {
        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(content));
            return TryRanges(workbook, out var header, out var sample)
                && header!.ColumnCount() == expectedColumns
                && sample!.ColumnCount() == expectedColumns
                && sample.RangeAddress.FirstAddress.RowNumber > header.RangeAddress.FirstAddress.RowNumber;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static IReadOnlyList<string> ValuesFor(
        ReportDashboardCategory category,
        string? subReport,
        ReportDashboardPreviewRow row,
        int rowNumber,
        int headerCount,
        bool includeChartColumn)
    {
        if (IsWorkPermitMaglumat(category, subReport))
            return ReportDashboardWorkPermitMaglumatExcel.Cells(row, rowNumber);

        if (ReportDashboardCatalog.IsInvitationCancelledSubReport(subReport))
            return ReportDashboardInvitationTableExcel.CancelledCells(row);

        if (ReportDashboardCatalog.IsInvitationReadySubReport(subReport)
            || ReportDashboardCatalog.IsInvitationExpiredSubReport(subReport))
            return ReportDashboardInvitationTableExcel.Cells(row, includeChartColumn, processTable: false);

        if (ReportDashboardCatalog.IsInvitationUsedSubReport(subReport))
            return ReportDashboardInvitationTableExcel.UsedCells(row, includeChartColumn);

        if (ReportDashboardCatalog.IsInvitationProcessSubReport(subReport))
            return ReportDashboardInvitationTableExcel.Cells(row, includeChartColumn: false, processTable: true);

        return GenericCells(row, headerCount);
    }

    private static byte[] Fill(byte[] template, IReadOnlyList<IReadOnlyList<string>> rows, DateTime? titleDate)
    {
        using var workbook = new XLWorkbook(new MemoryStream(template));
        if (!TryRanges(workbook, out var header, out var sample) || header == null || sample == null)
            return template;

        var sheet = sample.Worksheet;
        if (titleDate is { } date)
            RefreshTitleDate(sheet, header, date);
        var sampleRow = sample.RangeAddress.FirstAddress.RowNumber;
        var columnCount = header.ColumnCount();
        var last = sheet.LastRowUsed()?.RowNumber() ?? sampleRow;
        if (last > sampleRow)
            sheet.Rows(sampleRow + 1, last).Delete();

        if (rows.Count == 0)
        {
            ClearRow(sheet, sampleRow, columnCount);
            return Save(workbook);
        }

        for (var index = 1; index < rows.Count; index++)
            sheet.Row(sampleRow).CopyTo(sheet.Row(sampleRow + index));

        for (var index = 0; index < rows.Count; index++)
            WriteRow(sheet, sampleRow + index, columnCount, rows[index]);

        return Save(workbook);
    }

    private static List<string> GenericCells(ReportDashboardPreviewRow row, int headerCount)
    {
        var values = new List<string>
        {
            row.Name ?? string.Empty,
            row.Project ?? string.Empty,
            row.ColumnA ?? string.Empty,
            row.ColumnB ?? string.Empty
        };

        string?[] extras =
        [
            row.ColumnC, row.ColumnD, row.ColumnE, row.ColumnF, row.ColumnG, row.ColumnH, row.ColumnI
        ];
        var extraCount = Math.Max(0, headerCount - 5);
        for (var i = 0; i < extraCount && i < extras.Length; i++)
            values.Add(extras[i] ?? string.Empty);

        values.Add(ReportDashboardLocalization.Status(row.Status));
        if (values.Count > headerCount)
            values.RemoveRange(headerCount, values.Count - headerCount);
        while (values.Count < headerCount)
            values.Add(string.Empty);
        return values;
    }

    private static bool IsWorkPermitMaglumat(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.WorkPermit
        && string.Equals(subReport, "active-by-project", StringComparison.OrdinalIgnoreCase);

    private static string SheetName(ReportDashboardCategory category, string? subReport) =>
        IsWorkPermitMaglumat(category, subReport) ? "A.Maglumat" : "Report";

    /// <summary>
    /// A Maglumat keeps the administrator's title wording and replaces the first dd.MM.yyyy with the export day.
    /// </summary>
    private static void RefreshTitleDate(IXLWorksheet sheet, IXLRange header, DateTime date)
    {
        var headerRow = header.RangeAddress.FirstAddress.RowNumber;
        if (headerRow <= 1)
            return;

        var cell = sheet.Cell(headerRow - 1, header.RangeAddress.FirstAddress.ColumnNumber);
        var text = cell.GetString();
        if (string.IsNullOrEmpty(text))
            return;

        var updated = new Regex(@"\d{2}\.\d{2}\.\d{4}").Replace(text, date.ToString("dd.MM.yyyy"), 1);
        if (!string.Equals(updated, text, StringComparison.Ordinal))
            cell.Value = updated;
    }

    private static void WriteRow(IXLWorksheet sheet, int rowNumber, int columnCount, IReadOnlyList<string> values)
    {
        var count = Math.Min(columnCount, values.Count);
        for (var column = 0; column < count; column++)
            sheet.Cell(rowNumber, column + 1).Value = values[column] ?? string.Empty;
        for (var column = count; column < columnCount; column++)
            sheet.Cell(rowNumber, column + 1).Value = string.Empty;
    }

    private static void ClearRow(IXLWorksheet sheet, int rowNumber, int columnCount)
    {
        for (var column = 1; column <= columnCount; column++)
            sheet.Cell(rowNumber, column).Value = string.Empty;
    }

    private static bool TryRanges(XLWorkbook workbook, out IXLRange? header, out IXLRange? sample)
    {
        header = Range(workbook, HeaderName);
        sample = Range(workbook, SampleName);
        return header != null && sample != null;
    }

    private static IXLRange? Range(XLWorkbook workbook, string name)
    {
        var defined = workbook.DefinedNames
            .FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
        return defined?.Ranges.FirstOrDefault();
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
