#nullable enable

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Column caption above a yellow Word table cell — the same signal Excel uses
/// (<c>Familiýasy</c>, <c>Doglan senesi we ýeri</c>), not the previous data cell.
/// </summary>
public static class ScanWordTableHeader
{
    public static IReadOnlyDictionary<int, string> MapForYellows(
        byte[]? wordBytes,
        IReadOnlyList<ScanOfficeYellowSpan> yellows)
    {
        var map = new Dictionary<int, string>();
        if (wordBytes is not { Length: > 64 } || yellows == null || yellows.Count == 0)
            return map;

        try
        {
            using var stream = new MemoryStream(wordBytes, writable: false);
            using var document = WordprocessingDocument.Open(stream, false);
            var byAddress = WordTemplateAddressing.EnumerateParagraphs(document)
                .ToDictionary(static p => p.Address, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < yellows.Count; i++)
            {
                if (yellows[i].Region is not DocumentRegion.WordSpan span)
                    continue;
                if (!byAddress.TryGetValue(span.ParagraphAddress, out var para))
                    continue;
                var header = TryGetColumnHeader(para.Paragraph);
                if (!string.IsNullOrWhiteSpace(header))
                    map[i] = header.Trim();
            }
        }
        catch (Exception)
        {
            return map;
        }

        return map;
    }

    public static string? TryGet(byte[]? wordBytes, DocumentRegion? region)
    {
        if (wordBytes is not { Length: > 64 } || region is not DocumentRegion.WordSpan span)
            return null;

        try
        {
            using var stream = new MemoryStream(wordBytes, writable: false);
            using var document = WordprocessingDocument.Open(stream, false);
            foreach (var para in WordTemplateAddressing.EnumerateParagraphs(document))
            {
                if (!string.Equals(para.Address, span.ParagraphAddress, StringComparison.OrdinalIgnoreCase))
                    continue;
                return TryGetColumnHeader(para.Paragraph);
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    private static string? TryGetColumnHeader(Paragraph paragraph)
    {
        var table = paragraph.Ancestors<Table>().FirstOrDefault();
        var row = paragraph.Ancestors<TableRow>().FirstOrDefault();
        var cell = paragraph.Ancestors<TableCell>().FirstOrDefault();
        if (table == null || row == null || cell == null)
            return null;

        var rows = table.Elements<TableRow>().ToList();
        var rowIndex = rows.IndexOf(row);
        if (rowIndex <= 0)
            return null;

        var col = ColumnIndex(row, cell);
        for (var r = rowIndex - 1; r >= 0; r--)
        {
            var headerCell = CellAtColumn(rows[r], col);
            if (headerCell == null)
                continue;
            var text = CellText(headerCell);
            if (string.IsNullOrWhiteSpace(text) || IsColumnIndexLabel(text))
                continue;
            if (LooksLikeColumnHeader(text))
                return text;
        }

        return null;
    }

    internal static bool LooksLikeColumnHeader(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (ScanExcelColumnProfiles.Match(text) != null)
            return true;
        return ScanFormFieldLabelHints.LooksLikeFormFieldLabel(text);
    }

    private static string CellText(TableCell cell) =>
        string.Join(
            " ",
            cell.Elements<Paragraph>().Select(WordTemplateAddressing.GetParagraphText)
                .Where(static t => !string.IsNullOrWhiteSpace(t)));

    private static TableCell? CellAtColumn(TableRow row, int column)
    {
        var cursor = 0;
        foreach (var cell in row.Elements<TableCell>())
        {
            var span = GridSpan(cell);
            if (column >= cursor && column < cursor + span)
                return cell;
            cursor += span;
        }

        return null;
    }

    private static int ColumnIndex(TableRow row, TableCell target)
    {
        var col = 0;
        foreach (var cell in row.Elements<TableCell>())
        {
            if (ReferenceEquals(cell, target))
                return col;
            col += GridSpan(cell);
        }

        return col;
    }

    private static int GridSpan(TableCell cell)
    {
        var span = cell.TableCellProperties?.GridSpan?.Val;
        return span != null && span.HasValue && span.Value > 0 ? span.Value : 1;
    }

    private static bool IsColumnIndexLabel(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length is < 1 or > 6)
            return false;
        foreach (var ch in trimmed)
        {
            if (ch is not (>= '0' and <= '9') and not '.')
                return false;
        }

        return trimmed.Any(char.IsDigit);
    }
}