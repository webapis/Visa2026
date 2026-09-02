#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Shared guessing pattern for every yellow mark: the printed text immediately
/// to the left and the parenthetical caption under the line, combined with value
/// shape. Used on Word letters and Excel headers — not a single-form special case.
/// </summary>
public static class ScanSurroundPlaceholderPattern
{
    public const int NearbyMinScore = 70;
    public const int ShapeOnlyMinScore = 80;

    public static IReadOnlyList<ScanTokenAlternative> Rank(
        string? yellowText,
        string? nearbyLabel,
        string? columnHeader,
        ApplicationProfilePlaceholderSet placeholderSet,
        UserReportPlaceholderScope usage)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);

        var text = yellowText?.Trim() ?? string.Empty;
        var nearby = Join(nearbyLabel, columnHeader);
        if (text.Length == 0 && nearby.Length == 0)
            return Array.Empty<ScanTokenAlternative>();

        var catalog = ScanPlaceholderCatalogIndex.Build(placeholderSet);
        var role = ScanLetterRoleHint.FromYellowAndNearby(text, nearbyLabel, columnHeader);
        var group = string.IsNullOrWhiteSpace(nearby)
            ? UserReportPlaceholderRelatedBo.Unknown
            : ScanCompoundLabelGroup.Identify(nearbyLabel, columnHeader, placeholderSet);
        var ranked = new Dictionary<string, ScanTokenAlternative>(StringComparer.OrdinalIgnoreCase);

        void Add(string code, int score, string reason)
        {
            if (score < 40 || !placeholderSet.Contains(code))
                return;

            var remapped = ScanFormCaptionHints.RemapByRole(code, role);
            if (!placeholderSet.Contains(remapped))
                remapped = code;
            if (!placeholderSet.Contains(remapped))
                return;

            var entry = placeholderSet.Allowed.First(e =>
                string.Equals(e.ShortCode, remapped, StringComparison.OrdinalIgnoreCase));
            var token = entry.BuildWordToken(
                entry.Scope == UserReportPlaceholderScope.Row
                    ? UserReportPlaceholderScope.Row
                    : usage);
            if (!ranked.TryGetValue(remapped, out var existing) || score > existing.ScorePercent)
                ranked[remapped] = new ScanTokenAlternative(token, remapped, Math.Min(100, score), reason);
        }

        foreach (var slot in ScanFormCaptionHints.Slots(nearby))
        {
            foreach (var preferred in ScanFormCaptionHints.PreferCodes(slot, role, nearby))
            {
                if (Fits(text, preferred))
                    Add(preferred, 94, "Immediate caption slot");
            }

            foreach (var (entry, score) in catalog.ScoreHeader(slot))
            {
                if (score < 55 || !Fits(text, entry.ShortCode))
                    continue;
                Add(entry.ShortCode, Math.Min(96, score + 16), "Immediate caption catalog");
            }
        }

        var stem = StemWithoutCaption(nearby);
        if (stem.Length >= 3)
        {
            foreach (var preferred in ScanFormFieldLabelHints.PreferCodes(stem, role))
            {
                if (LabelCompatible(text, preferred))
                    Add(preferred, 93, "Left field label");
            }

            foreach (var (entry, score) in catalog.ScoreHeader(stem))
            {
                if (score < 55 || !Fits(text, entry.ShortCode))
                    continue;
                Add(entry.ShortCode, Math.Min(94, score + 12), "Immediate left label");
            }
        }

        var preferFromCaption = ScanFormCaptionHints.Slots(nearby)
            .SelectMany(slot => ScanFormCaptionHints.PreferCodes(slot, role, nearby))
            .Concat(ScanFormFieldLabelHints.PreferCodes(stem, role))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var shape in ScanShapeTokenMatcher.ScoreSnippet(text, placeholderSet, usage))
        {
            var remapped = ScanFormCaptionHints.RemapByRole(shape.ShortCode, role);
            var entry = placeholderSet.Allowed.FirstOrDefault(e =>
                string.Equals(e.ShortCode, remapped, StringComparison.OrdinalIgnoreCase));
            var boost = 0;
            if (group != UserReportPlaceholderRelatedBo.Unknown
                && entry != null
                && entry.RelatedBo == group
                && (preferFromCaption.Count == 0
                    || preferFromCaption.Contains(shape.ShortCode, StringComparer.OrdinalIgnoreCase)
                    || preferFromCaption.Contains(remapped, StringComparer.OrdinalIgnoreCase)))
                boost += 20;
            if (preferFromCaption.Contains(shape.ShortCode)
                || preferFromCaption.Contains(remapped))
                boost += 12;
            Add(remapped, shape.ScorePercent + boost, "Value shape + surround");
        }

        return ranked.Values
            .OrderByDescending(static a => a.ScorePercent)
            .ThenBy(static a => a.ShortCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<ScanDetectedFieldDraft> TryDraft(
        string yellowText,
        int pageIndex,
        DocumentRegion? region,
        string? nearbyLabel,
        string? columnHeader,
        ApplicationProfilePlaceholderSet placeholderSet,
        HashSet<string> usedHeaderCodes,
        int minScore)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        ArgumentNullException.ThrowIfNull(usedHeaderCodes);

        var ranked = Rank(
            yellowText,
            nearbyLabel,
            columnHeader,
            placeholderSet,
            UserReportPlaceholderScope.Header);
        var top = ranked.FirstOrDefault();
        if (top == null || top.ScorePercent < minScore)
            return Array.Empty<ScanDetectedFieldDraft>();

        var entry = placeholderSet.Allowed.FirstOrDefault(e =>
            string.Equals(e.ShortCode, top.ShortCode, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            return Array.Empty<ScanDetectedFieldDraft>();

        var usage = entry.Scope == UserReportPlaceholderScope.Row
            ? UserReportPlaceholderScope.Row
            : UserReportPlaceholderScope.Header;
        if (usage == UserReportPlaceholderScope.Header && !usedHeaderCodes.Add(top.ShortCode))
            return Array.Empty<ScanDetectedFieldDraft>();

        return
        [
            new ScanDetectedFieldDraft
            {
                FieldId = Guid.NewGuid().ToString("N"),
                PageIndex = pageIndex,
                LabelText = yellowText,
                ProposedToken = entry.BuildWordToken(usage),
                Confidence = top.ScorePercent >= 80
                    ? ScanFieldConfidence.High
                    : ScanFieldConfidence.Medium,
                Scope = usage == UserReportPlaceholderScope.Row
                    ? ScanFieldScope.Row
                    : ScanFieldScope.Header,
                Box = ScanBoundingBox.FullPage,
                SourceRegion = region,
                NearbyLabel = nearbyLabel,
                ColumnHeader = columnHeader,
                Alternatives = ranked.Take(5).ToList(),
            },
        ];
    }

    internal static int MinScore(string? nearbyLabel, string? columnHeader) =>
        string.IsNullOrWhiteSpace(Join(nearbyLabel, columnHeader))
            ? ShapeOnlyMinScore
            : NearbyMinScore;

    private static bool Fits(string yellowText, string code)
    {
        if (string.IsNullOrWhiteSpace(yellowText) || string.IsNullOrWhiteSpace(code))
            return false;
        if (ScanCompoundYellowParts.IsCommaCombination(yellowText))
        {
            var parts = ScanCompoundYellowParts.SplitSegments(yellowText);
            return parts.Any(p => ScanCompoundYellowParts.SegmentFitsCode(p.Text, code));
        }

        return ScanCompoundYellowParts.SegmentFitsCode(yellowText, code);
    }

    /// <summary>
    /// Field-label prefers may map long text (position, specialty, previous workplace)
    /// that <see cref="ScanCompoundYellowParts.SegmentFitsCode"/> keeps conservative so
    /// comma binders do not steal names as birth-place or authority as job title.
    /// </summary>
    private static bool LabelCompatible(string yellowText, string code)
    {
        if (Fits(yellowText, code))
            return true;
        if (string.IsNullOrWhiteSpace(yellowText) || string.IsNullOrWhiteSpace(code))
            return false;
        if (ScanCompoundYellowParts.DateLikeShape.IsMatch(yellowText)
            || ScanCompoundYellowParts.PhoneShape.IsMatch(yellowText)
            || ScanCompoundYellowParts.LooksLikePassportNumber(yellowText))
            return false;

        return code.Equals("POSN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("ACPOS", StringComparison.OrdinalIgnoreCase)
            || code.Equals("EGSP", StringComparison.OrdinalIgnoreCase)
            || code.Equals("EGLV", StringComparison.OrdinalIgnoreCase)
            || code.Equals("EGIN", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PWTM", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PVFM", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PFWC", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PFAD", StringComparison.OrdinalIgnoreCase)
            || code.Equals("PBPL", StringComparison.OrdinalIgnoreCase)
            || code.Equals("RGEL", StringComparison.OrdinalIgnoreCase)
            || code.Equals("ACNAM", StringComparison.OrdinalIgnoreCase);
    }

    private static string Join(string? nearbyLabel, string? columnHeader) =>
        string.Join(
            " ",
            new[] { nearbyLabel, columnHeader }.Where(static s => !string.IsNullOrWhiteSpace(s)));

    private static string StemWithoutCaption(string nearby)
    {
        if (string.IsNullOrWhiteSpace(nearby))
            return string.Empty;
        var without = System.Text.RegularExpressions.Regex.Replace(nearby, @"\([^)]*\)", " ");
        return TemplateTextNormalizer.NormalizeFolded(without);
    }
}