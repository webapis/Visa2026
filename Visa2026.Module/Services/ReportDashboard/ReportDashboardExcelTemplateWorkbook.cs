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
    public const string CompanyFontName = "Times New Roman";
    private const string HeaderBackground = "#D9D9D9";

    /// <summary>One workbook. Each part is already a filled report; its first sheet is copied.</summary>
    public static byte[] Combine(IReadOnlyList<(string SheetName, byte[] Workbook)> sheets)
    {
        using var target = new XLWorkbook();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (sheetName, workbook) in sheets)
        {
            using var source = new XLWorkbook(new MemoryStream(workbook));
            CopySheet(source.Worksheets.First(), target.Worksheets.Add(UniqueSheetName(sheetName, used)));
        }

        if (target.Worksheets.Count == 0)
            target.Worksheets.Add("Report");
        return Save(target);
    }

    private static void CopySheet(IXLWorksheet source, IXLWorksheet dest)
    {
        dest.Style.Font.FontName = source.Style.Font.FontName;
        dest.Style.Font.FontSize = source.Style.Font.FontSize;
        var used = source.RangeUsed();
        if (used == null)
            return;

        var firstColumn = used.RangeAddress.FirstAddress.ColumnNumber;
        var lastColumn = used.RangeAddress.LastAddress.ColumnNumber;
        var firstRow = used.RangeAddress.FirstAddress.RowNumber;
        var lastRow = used.RangeAddress.LastAddress.RowNumber;
        for (var column = firstColumn; column <= lastColumn; column++)
            dest.Column(column).Width = source.Column(column).Width;
        for (var row = firstRow; row <= lastRow; row++)
        {
            dest.Row(row).Height = source.Row(row).Height;
            for (var column = firstColumn; column <= lastColumn; column++)
                CopyCell(source.Cell(row, column), dest.Cell(row, column));
        }

        foreach (var merged in source.MergedRanges)
        {
            var address = merged.RangeAddress;
            dest.Range(
                address.FirstAddress.RowNumber,
                address.FirstAddress.ColumnNumber,
                address.LastAddress.RowNumber,
                address.LastAddress.ColumnNumber).Merge();
        }

        if (source.AutoFilter.IsEnabled && source.AutoFilter.Range != null)
        {
            var filter = source.AutoFilter.Range.RangeAddress;
            dest.Range(
                filter.FirstAddress.RowNumber,
                filter.FirstAddress.ColumnNumber,
                filter.LastAddress.RowNumber,
                filter.LastAddress.ColumnNumber).SetAutoFilter();
        }

        if (source.SheetView.SplitRow > 0)
            dest.SheetView.FreezeRows(source.SheetView.SplitRow);
    }

    private static void CopyCell(IXLCell source, IXLCell dest)
    {
        dest.Value = source.Value;
        var from = source.Style;
        var to = dest.Style;
        to.Font.FontName = from.Font.FontName;
        to.Font.FontSize = from.Font.FontSize;
        to.Font.Bold = from.Font.Bold;
        to.Font.Italic = from.Font.Italic;
        to.Font.FontColor = from.Font.FontColor;
        to.Fill.PatternType = from.Fill.PatternType;
        to.Fill.BackgroundColor = from.Fill.BackgroundColor;
        to.Alignment.Horizontal = from.Alignment.Horizontal;
        to.Alignment.Vertical = from.Alignment.Vertical;
        to.Alignment.WrapText = from.Alignment.WrapText;
        to.NumberFormat.Format = from.NumberFormat.Format;
        to.Border.TopBorder = from.Border.TopBorder;
        to.Border.BottomBorder = from.Border.BottomBorder;
        to.Border.LeftBorder = from.Border.LeftBorder;
        to.Border.RightBorder = from.Border.RightBorder;
        to.Border.TopBorderColor = from.Border.TopBorderColor;
        to.Border.BottomBorderColor = from.Border.BottomBorderColor;
        to.Border.LeftBorderColor = from.Border.LeftBorderColor;
        to.Border.RightBorderColor = from.Border.RightBorderColor;
    }

    public static byte[] Export(
        byte[]? template,
        string title,
        IReadOnlyList<string> headers,
        ReportDashboardCategory category,
        string? subReport,
        IReadOnlyList<ReportDashboardPreviewRow> rows,
        bool includeChartColumn)
    {
        var companyInvitation = IsInvitationCompanySheet(category, subReport) && !includeChartColumn;
        var companyProcess = IsInvitationProcessCompanySheet(category, subReport);
        var columnCount = companyInvitation
            ? ReportDashboardInvitationTableExcel.CompanyHeaders.Length
            : companyProcess
                ? ReportDashboardInvitationTableExcel.ProcessCompanyHeaders.Length
                : headers.Count;
        var source = template != null && MatchesColumns(template, columnCount)
            ? template
            : companyInvitation
                ? BuildInvitationCompanyStarter()
                : companyProcess
                    ? BuildInvitationProcessCompanyStarter()
                    : IsWorkPermitCalisma(category, subReport)
                        ? BuildCalismaListesiStarter()
                        : IsVisaAktivPersoneller(category, subReport)
                            ? BuildCalismaListesiStarter("aktiv personeller")
                            : IsVisaUzatilanlar(category, subReport)
                                ? BuildUzatilanlarStarter()
                                : BuildStarter(title, headers, SheetName(category, subReport), CompanyFont(category));

        var values = new List<IReadOnlyList<string>>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
            values.Add(ValuesFor(category, subReport, rows[index], index + 1, headers.Count, includeChartColumn));

        var titleDate = IsWorkPermitMaglumat(category, subReport) ? DateTime.Today : (DateTime?)null;
        var companyHeaders = companyInvitation
            ? ReportDashboardInvitationTableExcel.CompanyHeaders
            : companyProcess
                ? ReportDashboardInvitationTableExcel.ProcessCompanyHeaders
                : null;
        return Fill(
            source,
            values,
            titleDate,
            companyHeaders != null,
            colorLastColumn: companyInvitation,
            companyHeaders,
            CompanyFont(category) || IsVisaAktivPersoneller(category, subReport) || IsVisaUzatilanlar(category, subReport),
            totalLastColumn: IsWorkPermitSanaw(category, subReport),
            headerLabels: IsWorkPermitSanaw(category, subReport)
                || IsWorkPermitCalisma(category, subReport)
                || IsVisaAktivPersoneller(category, subReport)
                || IsVisaUzatilanlar(category, subReport)
                ? headers
                : null,
            columnWidths: MaglumatColumnWidths(category, subReport),
            integerColumn: IsWorkPermitCalisma(category, subReport) || IsVisaAktivPersoneller(category, subReport)
                ? 12
                : null,
            sheetName: companyProcess ? ReportDashboardCatalog.InvitationProcessSheetName(subReport) : null);
    }

    public static bool IsInvitationCompanySheet(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.Invitation
        && (ReportDashboardCatalog.IsInvitationReadySubReport(subReport)
            || ReportDashboardCatalog.IsInvitationExpiredSubReport(subReport));

    public static bool IsInvitationProcessCompanySheet(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.Invitation
        && ReportDashboardCatalog.IsInvitationProcessSubReport(subReport);

    public static byte[] ApplyInvitationCompanyHeaders(byte[] template)
    {
        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(template));
            if (!TryRanges(workbook, out var header, out _) || header == null)
                return BuildInvitationCompanyStarter();

            WriteInvitationCompanyHeaders(header.Worksheet, header);
            return Save(workbook);
        }
        catch (Exception)
        {
            return BuildInvitationCompanyStarter();
        }
    }

    public static byte[] ApplyInvitationProcessCompanyHeaders(byte[] template)
    {
        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(template));
            if (!TryRanges(workbook, out var header, out _) || header == null)
                return BuildInvitationProcessCompanyStarter();

            WriteCompanyHeaders(header.Worksheet, header, ReportDashboardInvitationTableExcel.ProcessCompanyHeaders);
            return Save(workbook);
        }
        catch (Exception)
        {
            return BuildInvitationProcessCompanyStarter();
        }
    }

    private static void WriteInvitationCompanyHeaders(IXLWorksheet sheet, IXLRange header) =>
        WriteCompanyHeaders(sheet, header, ReportDashboardInvitationTableExcel.CompanyHeaders);

    private static void WriteCompanyHeaders(IXLWorksheet sheet, IXLRange header, string[] headers)
    {
        var row = header.RangeAddress.FirstAddress.RowNumber;
        var startColumn = header.RangeAddress.FirstAddress.ColumnNumber;
        var count = Math.Min(headers.Length, header.ColumnCount());
        for (var index = 0; index < count; index++)
            sheet.Cell(row, startColumn + index).Value = headers[index];
    }

    public static byte[] BuildInvitationCompanyStarter()
    {
        var headers = ReportDashboardInvitationTableExcel.CompanyHeaders;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Invitation");
        var columnCount = headers.Length;

        for (var column = 0; column < columnCount; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        var header = sheet.Range(1, 1, 1, columnCount);
        header.Style.Font.FontName = CompanyFontName;
        header.Style.Font.FontSize = 11;
        header.Style.Font.Bold = true;
        header.Style.Alignment.WrapText = true;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBackground);
        header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.OutsideBorderColor = XLColor.Black;
        header.Style.Border.InsideBorderColor = XLColor.Black;
        sheet.Row(1).Height = 48;

        var sample = sheet.Range(2, 1, 2, columnCount);
        sample.Style.Font.FontName = CompanyFontName;
        sample.Style.Font.FontSize = 11;
        sample.Style.Alignment.WrapText = true;
        sample.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sample.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.OutsideBorderColor = XLColor.Black;
        sample.Style.Border.InsideBorderColor = XLColor.Black;
        for (var column = 1; column <= columnCount; column++)
        {
            var horizontal = column is 2 or 3 or 4 or 5
                ? XLAlignmentHorizontalValues.Left
                : XLAlignmentHorizontalValues.Center;
            sample.Cell(1, column).Style.Alignment.Horizontal = horizontal;
        }

        double[] widths = [8, 22, 38, 22, 18, 16, 16, 14, 24, 20, 18, 26];
        for (var column = 0; column < columnCount; column++)
            sheet.Column(column + 1).Width = widths[column];

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, 2, columnCount).SetAutoFilter();
        sheet.SheetView.ZoomScale = 100;

        workbook.DefinedNames.Add(HeaderName, header);
        workbook.DefinedNames.Add(SampleName, sample);
        ApplyCompanyFont(sheet);
        return Save(workbook);
    }

    public static byte[] BuildInvitationProcessCompanyStarter()
    {
        var headers = ReportDashboardInvitationTableExcel.ProcessCompanyHeaders;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Invitation Process");
        var columnCount = headers.Length;

        for (var column = 0; column < columnCount; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        var header = sheet.Range(1, 1, 1, columnCount);
        header.Style.Font.FontName = CompanyFontName;
        header.Style.Font.FontSize = 11;
        header.Style.Font.Bold = true;
        header.Style.Alignment.WrapText = true;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBackground);
        header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.OutsideBorderColor = XLColor.Black;
        header.Style.Border.InsideBorderColor = XLColor.Black;
        sheet.Row(1).Height = 48;

        var sample = sheet.Range(2, 1, 2, columnCount);
        sample.Style.Font.FontName = CompanyFontName;
        sample.Style.Font.FontSize = 11;
        sample.Style.Alignment.WrapText = true;
        sample.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sample.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.OutsideBorderColor = XLColor.Black;
        sample.Style.Border.InsideBorderColor = XLColor.Black;
        for (var column = 1; column <= columnCount; column++)
        {
            var horizontal = column is 2 or 3 or 4 or 5 or 12 or 13
                ? XLAlignmentHorizontalValues.Left
                : XLAlignmentHorizontalValues.Center;
            sample.Cell(1, column).Style.Alignment.Horizontal = horizontal;
        }

        double[] widths = [8, 22, 38, 22, 18, 16, 16, 14, 24, 18, 20, 22, 16];
        for (var column = 0; column < columnCount; column++)
            sheet.Column(column + 1).Width = widths[column];

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, 2, columnCount).SetAutoFilter();
        sheet.SheetView.ZoomScale = 100;

        workbook.DefinedNames.Add(HeaderName, header);
        workbook.DefinedNames.Add(SampleName, sample);
        ApplyCompanyFont(sheet);
        return Save(workbook);
    }

    public static byte[] BuildCalismaListesiStarter(string? sheetName = null)
    {
        var headers = ReportDashboardWorkPermitCalismaExcel.Headers;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "çalışma izni listesi" : sheetName);
        var columnCount = headers.Length;

        for (var column = 0; column < columnCount; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        var header = sheet.Range(1, 1, 1, columnCount);
        header.Style.Font.FontName = CompanyFontName;
        header.Style.Font.FontSize = 11;
        header.Style.Font.Bold = true;
        header.Style.Alignment.WrapText = true;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBackground);
        header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.OutsideBorderColor = XLColor.Black;
        header.Style.Border.InsideBorderColor = XLColor.Black;
        sheet.Row(1).Height = 32;

        var sample = sheet.Range(2, 1, 2, columnCount);
        sample.Style.Font.FontName = CompanyFontName;
        sample.Style.Font.FontSize = 11;
        sample.Style.Alignment.WrapText = true;
        sample.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sample.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.OutsideBorderColor = XLColor.Black;
        sample.Style.Border.InsideBorderColor = XLColor.Black;

        double[] widths = [8, 16, 18, 22, 14, 16, 16, 28, 16, 14, 16, 12, 36, 12];
        for (var column = 0; column < columnCount; column++)
            sheet.Column(column + 1).Width = widths[column];

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, 2, columnCount).SetAutoFilter();
        sheet.SheetView.ZoomScale = 100;

        workbook.DefinedNames.Add(HeaderName, header);
        workbook.DefinedNames.Add(SampleName, sample);
        ApplyCompanyFont(sheet);
        return Save(workbook);
    }

    public static byte[] BuildStarter(string title, IReadOnlyList<string> headers, string? sheetName = null, bool companyFont = false)
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
        var maglumatWidths = MaglumatColumnWidths(sheet.Name);
        if (maglumatWidths != null)
            ApplyColumnWidths(sheet, maglumatWidths);
        sheet.Row(2).Height = 30;
        sheet.Row(3).Height = 36;

        workbook.DefinedNames.Add(HeaderName, header);
        workbook.DefinedNames.Add(SampleName, sample);
        if (companyFont)
            ApplyCompanyFont(sheet);

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
        if (IsVisaUzatilanlar(category, subReport))
            return ReportDashboardVisaUzatilanlarExcel.Cells(row, rowNumber);
        if (IsWorkPermitCalisma(category, subReport) || IsVisaAktivPersoneller(category, subReport))
            return ReportDashboardWorkPermitCalismaExcel.Cells(row, rowNumber);
        if (IsWorkPermitSanaw(category, subReport))
            return ReportDashboardWorkPermitSanawExcel.Cells(row, rowNumber);
        if (IsWorkPermitMaglumat3(category, subReport))
            return ReportDashboardWorkPermitMaglumatExcel.CellsWithAdditionalLocations(row, rowNumber);
        if (IsWorkPermitMaglumat2(category, subReport))
            return ReportDashboardWorkPermitMaglumatExcel.CellsWithLocations(row, rowNumber);
        if (IsWorkPermitMaglumat(category, subReport))
            return ReportDashboardWorkPermitMaglumatExcel.Cells(row, rowNumber);

        if (ReportDashboardCatalog.IsInvitationCancelledSubReport(subReport))
            return ReportDashboardInvitationTableExcel.CancelledCells(row);

        if (ReportDashboardCatalog.IsInvitationReadySubReport(subReport)
            || ReportDashboardCatalog.IsInvitationExpiredSubReport(subReport))
            return ReportDashboardInvitationTableExcel.ActiveCells(row, rowNumber, includeChartColumn);

        if (ReportDashboardCatalog.IsInvitationUsedSubReport(subReport))
            return ReportDashboardInvitationTableExcel.UsedCells(row, includeChartColumn);

        if (ReportDashboardCatalog.IsInvitationProcessSubReport(subReport))
            return ReportDashboardInvitationTableExcel.ProcessCells(row, rowNumber);

        return GenericCells(row, headerCount);
    }

    private static byte[] Fill(
        byte[] template,
        IReadOnlyList<IReadOnlyList<string>> rows,
        DateTime? titleDate,
        bool companySheet = false,
        bool colorLastColumn = false,
        string[]? companyHeaders = null,
        bool companyFont = false,
        bool totalLastColumn = false,
        IReadOnlyList<string>? headerLabels = null,
        double[]? columnWidths = null,
        int? integerColumn = null,
        string? sheetName = null)
    {
        using var workbook = new XLWorkbook(new MemoryStream(template));
        if (!TryRanges(workbook, out var header, out var sample) || header == null || sample == null)
            return template;

        var sheet = sample.Worksheet;
        if (!string.IsNullOrWhiteSpace(sheetName))
            sheet.Name = UniqueSheetName(sheetName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        if (titleDate is { } date)
            RefreshTitleDate(sheet, header, date);
        var sampleRow = sample.RangeAddress.FirstAddress.RowNumber;
        var columnCount = header.ColumnCount();
        var last = sheet.LastRowUsed()?.RowNumber() ?? sampleRow;
        if (last > sampleRow)
            sheet.Rows(sampleRow + 1, last).Delete();

        if (columnWidths != null)
            ApplyColumnWidths(sheet, columnWidths);

        if (rows.Count == 0)
        {
            ClearRow(sheet, sampleRow, columnCount);
            if (companyFont)
            {
                ApplyCompanyFont(sheet);
                ApplySheetGrid(sheet, header.RangeAddress.FirstAddress.RowNumber, sampleRow, columnCount);
            }
            return Save(workbook);
        }

        for (var index = 1; index < rows.Count; index++)
            sheet.Row(sampleRow).CopyTo(sheet.Row(sampleRow + index));

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = sampleRow + index;
            WriteRow(sheet, rowNumber, columnCount, rows[index], integerColumn);
            if (totalLastColumn)
                WriteCountCell(sheet.Cell(rowNumber, columnCount), rows[index]);
            if (companySheet)
            {
                if (colorLastColumn)
                    ColorRemainingDays(sheet.Cell(rowNumber, columnCount));
            }
        }

        if (companySheet)
        {
            WriteCompanyHeaders(sheet, header, companyHeaders ?? ReportDashboardInvitationTableExcel.CompanyHeaders);
            var lastRow = sampleRow + Math.Max(0, rows.Count - 1);
            sheet.Range(header.RangeAddress.FirstAddress.RowNumber, 1, lastRow, columnCount).SetAutoFilter();
        }

        if (totalLastColumn && rows.Count > 0)
            WriteLastColumnTotal(sheet, sampleRow, sampleRow + rows.Count - 1, columnCount);

        if (headerLabels != null)
            WriteCompanyHeaders(sheet, header, headerLabels.ToArray());

        if (companyFont)
            ApplyCompanyFont(sheet);

        if (companyFont)
        {
            var headerRow = header.RangeAddress.FirstAddress.RowNumber;
            var lastDataRow = sampleRow + rows.Count - 1;
            if (totalLastColumn)
                lastDataRow++;
            FitSheetRows(sheet, headerRow, sampleRow, lastDataRow, columnCount);
            ApplySheetGrid(sheet, headerRow, lastDataRow, columnCount);
        }

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

    // Measured from the company A. Maglumat sheet at 100% zoom.
    // Shared columns stay aligned. Location columns use the address width.
    private static readonly double[] MaglumatWidths = [15.7, 29.1, 16.1, 15.7, 15.7, 10.3, 10.8, 15.6, 15.7, 15.7, 15.7];
    private static readonly double[] Maglumat2Widths = [15.7, 29.1, 16.1, 15.7, 15.7, 10.3, 10.8, 15.6, 15.7, 15.6, 15.7, 15.7];
    private static readonly double[] Maglumat3Widths = [15.7, 29.1, 16.1, 15.7, 15.7, 10.3, 10.8, 15.6, 15.7, 15.6, 15.6, 15.7, 15.7];

    public static double[]? MaglumatColumnWidths(IReadOnlyList<string>? headers)
    {
        if (headers == null || headers.Count < 11
            || !string.Equals(headers[1], "F.A.A", StringComparison.Ordinal))
            return null;

        if (headers.Count >= 13)
            return Maglumat3Widths;
        return headers.Count == 12 ? Maglumat2Widths : MaglumatWidths;
    }

    private static double[]? MaglumatColumnWidths(string? sheetName)
    {
        if (string.Equals(sheetName, "Goşmaça barjak ýerleri", StringComparison.Ordinal))
            return Maglumat3Widths;
        if (string.Equals(sheetName, "A.Maglumat 2", StringComparison.Ordinal))
            return Maglumat2Widths;
        return string.Equals(sheetName, "A.Maglumat", StringComparison.Ordinal) ? MaglumatWidths : null;
    }

    private static double[]? MaglumatColumnWidths(ReportDashboardCategory category, string? subReport)
    {
        if (category != ReportDashboardCategory.WorkPermit)
            return null;
        if (IsWorkPermitMaglumat3(category, subReport))
            return Maglumat3Widths;
        if (IsWorkPermitMaglumat2(category, subReport))
            return Maglumat2Widths;
        return string.Equals(subReport, "active-by-project", StringComparison.OrdinalIgnoreCase)
            ? MaglumatWidths
            : null;
    }

    private static void ApplyColumnWidths(IXLWorksheet sheet, double[] widths)
    {
        for (var column = 0; column < widths.Length; column++)
            sheet.Column(column + 1).Width = widths[column];
    }

    public static byte[] BuildUzatilanlarStarter()
    {
        var headers = ReportDashboardVisaUzatilanlarExcel.Headers;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("uzatılanlar");
        var columnCount = headers.Length;

        for (var column = 0; column < columnCount; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        var header = sheet.Range(1, 1, 1, columnCount);
        header.Style.Font.FontName = CompanyFontName;
        header.Style.Font.FontSize = 11;
        header.Style.Font.Bold = true;
        header.Style.Alignment.WrapText = true;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBackground);
        header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        header.Style.Border.OutsideBorderColor = XLColor.Black;
        header.Style.Border.InsideBorderColor = XLColor.Black;
        sheet.Row(1).Height = 32;

        var sample = sheet.Range(2, 1, 2, columnCount);
        sample.Style.Font.FontName = CompanyFontName;
        sample.Style.Font.FontSize = 11;
        sample.Style.Alignment.WrapText = true;
        sample.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sample.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sample.Style.Border.OutsideBorderColor = XLColor.Black;
        sample.Style.Border.InsideBorderColor = XLColor.Black;

        var widths = ReportDashboardVisaUzatilanlarExcel.Widths;
        for (var column = 0; column < columnCount; column++)
            sheet.Column(column + 1).Width = widths[column];

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, 2, columnCount).SetAutoFilter();
        sheet.SheetView.ZoomScale = 100;

        workbook.DefinedNames.Add(HeaderName, header);
        workbook.DefinedNames.Add(SampleName, sample);
        ApplyCompanyFont(sheet);
        return Save(workbook);
    }

    private static bool IsVisaUzatilanlar(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.VisaExtension
        && string.Equals(subReport, ReportDashboardCatalog.VisaUzatilanlarKey, StringComparison.OrdinalIgnoreCase);

    private static bool IsVisaAktivPersoneller(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.VisaExtension
        && string.Equals(subReport, ReportDashboardCatalog.VisaAktivPersonellerKey, StringComparison.OrdinalIgnoreCase);

    private static bool IsWorkPermitCalisma(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.WorkPermit
        && string.Equals(subReport, ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey, StringComparison.OrdinalIgnoreCase);

    private static bool IsWorkPermitSanaw(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.WorkPermit
        && string.Equals(subReport, ReportDashboardCatalog.WorkPermitSanawKey, StringComparison.OrdinalIgnoreCase);

    private static bool IsWorkPermitMaglumat3(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.WorkPermit
        && string.Equals(subReport, ReportDashboardCatalog.WorkPermitMaglumat3Key, StringComparison.OrdinalIgnoreCase);

    private static bool IsWorkPermitMaglumat2(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.WorkPermit
        && string.Equals(subReport, ReportDashboardCatalog.WorkPermitMaglumat2Key, StringComparison.OrdinalIgnoreCase);

    private static bool IsWorkPermitMaglumat(ReportDashboardCategory category, string? subReport) =>
        category == ReportDashboardCategory.WorkPermit
        && (string.Equals(subReport, "active-by-project", StringComparison.OrdinalIgnoreCase)
            || IsWorkPermitMaglumat2(category, subReport)
            || IsWorkPermitMaglumat3(category, subReport));

    private static string UniqueSheetName(string name, HashSet<string> used)
    {
        var cleaned = new string((name ?? string.Empty)
            .Where(c => c is not ('\\' or '/' or '*' or '?' or ':' or '[' or ']'))
            .ToArray()).Trim();
        if (cleaned.Length == 0)
            cleaned = "Report";
        if (cleaned.Length > 31)
            cleaned = cleaned.Substring(0, 31);

        var candidate = cleaned;
        var suffix = 2;
        while (!used.Add(candidate))
        {
            var tail = " " + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var stem = cleaned.Length + tail.Length > 31
                ? cleaned.Substring(0, 31 - tail.Length)
                : cleaned;
            candidate = stem + tail;
            suffix++;
        }

        return candidate;
    }

    private static string SheetName(ReportDashboardCategory category, string? subReport)
    {
        if (IsVisaUzatilanlar(category, subReport))
            return "uzatılanlar";
        if (IsVisaAktivPersoneller(category, subReport))
            return "aktiv personeller";
        if (IsWorkPermitCalisma(category, subReport))
            return "çalışma izni listesi";
        if (IsWorkPermitSanaw(category, subReport))
            return "Sanaw";
        if (IsWorkPermitMaglumat3(category, subReport))
            return "Goşmaça barjak ýerleri";
        if (IsWorkPermitMaglumat2(category, subReport))
            return "A.Maglumat 2";
        return IsWorkPermitMaglumat(category, subReport) ? "A.Maglumat" : "Report";
    }

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

    private static void WriteCountCell(IXLCell cell, IReadOnlyList<string> values)
    {
        var text = values.Count == 0 ? string.Empty : values[^1]?.Trim() ?? string.Empty;
        if (int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var count))
            cell.Value = count;
    }

    private static void WriteLastColumnTotal(IXLWorksheet sheet, int firstDataRow, int lastDataRow, int column)
    {
        var totalRow = lastDataRow + 1;
        var first = sheet.Cell(firstDataRow, column).Address.ToStringRelative();
        var last = sheet.Cell(lastDataRow, column).Address.ToStringRelative();
        var cell = sheet.Cell(totalRow, column);
        cell.FormulaA1 = $"SUM({first}:{last})";
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontName = CompanyFontName;
    }

    private static void WriteRow(
        IXLWorksheet sheet, int rowNumber, int columnCount, IReadOnlyList<string> values, int? integerColumn = null)
    {
        var count = Math.Min(columnCount, values.Count);
        for (var column = 0; column < count; column++)
        {
            var cell = sheet.Cell(rowNumber, column + 1);
            if (integerColumn == column + 1
                && int.TryParse(values[column], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var number))
            {
                cell.Value = number;
                continue;
            }

            WriteCell(cell, values[column]);
        }
        for (var column = count; column < columnCount; column++)
            sheet.Cell(rowNumber, column + 1).Value = string.Empty;
    }

    private static void WriteCell(IXLCell cell, string? value)
    {
        var text = value ?? string.Empty;
        if (DateTime.TryParseExact(
                text,
                "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var date))
        {
            cell.Value = date;
            cell.Style.DateFormat.Format = "dd.MM.yyyy";
            return;
        }

        cell.Value = text;
    }

    private static void ColorRemainingDays(IXLCell cell)
    {
        const string marker = "geçerli gün sayı : ";
        var text = cell.GetString();
        var index = text.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
            return;
        if (!int.TryParse(text[(index + marker.Length)..].Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var days))
            return;

        var (background, font) = days switch
        {
            < 0 => ("#CC0000", "#FFFFFF"),
            < 14 => ("#E06666", "#FFFFFF"),
            < 30 => ("#F6B26B", "#000000"),
            < 90 => ("#FFE599", "#000000"),
            _ => ("#FFF2CC", "#000000"),
        };
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(background);
        cell.Style.Font.FontColor = XLColor.FromHtml(font);
        cell.Style.Font.Bold = days < 30;
    }

    private static void FitSheetRows(IXLWorksheet sheet, int headerRow, int firstDataRow, int lastDataRow, int columnCount)
    {
        if (headerRow > 1)
            FitContentRow(sheet, headerRow - 1, columnCount, title: true);
        FitContentRow(sheet, headerRow, columnCount, title: false);
        for (var rowNumber = firstDataRow; rowNumber <= lastDataRow; rowNumber++)
            FitContentRow(sheet, rowNumber, columnCount, title: false);
    }

    private static void FitContentRow(IXLWorksheet sheet, int rowNumber, int columnCount, bool title)
    {
        var lines = 1;
        var mergedWidth = 0d;
        for (var column = 1; column <= columnCount; column++)
            mergedWidth += Math.Max(4, sheet.Column(column).Width);

        for (var column = 1; column <= columnCount; column++)
        {
            var cell = sheet.Cell(rowNumber, column);
            var text = cell.GetString().Replace("\r\n", "\n", StringComparison.Ordinal);
            if (string.IsNullOrEmpty(text))
                continue;

            cell.Style.Alignment.WrapText = true;
            var width = title ? mergedWidth : Math.Max(4, sheet.Column(column).Width);
            var perLine = Math.Max(1, (int)Math.Floor(width * 0.95));
            var count = 0;
            foreach (var chunk in text.Split('\n'))
                count += Math.Max(1, (int)Math.Ceiling(Math.Max(1, chunk.Length) / (double)perLine));
            lines = Math.Max(lines, count);
        }

        var lineHeight = title ? 18 : 15;
        sheet.Row(rowNumber).Height = Math.Max(18, lines * lineHeight);
    }

    private static void ApplySheetGrid(IXLWorksheet sheet, int headerRow, int lastRow, int columnCount)
    {
        var bottom = Math.Max(headerRow, lastRow);
        var table = sheet.Range(headerRow, 1, bottom, columnCount);
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.OutsideBorderColor = XLColor.Black;
        table.Style.Border.InsideBorderColor = XLColor.Black;

        var header = sheet.Range(headerRow, 1, headerRow, columnCount);
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBackground);
        header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        header.Style.Alignment.WrapText = true;
    }

    private static void ClearRow(IXLWorksheet sheet, int rowNumber, int columnCount)
    {
        for (var column = 1; column <= columnCount; column++)
            sheet.Cell(rowNumber, column).Value = string.Empty;
    }

    public static byte[] ApplyHeaderLabels(byte[] content, IReadOnlyList<string> headers)
    {
        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(content));
            if (!TryRanges(workbook, out var header, out _) || header == null)
                return content;

            WriteCompanyHeaders(header.Worksheet, header, headers.ToArray());
            return Save(workbook);
        }
        catch (Exception)
        {
            return content;
        }
    }

    public static byte[] WithCompanyFont(byte[] content, double[]? columnWidths = null)
    {
        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(content));
            workbook.Style.Font.FontName = CompanyFontName;
            foreach (var worksheet in workbook.Worksheets)
                ApplyCompanyFont(worksheet);
            if (TryRanges(workbook, out var header, out var sample) && header != null && sample != null)
            {
                var gridSheet = header.Worksheet;
                if (columnWidths != null)
                    ApplyColumnWidths(gridSheet, columnWidths);
                var lastRow = Math.Max(
                    sample.RangeAddress.LastAddress.RowNumber,
                    gridSheet.LastRowUsed()?.RowNumber() ?? sample.RangeAddress.LastAddress.RowNumber);
                ApplySheetGrid(
                    gridSheet,
                    header.RangeAddress.FirstAddress.RowNumber,
                    lastRow,
                    header.ColumnCount());
            }
            return Save(workbook);
        }
        catch (Exception)
        {
            return content;
        }
    }

    private static bool CompanyFont(ReportDashboardCategory category) =>
        category is ReportDashboardCategory.Invitation or ReportDashboardCategory.WorkPermit;

    private static void ApplyCompanyFont(IXLWorksheet sheet)
    {
        sheet.Style.Font.FontName = CompanyFontName;
        var used = sheet.RangeUsed();
        if (used != null)
            used.Style.Font.FontName = CompanyFontName;
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
