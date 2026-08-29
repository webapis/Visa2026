#nullable enable

using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Splits compound yellow-highlight text and maps snippets to library tokens when the vision
/// model returns a single unmapped field for multiple values (e.g. number+date, count+period+category).
/// </summary>
public static class ScanYellowHighlightTokenResolver
{
    private static readonly Regex AppNumber = new(
        @"№?\s*\d+\s*/\s*-?\s*\d+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex DateLike = new(
        @"\b\d{1,2}[./-]\d{1,2}[./-]\d{2,4}(?:\s*ý\.?)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex CountWithWords = new(
        @"\b(\d+)\s*\(([^)]+)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex VisaPeriod = new(
        @"\b\d+\s*\([^)]+\)\s*aý\b|\b\d+\s*aý\b|\b\([^)]+\)\s*aý\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex VisaCategory = new(
        @"\b(köp\s+gezeklik|bir\s+gezeklik|iki\s+gezeklik|üç\s+gezeklik|multiple|single)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex Urgency = new(
        @"\b(Adaty|Gyssagly|Oran\s+gyssagly)\s+tertipde\s*!?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static IReadOnlyList<ScanDetectedFieldDraft> ResolveFromYellowText(
        string? yellowText,
        ScanBoundingBox box,
        int pageIndex,
        ApplicationProfilePlaceholderSet placeholderSet,
        HashSet<string> usedShortCodes,
        DocumentRegion? sourceRegion = null)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        ArgumentNullException.ThrowIfNull(usedShortCodes);

        var text = yellowText?.Trim() ?? string.Empty;
        if (text.Length == 0)
            return Array.Empty<ScanDetectedFieldDraft>();

        var drafts = new List<ScanDetectedFieldDraft>();
        var candidates = ExtractCandidates(text).ToList();
        foreach (var (snippet, code, index, length) in candidates)
        {
            if (string.IsNullOrWhiteSpace(snippet) || !placeholderSet.Contains(code) || !usedShortCodes.Add(code))
                continue;

            var entry = placeholderSet.Allowed.First(e =>
                string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));

            drafts.Add(new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                PageIndex = pageIndex,
                LabelText = snippet.Trim(),
                ProposedToken = entry.BuildWordToken(
                    entry.Scope == UserReportPlaceholderScope.Row
                        ? UserReportPlaceholderScope.Row
                        : UserReportPlaceholderScope.Header),
                Confidence = ScanFieldConfidence.High,
                Scope = entry.Scope == UserReportPlaceholderScope.Row ? ScanFieldScope.Row : ScanFieldScope.Header,
                Box = SliceBox(box, text.Length, index, length),
                SourceRegion = SliceRegion(sourceRegion, text.Length, index, length),
            });
        }

        return drafts;
    }

    private static DocumentRegion? SliceRegion(DocumentRegion? parent, int textLength, int index, int length)
    {
        if (parent is null || textLength <= 0 || length <= 0)
            return parent;

        if (parent is DocumentRegion.WordSpan span)
        {
            // index/length are relative to trimmed yellow text; parent span may include leading spaces.
            // Best-effort: offset within the yellow mark's paragraph span.
            var start = span.Start + Math.Clamp(index, 0, Math.Max(0, span.Length - 1));
            var len = Math.Min(length, Math.Max(0, span.Start + span.Length - start));
            if (len <= 0)
                return span;
            return new DocumentRegion.WordSpan(span.ParagraphAddress, start, len);
        }

        // Excel cells are atomic — compound splits share the same cell (last write wins unless split earlier).
        return parent;
    }

    /// <summary>
    /// True when every library token we can recognize in the yellow text is already mapped
    /// (AI often re-emits the compound span as a null-token gap after splitting).
    /// </summary>
    public static bool IsYellowTextFullyMapped(
        string? yellowText,
        ApplicationProfilePlaceholderSet placeholderSet,
        IReadOnlyCollection<string> usedShortCodes)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        ArgumentNullException.ThrowIfNull(usedShortCodes);

        var text = yellowText?.Trim() ?? string.Empty;
        if (text.Length == 0)
            return false;

        var codes = ExtractCandidates(text)
            .Select(static c => c.ShortCode)
            .Where(placeholderSet.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return codes.Count > 0
            && codes.All(c => usedShortCodes.Contains(c));
    }

    private static ScanBoundingBox SliceBox(ScanBoundingBox parent, int textLength, int index, int length)
    {
        var box = parent.Clamp();
        if (textLength <= 0 || length <= 0)
            return box;

        var width = box.Right - box.Left;
        var height = box.Bottom - box.Top;
        // Prefer horizontal slice for wide bands; vertical for tall ones.
        if (width >= height * 1.4)
        {
            var left = box.Left + width * index / textLength;
            var right = box.Left + width * Math.Min(textLength, index + length) / textLength;
            if (right - left < width * 0.08)
                right = Math.Min(box.Right, left + width * 0.08);
            return new ScanBoundingBox(left, box.Top, right, box.Bottom).Clamp();
        }

        var top = box.Top + height * index / textLength;
        var bottom = box.Top + height * Math.Min(textLength, index + length) / textLength;
        if (bottom - top < height * 0.08)
            bottom = Math.Min(box.Bottom, top + height * 0.08);
        return new ScanBoundingBox(box.Left, top, box.Right, bottom).Clamp();
    }

    private static IEnumerable<(string Snippet, string ShortCode, int Index, int Length)> ExtractCandidates(string text)
    {
        foreach (Match m in Urgency.Matches(text))
            yield return (m.Value, "Urgency_NameTm", m.Index, m.Length);

        foreach (Match m in AppNumber.Matches(text))
            yield return (m.Value, "AFNUM", m.Index, m.Length);

        foreach (Match m in DateLike.Matches(text))
            yield return (m.Value, "ADAT", m.Index, m.Length);

        foreach (Match m in CountWithWords.Matches(text))
        {
            var after = text[(m.Index + m.Length)..];
            if (after.TrimStart().StartsWith("aý", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return (m.Groups[1].Value, "TPCNT", m.Groups[1].Index, m.Groups[1].Length);
            yield return (m.Groups[2].Value.Trim(), "TPCTX", m.Groups[2].Index, m.Groups[2].Length);
        }

        foreach (Match m in VisaPeriod.Matches(text))
            yield return (m.Value.Trim(), "VPER", m.Index, m.Length);

        foreach (Match m in VisaCategory.Matches(text))
            yield return (m.Value.Trim(), "VCAT", m.Index, m.Length);
    }
}