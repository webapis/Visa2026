#nullable enable

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Finds library <c>{{…}}</c> clusters in a saved Word/Excel template so Review can reopen
/// after yellow highlighter has been stripped.
/// </summary>
public static class ScanOfficeLibraryTokenExtractor
{
    public const string FieldPlanSource = "existing-tokens";

    public static IReadOnlyList<ScanOfficeYellowSpan> Extract(
        byte[] officeBytes,
        ScanSourceKind kind,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        ArgumentNullException.ThrowIfNull(officeBytes);
        ArgumentNullException.ThrowIfNull(placeholderSet);
        if (officeBytes.Length < 64)
            return Array.Empty<ScanOfficeYellowSpan>();

        return kind switch
        {
            ScanSourceKind.Word => ExtractWord(officeBytes, placeholderSet),
            ScanSourceKind.Excel => ExtractExcel(officeBytes, placeholderSet),
            _ => Array.Empty<ScanOfficeYellowSpan>(),
        };
    }

    internal static IReadOnlyList<(int Start, int Length, string Text)> ClusterLibraryTokens(
        string paragraphText,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        var results = new List<(int Start, int Length, string Text)>();
        if (string.IsNullOrEmpty(paragraphText))
            return results;

        var i = 0;
        while (i < paragraphText.Length)
        {
            var open = paragraphText.IndexOf("{{", i, StringComparison.Ordinal);
            if (open < 0)
                break;
            var close = paragraphText.IndexOf("}}", open + 2, StringComparison.Ordinal);
            if (close < 0)
                break;

            var inner = paragraphText[(open + 2)..close];
            if (!IsLibraryPlaceholder(inner, placeholderSet))
            {
                i = close + 2;
                continue;
            }

            var start = open;
            var end = close + 2;
            while (true)
            {
                var nextOpen = paragraphText.IndexOf("{{", end, StringComparison.Ordinal);
                if (nextOpen < 0)
                    break;
                var between = paragraphText[end..nextOpen];
                if (!IsTokenJoiner(between))
                    break;
                var nextClose = paragraphText.IndexOf("}}", nextOpen + 2, StringComparison.Ordinal);
                if (nextClose < 0)
                    break;
                var nextInner = paragraphText[(nextOpen + 2)..nextClose];
                if (!IsLibraryPlaceholder(nextInner, placeholderSet))
                    break;
                end = nextClose + 2;
            }

            results.Add((start, end - start, paragraphText[start..end]));
            i = end;
        }

        return results;
    }

    internal static bool IsTokenJoiner(string between)
    {
        foreach (var c in between)
        {
            if (char.IsWhiteSpace(c) || c is ',' or '/' or '|' or ';' or '·')
                continue;
            return false;
        }

        return true;
    }

    internal static bool IsLibraryPlaceholder(string tokenInner, ApplicationProfilePlaceholderSet placeholderSet)
    {
        var trimmed = (tokenInner ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed[0] is '#' or '/')
            return false;

        return TemplateTokenSyntax.TryGetShortCode("{{" + trimmed + "}}", out var code)
            && placeholderSet.Contains(code);
    }

    private static IReadOnlyList<ScanOfficeYellowSpan> ExtractWord(
        byte[] bytes,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var results = new List<ScanOfficeYellowSpan>();

        foreach (var addressed in WordTemplateAddressing.EnumerateParagraphs(document))
        {
            var fullText = WordTemplateAddressing.GetParagraphText(addressed.Paragraph);
            if (string.IsNullOrWhiteSpace(fullText))
                continue;

            foreach (var cluster in ClusterLibraryTokens(fullText, placeholderSet))
            {
                results.Add(new ScanOfficeYellowSpan
                {
                    Text = cluster.Text,
                    Region = new DocumentRegion.WordSpan(
                        addressed.Address,
                        cluster.Start,
                        cluster.Length),
                    PageIndex = 0,
                });
            }
        }

        return results;
    }

    private static IReadOnlyList<ScanOfficeYellowSpan> ExtractExcel(
        byte[] bytes,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var workbook = new XLWorkbook(stream);
        var results = new List<ScanOfficeYellowSpan>();

        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
            return results;

        foreach (var cell in sheet.CellsUsed())
        {
            var text = cell.GetFormattedString()?.Trim() ?? string.Empty;
            if (text.Length == 0)
                text = cell.GetString()?.Trim() ?? string.Empty;
            if (text.Length == 0)
                continue;

            var clusters = ClusterLibraryTokens(text, placeholderSet);
            if (clusters.Count == 0)
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
}