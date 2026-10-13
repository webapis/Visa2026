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
        bool includeChartColumn)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Active Invitation");
        var columnCount = Math.Max(1, headers.Count);

        for (var column = 0; column < headers.Count; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        var headerRange = sheet.Range(1, 1, 1, columnCount);
        headerRange.Style.Font.Bold = true;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var values = Cells(rows[rowIndex], includeChartColumn);
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

    public static List<string> Cells(ReportDashboardPreviewRow row, bool includeChartColumn)
    {
        var values = new List<string>
        {
            row.Name,
            row.Project,
            row.ColumnA,
            row.ColumnB,
            JoinLines(row.ColumnC, row.ColumnD),
        };

        foreach (var ministry in row.MinistryCells)
            values.Add((ministry ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal));

        var migration = row.ColumnI ?? string.Empty;
        if (!string.IsNullOrEmpty(row.ColumnBadge))
        {
            migration = migration.Length == 0
                ? row.ColumnBadge
                : migration + "\n" + row.ColumnBadge;
        }

        values.Add(migration);
        values.Add(row.ColumnE);
        values.Add(JoinLines(row.ColumnF, ReportDashboardLocalization.Status(row.ExpiresBadge)));
        values.Add(row.ColumnH);
        if (includeChartColumn)
            values.Add(row.Status);
        return values;
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
