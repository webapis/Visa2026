using ClosedXML.Excel;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Writes placeholder tokens into an existing <c>.xlsx</c>. Cell values only — never number formats,
/// styles, widths, merges, or formulas.
/// </summary>
internal static class ExcelTemplateTokenWriter
{
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

            if (TryWriteCell(workbook, cell, TemplateTokenSyntax.Wrap(substitution.Token), out var reason))
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

            if (!TryWriteCell(workbook, start, TemplateTokenSyntax.LoopOpen(loop.CollectionToken), out var startReason))
            {
                skipped.Add(new TemplateWriteSkip(loop.Start, loop.CollectionToken, startReason));
                continue;
            }

            if (!TryWriteCell(workbook, end, TemplateTokenSyntax.LoopClose(loop.CollectionToken), out var endReason))
            {
                skipped.Add(new TemplateWriteSkip(loop.End, loop.CollectionToken, endReason));
                continue;
            }

            appliedLoops.Add(loop);
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return new TokenWriteResult(output.ToArray(), applied, appliedLoops, skipped);
    }

    private static bool TryWriteCell(XLWorkbook workbook, DocumentRegion.ExcelCell region, string value, out string reason)
    {
        reason = string.Empty;

        var worksheet = workbook.Worksheets
            .FirstOrDefault(w => string.Equals(w.Name, region.SheetName, StringComparison.OrdinalIgnoreCase));
        if (worksheet == null)
        {
            reason = $"Sheet '{region.SheetName}' not found.";
            return false;
        }

        IXLCell cell;
        try
        {
            cell = worksheet.Cell(region.CellReference);
        }
        catch (ArgumentException)
        {
            reason = $"Invalid cell reference '{region.CellReference}'.";
            return false;
        }

        if (!string.IsNullOrEmpty(cell.FormulaA1))
        {
            reason = "Cell holds a formula.";
            return false;
        }

        // A merged range keeps its content in the anchor cell; writing to any other member is a no-op.
        if (cell.IsMerged())
        {
            var anchor = cell.MergedRange().FirstCell();
            if (!string.Equals(anchor.Address.ToStringRelative(), cell.Address.ToStringRelative(), StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Cell is a non-anchor member of merged range '{cell.MergedRange().RangeAddress}'.";
                return false;
            }
        }

        cell.Value = value;
        return true;
    }
}
