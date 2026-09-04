#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Scores column headers and labels against the profile placeholder manual (catalog).</summary>
public sealed class ScanPlaceholderCatalogIndex
{
    private readonly IReadOnlyList<(UserReportPlaceholderCatalogEntry Entry, string NormalizedKey)> _keys;

    private ScanPlaceholderCatalogIndex(IReadOnlyList<(UserReportPlaceholderCatalogEntry Entry, string NormalizedKey)> keys)
    {
        _keys = keys;
    }

    public static ScanPlaceholderCatalogIndex Build(ApplicationProfilePlaceholderSet placeholderSet)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        var keys = new List<(UserReportPlaceholderCatalogEntry, string)>();

        foreach (var entry in placeholderSet.Allowed)
        {
            AddKey(keys, entry, entry.ShortCode);
            AddKey(keys, entry, entry.LabelEn);
            AddKey(keys, entry, entry.LabelTk);
            AddKey(keys, entry, entry.LabelTr);
            AddKey(keys, entry, entry.LabelRu);

            var pathTail = entry.CanonicalPath;
            var slash = pathTail.LastIndexOf('_');
            if (slash >= 0 && slash < pathTail.Length - 1)
                AddKey(keys, entry, pathTail[(slash + 1)..]);
            AddKey(keys, entry, pathTail);
        }

        return new ScanPlaceholderCatalogIndex(keys);
    }

    public IReadOnlyList<(UserReportPlaceholderCatalogEntry Entry, int Score)> ScoreHeader(string? headerText)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(headerText);
        if (folded.Length == 0)
            return Array.Empty<(UserReportPlaceholderCatalogEntry, int)>();

        var scores = new Dictionary<string, (UserReportPlaceholderCatalogEntry Entry, int Score)>(StringComparer.OrdinalIgnoreCase);

        foreach (var (entry, key) in _keys)
        {
            if (key.Length < 2)
                continue;

            var score = 0;
            if (string.Equals(folded, key, StringComparison.Ordinal))
                score = 100;
            else if (folded.Contains(key, StringComparison.Ordinal) || key.Contains(folded, StringComparison.Ordinal))
                score = Math.Max(score, 72);
            else if (HeaderWordsOverlap(folded, key))
                score = 55;

            if (score == 0)
                continue;

            if (!scores.TryGetValue(entry.ShortCode, out var existing) || score > existing.Score)
                scores[entry.ShortCode] = (entry, score);
        }

        return scores.Values
            .OrderByDescending(static x => x.Score)
            .ThenBy(static x => x.Entry.ShortCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddKey(
        List<(UserReportPlaceholderCatalogEntry Entry, string NormalizedKey)> keys,
        UserReportPlaceholderCatalogEntry entry,
        string? raw)
    {
        var folded = TemplateTextNormalizer.NormalizeFolded(raw);
        if (folded.Length < 2)
            return;

        if (!keys.Any(k => k.Entry.ShortCode == entry.ShortCode && k.NormalizedKey == folded))
            keys.Add((entry, folded));
    }

    private static bool HeaderWordsOverlap(string header, string key)
    {
        var headerWords = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keyWords = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return headerWords.Any(hw => keyWords.Any(kw => hw.Length >= 4 && kw.Length >= 4
            && (hw.Contains(kw, StringComparison.Ordinal) || kw.Contains(hw, StringComparison.Ordinal))));
    }
}
