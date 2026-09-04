#nullable enable

using System.Globalization;
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

    private static IReadOnlyList<ScanOfficeYellowSpan> ExtractWord(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
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
                while (j < segments.Count && segments[j].Yellow)
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
                    results.Add(new ScanOfficeYellowSpan
                    {
                        Text = mark,
                        Region = new DocumentRegion.WordSpan(
                            addressed.Address,
                            start + lead,
                            mark.Length),
                        PageIndex = 0,
                    });
                }

                i = j;
            }
        }

        return results;
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