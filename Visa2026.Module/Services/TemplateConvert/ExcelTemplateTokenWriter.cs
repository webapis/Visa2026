using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Writes placeholder tokens into an existing <c>.xlsx</c>. Replaces cell values and clears yellow
/// fill on written cells; never changes number formats, widths, merges, or formulas.
/// </summary>
internal static class ExcelTemplateTokenWriter
{
    /// <summary>
    /// Clears solid yellow (and yellowish) cell fills from the workbook after Create-from-yellow-marks.
    /// Officer highlighter is often indexed/theme or on a merged non-anchor; those used to survive
    /// catalog Preview as a bright cell on filled people rows.
    /// </summary>
    public static byte[] StripAllYellowFills(byte[] sourceContent)
    {
        using var input = new MemoryStream(sourceContent, writable: false);
        using var workbook = new XLWorkbook(input);

        foreach (var sheet in workbook.Worksheets)
        {
            foreach (var cell in sheet.CellsUsed(XLCellsUsedOptions.All))
                ClearIfYellow(cell.Style.Fill);

            foreach (var merged in sheet.MergedRanges)
            {
                foreach (var cell in merged.Cells())
                    ClearIfYellow(cell.Style.Fill);
            }

            foreach (var row in sheet.RowsUsed(XLCellsUsedOptions.All))
                ClearIfYellow(row.Style.Fill);

            foreach (var column in sheet.ColumnsUsed(XLCellsUsedOptions.All))
                ClearIfYellow(column.Style.Fill);
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return NeutralizeYellowStyleFills(output.ToArray());
    }

    internal static void ClearIfYellow(IXLFill fill)
    {
        if (fill.PatternType is XLFillPatternValues.None or XLFillPatternValues.Gray125)
            return;

        if (!IsYellowishXlColor(fill.BackgroundColor) && !IsYellowishXlColor(fill.PatternColor))
            return;

        fill.PatternType = XLFillPatternValues.None;
        fill.BackgroundColor = XLColor.NoColor;
    }

    internal static bool IsYellowishXlColor(XLColor color)
    {
        try
        {
            if (color.ColorType == XLColorType.Indexed)
            {
                var index = color.Indexed;
                if (index is 5 or 13 or 43 or 51)
                    return true;
            }

            var c = color.Color;
            return IsHighlighterYellowRgb(c.R, c.G, c.B);
        }
        catch
        {
            return false;
        }
    }

    internal static bool IsHighlighterYellowRgb(int r, int g, int b) =>
        r >= 180 && g >= 160 && ((r + g) / 2.0 - b) >= 35 && b <= 210;

    /// <summary>
    /// Shared <c>xf</c> fills in <c>xl/styles.xml</c> stay yellow even when one cell was cleared.
    /// Neutralize yellowish pattern fills so leftover style indexes cannot paint Preview.
    /// </summary>
    private static byte[] NeutralizeYellowStyleFills(byte[] xlsx)
    {
        using var buffer = new MemoryStream();
        buffer.Write(xlsx, 0, xlsx.Length);
        buffer.Position = 0;

        using (var document = SpreadsheetDocument.Open(buffer, true))
        {
            var stylesheet = document.WorkbookPart?.WorkbookStylesPart?.Stylesheet;
            var fills = stylesheet?.Fills;
            if (fills == null)
                return xlsx;

            var changed = false;
            foreach (var fill in fills.Elements<Fill>())
            {
                var pattern = fill.PatternFill;
                if (pattern == null)
                    continue;
                if (!IsYellowSpreadsheetColor(pattern.ForegroundColor)
                    && !IsYellowSpreadsheetColor(pattern.BackgroundColor))
                    continue;

                pattern.PatternType = PatternValues.None;
                pattern.ForegroundColor = null;
                pattern.BackgroundColor = null;
                changed = true;
            }

            if (!changed)
                return xlsx;

            stylesheet!.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    private static bool IsYellowSpreadsheetColor(ColorType? color)
    {
        if (color == null)
            return false;

        if (color.Rgb?.Value is { Length: > 0 } rgb && IsYellowishHex(rgb))
            return true;

        if (color.Indexed?.Value is uint indexed && indexed is 5 or 13 or 43 or 51)
            return true;

        return false;
    }

    private static bool IsYellowishHex(string hex)
    {
        hex = hex.Trim().TrimStart('#');
        if (hex.Length == 8)
            hex = hex[^6..];
        if (hex.Length != 6)
            return false;
        if (!int.TryParse(hex[0..2], System.Globalization.NumberStyles.HexNumber, null, out var r)
            || !int.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g)
            || !int.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b))
            return false;

        return IsHighlighterYellowRgb(r, g, b);
    }

    public static TokenWriteResult Write(
        byte[] sourceContent,
        IReadOnlyList<TokenSubstitution> substitutions,
        IReadOnlyList<LoopMarker> loops)
    {
        var applied = new List<TokenSubstitution>();
        var appliedLoops = new List<LoopMarker>();
        var skipped = new List<TemplateWriteSkip>();

        using var input = new MemoryStream(sourceContent, writable: false);
        using var workbook = new XLWorkbook(input);

        foreach (var substitution in substitutions)
        {
            if (substitution.Region is not DocumentRegion.ExcelCell cell)
            {
                skipped.Add(new TemplateWriteSkip(substitution.Region, substitution.Token, "Region is not an Excel cell."));
                continue;
            }

            if (TryWriteCell(
                    workbook,
                    cell,
                    substitution.Token.Contains("{{", StringComparison.Ordinal)
                        ? substitution.Token
                        : TemplateTokenSyntax.Wrap(substitution.Token),
                    out var reason))
                applied.Add(substitution);
            else
                skipped.Add(new TemplateWriteSkip(substitution.Region, substitution.Token, reason));
        }

        foreach (var loop in loops)
        {
            if (loop.Start is not DocumentRegion.ExcelCell start || loop.End is not DocumentRegion.ExcelCell end)
            {
                skipped.Add(new TemplateWriteSkip(loop.Start, loop.CollectionToken, "Loop boundaries are not Excel cells."));
                continue;
            }

            var open = TemplateTokenSyntax.LoopOpen(loop.CollectionToken);
            if (!TryWriteLoopOpenCell(workbook, start, open, out var startReason))
            {
                skipped.Add(new TemplateWriteSkip(loop.Start, loop.CollectionToken, startReason));
                continue;
            }

            var close = TemplateTokenSyntax.LoopClose(loop.CollectionToken);
            if (!TryWriteLoopCloseCell(workbook, end, close, out var endReason))
            {
                // Open already written; close is optional for ExcelReportGenerator.
                skipped.Add(new TemplateWriteSkip(loop.End, loop.CollectionToken, endReason));
            }

            appliedLoops.Add(loop);
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return new TokenWriteResult(output.ToArray(), applied, appliedLoops, skipped);
    }

    /// <summary>
    /// Writes <c>{{#ds.rows}}</c>, prepending when the cell already holds a row token (e.g. <c>{{.RNUM}}</c> in A).
    /// </summary>
    private static bool TryWriteLoopOpenCell(
        XLWorkbook workbook,
        DocumentRegion.ExcelCell region,
        string openToken,
        out string reason)
    {
        reason = string.Empty;

        if (!TryGetWritableCell(workbook, region, out var cell, out reason))
            return false;

        var existing = cell.GetFormattedString();
        if (string.IsNullOrWhiteSpace(existing))
            cell.Value = openToken;
        else if (existing.Contains(openToken, StringComparison.Ordinal))
            cell.Value = existing;
        else
            cell.Value = openToken + existing;

        cell.Style.Fill.PatternType = XLFillPatternValues.None;
        return true;
    }

    /// <summary>
    /// Writes <c>{{/ds.rows}}</c> into an empty cell on the close row — never overwrite sample/footer text.
    /// </summary>
    private static bool TryWriteLoopCloseCell(
        XLWorkbook workbook,
        DocumentRegion.ExcelCell region,
        string closeToken,
        out string reason)
    {
        reason = string.Empty;

        if (!TryGetWritableCell(workbook, region, out var preferred, out reason))
            return false;

        if (TrySetCloseToken(preferred, closeToken))
            return true;

        var worksheet = preferred.Worksheet;
        var row = preferred.Address.RowNumber;
        for (var column = 1; column <= 26; column++)
        {
            var candidate = worksheet.Cell(row, column);
            if (!IsWritableCell(candidate, out _))
                continue;
            if (!TrySetCloseToken(candidate, closeToken))
                continue;

            return true;
        }

        reason = "Close-row cell already has content; {{/ds.rows}} is optional.";
        return false;
    }

    private static bool TrySetCloseToken(IXLCell cell, string closeToken)
    {
        var existing = cell.GetFormattedString();
        if (!string.IsNullOrWhiteSpace(existing)
            && !existing.Contains(closeToken, StringComparison.Ordinal))
            return false;

        cell.Value = closeToken;
        cell.Style.Fill.PatternType = XLFillPatternValues.None;
        return true;
    }

    private static bool TryWriteCell(XLWorkbook workbook, DocumentRegion.ExcelCell region, string value, out string reason)
    {
        if (!TryGetWritableCell(workbook, region, out var cell, out reason))
            return false;

        cell.Value = value;
        cell.Style.Fill.PatternType = XLFillPatternValues.None;
        return true;
    }

    private static bool TryGetWritableCell(
        XLWorkbook workbook,
        DocumentRegion.ExcelCell region,
        out IXLCell cell,
        out string reason)
    {
        reason = string.Empty;
        cell = null!;

        var worksheet = workbook.Worksheets
            .FirstOrDefault(w => string.Equals(w.Name, region.SheetName, StringComparison.OrdinalIgnoreCase));
        if (worksheet == null)
        {
            reason = $"Sheet '{region.SheetName}' not found.";
            return false;
        }

        try
        {
            cell = worksheet.Cell(region.CellReference);
        }
        catch (ArgumentException)
        {
            reason = $"Invalid cell reference '{region.CellReference}'.";
            return false;
        }

        return IsWritableCell(cell, out reason);
    }

    private static bool IsWritableCell(IXLCell cell, out string reason)
    {
        reason = string.Empty;

        if (!string.IsNullOrEmpty(cell.FormulaA1))
        {
            reason = "Cell holds a formula.";
            return false;
        }

        if (cell.IsMerged())
        {
            var anchor = cell.MergedRange().FirstCell();
            if (!string.Equals(anchor.Address.ToStringRelative(), cell.Address.ToStringRelative(), StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Cell is a non-anchor member of merged range '{cell.MergedRange().RangeAddress}'.";
                return false;
            }
        }

        return true;
    }
}
