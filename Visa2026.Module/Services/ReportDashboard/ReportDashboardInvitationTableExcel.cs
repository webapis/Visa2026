using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Spreadsheet of the Active Invitation preview table, same columns and same rows the officer is looking at.
/// </summary>
public static class ReportDashboardInvitationTableExcel
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Build(
        IReadOnlyList<string> headers,
        IReadOnlyList<ReportDashboardPreviewRow> rows,
        bool includeChartColumn,
        bool processTable = false)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(processTable ? "Invitation Process" : "Active Invitation");
        var columnCount = Math.Max(1, headers.Count);

        for (var column = 0; column < headers.Count; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        var headerRange = sheet.Range(1, 1, 1, columnCount);
        headerRange.Style.Font.Bold = true;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var values = Cells(rows[rowIndex], includeChartColumn, processTable);
            var count = Math.Min(headers.Count, values.Count);
            for (var column = 0; column < count; column++)
            {
                var cell = sheet.Cell(rowIndex + 2, column + 1);
                cell.Value = values[column];
                if (values[column].Contains('\n', StringComparison.Ordinal))
                    cell.Style.Alignment.WrapText = true;
            }
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns(1, columnCount).AdjustToContents(1, 60);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Header row of the company invitation sheet. The on-screen table keeps its own captions.</summary>
    public static readonly string[] CompanyHeaders =
    [
        "S.No",
        "Adı Soyadı",
        "Görevi",
        "Projesi",
        "Firması",
        "Davetiye için yazı No",
        "İlgili Bakanlığın Başvuru tarihi",
        "Başvurulan vize süresi",
        "İlgili Bakanlıktan çıkan tarih ve numarası",
        "Yabancı Şube CO verilen Tarihi ve No",
        "Davetiyenin son geçerlilik tarihi",
        "NOT",
    ];

    public static List<string> ActiveCells(ReportDashboardPreviewRow row, int rowNumber, bool includeChartColumn)
    {
        var values = new List<string>
        {
            rowNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.Name,
            row.ColumnA,
            row.Department,
            row.ColumnB,
            row.ColumnC,
            OfficerDay(row.ColumnD),
            row.ColumnH,
            CompanyLines(row.LastApproval, stripNumberMark: false),
            CompanyLines(row.ColumnI, stripNumberMark: true),
            OfficerDay(row.ColumnF),
            row.RemainingDaysNote,
        };
        if (includeChartColumn)
            values.Add(row.Status);
        return values;
    }

    public static List<string> Cells(ReportDashboardPreviewRow row, bool includeChartColumn, bool processTable = false)
    {
        var values = new List<string>
        {
            row.Name,
            row.Department,
            row.ColumnA,
            row.ColumnB,
            JoinLines(row.ColumnC, row.ColumnD),
        };

        foreach (var ministry in row.MinistryCells)
            values.Add((ministry ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal));

        var migration = MigrationCell(row);
        values.Add(migration);
        if (processTable)
        {
            values.Add(ReportDashboardLocalization.Status(row.ColumnE));
            if (includeChartColumn)
                values.Add(row.Status);
            return values;
        }

        values.Add(row.ColumnE);
        values.Add(JoinLines(row.ColumnF, ReportDashboardLocalization.Status(row.ExpiresBadge)));
        values.Add(row.ColumnH);
        if (includeChartColumn)
            values.Add(row.Status);
        return values;
    }

    public static List<string> UsedCells(ReportDashboardPreviewRow row, bool includeChartColumn)
    {
        var values = Cells(row, includeChartColumn: false, processTable: false);
        values.Add(row.VisaNumber);
        values.Add(row.VisaIssuedDate);
        values.Add(row.VisaPlace);
        values.Add(row.VisaStartDate);
        values.Add(row.VisaEndDate);
        if (includeChartColumn)
            values.Add(row.Status);
        return values;
    }

    public static List<string> CancelledCells(ReportDashboardPreviewRow row)
    {
        return
        [
            row.Name,
            row.Department,
            row.ColumnE,
            row.ColumnA,
            JoinLines(row.ColumnF, ReportDashboardLocalization.Status(row.ExpiresBadge)),
            row.ColumnH,
            row.ColumnC,
            row.ColumnD,
            row.ColumnI ?? string.Empty,
        ];
    }

    private static string OfficerDay(string? text)
    {
        var value = text?.Trim() ?? string.Empty;
        if (value.Length == 0)
            return string.Empty;
        if (TryOfficerDate(value, out var date))
            return date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
        return value;
    }

    private static string CompanyLines(string? text, bool stripNumberMark)
    {
        var lines = new List<string>();
        foreach (var raw in (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var value = raw.Trim();
            if (value.Length == 0)
                continue;
            if (TryOfficerDate(value, out var date))
            {
                lines.Add(date.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture));
                continue;
            }

            if (stripNumberMark)
                value = value.TrimStart('№').Trim();
            if (value.Length > 0)
                lines.Add(value);
        }

        return string.Join("\n", lines);
    }

    private static bool TryOfficerDate(string text, out DateTime date)
    {
        var cultures = new[]
        {
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.CultureInfo.GetCultureInfo("en-US"),
        };
        foreach (var culture in cultures)
        {
            if (DateTime.TryParseExact(text, "MMM dd, yyyy", culture, System.Globalization.DateTimeStyles.None, out date))
                return true;
        }

        date = default;
        return false;
    }

    private static string MigrationCell(ReportDashboardPreviewRow row)
    {
        var migration = row.ColumnI ?? string.Empty;
        if (string.IsNullOrEmpty(row.ColumnBadge))
            return migration;

        var badge = ReportDashboardLocalization.Status(row.ColumnBadge);
        return migration.Length == 0 ? badge : migration + "\n" + badge;
    }

    private static string JoinLines(string? first, string? second)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(first))
            lines.Add(first.Trim());
        if (!string.IsNullOrWhiteSpace(second))
            lines.Add(second.Trim());
        return string.Join("\n", lines);
    }
}
