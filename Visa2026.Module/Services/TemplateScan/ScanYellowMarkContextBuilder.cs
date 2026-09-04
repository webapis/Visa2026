#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Nearby printed text for an escalated yellow mark — not the Office file.</summary>
public sealed class ScanYellowMarkContext
{
    public string? SurroundingSnippet { get; init; }

    public string? PrintedLabel { get; init; }

    /// <summary>Parenthetical field map under the yellow line, e.g. (pasportyň seriýasy we belgisi, nirede we haçan berildi, möhleti).</summary>
    public string? FollowingCaption { get; init; }

    public string? SheetName { get; init; }

    public string? HeaderRow { get; init; }
}

/// <summary>
/// Builds a short Word paragraph / Excel header-row snippet so Azure can see
/// the printed label next to the yellow sample without receiving the package.
/// </summary>
public static class ScanYellowMarkContextBuilder
{
    public const int MaxSnippetChars = 220;

    public static IReadOnlyDictionary<string, ScanYellowMarkContext> Build(
        byte[]? officeBytes,
        ScanSourceKind sourceKind,
        IReadOnlyList<ScanDetectedFieldDraft> drafts)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        var map = new Dictionary<string, ScanYellowMarkContext>(StringComparer.Ordinal);
        if (officeBytes is not { Length: > 64 } || drafts.Count == 0)
            return map;

        try
        {
            if (sourceKind == ScanSourceKind.Word)
                FillWord(officeBytes, drafts, map);
            else if (sourceKind == ScanSourceKind.Excel)
                FillExcel(officeBytes, drafts, map);
        }
        catch
        {
            // Context is optional — Analyze must not fail.
        }

        return map;
    }

    private static void FillWord(
        byte[] officeBytes,
        IReadOnlyList<ScanDetectedFieldDraft> drafts,
        Dictionary<string, ScanYellowMarkContext> map)
    {
        using var stream = new MemoryStream(officeBytes, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var paragraphs = WordTemplateAddressing.EnumerateParagraphs(document)
            .Select(static a => (a.Address, Text: WordTemplateAddressing.GetParagraphText(a.Paragraph)))
            .ToList();

        foreach (var draft in drafts)
        {
            if (string.IsNullOrWhiteSpace(draft.FieldId))
                continue;

            string? paragraphText = null;
            string? previousParagraph = null;
            string? nextParagraph = null;
            var start = -1;
            var length = 0;

            if (draft.SourceRegion is DocumentRegion.WordSpan span)
            {
                var index = paragraphs.FindIndex(p =>
                    string.Equals(p.Address, span.ParagraphAddress, StringComparison.Ordinal));
                if (index >= 0)
                {
                    paragraphText = paragraphs[index].Text;
                    start = span.Start;
                    length = span.Length;
                    if (index > 0)
                        previousParagraph = paragraphs[index - 1].Text;
                    if (index + 1 < paragraphs.Count)
                        nextParagraph = paragraphs[index + 1].Text;
                }
            }

            if (paragraphText == null && !string.IsNullOrWhiteSpace(draft.LabelText))
            {
                var needle = draft.LabelText.Trim();
                for (var i = 0; i < paragraphs.Count; i++)
                {
                    var found = paragraphs[i].Text.IndexOf(needle, StringComparison.Ordinal);
                    if (found < 0)
                        continue;
                    paragraphText = paragraphs[i].Text;
                    start = found;
                    length = needle.Length;
                    if (i > 0)
                        previousParagraph = paragraphs[i - 1].Text;
                    if (i + 1 < paragraphs.Count)
                        nextParagraph = paragraphs[i + 1].Text;
                    break;
                }
            }

            if (paragraphText == null || start < 0)
                continue;

            var printed = ExtractPrintedLabel(paragraphText, start, previousParagraph);
            map[draft.FieldId] = new ScanYellowMarkContext
            {
                SurroundingSnippet = MarkAndTrim(paragraphText, start, length),
                PrintedLabel = printed,
                FollowingCaption = ExtractFollowingCaption(paragraphText, start, length, nextParagraph),
            };
        }
    }

    private static void FillExcel(
        byte[] officeBytes,
        IReadOnlyList<ScanDetectedFieldDraft> drafts,
        Dictionary<string, ScanYellowMarkContext> map)
    {
        using var stream = new MemoryStream(officeBytes, writable: false);
        using var workbook = new XLWorkbook(stream);

        foreach (var draft in drafts)
        {
            if (string.IsNullOrWhiteSpace(draft.FieldId)
                || draft.SourceRegion is not DocumentRegion.ExcelCell excelCell)
                continue;

            var sheet = workbook.Worksheets.FirstOrDefault(w =>
                string.Equals(w.Name, excelCell.SheetName, StringComparison.OrdinalIgnoreCase));
            if (sheet == null)
                continue;

            IXLCell cell;
            try
            {
                cell = sheet.Cell(excelCell.CellReference);
            }
            catch (ArgumentException)
            {
                continue;
            }

            var headerRow = ScanExcelWorkbookHelper.FindHeaderRow(
                sheet,
                cell.Address.ColumnNumber,
                cell.Address.RowNumber);

            map[draft.FieldId] = new ScanYellowMarkContext
            {
                SheetName = sheet.Name,
                HeaderRow = headerRow is int row
                    ? FormatHeaderRow(sheet, row, cell.Address.ColumnNumber)
                    : null,
                PrintedLabel = draft.ColumnHeader,
                SurroundingSnippet = draft.ColumnHeader == null
                    ? null
                    : draft.ColumnHeader + " | " + (draft.LabelText ?? string.Empty),
            };
        }
    }

    internal static string MarkAndTrim(string paragraph, int start, int length)
    {
        var text = paragraph ?? string.Empty;
        if (text.Length == 0)
            return string.Empty;

        var safeStart = Math.Clamp(start, 0, text.Length);
        var safeLength = Math.Clamp(length, 0, text.Length - safeStart);
        var marked = text[..safeStart] + "<<<" + text.Substring(safeStart, safeLength) + ">>>" + text[(safeStart + safeLength)..];

        if (marked.Length <= MaxSnippetChars)
            return marked;

        var markAt = marked.IndexOf("<<<", StringComparison.Ordinal);
        var from = Math.Max(0, markAt - MaxSnippetChars / 3);
        if (from + MaxSnippetChars > marked.Length)
            from = Math.Max(0, marked.Length - MaxSnippetChars);

        var take = Math.Min(MaxSnippetChars, marked.Length - from);
        var slice = marked.Substring(from, take);
        if (from > 0)
            slice = "…" + slice;
        if (from + take < marked.Length)
            slice += "…";

        return slice;
    }

    internal static string? ExtractPrintedLabel(
        string paragraph,
        int yellowStart,
        string? previousParagraph = null)
    {
        var start = Math.Clamp(yellowStart, 0, paragraph.Length);
        var before = paragraph[..start].TrimEnd();
        before = before.TrimEnd('_', ' ', '.', ':', ';', '-', '—', '–', '\t');

        var lastBreak = Math.Max(before.LastIndexOf('\n'), before.LastIndexOf('\r'));
        var line = lastBreak >= 0 ? before[(lastBreak + 1)..].Trim() : before;

        // Direct previous paragraph only when this line has no left-side label.
        if (line.Length < 8 && LooksLikeImmediateFormLabel(previousParagraph))
        {
            var prev = previousParagraph!.Trim();
            if (prev.Length > 80)
                prev = prev[^80..].Trim();
            if (prev.Length == 0)
                return line.Length == 0 ? null : line;
            return line.Length == 0 ? prev : prev + " " + line;
        }

        if (line.Length > 80)
            line = line[^80..].Trim();

        return line.Length == 0 ? null : line;
    }

    internal static string? ExtractFollowingCaption(
        string paragraph,
        int yellowStart,
        int yellowLength,
        string? nextParagraph)
    {
        var end = Math.Clamp(yellowStart, 0, paragraph.Length)
            + Math.Clamp(yellowLength, 0, Math.Max(0, paragraph.Length - yellowStart));
        var after = end < paragraph.Length ? paragraph[end..].Trim() : string.Empty;
        var combined = after;
        if (!string.IsNullOrWhiteSpace(nextParagraph))
        {
            combined = combined.Length == 0
                ? nextParagraph.Trim()
                : combined + " " + nextParagraph.Trim();
        }

        return ScanFormCaptionHints.ExtractParentheticalList(combined);
    }

    /// <summary>
    /// Immediate left / previous-line printed captions only — not company rows two lines above.
    /// </summary>
    internal static bool LooksLikeImmediateFormLabel(string? text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length is < 4 or > 140)
            return false;

        var folded = TemplateTextNormalizer.NormalizeFolded(trimmed);
        if (folded.Contains("wekil", StringComparison.Ordinal)
            || folded.Contains("yolbascy", StringComparison.Ordinal)
            || folded.Contains("cagryl", StringComparison.Ordinal)
            || folded.Contains("gol cekiji", StringComparison.Ordinal)
            || folded.Contains("pasport", StringComparison.Ordinal)
            || folded.Contains("hasaba", StringComparison.Ordinal)
            || folded.Contains("karhana", StringComparison.Ordinal)
            || folded.Contains("doglan senesi", StringComparison.Ordinal)
            || folded.Contains("familiyasy", StringComparison.Ordinal))
            return true;

        if (ScanFormFieldLabelHints.LooksLikeFormFieldLabel(trimmed))
            return true;

        if (ScanFormCaptionHints.ExtractParentheticalList(trimmed) != null)
            return true;

        return trimmed.Contains(':') && !ScanCompoundYellowParts.IsCommaCombination(trimmed);
    }

    private static string? FormatHeaderRow(IXLWorksheet sheet, int headerRow, int focusColumn)
    {
        var last = sheet.LastColumnUsed()?.ColumnNumber() ?? focusColumn;
        var from = Math.Max(1, focusColumn - 12);
        var to = Math.Min(last, focusColumn + 12);
        var parts = new List<string>();
        for (var column = from; column <= to; column++)
        {
            var text = ScanExcelWorkbookHelper.ReadCellText(sheet.Cell(headerRow, column));
            if (string.IsNullOrWhiteSpace(text))
                continue;
            parts.Add(sheet.Cell(headerRow, column).Address.ColumnLetter + ": " + text);
        }

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }
}
