using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateScan;

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

        var occupied = substitutions
            .Select(static s => s.Region as DocumentRegion.ExcelCell)
            .Where(static c => c != null)
            .Select(static c => Key(c!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!TryPlaceExcelLoopMarker(sheetGroup.Key, templateRow, endRow, occupied, workbook: null, out loop))
            return false;

        return true;
    }

    private static bool TryPlaceExcelLoopMarker(
        string sheetName,
        int templateRow,
        int endRow,
        HashSet<string> occupied,
        XLWorkbook? workbook,
        out LoopMarker loop)
    {
        loop = null!;

        // Seeded sanaw (Sanaw_ckl_map.md §6): {{#ds.rows}} in column A on the template data row.
        // Yellow № often maps {{.RNUM}} into A — still use A; writer prepends the loop open.
        if (TryPlaceExcelLoopAtColumn(
                sheetName, templateRow, endRow, column: 1, allowOccupiedStart: true, occupied, workbook, out loop))
            return true;

        // A unusable (merged/formula): prepend onto the leftmost occupied data column on the row.
        for (var column = 2; column <= 26; column++)
        {
            var startKey = Key(new DocumentRegion.ExcelCell(
                sheetName,
                XLHelper.GetColumnLetterFromNumber(column) + templateRow));
            if (!occupied.Contains(startKey))
                continue;

            if (TryPlaceExcelLoopAtColumn(
                    sheetName, templateRow, endRow, column, allowOccupiedStart: true, occupied, workbook, out loop))
                return true;
        }

        // Last resort: empty unmerged column (avoid far-right T when A–N are full of tokens).
        for (var column = 2; column <= 26; column++)
        {
            if (TryPlaceExcelLoopAtColumn(
                    sheetName, templateRow, endRow, column, allowOccupiedStart: false, occupied, workbook, out loop))
                return true;
        }

        return false;
    }

    private static bool TryPlaceExcelLoopAtColumn(
        string sheetName,
        int templateRow,
        int endRow,
        int column,
        bool allowOccupiedStart,
        HashSet<string> occupied,
        XLWorkbook? workbook,
        out LoopMarker loop)
    {
        loop = null!;

        var startRef = XLHelper.GetColumnLetterFromNumber(column) + templateRow;
        var startCell = new DocumentRegion.ExcelCell(sheetName, startRef);
        if (!CanWriteLoopCell(workbook, sheetName, startRef))
            return false;

        if (!allowOccupiedStart && occupied.Contains(Key(startCell)))
            return false;

        if (!TryResolveLoopEndCell(
                sheetName,
                endRow,
                preferredColumn: column,
                occupied,
                workbook,
                out var endCell))
            return false;

        loop = new LoopMarker(startCell, endCell, RowsCollectionToken);
        return true;
    }

    private static bool TryResolveLoopEndCell(
        string sheetName,
        int endRow,
        int preferredColumn,
        HashSet<string> occupied,
        XLWorkbook? workbook,
        out DocumentRegion.ExcelCell endCell)
    {
        endCell = null!;

        var columns = new List<int> { preferredColumn };
        for (var column = 1; column <= 26; column++)
        {
            if (column != preferredColumn)
                columns.Add(column);
        }

        foreach (var column in columns)
        {
            var endRef = XLHelper.GetColumnLetterFromNumber(column) + endRow;
            var candidate = new DocumentRegion.ExcelCell(sheetName, endRef);
            if (occupied.Contains(Key(candidate)))
                continue;
            if (!CanWriteLoopCell(workbook, sheetName, endRef))
                continue;

            endCell = candidate;
            return true;
        }

        // Close marker is optional for ExcelReportGenerator — keep preferred cell; writer no-ops if busy.
        var fallbackRef = XLHelper.GetColumnLetterFromNumber(preferredColumn) + endRow;
        endCell = new DocumentRegion.ExcelCell(sheetName, fallbackRef);
        return workbook == null || CanWriteLoopCell(workbook, sheetName, fallbackRef);
    }

    private static bool CanWriteLoopCell(XLWorkbook? workbook, string sheetName, string cellReference)
    {
        if (workbook == null)
            return true;

        var worksheet = workbook.Worksheets
            .FirstOrDefault(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));
        if (worksheet == null)
            return false;

        IXLCell cell;
        try
        {
            cell = worksheet.Cell(cellReference);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(cell.FormulaA1))
            return false;

        return !cell.IsMerged();
    }

    internal static bool TryParseCellReference(string reference, out int column, out int row)
    {
        column = 0;
        row = 0;
        if (string.IsNullOrWhiteSpace(reference))
            return false;

        reference = reference.Replace("$", string.Empty, StringComparison.Ordinal);

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

    /// <summary>
    /// Create-from-yellow-marks Excel path: row tokens are already mapped — add ItemList loop markers
    /// so <see cref="ExcelReports.ExcelReportGenerator"/> can merge in Resminamalar preview.
    /// Pass <paramref name="workbookContent"/> so placement skips merged / formula cells.
    /// </summary>
    internal static IReadOnlyList<LoopMarker> PlanExcelLoopsFromSubstitutions(
        IReadOnlyList<TokenSubstitution> substitutions,
        byte[]? workbookContent = null)
    {
        ArgumentNullException.ThrowIfNull(substitutions);

        var excelRowSubs = substitutions
            .Select(s => (Sub: s, Cell: s.Region as DocumentRegion.ExcelCell))
            .Where(x => x.Cell != null && IsRowScopedSubstitutionToken(x.Sub.Token))
            .Select(x => (x.Sub, Cell: x.Cell!))
            .ToList();

        if (excelRowSubs.Count == 0)
            return Array.Empty<LoopMarker>();

        var sheetGroup = excelRowSubs
            .GroupBy(x => x.Cell.SheetName, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static g => g.Count())
            .First();

        var parsed = new List<(DocumentRegion.ExcelCell Cell, int Column, int Row)>();
        foreach (var item in sheetGroup)
        {
            if (!TryParseCellReference(item.Cell.CellReference, out var column, out var row))
                continue;

            parsed.Add((item.Cell, column, row));
        }

        if (parsed.Count == 0)
            return Array.Empty<LoopMarker>();

        var occupied = substitutions
            .Select(static s => s.Region as DocumentRegion.ExcelCell)
            .Where(static c => c != null)
            .Select(static c => Key(c!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        XLWorkbook? workbook = null;
        try
        {
            if (workbookContent is { Length: > 0 })
            {
                using var input = new MemoryStream(workbookContent, writable: false);
                workbook = new XLWorkbook(input);
            }

            var loops = new List<LoopMarker>();
            foreach (var templateRow in parsed.Select(static p => p.Row).Distinct().OrderBy(static r => r))
            {
                var rowTokens = sheetGroup
                    .Where(item =>
                        TryParseCellReference(item.Cell.CellReference, out _, out var row)
                        && row == templateRow)
                    .Select(item => item.Sub.Token);
                if (!IsRosterLoopRow(rowTokens))
                    continue;

                if (!TryPlaceExcelLoopMarker(
                        sheetGroup.Key,
                        templateRow,
                        endRow: templateRow + 1,
                        occupied,
                        workbook,
                        out var loop))
                    continue;

                loops.Add(loop);
            }

            return loops;
        }
        finally
        {
            workbook?.Dispose();
        }
    }

    /// <summary>
    /// Create-from-yellow-marks Word path: wrap the prototype table row that holds
    /// <c>{{.CODE}}</c> tokens with <c>{{#ds.rows}}</c> so Resminamalar lists every selected person.
    /// </summary>
    internal static IReadOnlyList<LoopMarker> PlanWordLoopsFromSubstitutions(
        IReadOnlyList<TokenSubstitution> substitutions,
        byte[]? documentContent = null)
    {
        ArgumentNullException.ThrowIfNull(substitutions);

        if (documentContent is not { Length: > 0 })
            return Array.Empty<LoopMarker>();

        var rowAddresses = substitutions
            .Select(static s => (Sub: s, Span: s.Region as DocumentRegion.WordSpan))
            .Where(static x => x.Span != null && IsRowScopedSubstitutionToken(x.Sub.Token))
            .Select(static x => x.Span!.ParagraphAddress)
            .ToHashSet(StringComparer.Ordinal);

        if (rowAddresses.Count == 0)
            return Array.Empty<LoopMarker>();

        return PlanWordTableRowLoop(documentContent, rowAddresses);
    }

    /// <summary>
    /// Merge-time fallback for already-saved Word sanaws that have row tokens but no loop markers.
    /// </summary>
    internal static byte[] EnsureWordTableRowsLoop(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0)
            return content;

        var loops = PlanWordTableRowLoop(content, rowParagraphAddresses: null);
        if (loops.Count == 0)
            return content;

        var written = WordTemplateTokenWriter.Write(
            content,
            Array.Empty<TokenSubstitution>(),
            loops);
        return written.Content;
    }

    private static IReadOnlyList<LoopMarker> PlanWordTableRowLoop(
        byte[] documentContent,
        HashSet<string>? rowParagraphAddresses)
    {
        byte[] loadable;
        try
        {
            loadable = WordOpenXmlPackage.EnsureLoadable(documentContent);
        }
        catch (InvalidDataException)
        {
            return Array.Empty<LoopMarker>();
        }
        catch (OpenXmlPackageException)
        {
            return Array.Empty<LoopMarker>();
        }

        using var input = new MemoryStream(loadable, writable: false);
        using var document = WordprocessingDocument.Open(input, false);
        var bodyText = document.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        if (bodyText.Contains("{{#ds.rows}}", StringComparison.Ordinal)
            || bodyText.Contains("{{#rows}}", StringComparison.Ordinal))
        {
            return Array.Empty<LoopMarker>();
        }

        var addressed = WordTemplateAddressing.EnumerateParagraphs(document);
        var groups = new Dictionary<TableRow, List<WordParagraphAddress>>();

        foreach (var addr in addressed)
        {
            var row = addr.Paragraph.Ancestors<TableRow>().FirstOrDefault();
            if (row == null)
                continue;

            var isCandidate = rowParagraphAddresses != null
                ? rowParagraphAddresses.Contains(addr.Address)
                : ParagraphLooksLikeRowToken(addr.Paragraph);
            if (!isCandidate)
                continue;

            if (!groups.TryGetValue(row, out var list))
            {
                list = [];
                groups[row] = list;
            }

            list.Add(addr);
        }

        var best = groups
            .Select(static g => (
                Row: g.Key,
                Hits: g.Value,
                Cells: g.Value
                    .Select(static a => a.Paragraph.Ancestors<TableCell>().FirstOrDefault())
                    .Where(static c => c != null)
                    .Distinct()
                    .Count()))
            .OrderByDescending(static x => x.Cells)
            .ThenByDescending(static x => x.Hits.Count)
            .FirstOrDefault();

        if (best.Row == null || best.Cells < 2)
            return Array.Empty<LoopMarker>();

        var rowParas = best.Row.Descendants<Paragraph>().ToList();
        if (rowParas.Count == 0)
            return Array.Empty<LoopMarker>();

        var addressByParagraph = new Dictionary<Paragraph, string>(ReferenceEqualityComparer.Instance);
        foreach (var addr in addressed)
            addressByParagraph.TryAdd(addr.Paragraph, addr.Address);

        if (!addressByParagraph.TryGetValue(rowParas[0], out var startAddr)
            || !addressByParagraph.TryGetValue(rowParas[^1], out var endAddr))
        {
            return Array.Empty<LoopMarker>();
        }

        return
        [
            new LoopMarker(
                new DocumentRegion.WordSpan(startAddr, 0, 0),
                new DocumentRegion.WordSpan(endAddr, 0, 0),
                RowsCollectionToken)
        ];
    }

    private static bool ParagraphLooksLikeRowToken(Paragraph paragraph)
    {
        var text = WordTemplateAddressing.GetParagraphText(paragraph);
        return text.Contains("{{.", StringComparison.Ordinal)
            && !IsSignatoryFooterToken(text);
    }

    private static string Key(DocumentRegion.ExcelCell cell) =>
        $"{cell.SheetName}!{cell.CellReference}";

    private static bool IsRowScopedSubstitutionToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (token.Contains("ds.", StringComparison.OrdinalIgnoreCase))
            return false;

        return !IsSignatoryFooterToken(token);
    }

    /// <summary>
    /// Signatory footer cells (ACPOS / ACFNM) are Header+Row in the catalog, so Excel yellow
    /// often emits <c>{{.ACPOS}}</c>. Those are not a roster table — do not wrap them with
    /// <c>{{#ds.rows}}</c>.
    /// </summary>
    private static bool IsSignatoryFooterToken(string token) =>
        TemplateTokenSyntax.TryGetShortCode(token, out var code)
        && ScanPlaceholderRoleCatalog.Resolve(code) == ScanPlaceholderRole.Signatory;

    private static bool IsRosterLoopRow(IEnumerable<string> tokens)
    {
        var codes = new List<string>();
        foreach (var token in tokens)
        {
            if (TemplateTokenSyntax.TryGetShortCode(token, out var code))
                codes.Add(code);
        }

        return codes.Count >= 2 && codes.Exists(static c =>
            ScanPlaceholderRoleCatalog.Resolve(c) != ScanPlaceholderRole.Signatory);
    }
}