#nullable enable

using System.Text.RegularExpressions;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Scores yellow sample literals by content shape against the placeholder manual.</summary>
public static class ScanShapeTokenMatcher
{
    private static readonly Regex DateLike = new(
        @"\b\d{1,2}[./-]\d{1,2}[./-]\d{2,4}(?:\s*ý\.?)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex PassportNumber = new(
        @"\b[A-Z]\d{6,9}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CountryCode = new(
        @"\b[A-Z]{3}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex VisaPeriod = new(
        @"\b\d+\s*\([^)]+\)\s*aý\b|\b\d+\s*aý\b|\b\([^)]+\)\s*aý\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex VisaCategory = new(
        @"\b(köp\s+gezeklik|bir\s+gezeklik|iki\s+gezeklik|üç\s+gezeklik|multiple|single)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> GenderWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "erkek", "ayal", "male", "female",
    };

    public static IReadOnlyList<ScanTokenAlternative> ScoreSnippet(
        string snippet,
        ApplicationProfilePlaceholderSet placeholderSet,
        UserReportPlaceholderScope usageScope,
        IReadOnlyList<string>? preferShortCodes = null)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        var text = snippet?.Trim() ?? string.Empty;
        if (text.Length == 0)
            return Array.Empty<ScanTokenAlternative>();

        var candidates = new List<ScanTokenAlternative>();

        void Prefer(string shortCode, int score, string reason)
        {
            if (!placeholderSet.Contains(shortCode))
                return;

            var entry = placeholderSet.Allowed.First(e =>
                string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));
            var boost = preferShortCodes?.Contains(shortCode, StringComparer.OrdinalIgnoreCase) == true ? 12 : 0;
            candidates.Add(new ScanTokenAlternative(
                entry.BuildWordToken(usageScope),
                shortCode,
                Math.Min(100, score + boost),
                reason));
        }

        var folded = TemplateTextNormalizer.NormalizeFolded(text);

        if (GenderWords.Contains(folded))
            Prefer("PGND", 88, "Gender word");

        if (CountryCode.IsMatch(text))
        {
            Prefer("PNAT", 70, "Three-letter country code");
            Prefer("PCBC", 62, "Three-letter country code");
            Prefer("PFAC", 58, "Three-letter country code");
        }

        if (DateLike.IsMatch(text))
        {
            Prefer("PDBT", 75, "Date shape");
            Prefer("PPED", 68, "Date shape");
            Prefer("ADAT", 40, "Date shape");
        }

        if (PassportNumber.IsMatch(text))
            Prefer("PPN", 90, "Passport number shape");

        if (VisaPeriod.IsMatch(text))
        {
            Prefer("AVPRD", 82, "Visa period phrase");
            Prefer("VPER", 70, "Visa period phrase");
        }

        if (VisaCategory.IsMatch(text))
        {
            Prefer("AVCAT", 82, "Visa category phrase");
            Prefer("VCAT", 70, "Visa category phrase");
        }

        if (int.TryParse(text, out var n) && n >= 0 && n <= 999)
            Prefer("RNUM", 85, "Row index number");

        if (text.Length >= TemplateTextNormalizer.MinimumMatchLength)
        {
            Prefer("PLN", 35, "Name-like text");
            Prefer("PFNM", 35, "Name-like text");
            Prefer("POSN", 30, "Position-like text");
            Prefer("EGSP", 30, "Specialty-like text");
            Prefer("EGIY", 28, "Education-like text");
            Prefer("PFAD", 28, "Address-like text");
            Prefer("ADRS", 26, "Address-like text");
            Prefer("PBPL", 24, "Place-like text");
        }

        return candidates
            .GroupBy(static c => c.ShortCode, StringComparer.OrdinalIgnoreCase)
            .Select(static g => g.OrderByDescending(static c => c.ScorePercent).First())
            .OrderByDescending(static c => c.ScorePercent)
            .ThenBy(static c => c.ShortCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
