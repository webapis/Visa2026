#nullable enable

using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Yellow-marks Word sanaws are a ministry table with <c>{{.CODE}}</c> cells.
/// Direct-to-migration seeds already wrap that row with <c>{{#ds.rows}}</c> and DocxTemplater
/// expands them. Scan copies (and scan-injected wraps that DocxTemplater cannot parse) clone
/// the prototype row once per selected person, then leave header <c>{{ds.*}}</c> to DocxTemplater.
/// </summary>
internal static class WordScanTableRowExpander
{
    private static readonly Regex RowToken = new(
        @"\{\{\.([A-Za-z0-9_]+)\}\}",
        RegexOptions.Compiled);

    public static byte[] ExpandPrototypeTableRow(
        byte[] content,
        IReadOnlyList<IDictionary<string, object>> rows)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0 || rows == null || rows.Count == 0)
            return content;

        byte[] loadable;
        try
        {
            loadable = WordOpenXmlPackage.EnsureLoadable(content);
        }
        catch (InvalidDataException)
        {
            return content;
        }
        catch (OpenXmlPackageException)
        {
            return content;
        }

        using var buffer = new MemoryStream();
        buffer.Write(loadable, 0, loadable.Length);
        buffer.Position = 0;

        using (var document = WordprocessingDocument.Open(buffer, true))
        {
            var body = document.MainDocumentPart?.Document?.Body;
            if (body == null)
                return content;

            var prototype = FindPrototypeRow(body);
            if (prototype == null)
                return content;

            var bodyText = body.InnerText ?? string.Empty;
            var hasLoop = bodyText.Contains("{{#ds.rows}}", StringComparison.Ordinal)
                || bodyText.Contains("{{#rows}}", StringComparison.Ordinal);
            if (hasLoop)
            {
                var rowText = prototype.InnerText ?? string.Empty;
                if (!rowText.Contains("{{#ds.rows}}", StringComparison.Ordinal)
                    && !rowText.Contains("{{#rows}}", StringComparison.Ordinal))
                {
                    return content;
                }

                StripRowLoopMarkers(body);
            }

            var parent = prototype.Parent;
            if (parent == null)
                return content;

            var copies = new List<TableRow> { prototype };
            var insertAfter = prototype;
            for (var i = 1; i < rows.Count; i++)
            {
                var clone = (TableRow)prototype.CloneNode(deep: true);
                parent.InsertAfter(clone, insertAfter);
                copies.Add(clone);
                insertAfter = clone;
            }

            for (var i = 0; i < copies.Count; i++)
                FillRow(copies[i], rows[i]);

            document.MainDocumentPart!.Document.Save();
            document.Save();
        }

        return buffer.ToArray();
    }

    private static TableRow? FindPrototypeRow(Body body)
    {
        TableRow? best = null;
        var bestCells = 0;
        foreach (var row in body.Descendants<TableRow>())
        {
            var cells = row.Elements<TableCell>().Count(CellHasRowToken);
            if (cells < 2 || cells < bestCells)
                continue;
            bestCells = cells;
            best = row;
        }

        return best;
    }

    private static bool CellHasRowToken(TableCell cell) =>
        cell.Descendants<Paragraph>().Any(static p =>
            WordTemplateAddressing.GetParagraphText(p).Contains("{{.", StringComparison.Ordinal));

    private static void StripRowLoopMarkers(Body body)
    {
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var source = WordTemplateAddressing.GetParagraphText(paragraph);
            if (string.IsNullOrEmpty(source)
                || (!source.Contains("{{#ds.rows}}", StringComparison.Ordinal)
                    && !source.Contains("{{/ds.rows}}", StringComparison.Ordinal)
                    && !source.Contains("{{#rows}}", StringComparison.Ordinal)
                    && !source.Contains("{{/rows}}", StringComparison.Ordinal)))
            {
                continue;
            }

            var stripped = source
                .Replace("{{#ds.rows}}", string.Empty, StringComparison.Ordinal)
                .Replace("{{/ds.rows}}", string.Empty, StringComparison.Ordinal)
                .Replace("{{#rows}}", string.Empty, StringComparison.Ordinal)
                .Replace("{{/rows}}", string.Empty, StringComparison.Ordinal);
            if (stripped != source)
                ReplaceParagraphText(paragraph, stripped);
        }
    }

    private static void FillRow(TableRow row, IDictionary<string, object> data)
    {
        foreach (var paragraph in row.Descendants<Paragraph>())
        {
            var source = WordTemplateAddressing.GetParagraphText(paragraph);
            if (!source.Contains("{{.", StringComparison.Ordinal))
                continue;

            var filled = RowToken.Replace(source, match => Resolve(match.Groups[1].Value, data));
            if (filled == source)
                continue;

            ReplaceParagraphText(paragraph, filled);
        }
    }

    private static string Resolve(string code, IDictionary<string, object> data)
    {
        if (data.TryGetValue(code, out var value) && !IsMissing(value))
            return Convert.ToString(value) ?? string.Empty;

        var canonical = UserReportPlaceholderAliasRegistry.ResolveCanonicalPropertyPath(code);
        if (!string.Equals(canonical, code, StringComparison.OrdinalIgnoreCase)
            && data.TryGetValue(canonical, out value)
            && !IsMissing(value))
        {
            return Convert.ToString(value) ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool IsMissing(object? value) =>
        value == null || (value is string text && string.IsNullOrWhiteSpace(text));

    private static void ReplaceParagraphText(Paragraph paragraph, string value)
    {
        var texts = paragraph.Descendants<Text>().ToList();
        if (texts.Count == 0)
        {
            paragraph.AppendChild(new Run(new Text(value) { Space = SpaceProcessingModeValues.Preserve }));
            return;
        }

        texts[0].Text = value;
        if (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1])))
            texts[0].Space = SpaceProcessingModeValues.Preserve;

        for (var i = 1; i < texts.Count; i++)
            texts[i].Remove();
    }
}