using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Writes placeholder tokens into an existing <c>.docx</c> without touching anything else.
/// The token is inserted into the first <c>w:t</c> the span touches, so it inherits that run's
/// formatting and no run is ever created, merged, or removed.
/// </summary>
internal static class WordTemplateTokenWriter
{
    public static TokenWriteResult Write(
        byte[] sourceContent,
        IReadOnlyList<TokenSubstitution> substitutions,
        IReadOnlyList<LoopMarker> loops)
    {
        var applied = new List<TokenSubstitution>();
        var appliedLoops = new List<LoopMarker>();
        var skipped = new List<TemplateWriteSkip>();

        using var buffer = new MemoryStream();
        buffer.Write(sourceContent, 0, sourceContent.Length);
        buffer.Position = 0;

        using (var document = WordprocessingDocument.Open(buffer, true))
        {
            var paragraphs = WordTemplateAddressing.EnumerateParagraphs(document)
                .ToDictionary(static a => a.Address, static a => a.Paragraph, StringComparer.Ordinal);

            ApplySubstitutions(paragraphs, substitutions, applied, skipped);
            ApplyLoops(paragraphs, loops, appliedLoops, skipped);

            document.MainDocumentPart?.Document.Save();
            document.Save();
        }

        return new TokenWriteResult(buffer.ToArray(), applied, appliedLoops, skipped);
    }

    private static void ApplySubstitutions(
        IReadOnlyDictionary<string, Paragraph> paragraphs,
        IReadOnlyList<TokenSubstitution> substitutions,
        List<TokenSubstitution> applied,
        List<TemplateWriteSkip> skipped)
    {
        foreach (var group in substitutions.GroupBy(static s => (s.Region as DocumentRegion.WordSpan)?.ParagraphAddress))
        {
            if (group.Key == null)
            {
                foreach (var substitution in group)
                    skipped.Add(new TemplateWriteSkip(substitution.Region, substitution.Token, "Region is not a Word span."));
                continue;
            }

            if (!paragraphs.TryGetValue(group.Key, out var paragraph))
            {
                foreach (var substitution in group)
                    skipped.Add(new TemplateWriteSkip(substitution.Region, substitution.Token, $"Paragraph '{group.Key}' not found."));
                continue;
            }

            var spans = group
                .Select(static s => (Substitution: s, Span: (DocumentRegion.WordSpan)s.Region))
                .OrderByDescending(static x => x.Span.Start)
                .ToList();

            if (TemplateSpanEditor.HasOverlap(spans.Select(static x => (x.Span.Start, x.Span.Length)).ToList()))
            {
                foreach (var item in spans)
                    skipped.Add(new TemplateWriteSkip(item.Substitution.Region, item.Substitution.Token, "Overlapping spans in one paragraph."));
                continue;
            }

            foreach (var (substitution, span) in spans)
            {
                if (TryReplaceSpan(paragraph, span.Start, span.Length, TemplateTokenSyntax.Wrap(substitution.Token), out var reason))
                    applied.Add(substitution);
                else
                    skipped.Add(new TemplateWriteSkip(substitution.Region, substitution.Token, reason));
            }
        }
    }

    private static void ApplyLoops(
        IReadOnlyDictionary<string, Paragraph> paragraphs,
        IReadOnlyList<LoopMarker> loops,
        List<LoopMarker> applied,
        List<TemplateWriteSkip> skipped)
    {
        foreach (var loop in loops)
        {
            if (loop.Start is not DocumentRegion.WordSpan start || loop.End is not DocumentRegion.WordSpan end)
            {
                skipped.Add(new TemplateWriteSkip(loop.Start, loop.CollectionToken, "Loop boundaries are not Word spans."));
                continue;
            }

            if (!paragraphs.TryGetValue(start.ParagraphAddress, out var startParagraph)
                || !paragraphs.TryGetValue(end.ParagraphAddress, out var endParagraph))
            {
                skipped.Add(new TemplateWriteSkip(loop.Start, loop.CollectionToken, "Loop boundary paragraph not found."));
                continue;
            }

            PrependText(startParagraph, TemplateTokenSyntax.LoopOpen(loop.CollectionToken));
            AppendText(endParagraph, TemplateTokenSyntax.LoopClose(loop.CollectionToken));
            applied.Add(loop);
        }
    }

    private static bool TryReplaceSpan(Paragraph paragraph, int start, int length, string token, out string reason)
    {
        reason = string.Empty;
        var textNodes = paragraph.Descendants<Text>().ToList();
        var total = textNodes.Sum(static t => (t.Text ?? string.Empty).Length);

        if (length <= 0)
        {
            reason = "Span length must be positive.";
            return false;
        }

        if (start < 0 || start + length > total)
        {
            reason = $"Span [{start}, {start + length}) is outside paragraph text of length {total}.";
            return false;
        }

        var end = start + length;
        var position = 0;
        var tokenWritten = false;

        foreach (var text in textNodes)
        {
            var segment = text.Text ?? string.Empty;
            var segmentStart = position;
            var segmentEnd = position + segment.Length;
            position = segmentEnd;

            if (segmentEnd <= start || segmentStart >= end)
                continue;

            var removeStart = Math.Max(0, start - segmentStart);
            var removeEnd = Math.Min(segment.Length, end - segmentStart);
            var replacement = tokenWritten ? string.Empty : token;
            tokenWritten = true;

            var updated = segment[..removeStart] + replacement + segment[removeEnd..];
            SetText(text, updated);
        }

        return tokenWritten;
    }

    private static void PrependText(Paragraph paragraph, string value)
    {
        var first = paragraph.Descendants<Text>().FirstOrDefault();
        if (first == null)
        {
            paragraph.AppendChild(new Run(CreateText(value)));
            return;
        }

        SetText(first, value + (first.Text ?? string.Empty));
    }

    private static void AppendText(Paragraph paragraph, string value)
    {
        var last = paragraph.Descendants<Text>().LastOrDefault();
        if (last == null)
        {
            paragraph.AppendChild(new Run(CreateText(value)));
            return;
        }

        SetText(last, (last.Text ?? string.Empty) + value);
    }

    private static Text CreateText(string value) =>
        new(value) { Space = SpaceProcessingModeValues.Preserve };

    /// <summary>Leading or trailing spaces are dropped by consumers unless <c>xml:space</c> is preserved.</summary>
    private static void SetText(Text text, string value)
    {
        text.Text = value;
        if (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1])))
            text.Space = SpaceProcessingModeValues.Preserve;
    }
}
