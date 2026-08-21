using ClosedXML.Excel;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Derives <c>{{#ds.rows}}</c> / <c>{{/ds.rows}}</c> markers and first-row substitutions from an
/// E5 candidate report. The writer already accepts loops; this is what unblocks roster Convert.
/// </summary>
internal static class TemplateRosterLoopPlanner
{
    public const string RowsCollectionToken = "ds.rows";

    public static TemplateMappingPlan Build(TemplateCandidateReport candidate, TemplateSourceFormat format)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var matches = candidate.Highlights
            .Where(static h => h.Kind == HighlightKind.Match && !string.IsNullOrWhiteSpace(h.Token))
            .ToList();

        var headerSubs = matches
            .Where(static h => h.RowIndex == null)
            .Select(static h => new TokenSubstitution(h.Region, h.Token!))
            .ToList();

        var rowMatches = matches
            .Where(static h => h.RowIndex != null)
            .ToList();

        var gaps = candidate.Highlights
            .Where(static h => h.Kind == HighlightKind.Gap)
            .Select(static h => new MappingGap(h.MatchedText, SuggestedPropertyName: null, h.Region))
            .ToList();

        if (!candidate.RosterLoopDetected || rowMatches.Count == 0)
            return new TemplateMappingPlan(headerSubs, Array.Empty<LoopMarker>(), gaps, Rationale: null);

        var firstRowIndex = rowMatches.Min(static h => h.RowIndex!.Value);
        var firstRowSubs = rowMatches
            .Where(h => h.RowIndex == firstRowIndex)
            .Select(h => new TokenSubstitution(h.Region, h.Token!))
            .ToList();

        var substitutions = headerSubs.Concat(firstRowSubs).ToList();

        if (!TryBuildLoop(format, rowMatches, firstRowIndex, substitutions, out var loop))
            return new TemplateMappingPlan(substitutions, Array.Empty<LoopMarker>(), gaps, Rationale: null);

        return new TemplateMappingPlan(substitutions, new[] { loop }, gaps, Rationale: null);
    }

    private static bool TryBuildLoop(
        TemplateSourceFormat format,
        IReadOnlyList<HighlightRegion> rowMatches,
        int firstRowIndex,
        IReadOnlyList<TokenSubstitution> substitutions,
        out LoopMarker loop)
    {
        loop = null!;
        return format switch
        {
            TemplateSourceFormat.Xlsx => TryBuildExcelLoop(rowMatches, firstRowIndex, substitutions, out loop),
            TemplateSourceFormat.Docx => TryBuildWordLoop(rowMatches, firstRowIndex, out loop),
            _ => false,
        };
    }

    private static bool TryBuildWordLoop(
        IReadOnlyList<HighlightRegion> rowMatches,
        int firstRowIndex,
        out LoopMarker loop)
    {
        loop = null!;

        var wordMatches = rowMatches
            .Where(static h => h.Region is DocumentRegion.WordSpan)
            .ToList();
        if (wordMatches.Count == 0)
            return false;

        var lastRowIndex = wordMatches.Max(static h => h.RowIndex!.Value);
        var start = wordMatches.First(h => h.RowIndex == firstRowIndex).Region as DocumentRegion.WordSpan;
        var end = wordMatches.Last(h => h.RowIndex == lastRowIndex).Region as DocumentRegion.WordSpan;
        if (start == null || end == null)
            return false;

        // Writer only needs paragraph addresses; Start/Length are ignored for loop markers.
        loop = new LoopMarker(
            new DocumentRegion.WordSpan(start.ParagraphAddress, 0, 0),
            new DocumentRegion.WordSpan(end.ParagraphAddress, 0, 0),
            RowsCollectionToken);
        return true;
    }

    private static bool TryBuildExcelLoop(
        IReadOnlyList<HighlightRegion> rowMatches,
        int firstRowIndex,
        IReadOnlyList<TokenSubstitution> substitutions,
        out LoopMarker loop)
    {
        loop = null!;

        var excelMatches = rowMatches
            .Select(static h => (Highlight: h, Cell: h.Region as DocumentRegion.ExcelCell))
            .Where(static x => x.Cell != null)
            .Select(static x => (x.Highlight, Cell: x.Cell!))
            .ToList();
        if (excelMatches.Count == 0)
            return false;

        var sheetGroup = excelMatches
            .GroupBy(static x => x.Cell.SheetName, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static g => g.Count())
            .First();

        var parsed = new List<(DocumentRegion.ExcelCell Cell, int Column, int Row, int RowIndex)>();
        foreach (var item in sheetGroup)
        {
            if (!TryParseCellReference(item.Cell.CellReference, out var column, out var row))
                continue;

            parsed.Add((item.Cell, column, row, item.Highlight.RowIndex!.Value));
        }

        if (parsed.Count == 0)
            return false;

        var firstRowCells = parsed.Where(p => p.RowIndex == firstRowIndex).ToList();
        if (firstRowCells.Count == 0)
            return false;

        // ExcelReportGenerator treats the {{#ds.rows}} row as the prototype and deletes the
        // {{/ds.rows}} row when it sits below. Place the close on the next physical row.
        var templateRow = firstRowCells.Min(static p => p.Row);
        var endRow = templateRow + 1;
        var markerColumn = parsed.Max(static p => p.Column) + 1;

        var occupied = substitutions
            .Select(static s => s.Region as DocumentRegion.ExcelCell)
            .Where(static c => c != null)
            .Select(static c => Key(c!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < 26; attempt++)
        {
            var column = markerColumn + attempt;
            if (column > 100)
                return false;

            var startRef = XLHelper.GetColumnLetterFromNumber(column) + templateRow;
            var endRef = XLHelper.GetColumnLetterFromNumber(column) + endRow;
            var startCell = new DocumentRegion.ExcelCell(sheetGroup.Key, startRef);
            var endCell = new DocumentRegion.ExcelCell(sheetGroup.Key, endRef);

            if (occupied.Contains(Key(startCell)) || occupied.Contains(Key(endCell)))
                continue;

            loop = new LoopMarker(startCell, endCell, RowsCollectionToken);
            return true;
        }

        return false;
    }

    private static string Key(DocumentRegion.ExcelCell cell) =>
        $"{cell.SheetName}!{cell.CellReference}";

    internal static bool TryParseCellReference(string reference, out int column, out int row)
    {
        column = 0;
        row = 0;
        if (string.IsNullOrWhiteSpace(reference))
            return false;

        var i = 0;
        while (i < reference.Length && char.IsLetter(reference[i]))
            i++;

        if (i == 0 || i == reference.Length)
            return false;

        var letters = reference[..i];
        if (!int.TryParse(reference[i..], out row) || row <= 0)
            return false;

        try
        {
            column = XLHelper.GetColumnNumberFromLetter(letters);
            return column > 0;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}