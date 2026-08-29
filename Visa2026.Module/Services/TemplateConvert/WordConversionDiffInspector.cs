using System.Security.Cryptography;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Proves a converted <c>.docx</c> differs from its source only by the approved token substitutions.
/// Compares structural and formatting invariants rather than raw bytes, because the OpenXml SDK
/// legitimately renormalises parts it rewrites.
/// </summary>
internal static class WordConversionDiffInspector
{
    public static void Inspect(TemplateDiffGateRequest request, List<string> violations)
    {
        using var originalStream = new MemoryStream(request.OriginalContent, writable: false);
        using var convertedStream = new MemoryStream(request.ConvertedContent, writable: false);
        using var original = WordprocessingDocument.Open(originalStream, false);
        using var converted = WordprocessingDocument.Open(convertedStream, false);

        var originalMain = original.MainDocumentPart;
        var convertedMain = converted.MainDocumentPart;
        if (originalMain == null || convertedMain == null)
        {
            violations.Add("Word document has no main document part.");
            return;
        }

        CompareParts(originalMain, convertedMain, violations);
        CompareFormattingParts(originalMain, convertedMain, violations);
        CompareSectionProperties(originalMain, convertedMain, violations);
        CompareTableShape(originalMain, convertedMain, violations);
        CompareImageParts(originalMain, convertedMain, violations);
        CompareParagraphs(original, converted, request, violations);
    }

    private static void CompareParts(MainDocumentPart original, MainDocumentPart converted, List<string> violations)
    {
        if (original.HeaderParts.Count() != converted.HeaderParts.Count())
            violations.Add("Header part count changed.");

        if (original.FooterParts.Count() != converted.FooterParts.Count())
            violations.Add("Footer part count changed.");
    }

    private static void CompareFormattingParts(MainDocumentPart original, MainDocumentPart converted, List<string> violations)
    {
        Compare("styles.xml", original.StyleDefinitionsPart?.Styles?.OuterXml, converted.StyleDefinitionsPart?.Styles?.OuterXml);
        Compare("numbering.xml", original.NumberingDefinitionsPart?.Numbering?.OuterXml, converted.NumberingDefinitionsPart?.Numbering?.OuterXml);
        Compare("theme", original.ThemePart?.Theme?.OuterXml, converted.ThemePart?.Theme?.OuterXml);

        void Compare(string label, string? left, string? right)
        {
            if (!string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal))
                violations.Add($"Formatting part '{label}' changed.");
        }
    }

    private static void CompareSectionProperties(MainDocumentPart original, MainDocumentPart converted, List<string> violations)
    {
        var left = original.Document?.Body?.Elements<SectionProperties>().Select(static s => s.OuterXml).ToList() ?? new List<string>();
        var right = converted.Document?.Body?.Elements<SectionProperties>().Select(static s => s.OuterXml).ToList() ?? new List<string>();

        if (!left.SequenceEqual(right, StringComparer.Ordinal))
            violations.Add("Section properties (page setup) changed.");
    }

    private static void CompareTableShape(MainDocumentPart original, MainDocumentPart converted, List<string> violations)
    {
        var left = original.Document?.Body;
        var right = converted.Document?.Body;
        if (left == null || right == null)
            return;

        CompareCount("table", left.Descendants<Table>().Count(), right.Descendants<Table>().Count());
        CompareCount("table row", left.Descendants<TableRow>().Count(), right.Descendants<TableRow>().Count());
        CompareCount("table cell", left.Descendants<TableCell>().Count(), right.Descendants<TableCell>().Count());

        void CompareCount(string label, int leftCount, int rightCount)
        {
            if (leftCount != rightCount)
                violations.Add($"{label} count changed ({leftCount} -> {rightCount}).");
        }
    }

    private static void CompareImageParts(MainDocumentPart original, MainDocumentPart converted, List<string> violations)
    {
        var left = HashImageParts(original);
        var right = HashImageParts(converted);

        if (left.Count != right.Count)
        {
            violations.Add($"Image part count changed ({left.Count} -> {right.Count}).");
            return;
        }

        if (!left.OrderBy(static h => h, StringComparer.Ordinal).SequenceEqual(right.OrderBy(static h => h, StringComparer.Ordinal), StringComparer.Ordinal))
            violations.Add("Image content changed.");
    }

    private static List<string> HashImageParts(MainDocumentPart part)
    {
        var hashes = new List<string>();
        foreach (var imagePart in part.ImageParts)
        {
            using var stream = imagePart.GetStream();
            hashes.Add(Convert.ToHexString(SHA256.HashData(ReadAll(stream))));
        }

        return hashes;
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static void CompareParagraphs(
        WordprocessingDocument original,
        WordprocessingDocument converted,
        TemplateDiffGateRequest request,
        List<string> violations)
    {
        var left = WordTemplateAddressing.EnumerateParagraphs(original);
        var right = WordTemplateAddressing.EnumerateParagraphs(converted);

        if (left.Count != right.Count)
        {
            violations.Add($"Paragraph count changed ({left.Count} -> {right.Count}).");
            return;
        }

        var expectations = WordTextExpectation.Build(request);

        for (var i = 0; i < left.Count; i++)
        {
            var address = left[i].Address;
            if (!string.Equals(address, right[i].Address, StringComparison.Ordinal))
            {
                violations.Add($"Paragraph order changed at index {i}.");
                continue;
            }

            var originalText = WordTemplateAddressing.GetParagraphText(left[i].Paragraph);
            var expected = expectations.Expect(address, originalText);
            var actual = WordTemplateAddressing.GetParagraphText(right[i].Paragraph);

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                violations.Add($"Text at '{address}' does not match the approved substitutions.");

            CompareRunFormatting(address, left[i].Paragraph, right[i].Paragraph, violations);
        }
    }

    private static void CompareRunFormatting(string address, Paragraph original, Paragraph converted, List<string> violations)
    {
        var left = original.Elements<Run>().Select(static r => FingerprintRunFormatting(r)).ToList();
        var right = converted.Elements<Run>().Select(static r => FingerprintRunFormatting(r)).ToList();

        if (left.Count != right.Count)
        {
            violations.Add($"Run count changed at '{address}' ({left.Count} -> {right.Count}).");
            return;
        }

        if (!left.SequenceEqual(right, StringComparer.Ordinal))
            violations.Add($"Run formatting changed at '{address}'.");

        var leftParagraphMark = original.ParagraphProperties?.OuterXml ?? string.Empty;
        var rightParagraphMark = converted.ParagraphProperties?.OuterXml ?? string.Empty;
        if (!string.Equals(leftParagraphMark, rightParagraphMark, StringComparison.Ordinal))
            violations.Add($"Paragraph formatting changed at '{address}'.");
    }

    /// <summary>
    /// Ignores yellow highlighter / yellowish shading so Create-from-yellow-marks can strip marks
    /// after token write without failing the Convert/Scan shared diff gate.
    /// </summary>
    private static string FingerprintRunFormatting(Run run)
    {
        var props = run.RunProperties;
        if (props == null)
            return string.Empty;

        var ghost = new Run((RunProperties)props.CloneNode(deep: true));
        WordTemplateTokenWriter.ClearHighlightMark(ghost);
        return ghost.RunProperties?.OuterXml ?? string.Empty;
    }
}

/// <summary>Derives the text the gate expects per paragraph from the approved substitutions and loops.</summary>
internal sealed class WordTextExpectation
{
    private readonly Dictionary<string, List<(int Start, int Length, string Text)>> _edits;
    private readonly Dictionary<string, string> _prefixes;
    private readonly Dictionary<string, string> _suffixes;

    private WordTextExpectation(
        Dictionary<string, List<(int Start, int Length, string Text)>> edits,
        Dictionary<string, string> prefixes,
        Dictionary<string, string> suffixes)
    {
        _edits = edits;
        _prefixes = prefixes;
        _suffixes = suffixes;
    }

    public static WordTextExpectation Build(TemplateDiffGateRequest request)
    {
        var edits = new Dictionary<string, List<(int, int, string)>>(StringComparer.Ordinal);
        var prefixes = new Dictionary<string, string>(StringComparer.Ordinal);
        var suffixes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var substitution in request.Substitutions)
        {
            if (substitution.Region is not DocumentRegion.WordSpan span)
                continue;

            if (!edits.TryGetValue(span.ParagraphAddress, out var list))
            {
                list = new List<(int, int, string)>();
                edits[span.ParagraphAddress] = list;
            }

            list.Add((span.Start, span.Length, TemplateTokenSyntax.Wrap(substitution.Token)));
        }

        foreach (var loop in request.Loops)
        {
            if (loop.Start is DocumentRegion.WordSpan start)
            {
                prefixes.TryGetValue(start.ParagraphAddress, out var existing);
                prefixes[start.ParagraphAddress] = TemplateTokenSyntax.LoopOpen(loop.CollectionToken) + existing;
            }

            if (loop.End is DocumentRegion.WordSpan end)
            {
                suffixes.TryGetValue(end.ParagraphAddress, out var existing);
                suffixes[end.ParagraphAddress] = existing + TemplateTokenSyntax.LoopClose(loop.CollectionToken);
            }
        }

        return new WordTextExpectation(edits, prefixes, suffixes);
    }

    public string Expect(string address, string originalText)
    {
        var text = _edits.TryGetValue(address, out var edits)
            ? TemplateSpanEditor.Apply(originalText, edits)
            : originalText;

        _prefixes.TryGetValue(address, out var prefix);
        _suffixes.TryGetValue(address, out var suffix);
        return (prefix ?? string.Empty) + text + (suffix ?? string.Empty);
    }
}
