#nullable enable

using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Comma-combination guessing starts from the printed label: identify the catalog
/// <see cref="UserReportPlaceholderRelatedBo"/> group, then pick unused codes in that group only.
/// </summary>
public static class ScanCompoundLabelGroup
{
    private static readonly Regex Parenthetical = new(
        @"\([^)]*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> CompositeShortCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "EGIY", "RPCL", "CHPL", "ACRGL", "RPPL",
    };

    public static UserReportPlaceholderRelatedBo Identify(
        string? nearbyLabel,
        string? columnHeader,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);

        var role = ScanLetterRoleHint.FromNearbyText(nearbyLabel, columnHeader);
        if (role == ScanLetterRole.Wekil)
            return UserReportPlaceholderRelatedBo.AuthorizedRepresentative;
        if (role == ScanLetterRole.Signatory)
            return UserReportPlaceholderRelatedBo.CompanySignatory;
        if (role == ScanLetterRole.Applicant)
            return UserReportPlaceholderRelatedBo.Person;

        var stem = Stem(nearbyLabel, columnHeader);
        if (stem.Length == 0)
            return UserReportPlaceholderRelatedBo.Unknown;

        var scores = new Dictionary<UserReportPlaceholderRelatedBo, int>();

        void Add(UserReportPlaceholderRelatedBo relatedBo, int points)
        {
            if (relatedBo == UserReportPlaceholderRelatedBo.Unknown || points <= 0)
                return;
            scores.TryGetValue(relatedBo, out var current);
            scores[relatedBo] = current + points;
        }

        foreach (var relatedBo in placeholderSet.Allowed
                     .Select(static e => e.RelatedBo)
                     .Where(static b => b != UserReportPlaceholderRelatedBo.Unknown)
                     .Distinct())
        {
            var display = TemplateTextNormalizer.NormalizeFolded(
                UserReportPlaceholderRelatedBoCatalog.DisplayNameEn(relatedBo));
            var enumName = TemplateTextNormalizer.NormalizeFolded(relatedBo.ToString());
            if (display.Length >= 4 && StemMatches(stem, display))
                Add(relatedBo, stem == display ? 100 : 82);
            if (enumName.Length >= 6 && StemMatches(stem, enumName))
                Add(relatedBo, 80);
        }

        var catalog = ScanPlaceholderCatalogIndex.Build(placeholderSet);
        foreach (var (entry, score) in catalog.ScoreHeader(stem))
            Add(entry.RelatedBo, score);

        if (LooksLikeEducation(stem))
            Add(UserReportPlaceholderRelatedBo.Education, 90);
        if (LooksLikeHiredPerson(stem))
            Add(UserReportPlaceholderRelatedBo.Person, 92);
        if (stem.Contains("pasport", StringComparison.Ordinal))
            Add(UserReportPlaceholderRelatedBo.Passport, 70);
        if (stem.Contains("karhana", StringComparison.Ordinal)
            || stem.Contains("yuridiki", StringComparison.Ordinal))
            Add(UserReportPlaceholderRelatedBo.CompanyProfile, 80);

        if (scores.Count == 0)
            return UserReportPlaceholderRelatedBo.Unknown;

        var ranked = scores
            .OrderByDescending(static p => p.Value)
            .ThenBy(static p => UserReportPlaceholderRelatedBoCatalog.SortOrder(p.Key))
            .ToList();
        var best = ranked[0];
        if (best.Value < 55)
            return UserReportPlaceholderRelatedBo.Unknown;

        return best.Key;
    }

    public static IReadOnlyList<string>? BindParts(
        IReadOnlyList<(string Text, int Offset, int Length)> segments,
        ApplicationProfilePlaceholderSet placeholderSet,
        UserReportPlaceholderRelatedBo group,
        string? nearbyLabel = null)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(placeholderSet);
        if (group == UserReportPlaceholderRelatedBo.Unknown || segments.Count < 2)
            return null;

        var nearbyFolded = TemplateTextNormalizer.NormalizeFolded(nearbyLabel);
        var entries = placeholderSet.Allowed
            .Where(e => e.RelatedBo == group && !CompositeShortCodes.Contains(e.ShortCode))
            .ToList();
        if (entries.Count < 2)
            return null;

        var ranked = new List<(int Segment, string Code, int Score)>();
        for (var i = 0; i < segments.Count; i++)
        {
            foreach (var entry in entries)
            {
                var score = ScoreSegment(segments[i].Text, entry)
                    + DatePreferenceBoost(entry.ShortCode, nearbyFolded);
                if (score >= 40)
                    ranked.Add((i, entry.ShortCode, score));
            }
        }

        var assigned = new string?[segments.Count];
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hit in ranked
                     .OrderByDescending(static h => h.Score)
                     .ThenBy(static h => h.Code, StringComparer.OrdinalIgnoreCase))
        {
            if (assigned[hit.Segment] != null || !used.Add(hit.Code))
                continue;
            assigned[hit.Segment] = hit.Code;
        }

        if (assigned.Any(static c => c == null))
            return null;

        return assigned.Select(static c => c!).ToList();
    }

    internal static int ScoreSegment(string segment, UserReportPlaceholderCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var text = (segment ?? string.Empty).Trim();
        if (text.Length == 0)
            return 0;

        var folded = TemplateTextNormalizer.NormalizeFolded(text);
        var example = TemplateTextNormalizer.NormalizeFolded(entry.ExampleValue);
        if (HasUsableExample(example))
        {
            if (folded == example)
                return 100;
            if (folded.Length >= 8 && example.Length >= 8
                && (folded.Contains(example, StringComparison.Ordinal)
                    || example.Contains(folded, StringComparison.Ordinal)))
                return 90;
            if (WordsOverlap(folded, example))
                return 82;
        }

        if (IsIso3(text))
            return LooksLikeCountryCode(entry.ShortCode) ? 88 : 0;

        if (IsGraduationYear(text))
            return entry.ShortCode.Equals("EGYR", StringComparison.OrdinalIgnoreCase) ? 90 : 0;

        if (ScanCompoundYellowParts.SegmentFitsCode(text, entry.ShortCode))
            return 85;

        var words = WordCount(text);
        if (entry.ShortCode.Equals("EGIN", StringComparison.OrdinalIgnoreCase) && words >= 2 && !IsIso3(text))
            return 76;
        if (entry.ShortCode.Equals("EGLV", StringComparison.OrdinalIgnoreCase) && words == 1 && !IsIso3(text))
            return 70;

        return 0;
    }

    private static int DatePreferenceBoost(string shortCode, string nearbyFolded)
    {
        if (string.IsNullOrEmpty(nearbyFolded))
            return 0;

        if (nearbyFolded.Contains("doglan", StringComparison.Ordinal))
        {
            if (shortCode.Equals("PDBT", StringComparison.OrdinalIgnoreCase))
                return 20;
            if (shortCode.Equals("ACRDT", StringComparison.OrdinalIgnoreCase)
                || shortCode.Equals("ADAT", StringComparison.OrdinalIgnoreCase))
                return -15;
        }

        if (!nearbyFolded.Contains("mohlet", StringComparison.Ordinal))
            return 0;

        if (shortCode.Equals("CHPE", StringComparison.OrdinalIgnoreCase)
            || shortCode.Equals("PPED", StringComparison.OrdinalIgnoreCase))
            return 20;

        if (shortCode.Equals("CHPD", StringComparison.OrdinalIgnoreCase)
            || shortCode.Equals("RPPD", StringComparison.OrdinalIgnoreCase))
            return -15;

        return 0;
    }

    private static string Stem(params string?[] fragments)
    {
        var joined = string.Join(
            " ",
            fragments.Where(static s => !string.IsNullOrWhiteSpace(s)));
        if (joined.Length == 0)
            return string.Empty;

        var withoutSlots = Parenthetical.Replace(joined, " ");
        return TemplateTextNormalizer.NormalizeFolded(withoutSlots);
    }

    private static bool StemMatches(string stem, string key) =>
        stem == key
        || (key.Length >= 4 && (stem.Contains(key, StringComparison.Ordinal) || key.Contains(stem, StringComparison.Ordinal)));

    private static bool LooksLikeHiredPerson(string stem) =>
        stem.Contains("cagryl", StringComparison.Ordinal)
        || stem.Contains("cagrylan adam", StringComparison.Ordinal)
        || stem.Contains("hired person", StringComparison.Ordinal)
        || stem.Contains("invitee", StringComparison.Ordinal);

    private static bool LooksLikeEducation(string stem) =>
        stem.Contains("bilim", StringComparison.Ordinal)
        || stem.Contains("okuw", StringComparison.Ordinal)
        || stem.Contains("okan", StringComparison.Ordinal)
        || stem.Contains("education", StringComparison.Ordinal)
        || stem.Contains("hunar", StringComparison.Ordinal);

    private static bool HasUsableExample(string example) =>
        example.Length >= 2
        && !example.Contains("...", StringComparison.Ordinal)
        && !example.Contains("\u2026", StringComparison.Ordinal);

    private static bool IsIso3(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length == 3 && trimmed.All(static ch => ch is >= 'A' and <= 'Z');
    }

    private static bool IsGraduationYear(string text) =>
        text.Length == 4
        && int.TryParse(text, out var year)
        && year is >= 1950 and <= 2040;

    private static bool LooksLikeCountryCode(string shortCode) =>
        shortCode.Equals("EGCC", StringComparison.OrdinalIgnoreCase)
        || shortCode.Equals("PNAT", StringComparison.OrdinalIgnoreCase)
        || shortCode.Equals("PPCC", StringComparison.OrdinalIgnoreCase)
        || shortCode.Equals("PCBC", StringComparison.OrdinalIgnoreCase)
        || shortCode.Equals("PFAC", StringComparison.OrdinalIgnoreCase)
        || shortCode.EndsWith("CC", StringComparison.OrdinalIgnoreCase);

    private static int WordCount(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static bool WordsOverlap(string left, string right)
    {
        var leftWords = left.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rightWords = right.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return leftWords.Any(lw => lw.Length >= 5 && rightWords.Any(rw =>
            rw.Length >= 5
            && (lw.Contains(rw, StringComparison.Ordinal) || rw.Contains(lw, StringComparison.Ordinal))));
    }
}