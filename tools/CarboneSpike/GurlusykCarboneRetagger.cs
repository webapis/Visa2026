using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Visa2026.Tools.CarboneSpike;

internal static class GurlusykCarboneRetagger
{
    private static readonly Regex DsHeaderTag = new(@"\{\{ds\.([^}]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex DotRowTag = new(@"\{\{\.([^}]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex DsRowsTag = new(@"\{\{ds\.rows\.([^}]+)\}\}", RegexOptions.Compiled);

    public static string RetagDefault()
    {
        var source = RepoPaths.ModuleTemplates(Path.Combine("Excel", "433_gurlusyk_uzt.xlsx"));
        var destDir = Path.Combine(RepoPaths.Root(), "tools", "CarboneSpike", "templates", "spike");
        Directory.CreateDirectory(destDir);
        var dest = Path.Combine(destDir, "433_gurlusyk_uzt.carbone.xlsx");
        Retag(source, dest);
        return dest;
    }

    public static void Inspect(string path)
    {
        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.First();
        Console.WriteLine($"Sheet={worksheet.Name} rows={worksheet.LastRowUsed()?.RowNumber()} cols={worksheet.LastColumnUsed()?.ColumnNumber()} merges={worksheet.MergedRanges.Count()}");
        for (int r = 1; r <= worksheet.LastRowUsed()?.RowNumber(); r++)
        {
            foreach (var cell in worksheet.Row(r).CellsUsed())
            {
                var text = GetCellText(cell);
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                if (text.Contains('{', StringComparison.Ordinal) || text.Contains("{{", StringComparison.Ordinal))
                    Console.WriteLine($"{cell.Address}: [{text.ReplaceLineEndings("\\n")}] type={cell.DataType}");
            }
        }
    }

    public static void Retag(string sourcePath, string destPath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException(sourcePath);

        File.Copy(sourcePath, destPath, overwrite: true);

        using var workbook = new XLWorkbook(destPath);
        var worksheet = workbook.Worksheets.First();

        int? dataRow = FindRow(worksheet, "{{#ds.rows}}");
        if (dataRow == null)
            throw new InvalidOperationException("Template must contain {{#ds.rows}}.");

        int? endRow = FindRow(worksheet, "{{/ds.rows}}");
        if (endRow.HasValue && endRow.Value > dataRow.Value)
            worksheet.Row(endRow.Value).Delete();

        foreach (var cell in worksheet.CellsUsed())
        {
            if (cell.Address.RowNumber == dataRow.Value)
                continue;

            var text = GetCellText(cell);
            if (string.IsNullOrEmpty(text))
                continue;

            var converted = ConvertHeaderTags(text);
            if (!string.Equals(text, converted, StringComparison.Ordinal))
                SetCellText(cell, converted);
        }

        var rowTags = new Dictionary<int, string>();
        foreach (var cell in worksheet.Row(dataRow.Value).CellsUsed())
        {
            var text = GetCellText(cell);
            if (string.IsNullOrEmpty(text))
                continue;

            if (text.Contains("{{#ds.rows}}", StringComparison.Ordinal))
            {
                SetCellText(cell, string.Empty);
                continue;
            }

            var converted = ConvertDataRowTags(text);
            if (!string.IsNullOrEmpty(converted))
            {
                SetCellText(cell, converted);
                rowTags[cell.Address.ColumnNumber] = converted;
            }
        }

        worksheet.Row(dataRow.Value).InsertRowsBelow(1);
        var endMarkerRow = dataRow.Value + 1;
        foreach (var (col, firstRowTag) in rowTags)
        {
            var endTag = firstRowTag.Replace("[i]", "[i+1]", StringComparison.Ordinal);
            SetCellText(worksheet.Cell(endMarkerRow, col), endTag);
        }

        EnsureLibreOfficeConverterOption(worksheet);
        workbook.Save();
    }

    private static void EnsureLibreOfficeConverterOption(IXLWorksheet worksheet)
    {
        const int optionCol = 26;
        var optionCell = worksheet.Cell(1, optionCol);
        var existing = GetCellText(optionCell);
        if (!existing.Contains("{o.converter", StringComparison.Ordinal))
            SetCellText(optionCell, "{o.converter=L}");
    }

    private static int? FindRow(IXLWorksheet worksheet, string token)
    {
        foreach (var cell in worksheet.CellsUsed())
        {
            if (GetCellText(cell).Contains(token, StringComparison.Ordinal))
                return cell.Address.RowNumber;
        }

        return null;
    }

    private static string ConvertHeaderTags(string text)
    {
        text = NormalizeCellTagText(text);
        return DsHeaderTag.Replace(text, m => $"{{d.{m.Groups[1].Value}}}");
    }

    private static string ConvertDataRowTags(string text)
    {
        text = NormalizeCellTagText(text);
        if (DotRowTag.IsMatch(text))
            return DotRowTag.Replace(text, m => $"{{d.rows[i].{m.Groups[1].Value}}}");

        if (DsRowsTag.IsMatch(text))
            return DsRowsTag.Replace(text, m => $"{{d.rows[i].{m.Groups[1].Value}}}");

        return ConvertHeaderTags(text);
    }

    /// <summary>Strip Excel padding before placeholders so Carbone can parse tags.</summary>
    private static string NormalizeCellTagText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Legacy ministry cells often prefix tags with tab/newline for layout.
        text = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        while (text.StartsWith("\t", StringComparison.Ordinal) || text.StartsWith("\n", StringComparison.Ordinal))
            text = text[1..];

        return text;
    }

    private static string GetCellText(IXLCell cell) =>
        cell.GetFormattedString();

    private static void SetCellText(IXLCell cell, string text) =>
        cell.Value = text;
}
