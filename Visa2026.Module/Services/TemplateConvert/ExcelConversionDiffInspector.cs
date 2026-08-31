using ClosedXML.Excel;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Proves a converted <c>.xlsx</c> differs from its source only by the approved token substitutions:
/// same sheets, merges, widths, number formats, and formulas.
/// </summary>
internal static class ExcelConversionDiffInspector
{
    public static void Inspect(TemplateDiffGateRequest request, List<string> violations)
    {
        using var originalStream = new MemoryStream(request.OriginalContent, writable: false);
        using var convertedStream = new MemoryStream(request.ConvertedContent, writable: false);
        using var original = new XLWorkbook(originalStream);
        using var converted = new XLWorkbook(convertedStream);

        var originalSheets = original.Worksheets.Select(static w => w.Name).ToList();
        var convertedSheets = converted.Worksheets.Select(static w => w.Name).ToList();

        if (!originalSheets.SequenceEqual(convertedSheets, StringComparer.Ordinal))
        {
            violations.Add("Worksheet names or order changed.");
            return;
        }

        var expectations = ExcelTextExpectation.Build(request);
        var loopColumnsBySheet = BuildLoopMarkerColumns(request);

        foreach (var sheetName in originalSheets)
        {
            var left = original.Worksheet(sheetName);
            var right = converted.Worksheet(sheetName);

            CompareMergedRanges(sheetName, left, right, violations);
            loopColumnsBySheet.TryGetValue(sheetName, out var loopColumns);
            CompareColumnWidths(sheetName, left, right, violations, loopColumns);
            CompareCells(sheetName, left, right, expectations, violations);
        }
    }

    private static Dictionary<string, HashSet<int>> BuildLoopMarkerColumns(TemplateDiffGateRequest request)
    {
        var map = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var loop in request.Loops)
        {
            AddLoopColumn(map, loop.Start);
            AddLoopColumn(map, loop.End);
        }

        return map;

        static void AddLoopColumn(Dictionary<string, HashSet<int>> columnsBySheet, DocumentRegion region)
        {
            if (region is not DocumentRegion.ExcelCell cell)
                return;

            if (!TemplateRosterLoopPlanner.TryParseCellReference(cell.CellReference, out var column, out _))
                return;

            if (!columnsBySheet.TryGetValue(cell.SheetName, out var set))
            {
                set = new HashSet<int>();
                columnsBySheet[cell.SheetName] = set;
            }

            set.Add(column);
        }
    }

    private static void CompareMergedRanges(string sheetName, IXLWorksheet left, IXLWorksheet right, List<string> violations)
    {
        var leftRanges = left.MergedRanges.Select(static r => r.RangeAddress.ToStringRelative()).OrderBy(static a => a, StringComparer.Ordinal).ToList();
        var rightRanges = right.MergedRanges.Select(static r => r.RangeAddress.ToStringRelative()).OrderBy(static a => a, StringComparer.Ordinal).ToList();

        if (!leftRanges.SequenceEqual(rightRanges, StringComparer.Ordinal))
            violations.Add($"Merged ranges changed on sheet '{sheetName}'.");
    }

    private static void CompareColumnWidths(
        string sheetName,
        IXLWorksheet left,
        IXLWorksheet right,
        List<string> violations,
        HashSet<int>? loopMarkerColumns = null)
    {
        var leftWidths = left.ColumnsUsed().ToDictionary(static c => c.ColumnNumber(), static c => Math.Round(c.Width, 3));
        var rightWidths = right.ColumnsUsed().ToDictionary(static c => c.ColumnNumber(), static c => Math.Round(c.Width, 3));

        foreach (var column in leftWidths.Keys.Union(rightWidths.Keys))
        {
            if (loopMarkerColumns != null
                && loopMarkerColumns.Contains(column)
                && !leftWidths.ContainsKey(column))
            {
                continue;
            }

            leftWidths.TryGetValue(column, out var leftWidth);
            rightWidths.TryGetValue(column, out var rightWidth);
            if (Math.Abs(leftWidth - rightWidth) > 0.001)
            {
                violations.Add($"Column width changed on sheet '{sheetName}' column {column}.");
                return;
            }
        }
    }

    private static void CompareCells(
        string sheetName,
        IXLWorksheet left,
        IXLWorksheet right,
        ExcelTextExpectation expectations,
        List<string> violations)
    {
        var leftCells = left.CellsUsed().ToDictionary(static c => c.Address.ToStringRelative(), static c => c, StringComparer.OrdinalIgnoreCase);
        var rightCells = right.CellsUsed().ToDictionary(static c => c.Address.ToStringRelative(), static c => c, StringComparer.OrdinalIgnoreCase);

        foreach (var address in leftCells.Keys.Union(rightCells.Keys, StringComparer.OrdinalIgnoreCase))
        {
            leftCells.TryGetValue(address, out var leftCell);
            rightCells.TryGetValue(address, out var rightCell);

            var leftFormula = leftCell?.FormulaA1 ?? string.Empty;
            var rightFormula = rightCell?.FormulaA1 ?? string.Empty;
            if (!string.Equals(leftFormula, rightFormula, StringComparison.Ordinal))
            {
                violations.Add($"Formula changed at '{sheetName}'!{address}.");
                continue;
            }

            var leftFormat = leftCell?.Style.NumberFormat.Format ?? string.Empty;
            var rightFormat = rightCell?.Style.NumberFormat.Format ?? string.Empty;
            var leftFormatId = leftCell?.Style.NumberFormat.NumberFormatId ?? 0;
            var rightFormatId = rightCell?.Style.NumberFormat.NumberFormatId ?? 0;
            if (!string.Equals(leftFormat, rightFormat, StringComparison.Ordinal) || leftFormatId != rightFormatId)
            {
                violations.Add($"Number format changed at '{sheetName}'!{address}.");
                continue;
            }

            var originalText = leftCell?.GetFormattedString() ?? string.Empty;
            var expected = expectations.Expect(sheetName, address, originalText);
            var actual = rightCell?.GetFormattedString() ?? string.Empty;

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                violations.Add($"Value at '{sheetName}'!{address} does not match the approved substitutions.");
        }
    }
}

internal sealed class ExcelTextExpectation
{
    private readonly Dictionary<string, string> _replacements;

    private ExcelTextExpectation(Dictionary<string, string> replacements) => _replacements = replacements;

    public static ExcelTextExpectation Build(TemplateDiffGateRequest request)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var substitution in request.Substitutions)
        {
            if (substitution.Region is DocumentRegion.ExcelCell cell)
                replacements[Key(cell)] = TemplateTokenSyntax.Wrap(substitution.Token);
        }

        foreach (var loop in request.Loops)
        {
            if (loop.Start is DocumentRegion.ExcelCell start)
            {
                var open = TemplateTokenSyntax.LoopOpen(loop.CollectionToken);
                var startKey = Key(start);
                // Same cell as a row token (e.g. A5 {{#ds.rows}}{{.RNUM}}) — writer prepends.
                if (replacements.TryGetValue(startKey, out var existing)
                    && !existing.Contains(open, StringComparison.Ordinal))
                    replacements[startKey] = open + existing;
                else if (!replacements.ContainsKey(startKey))
                    replacements[startKey] = open;
            }

            if (loop.End is DocumentRegion.ExcelCell end)
            {
                var close = TemplateTokenSyntax.LoopClose(loop.CollectionToken);
                var endKey = Key(end);
                if (replacements.TryGetValue(endKey, out var existing)
                    && existing.Contains(close, StringComparison.Ordinal))
                    continue;

                if (!replacements.ContainsKey(endKey))
                    replacements[endKey] = close;
            }
        }

        return new ExcelTextExpectation(replacements);
    }

    public string Expect(string sheetName, string address, string originalText) =>
        _replacements.TryGetValue($"{sheetName}!{address}", out var token) ? token : originalText;

    private static string Key(DocumentRegion.ExcelCell cell) => $"{cell.SheetName}!{cell.CellReference}";
}
