#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Place manually: a click on the Review page snaps to a Word run or Excel cell.
/// An existing mark at that text is selected. Otherwise a new unlocked row is added.
/// </summary>
public sealed class ScanManualPlaceClick
{
    public string Text { get; set; } = string.Empty;

    public int PageIndex { get; set; }

    public int Occurrence { get; set; }

    /// <summary>PDF line around the click, so the same digit is not taken from an earlier sentence.</summary>
    public string? LineText { get; set; }

    /// <summary>Click position as a percent of the page width.</summary>
    public double? PinLeft { get; set; }

    /// <summary>Click position as a percent of the page height.</summary>
    public double? PinTop { get; set; }

    /// <summary>Id of the orange selection on the page, so deleting the row can clear it.</summary>
    public string? PickToken { get; set; }
}

public static class ScanManualPlacement
{
    public readonly record struct Request(
        string Text,
        int PageIndex,
        int Occurrence,
        string? LineText = null,
        double? PinLeft = null,
        double? PinTop = null,
        string? PickToken = null);

    public sealed record Result(string? SelectFieldId, ScanDetectedField? Added);

    /// <summary>OpenXML paragraph text for a Word span, so the editor can select that same sentence.</summary>
    public static string? ParagraphText(byte[]? officeBytes, DocumentRegion? region)
    {
        if (officeBytes is not { Length: > 64 } || region is not DocumentRegion.WordSpan span)
            return null;

        using var stream = new MemoryStream(officeBytes, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        foreach (var paragraph in WordTemplateAddressing.EnumerateParagraphs(document))
        {
            if (!string.Equals(paragraph.Address, span.ParagraphAddress, StringComparison.OrdinalIgnoreCase))
                continue;

            return WordTemplateAddressing.GetParagraphText(paragraph.Paragraph);
        }

        return null;
    }

    public static Result Resolve(
        byte[]? officeBytes,
        ScanSourceKind kind,
        Request request,
        IReadOnlyList<ScanDetectedField> existing)
    {
        ArgumentNullException.ThrowIfNull(existing);
        var needle = (request.Text ?? string.Empty).Trim();
        if (needle.Length == 0 || officeBytes is not { Length: > 64 })
            return new Result(null, null);

        return kind == ScanSourceKind.Excel
            ? ResolveExcel(officeBytes, needle, request, existing)
            : ResolveWord(officeBytes, needle, request, existing);
    }

    private readonly record struct WordHit(
        string Address,
        int Start,
        int Length,
        string Label,
        ScanFieldScope Scope,
        string ParagraphText);

    private static Result ResolveWord(
        byte[] bytes,
        string needle,
        Request request,
        IReadOnlyList<ScanDetectedField> existing)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var hits = new List<WordHit>();
        foreach (var paragraph in WordTemplateAddressing.EnumerateParagraphs(document))
        {
            var text = WordTemplateAddressing.GetParagraphText(paragraph.Paragraph);
            foreach (var start in FindStarts(text, needle))
            {
                var length = Math.Min(needle.Length, text.Length - start);
                if (length <= 0)
                    continue;

                hits.Add(new WordHit(
                    paragraph.Address,
                    start,
                    length,
                    text.Substring(start, length),
                    ScopeFor(paragraph.Paragraph),
                    text));
            }
        }

        if (hits.Count == 0)
            return new Result(null, null);

        var hit = hits[PickWordHit(hits, needle, request)];
        var region = new DocumentRegion.WordSpan(hit.Address, hit.Start, hit.Length);
        var existingId = FindWordField(existing, region);
        if (existingId != null)
            return new Result(existingId, null);

        return new Result(null, NewField(hit.Label, region, hit.Scope, request));
    }

    private static Result ResolveExcel(
        byte[] bytes,
        string needle,
        Request request,
        IReadOnlyList<ScanDetectedField> existing)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
            return new Result(null, null);

        var hits = new List<(string Reference, string Label, ScanFieldScope Scope)>();
        foreach (var cell in sheet.CellsUsed().OrderBy(static c => c.Address.RowNumber).ThenBy(static c => c.Address.ColumnNumber))
        {
            var text = ScanExcelWorkbookHelper.ReadCellText(cell).Trim();
            if (text.Length == 0 || FindStarts(text, needle).Count == 0)
                continue;

            var headerRow = ScanExcelWorkbookHelper.FindHeaderRow(sheet, cell.Address.ColumnNumber, cell.Address.RowNumber);
            var scope = headerRow != null && cell.Address.RowNumber > headerRow.Value
                ? ScanFieldScope.Row
                : ScanFieldScope.Header;
            hits.Add((cell.Address.ToStringRelative(), text, scope));
        }

        if (hits.Count == 0)
            return new Result(null, null);

        var hit = hits[PickExcelHit(hits, request)];
        var region = new DocumentRegion.ExcelCell(sheet.Name, hit.Reference);
        var existingId = FindExcelField(existing, region);
        if (existingId != null)
            return new Result(existingId, null);

        return new Result(null, NewField(hit.Label, region, hit.Scope, request));
    }

    private static ScanDetectedField NewField(
        string label,
        DocumentRegion region,
        ScanFieldScope scope,
        Request request) =>
        new()
        {
            FieldId = Guid.NewGuid().ToString("N"),
            Box = ScanBoundingBox.FullPage,
            PageIndex = Math.Max(0, request.PageIndex),
            LabelText = label,
            ProposedToken = null,
            Confidence = ScanFieldConfidence.Medium,
            Scope = scope,
            SourceRegion = region,
            PlacedManually = true,
            PinLeft = request.PinLeft,
            PinTop = request.PinTop,
            PickToken = string.IsNullOrWhiteSpace(request.PickToken) ? null : request.PickToken.Trim(),
        };

    private static int PickWordHit(IReadOnlyList<WordHit> hits, string needle, Request request)
    {
        var foldedLine = TemplateTextNormalizer.NormalizeFolded(request.LineText);
        if (foldedLine.Length >= 4)
        {
            var bestScore = 0;
            var best = new List<int>();
            for (var i = 0; i < hits.Count; i++)
            {
                var score = ContextScore(hits[i].ParagraphText, hits[i].Start, needle, foldedLine);
                if (score > bestScore)
                {
                    bestScore = score;
                    best.Clear();
                    best.Add(i);
                }
                else if (score == bestScore && score > 0)
                {
                    best.Add(i);
                }
            }

            if (best.Count > 0 && bestScore >= Math.Min(6, foldedLine.Length))
                return best[0];
        }

        return Math.Clamp(request.Occurrence, 0, hits.Count - 1);
    }

    private static int PickExcelHit(
        IReadOnlyList<(string Reference, string Label, ScanFieldScope Scope)> hits,
        Request request)
    {
        var foldedLine = TemplateTextNormalizer.NormalizeFolded(request.LineText);
        if (foldedLine.Length >= 4)
        {
            var matched = new List<int>();
            for (var i = 0; i < hits.Count; i++)
            {
                if (TemplateTextNormalizer.NormalizeFolded(hits[i].Label)
                    .Contains(foldedLine, StringComparison.Ordinal))
                    matched.Add(i);
            }

            if (matched.Count > 0)
                return matched[Math.Clamp(request.Occurrence, 0, matched.Count - 1)];
        }

        return Math.Clamp(request.Occurrence, 0, hits.Count - 1);
    }

    /// <summary>
    /// Score how well the text around this hit matches the PDF line the officer clicked.
    /// The window stays the length of that line so an earlier "1" in the same sentence does not win.
    /// </summary>
    private static int ContextScore(string paragraph, int start, string needle, string foldedLine)
    {
        var foldedNeedle = TemplateTextNormalizer.NormalizeFolded(needle);
        var needleAt = foldedNeedle.Length == 0
            ? 0
            : foldedLine.IndexOf(foldedNeedle, StringComparison.Ordinal);
        if (needleAt < 0)
            needleAt = 0;

        var foldedPara = TemplateTextNormalizer.NormalizeFolded(paragraph);
        var foldedStart = TemplateTextNormalizer.NormalizeFolded(
            start <= 0 ? string.Empty : paragraph[..Math.Min(start, paragraph.Length)]).Length;
        var from = Math.Max(0, foldedStart - needleAt);
        var len = Math.Min(foldedPara.Length - from, foldedLine.Length + 2);
        if (len <= 0)
            return 0;

        var window = foldedPara.Substring(from, len);
        return window.Contains(foldedLine, StringComparison.Ordinal) ? foldedLine.Length : 0;
    }

    private static string? FindWordField(IReadOnlyList<ScanDetectedField> existing, DocumentRegion.WordSpan hit)
    {
        foreach (var field in existing)
        {
            if (field.SourceRegion is not DocumentRegion.WordSpan span)
                continue;
            if (!string.Equals(span.ParagraphAddress, hit.ParagraphAddress, StringComparison.OrdinalIgnoreCase))
                continue;
            var hitEnd = hit.Start + hit.Length;
            var spanEnd = span.Start + span.Length;
            if (hit.Start < spanEnd && span.Start < hitEnd)
                return field.FieldId;
        }

        return null;
    }

    private static string? FindExcelField(IReadOnlyList<ScanDetectedField> existing, DocumentRegion.ExcelCell hit)
    {
        foreach (var field in existing)
        {
            if (field.SourceRegion is not DocumentRegion.ExcelCell cell)
                continue;
            if (string.Equals(cell.SheetName, hit.SheetName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(cell.CellReference, hit.CellReference, StringComparison.OrdinalIgnoreCase))
                return field.FieldId;
        }

        return null;
    }

    private static ScanFieldScope ScopeFor(Paragraph paragraph)
    {
        var cell = paragraph.Ancestors<TableCell>().FirstOrDefault();
        var row = paragraph.Ancestors<TableRow>().FirstOrDefault();
        var table = paragraph.Ancestors<Table>().FirstOrDefault();
        if (cell == null || row == null || table == null)
            return ScanFieldScope.Header;

        var rows = table.Elements<TableRow>().ToList();
        return rows.IndexOf(row) > 0 ? ScanFieldScope.Row : ScanFieldScope.Header;
    }

    private static List<int> FindStarts(string text, string needle)
    {
        var starts = new List<int>();
        if (text.Length == 0 || needle.Length == 0)
            return starts;

        var search = 0;
        while (search < text.Length)
        {
            var at = text.IndexOf(needle, search, StringComparison.OrdinalIgnoreCase);
            if (at < 0)
                break;

            starts.Add(at);
            search = at + Math.Max(needle.Length, 1);
        }

        if (starts.Count > 0)
            return starts;

        var foldedNeedle = TemplateTextNormalizer.NormalizeFolded(needle);
        if (foldedNeedle.Length == 0)
            return starts;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        if (folded.IndexOf(foldedNeedle, StringComparison.Ordinal) >= 0)
            starts.Add(0);

        return starts;
    }
}
