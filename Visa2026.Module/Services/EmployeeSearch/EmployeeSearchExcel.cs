using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.EmployeeSearch;

/// <summary>
/// Spreadsheet of the Find employees result table: the same rows and columns on screen.
/// </summary>
public static class EmployeeSearchExcel
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Build(
        IReadOnlyList<EmployeeSearchRow> rows,
        IReadOnlyList<EmployeeSearchColumnState> columns,
        IReadOnlyList<string>? notes)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName());
        var visible = (columns ?? Array.Empty<EmployeeSearchColumnState>()).Where(column => column.Included).ToList();
        if (visible.Count == 0)
            visible = EmployeeSearchColumns.Defaults().Where(column => column.Included).ToList();
        var headers = visible.Select(column => column.Caption).ToArray();

        var noteCount = 0;
        if (notes != null)
        {
            foreach (var note in notes)
            {
                if (string.IsNullOrWhiteSpace(note))
                    continue;

                noteCount++;
                var cell = sheet.Cell(noteCount, 1);
                cell.Value = note.Trim();
                cell.Style.Font.Italic = true;
                cell.Style.Alignment.WrapText = true;
                sheet.Range(noteCount, 1, noteCount, headers.Length).Merge();
            }
        }

        var headerRow = noteCount + 1;
        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(headerRow, column + 1).Value = headers[column];

        sheet.Range(headerRow, 1, headerRow, headers.Length).Style.Font.Bold = true;

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var excelRow = headerRow + 1 + index;
            for (var column = 0; column < visible.Count; column++)
                sheet.Cell(excelRow, column + 1).Value = Cell(row, visible[column].Key);
        }

        sheet.SheetView.FreezeRows(headerRow);
        var lastRow = Math.Max(headerRow, headerRow + rows.Count);
        sheet.Columns(1, headers.Length).AdjustToContents(1, Math.Min(lastRow, headerRow + 60));
        sheet.Range(headerRow, 1, lastRow, headers.Length).SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Cell(EmployeeSearchRow row, string key)
    {
        if (string.Equals(key, "Visa", StringComparison.OrdinalIgnoreCase))
            return VisaLabel(row.VisaStatus);
        if (string.Equals(key, "WhyMatched", StringComparison.OrdinalIgnoreCase))
            return string.Join("; ", row.Pills ?? Array.Empty<string>());
        return row.Values != null && row.Values.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
    }

    private static string SheetName()
    {
        var title = EmployeeSearchText.Get("Title");
        var cleaned = new string(title.Select(ch => "\\/?*[]:".Contains(ch) ? ' ' : ch).ToArray()).Trim();
        if (cleaned.Length == 0)
            cleaned = "Find employees";
        return cleaned.Length <= 31 ? cleaned : cleaned.Substring(0, 31);
    }

    private static string VisaLabel(string? status) => status switch
    {
        "Valid" => EmployeeSearchText.Get("Visa.Valid"),
        "Expiring" => EmployeeSearchText.Get("Visa.Expiring"),
        "Expired" => EmployeeSearchText.Get("Visa.Expired"),
        _ => EmployeeSearchText.Get("Visa.None"),
    };
}