#nullable enable

using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Splits one yellow span that holds several library tokens into Review sub-rows (6.1, 6.2, …)
/// and overlay segments. Generate still writes the parent compound token onto the original span.
/// </summary>
public sealed record ScanCompoundPart(
    int Index,
    string SegmentText,
    string? Token,
    string ShortCode,
    int Offset,
    int Length);

public static class ScanCompoundYellowParts
{
    internal static readonly Regex PassportNumberShape = new(
        @"\b[A-Z]\d{6,9}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static readonly Regex TmInternalPassportShape = new(
        @"\bI[-–]?\s*A[ŞS]\s*\d{5,8}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static readonly Regex PhoneShape = new(
        @"\+?\s*993[\s\-]?\d{2}[\s\-]?\d{2}[\s\-]?\d{2}[\s\-]?\d{2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static readonly Regex DateLikeShape = new(
        @"\b\d{1,2}[./-]\d{1,2}[./-]\d{4}(?:\s*[\u00FD\u00DD]\.?)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static bool LooksLikePassportNumber(string? text)
    {
        var value = text ?? string.Empty;
        return PassportNumberShape.IsMatch(value) || TmInternalPassportShape.IsMatch(value);
    }

    /// <summary>
    /// A yellow highlight with a comma is a combination candidate (several placeholders, 6.1 / 6.2 / …).
    /// Short numeric pairs such as 1,5 are not treated as combinations.
    /// </summary>
    public static bool IsCommaCombination(string? labelText)
    {
        var label = labelText ?? string.Empty;
        if (!label.Contains(',', StringComparison.Ordinal))
            return false;

        var segments = SplitByDelimiter(label, ',');
        if (segments.Count < 2)
            return false;

        return !segments.All(static s =>
            s.Text.Length <= 3 && s.Text.All(char.IsDigit));
    }

    public static IReadOnlyList<ScanCompoundPart> Split(string? labelText, string? proposedToken)
    {
        var codes = TemplateTokenSyntax.GetShortCodes(proposedToken);
        var tokens = SplitTokens(proposedToken);
        var label = labelText ?? string.Empty;

        if (IsCommaCombination(label))
            return AlignParts(SplitByDelimiter(label, ','), codes, tokens);

        if (codes.Count <= 1)
            return Array.Empty<ScanCompoundPart>();

        var segments = SplitSegments(label);
        var parts = new List<ScanCompoundPart>(codes.Count);

        if (segments.Count >= codes.Count)
        {
            for (var i = 0; i < codes.Count; i++)
            {
                var segment = segments[i];
                parts.Add(new ScanCompoundPart(
                    i + 1,
                    segment.Text,
                    i < tokens.Count ? tokens[i] : null,
                    codes[i],
                    segment.Offset,
                    segment.Length));
            }

            return parts;
        }

        var window = Math.Max(1, label.Trim().Length / codes.Count);
        var trimmedStart = label.Length - label.TrimStart().Length;
        for (var i = 0; i < codes.Count; i++)
        {
            var offset = Math.Min(label.Length, trimmedStart + i * window);
            var length = i == codes.Count - 1
                ? Math.Max(1, label.TrimEnd().Length - offset)
                : Math.Min(window, Math.Max(1, label.Length - offset));
            var text = offset < label.Length
                ? label.Substring(offset, Math.Min(length, label.Length - offset)).Trim()
                : label.Trim();
            if (text.Length == 0)
                text = codes[i];

            parts.Add(new ScanCompoundPart(
                i + 1,
                text,
                i < tokens.Count ? tokens[i] : null,
                codes[i],
                offset,
                Math.Max(1, text.Length)));
        }

        return parts;
    }

    public static DocumentRegion? SliceRegion(DocumentRegion? parent, string? labelText, ScanCompoundPart part)
    {
        if (parent is DocumentRegion.WordSpan span)
        {
            var start = span.Start + Math.Clamp(part.Offset, 0, Math.Max(0, span.Length));
            var length = Math.Min(Math.Max(1, part.Length), Math.Max(0, span.Start + span.Length - start));
            if (length <= 0)
                return span;
            return new DocumentRegion.WordSpan(span.ParagraphAddress, start, length);
        }

        return parent;
    }

    public static IReadOnlyList<(string Text, int Offset, int Length)> SplitSegments(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return Array.Empty<(string, int, int)>();

        if (label.Contains(',', StringComparison.Ordinal))
            return SplitByDelimiter(label, ',');

        if (label.Contains('/', StringComparison.Ordinal))
            return SplitByDelimiter(label, '/');

        var trimmed = label.Trim();
        var lead = label.Length - label.TrimStart().Length;
        return [(trimmed, lead, trimmed.Length)];
    }

    private static IReadOnlyList<ScanCompoundPart> AlignParts(
        IReadOnlyList<(string Text, int Offset, int Length)> segments,
        IReadOnlyList<string> codes,
        IReadOnlyList<string> tokens)
    {
        var assignedCode = new string?[segments.Count];
        var assignedToken = new string?[segments.Count];

        if (codes.Count == segments.Count)
        {
            for (var i = 0; i < codes.Count; i++)
            {
                assignedCode[i] = codes[i];
                assignedToken[i] = i < tokens.Count ? tokens[i] : null;
            }
        }
        else
        {
            for (var i = 0; i < codes.Count; i++)
            {
                var slot = BestUnusedSlot(segments, codes[i], assignedCode);
                if (slot < 0)
                    break;
                assignedCode[slot] = codes[i];
                assignedToken[slot] = i < tokens.Count ? tokens[i] : null;
            }
        }

        var parts = new List<ScanCompoundPart>(segments.Count);
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            parts.Add(new ScanCompoundPart(
                i + 1,
                segment.Text,
                assignedToken[i],
                assignedCode[i] ?? string.Empty,
                segment.Offset,
                segment.Length));
        }

        return parts;
    }

    private static int BestUnusedSlot(
        IReadOnlyList<(string Text, int Offset, int Length)> segments,
        string code,
        IReadOnlyList<string?> assigned)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            if (assigned[i] == null && SegmentFitsCode(segments[i].Text, code))
                return i;
        }

        for (var i = 0; i < assigned.Count; i++)
        {
            if (assigned[i] == null)
                return i;
        }

        return -1;
    }

    internal static bool SegmentFitsCode(string segment, string code)
    {
        if (code.Equals("PPN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("RPPN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("CHPN", StringComparison.OrdinalIgnoreCase))
            return LooksLikePassportNumber(segment);

        if (code.Equals("PPED", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PDBT", StringComparison.OrdinalIgnoreCase)
            || code.Equals("ADAT", StringComparison.OrdinalIgnoreCase)
            || code.Equals("ACRDT", StringComparison.OrdinalIgnoreCase)
            || code.EndsWith("DT", StringComparison.OrdinalIgnoreCase)
            || code.EndsWith("ED", StringComparison.OrdinalIgnoreCase)
            || code.Equals("RPPD", StringComparison.OrdinalIgnoreCase)
            || code.Equals("CHPD", StringComparison.OrdinalIgnoreCase)
            || code.Equals("CHPE", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PPIS", StringComparison.OrdinalIgnoreCase))
            return DateLikeShape.IsMatch(segment);

        if (code.Equals("PFN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("RPFN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("CHFN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("ACFNM", StringComparison.OrdinalIgnoreCase))
            return ScanShapeTokenMatcher.LooksLikePersonFullName(segment)
                || ScanShapeTokenMatcher.LooksLikeTitledPersonName(segment);

        if (code.Equals("PLN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PFNM", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PMNM", StringComparison.OrdinalIgnoreCase))
            return LooksLikeSingleNameWord(segment);

        if (code.Equals("PNAT", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PCBC", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PPCC", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PFAC", StringComparison.OrdinalIgnoreCase)
            || code.Equals("EGCC", StringComparison.OrdinalIgnoreCase))
            return LooksLikeIso3(segment);

        if (code.Equals("PPIN", StringComparison.OrdinalIgnoreCase))
            return LooksLikePersonalNumber(segment);

        if (code.Equals("PGND", StringComparison.OrdinalIgnoreCase))
            return ScanShapeTokenMatcher.LooksLikeGenderWord(segment);

        if (code.Equals("CSAL", StringComparison.OrdinalIgnoreCase))
            return ScanShapeTokenMatcher.LooksLikeMoneyAmount(segment);

        if (code.Equals("ACPHN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("RPPH", StringComparison.OrdinalIgnoreCase))
            return PhoneShape.IsMatch(segment);

        if (code.Equals("ACADR", StringComparison.OrdinalIgnoreCase))
            return LooksLikeStreetAddress(segment);

        if (code.Equals("PPAT", StringComparison.OrdinalIgnoreCase)
            || code.Equals("RPPA", StringComparison.OrdinalIgnoreCase)
            || code.Equals("CHPA", StringComparison.OrdinalIgnoreCase))
            return segment.Length >= 8
                && !DateLikeShape.IsMatch(segment)
                && !PhoneShape.IsMatch(segment)
                && !LooksLikePassportNumber(segment)
                && !ScanShapeTokenMatcher.LooksLikePersonFullName(segment);

        if (code.Equals("ACTAX", StringComparison.OrdinalIgnoreCase)
            || code.Equals("ACRGL", StringComparison.OrdinalIgnoreCase))
            return segment.Any(char.IsDigit)
                && !DateLikeShape.IsMatch(segment)
                && !PhoneShape.IsMatch(segment);

        return false;
    }

    private static bool LooksLikeIso3(string segment)
    {
        var trimmed = segment.Trim();
        return trimmed.Length == 3 && trimmed.All(static ch => ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
    }

    private static bool LooksLikePersonalNumber(string segment)
    {
        var digits = segment.Where(char.IsDigit).Count();
        return digits >= 7 && digits <= 15 && segment.Any(char.IsLetter) == false;
    }

    private static bool LooksLikeSingleNameWord(string segment)
    {
        var trimmed = segment.Trim().Trim('_', ' ', '-');
        if (trimmed.Length < 2)
            return false;
        var words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 1 && words[0].Count(char.IsLetter) >= 2;
    }

    internal static bool LooksLikeStreetAddress(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment)
            || DateLikeShape.IsMatch(segment)
            || PhoneShape.IsMatch(segment)
            || ScanShapeTokenMatcher.LooksLikePersonFullName(segment))
            return false;

        var folded = TemplateTextNormalizer.NormalizeFolded(segment);
        if (folded.Contains("sayol", StringComparison.Ordinal)
            || folded.Contains("salgy", StringComparison.Ordinal)
            || folded.Contains("kocesi", StringComparison.Ordinal)
            || folded.Contains("street", StringComparison.Ordinal)
            || folded.Contains("address", StringComparison.Ordinal)
            || folded.Contains(" yuridiki", StringComparison.Ordinal))
            return true;

        if (segment.Length >= 8
            && (segment.Any(char.IsDigit)
                || segment.Contains(',', StringComparison.Ordinal)
                || segment.Contains('.', StringComparison.Ordinal)))
            return true;

        return segment.Length >= 16;
    }

    private static IReadOnlyList<(string Text, int Offset, int Length)> SplitByDelimiter(string label, char delimiter)
    {
        var list = new List<(string Text, int Offset, int Length)>();
        var start = 0;
        for (var i = 0; i <= label.Length; i++)
        {
            if (i != label.Length && label[i] != delimiter)
                continue;

            var raw = label[start..i];
            var trimmed = raw.Trim();
            if (trimmed.Length > 0)
            {
                var lead = raw.Length - raw.TrimStart().Length;
                list.Add((trimmed, start + lead, trimmed.Length));
            }

            start = i + 1;
        }

        return list;
    }

    private static IReadOnlyList<string> SplitTokens(string? proposedToken)
    {
        if (string.IsNullOrWhiteSpace(proposedToken))
            return Array.Empty<string>();

        var tokens = new List<string>();
        var remaining = proposedToken.AsSpan();
        while (true)
        {
            var start = remaining.IndexOf("{{".AsSpan(), StringComparison.Ordinal);
            if (start < 0)
                break;

            remaining = remaining[start..];
            var end = remaining.IndexOf("}}".AsSpan(), StringComparison.Ordinal);
            if (end < 0)
                break;

            tokens.Add(remaining[..(end + 2)].ToString());
            remaining = remaining[(end + 2)..];
        }

        return tokens;
    }
}
