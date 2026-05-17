using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.ExcelReports;

/// <inheritdoc cref="IExcelReportGenerator"/>
public class ExcelReportGenerator : IExcelReportGenerator
{
    private readonly IExcelTemplatePlaceholderExtractor _placeholderExtractor;

    public ExcelReportGenerator(IExcelTemplatePlaceholderExtractor placeholderExtractor)
    {
        _placeholderExtractor = placeholderExtractor;
    }

    public Task GenerateAsync(
        UserReportTemplate template,
        Application application,
        Stream outputStream,
        IList<ApplicationItem>? applicationItems = null)
    {
        if (template.ExcelMergeMode != ExcelMergeMode.ItemList)
            throw new NotSupportedException("Excel list generation requires ExcelMergeMode.ItemList.");

        var content = template.TemplateFile.Content;
        if (content == null || content.Length == 0)
            throw new InvalidOperationException("User report template file has no content.");

        using var templateStream = new MemoryStream(content, 0, content.Length, writable: false, publiclyVisible: true);
        using var workbook = new XLWorkbook(templateStream);
        var worksheet = workbook.Worksheets.First();

        var headerData = BuildHeaderData(template, application);
        var items = applicationItems != null && applicationItems.Count > 0
            ? applicationItems.Where(i => i != null && !i.IsDeleted).ToList()
            : UserReportMergeDataHelper.GetActiveApplicationItems(application);

        int? templateRowNumber = FindRowContainingToken(worksheet, "{{#ds.rows}}");
        int? endRowNumber = FindRowContainingToken(worksheet, "{{/ds.rows}}");

        if (templateRowNumber == null)
            throw new InvalidOperationException("Excel list template must contain a row with {{#ds.rows}}.");

        if (endRowNumber.HasValue && endRowNumber.Value > templateRowNumber.Value)
            worksheet.Row(endRowNumber.Value).Delete();

        foreach (var cell in worksheet.CellsUsed())
        {
            if (cell.Address.RowNumber == templateRowNumber.Value)
                continue;

            MergeCellText(cell, headerData, rowData: null, template: null, item: null);
        }

        var templateRowIndex = templateRowNumber.Value;
        var templateRow = worksheet.Row(templateRowIndex);

        // Snapshot before insert: InsertRowsBelow shifts every row below the template (a sheet scratch row would survive as row 23+).
        var prototypeRow = CaptureRowSnapshot(worksheet, templateRowIndex);

        for (int i = items.Count - 1; i >= 1; i--)
            templateRow.InsertRowsBelow(1);

        for (int i = 0; i < items.Count; i++)
        {
            var row = worksheet.Row(templateRowIndex + i);
            if (i > 0)
                ApplyRowSnapshot(row, prototypeRow);

            var rowData = UserReportMergeDataHelper.BuildExcelItemListRowDictionary(items[i], i + 1);
            MergeRow(worksheet, row, headerData, rowData, template, items[i]);
        }

        if (items.Count == 0)
            MergeRow(worksheet, templateRow, headerData, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), template, items.FirstOrDefault());

        workbook.SaveAs(outputStream);
        return Task.CompletedTask;
    }

    public Task GenerateAsync(UserReportTemplate template, ApplicationItem applicationItem, Stream outputStream)
    {
        throw new NotSupportedException("Single-item Excel generation is planned for v1.1.");
    }

    private Dictionary<string, object> BuildHeaderData(UserReportTemplate template, Application application)
    {
        var data = UserReportMergeDataHelper.BuildApplicationHeaderDictionary(application);

        foreach (var placeholder in template.Placeholders.Where(p => p.IsValid && !p.IsRowProperty && !p.IsCollection))
        {
            var bindKey = UserReportMergeDataHelper.StripDocxModelPrefix(placeholder.PlaceholderKey.TrimStart('#', '/'));
            if (bindKey.StartsWith("rows.", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = UserReportMergeDataHelper.GetPropertyValue(application, placeholder.ResolvedPropertyPath);
            data[bindKey] = value ?? string.Empty;
        }

        return data;
    }

    private static int? FindRowContainingToken(IXLWorksheet worksheet, string token)
    {
        foreach (var cell in worksheet.CellsUsed())
        {
            if (cell.GetFormattedString().Contains(token, StringComparison.Ordinal))
                return cell.Address.RowNumber;
        }

        return null;
    }

    private static void MergeRow(
        IXLWorksheet worksheet,
        IXLRow row,
        IReadOnlyDictionary<string, object> headerData,
        IReadOnlyDictionary<string, object> rowData,
        UserReportTemplate template,
        ApplicationItem? item)
    {
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber()
            ?? row.LastCellUsed()?.Address.ColumnNumber
            ?? 1;

        for (int column = 1; column <= lastColumn; column++)
            MergeCellText(row.Cell(column), headerData, rowData, template, item);
    }

    private static void MergeCellText(
        IXLCell cell,
        IReadOnlyDictionary<string, object> headerData,
        IReadOnlyDictionary<string, object>? rowData,
        UserReportTemplate? template,
        ApplicationItem? item = null)
    {
        var text = cell.GetFormattedString();
        if (string.IsNullOrEmpty(text) || !text.Contains("{{", StringComparison.Ordinal))
            return;

        text = ReplaceTokens(text, token =>
        {
            if (token.StartsWith("/", StringComparison.Ordinal)
                || string.Equals(token, "#ds.rows", StringComparison.OrdinalIgnoreCase)
                || string.Equals(token, "ds.rows", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (token.StartsWith(".", StringComparison.Ordinal))
            {
                var key = token.TrimStart('.');
                if (TryResolveRowToken(key, rowData, item) is { } dotResolved)
                    return dotResolved;

                return string.Empty;
            }

            var bindKey = UserReportMergeDataHelper.StripDocxModelPrefix(token);
            if (bindKey.StartsWith("rows.", StringComparison.OrdinalIgnoreCase) && bindKey.Length > 5)
            {
                var rowKey = bindKey.Substring(5);
                if (TryResolveRowToken(rowKey, rowData, item) is { } rowResolved)
                    return rowResolved;
            }

            if (string.Equals(bindKey, "rows", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            if (headerData.TryGetValue(bindKey, out var headerValue))
                return FormatValue(headerValue);

            if (item != null)
                return FormatValue(UserReportMergeDataHelper.GetPropertyValue(item, bindKey));

            return string.Empty;
        });

        cell.Value = text;
    }

    private static string ReplaceTokens(string text, Func<string, string> resolve)
    {
        return UserReportPlaceholderPatterns.PlaceholderRegex.Replace(
            text,
            match =>
            {
                var token = match.Groups[1].Value;
                return resolve(token);
            });
    }

    private static string? TryResolveRowToken(
        string key,
        IReadOnlyDictionary<string, object>? rowData,
        ApplicationItem? item)
    {
        if (rowData != null && rowData.TryGetValue(key, out var rowValue))
            return FormatValue(rowValue);

        if (item != null)
            return FormatValue(UserReportMergeDataHelper.GetPropertyValue(item, key));

        return null;
    }

    private static string FormatValue(object? value) => value?.ToString() ?? string.Empty;

    private static Dictionary<int, string> CaptureRowSnapshot(IXLWorksheet worksheet, int rowNumber)
    {
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber()
            ?? worksheet.Row(rowNumber).LastCellUsed()?.Address.ColumnNumber
            ?? 1;

        var cells = new Dictionary<int, string>();
        for (int column = 1; column <= lastColumn; column++)
        {
            var text = worksheet.Cell(rowNumber, column).GetFormattedString();
            if (!string.IsNullOrEmpty(text))
                cells[column] = text;
        }

        return cells;
    }

    private static void ApplyRowSnapshot(IXLRow row, IReadOnlyDictionary<int, string> prototypeCells)
    {
        foreach (var (column, text) in prototypeCells)
            row.Cell(column).Value = text;
    }
}
