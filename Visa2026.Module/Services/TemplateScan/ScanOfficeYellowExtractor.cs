#nullable enable

using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DrawingColor = System.Drawing.Color;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>One contiguous yellow mark in an Office package (Word span or Excel cell).</summary>
public sealed class ScanOfficeYellowSpan
{
    public required string Text { get; init; }

    public required DocumentRegion Region { get; init; }

    public int PageIndex { get; init; }
}

public interface IScanOfficeYellowExtractor
{
    IReadOnlyList<ScanOfficeYellowSpan> Extract(byte[] officeBytes, ScanSourceKind kind);
}

/// <summary>
/// Finds officer yellow highlighter marks in .docx / .xlsx without vision or OCR.
/// Word: w:highlight yellow/darkYellow (+ common green marker) and yellow shading fills.
/// Excel: solid yellow-ish cell background fills.
/// </summary>
public sealed class ScanOfficeYellowExtractor : IScanOfficeYellowExtractor
{
    public IReadOnlyList<ScanOfficeYellowSpan> Extract(byte[] officeBytes, ScanSourceKind kind)
    {
        ArgumentNullException.ThrowIfNull(officeBytes);
        if (officeBytes.Length < 64)
            return Array.Empty<ScanOfficeYellowSpan>();

        return kind switch
        {
            ScanSourceKind.Word => ExtractWord(officeBytes),
            ScanSourceKind.Excel => ExtractExcel(officeBytes),
            _ => Array.Empty<ScanOfficeYellowSpan>(),
        };
    }

    public static ScanSourceKind KindFromFileName(string? fileName) =>
        string.Equals(Path.GetExtension(fileName), ".xlsx", StringComparison.OrdinalIgnoreCase)
            ? ScanSourceKind.Excel
            : ScanSourceKind.Word;

    /// <summary>True when the Office package still has yellow highlighter / yellow cell fill.</summary>
    public static bool HasHighlights(byte[] officeBytes, ScanSourceKind kind)
    {
        try
        {
            return new ScanOfficeYellowExtractor().Extract(officeBytes, kind).Count > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal static bool IsZipOfficePackage(byte[] bytes) =>
        bytes.Length >= 64 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K';

    private static IReadOnlyList<ScanOfficeYellowSpan> ExtractWord(byte[] bytes)
    {
        try
        {
            return ExtractWordCore(bytes);
        }
        catch (InvalidOperationException)
        {
            return Array.Empty<ScanOfficeYellowSpan>();
        }
        catch (OpenXmlPackageException)
        {
            return Array.Empty<ScanOfficeYellowSpan>();
        }
    }

    private static IReadOnlyList<ScanOfficeYellowSpan> ExtractWordCore(byte[] bytes)
    {
        using var document = WordOpenXmlPackage.OpenRead(bytes);
        var results = new List<ScanOfficeYellowSpan>();

        foreach (var addressed in WordTemplateAddressing.EnumerateParagraphs(document))
        {
            var paragraph = addressed.Paragraph;
            var runs = paragraph.Descendants<Run>().ToList();
            if (runs.Count == 0)
                continue;

            var fullText = WordTemplateAddressing.GetParagraphText(paragraph);
            if (string.IsNullOrWhiteSpace(fullText))
                continue;

            var cell = paragraph.Ancestors<TableCell>().FirstOrDefault();
            if (IsYellowShadedCell(cell) || IsYellowShadedParagraph(paragraph))
            {
                var shaded = SpansFromText(
                    addressed.Address,
                    fullText,
                    start: 0).ToList();
                AttachCountPairSpans(fullText, addressed.Address, shaded);
                results.AddRange(shaded);
                continue;
            }

            // Build per-run (start, length, yellow?) over concatenated w:t text.
            var cursor = 0;
            var segments = new List<(int Start, int Length, bool Yellow, string Text)>();
            foreach (var run in runs)
            {
                var text = string.Concat(run.Descendants<Text>().Select(static t => t.Text ?? string.Empty));
                if (text.Length == 0)
                    continue;

                segments.Add((cursor, text.Length, IsYellowRun(run), text));
                cursor += text.Length;
            }

            var paragraphSpans = new List<ScanOfficeYellowSpan>();

            // Merge consecutive yellow segments into spans.
            var i = 0;
            while (i < segments.Count)
            {
                if (!segments[i].Yellow)
                {
                    i++;
                    continue;
                }

                var start = segments[i].Start;
                var end = start + segments[i].Length;
                var sb = new System.Text.StringBuilder(segments[i].Text);
                var j = i + 1;
                while (j < segments.Count && segments[j].Yellow
                       && ShouldMergeYellowTexts(sb.ToString(), segments[j].Text))
                {
                    sb.Append(segments[j].Text);
                    end = segments[j].Start + segments[j].Length;
                    j++;
                }

                var raw = sb.ToString();
                var mark = raw.Trim();
                if (mark.Length > 0)
                {
                    var lead = raw.Length - raw.TrimStart().Length;
                    paragraphSpans.AddRange(SpansFromText(
                        addressed.Address,
                        mark,
                        start + lead));
                }

                i = j;
            }

            AttachCountPairSpans(fullText, addressed.Address, paragraphSpans);
            results.AddRange(paragraphSpans);
        }

        return results;
    }

    private static readonly Regex PrintedCountPair = new(
        @"(\d{1,3})\s*[\(\uFF08]\s*([^\)\uFF09]{1,24}?)\s*[\)\uFF09]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Printed <c>15 (on bäş)</c> / <c>20 (ýigrimi)</c>: officers highlight the digit or the
    /// words (Word run boundaries decide), rarely both. When either side of the pair carries
    /// yellow, mark the other side too so Review keeps TPCNT + TPCTX / BTDCNT + BTDCTX.
    /// A pair with no yellow at all stays untouched — yellow still drives the plan.
    /// </summary>
    internal static void AttachCountPairSpans(
        string paragraphText,
        string paragraphAddress,
        List<ScanOfficeYellowSpan> spans)
    {
        if (string.IsNullOrEmpty(paragraphText) || spans.Count == 0)
            return;

        var extras = new List<ScanOfficeYellowSpan>();
        var replaced = new List<ScanOfficeYellowSpan>();
        foreach (Match match in PrintedCountPair.Matches(paragraphText))
        {
            var digit = match.Groups[1];
            var words = match.Groups[2];
            if (!ScanOfficialLetterHints.LooksLikeIsolatedCountWords(words.Value))
                continue;

            var digitMark = FindMark(spans, extras, digit.Index, digit.Length);
            var wordsMark = FindMark(spans, extras, words.Index, words.Length);
            if (digitMark == null && wordsMark == null)
                continue;

            // One highlight swallowed the pair without its closing bracket ("15 (on bäş"):
            // the count regexes cannot split that, so mark the digit and the words instead.
            if (digitMark != null
                && ReferenceEquals(digitMark, wordsMark)
                && !PrintedCountPair.IsMatch(digitMark.Text))
            {
                replaced.Add(digitMark);
                digitMark = null;
                wordsMark = null;
            }

            if (digitMark == null)
                extras.Add(PairSpan(paragraphAddress, spans, digit.Value, digit.Index, digit.Length));
            if (wordsMark == null)
                extras.Add(PairSpan(paragraphAddress, spans, words.Value, words.Index, words.Length));
        }

        if (extras.Count == 0)
            return;

        foreach (var span in replaced)
            spans.Remove(span);
        spans.AddRange(extras);
        spans.Sort(CompareByParagraphOffset);
    }

    private static ScanOfficeYellowSpan PairSpan(
        string paragraphAddress,
        List<ScanOfficeYellowSpan> spans,
        string text,
        int start,
        int length) =>
        new()
        {
            Text = text,
            Region = new DocumentRegion.WordSpan(paragraphAddress, start, length),
            PageIndex = spans[0].PageIndex,
        };

    private static ScanOfficeYellowSpan? FindMark(
        List<ScanOfficeYellowSpan> spans,
        List<ScanOfficeYellowSpan> extras,
        int start,
        int length)
    {
        var end = start + length;
        foreach (var span in spans.Concat(extras))
        {
            if (span.Region is not DocumentRegion.WordSpan wordSpan)
                continue;
            var spanEnd = wordSpan.Start + wordSpan.Length;
            if (start < spanEnd && wordSpan.Start < end)
                return span;
        }

        return null;
    }

    private static int CompareByParagraphOffset(ScanOfficeYellowSpan a, ScanOfficeYellowSpan b)
    {
        var aStart = a.Region is DocumentRegion.WordSpan aa ? aa.Start : 0;
        var bStart = b.Region is DocumentRegion.WordSpan bb ? bb.Start : 0;
        return aStart.CompareTo(bStart);
    }

    internal static IReadOnlyList<ScanOfficeYellowSpan> SpansFromText(
        string paragraphAddress,
        string text,
        int start)
    {
        var mark = (text ?? string.Empty).Trim();
        if (mark.Length == 0)
            return Array.Empty<ScanOfficeYellowSpan>();

        var lead = (text ?? string.Empty).Length - (text ?? string.Empty).TrimStart().Length;
        var origin = start + lead;
        if (TrySplitDirectorTitleAndName(mark, out var title, out var name))
        {
            return
            [
                MakeSpan(paragraphAddress, title, origin),
                MakeSpan(paragraphAddress, name, origin + mark.IndexOf(name, StringComparison.Ordinal)),
            ];
        }

        return [MakeSpan(paragraphAddress, mark, origin)];
    }

    internal static bool ShouldMergeYellowTexts(string left, string right)
    {
        if (ScanOfficialLetterHints.LooksLikeBranchDirectorTitle(left)
            && LooksLikeStandalonePersonName(right))
            return false;
        return true;
    }

    internal static bool TrySplitDirectorTitleAndName(string text, out string title, out string name)
    {
        title = string.Empty;
        name = string.Empty;
        var raw = (text ?? string.Empty).Trim();
        var at = IndexOfFoldedNeedle(raw, "mudiri");
        if (at < 0)
            return false;

        var consumed = 0;
        for (var n = 1; at + n <= raw.Length; n++)
        {
            if (TemplateTextNormalizer.NormalizeFolded(raw.Substring(at, n))
                .Equals("mudiri", StringComparison.Ordinal))
            {
                consumed = n;
                break;
            }
        }

        if (consumed == 0)
            return false;

        title = raw[..(at + consumed)].Trim();
        name = raw[(at + consumed)..].Trim();
        return ScanOfficialLetterHints.LooksLikeBranchDirectorTitle(title)
            && LooksLikeStandalonePersonName(name);
    }

    private static int IndexOfFoldedNeedle(string text, string foldedNeedle)
    {
        for (var i = 0; i < text.Length; i++)
        {
            for (var j = i + 1; j <= text.Length; j++)
            {
                var folded = TemplateTextNormalizer.NormalizeFolded(text[i..j]);
                if (folded.Equals(foldedNeedle, StringComparison.Ordinal))
                    return i;
                if (folded.Length > foldedNeedle.Length)
                    break;
            }
        }

        return -1;
    }

    private static bool LooksLikeStandalonePersonName(string text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length is < 3 or > 80)
            return false;
        if (trimmed.Contains(',', StringComparison.Ordinal))
            return false;
        if (ScanOfficialLetterHints.LooksLikeBranchDirectorTitle(trimmed))
            return false;
        var words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words.Length is >= 1 and <= 4
            && words.All(static w => w.Any(char.IsLetter));
    }

    private static ScanOfficeYellowSpan MakeSpan(string paragraphAddress, string text, int start) =>
        new()
        {
            Text = text,
            Region = new DocumentRegion.WordSpan(paragraphAddress, Math.Max(0, start), text.Length),
            PageIndex = 0,
        };

    private static bool IsYellowShadedCell(TableCell? cell)
    {
        if (cell == null)
            return false;
        var shd = cell.TableCellProperties?.GetFirstChild<Shading>();
        return IsYellowShading(shd);
    }

    private static bool IsYellowShadedParagraph(Paragraph paragraph)
    {
        var shd = paragraph.ParagraphProperties?.GetFirstChild<Shading>();
        return IsYellowShading(shd);
    }

    private static bool IsYellowShading(Shading? shading)
    {
        if (shading == null)
            return false;
        var fill = shading.Fill?.Value;
        return !string.IsNullOrWhiteSpace(fill) && IsYellowHex(fill);
    }

    private static bool IsYellowRun(Run run)
    {
        var props = run.RunProperties;
        if (props == null)
            return false;

        var highlight = props.Highlight?.Val?.Value;
        if (highlight == HighlightColorValues.Yellow
            || highlight == HighlightColorValues.DarkYellow
            || highlight == HighlightColorValues.Green)
            return true;

        var fill = props.Shading?.Fill?.Value;
        if (!string.IsNullOrWhiteSpace(fill) && IsYellowHex(fill))
            return true;

        return false;
    }

    private static IReadOnlyList<ScanOfficeYellowSpan> ExtractExcel(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var workbook = new XLWorkbook(stream);
        var results = new List<ScanOfficeYellowSpan>();

        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
            return results;

        foreach (var cell in sheet.CellsUsed())
        {
            if (!IsYellowCell(cell))
                continue;

            var text = string.Empty;
            if (cell.DataType == XLDataType.DateTime && cell.TryGetValue(out DateTime dateTime) && dateTime.Year > 1)
                text = dateTime.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            if (text.Length == 0)
                text = cell.GetFormattedString()?.Trim() ?? string.Empty;
            if (text.Length == 0)
                text = cell.GetString()?.Trim() ?? string.Empty;
            if (text.Length == 0)
                continue;

            results.Add(new ScanOfficeYellowSpan
            {
                Text = text,
                Region = new DocumentRegion.ExcelCell(sheet.Name, cell.Address.ToStringRelative()),
                PageIndex = 0,
            });
        }

        return results;
    }

    private static bool IsYellowCell(IXLCell cell)
    {
        var fill = cell.Style.Fill;
        if (fill.PatternType is XLFillPatternValues.None or XLFillPatternValues.Gray125)
            return false;

        try
        {
            var color = fill.BackgroundColor;
            if (color.ColorType == XLColorType.Color)
                return IsHighlighterYellowRgb(color.Color);
            if (color.ColorType == XLColorType.Theme)
            {
                // Theme yellows are uncommon; treat indexed yellow if present.
                return false;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsYellowHex(string hex)
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

        return IsHighlighterYellowRgb(DrawingColor.FromArgb(r, g, b));
    }

    private static bool IsHighlighterYellowRgb(DrawingColor c)
    {
        if (c.R < 180 || c.G < 160)
            return false;
        var chroma = (c.R + c.G) / 2.0 - c.B;
        return chroma >= 35 && c.B <= 210;
    }
}