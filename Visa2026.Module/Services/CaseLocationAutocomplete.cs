using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services;

/// <summary>
/// Location multi-select helpers: 100-char normalize, chip parse/format, search excluding selected.
/// Reuses <see cref="CaseAddressAutocomplete"/> fold / rank / highlight.
/// </summary>
public static class CaseLocationAutocomplete
{
    public const int MaxLength = 100;
    public const int MaxResults = CaseAddressAutocomplete.MaxResults;
    public const int LimitTags = 3;

    public static string NormalizeDisplay(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        var collapsed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length > MaxLength)
            collapsed = collapsed[..MaxLength];
        return collapsed;
    }

    public static string FoldKey(string? text) =>
        CaseAddressAutocomplete.FoldKey(NormalizeDisplay(text));

    public static CaseAddressAutocomplete.SearchResult Search(
        string? query,
        IEnumerable<CaseAddressAutocomplete.SavedEntry> store,
        IEnumerable<string>? excludeLabels = null)
    {
        var exclude = new HashSet<string>(StringComparer.Ordinal);
        if (excludeLabels != null)
        {
            foreach (var label in excludeLabels)
            {
                var key = FoldKey(label);
                if (!string.IsNullOrEmpty(key))
                    exclude.Add(key);
            }
        }

        var filtered = store.Where(item => !exclude.Contains(FoldKey(item.Text)));
        var q = NormalizeDisplay(query);
        // Search with address MaxLength path via FoldKey on normalized query — call Search then re-filter.
        // CaseAddressAutocomplete.Search uses its own NormalizeDisplay (500). Pass already-normalized q.
        return CaseAddressAutocomplete.Search(q, filtered);
    }

    public static bool IsExactSaved(string? text, IEnumerable<CaseAddressAutocomplete.SavedEntry> store) =>
        CaseAddressAutocomplete.IsExactSaved(NormalizeDisplay(text), store);

    public static IReadOnlyList<(int Start, int End)> HighlightRanges(string text, string? query) =>
        CaseAddressAutocomplete.HighlightRanges(text, NormalizeDisplay(query));

    /// <summary>Split stored comma/semicolon/newline string into chip labels (never drops empty-only noise).</summary>
    public static IReadOnlyList<string> ParseChips(string? stored, string? noneValue = null)
    {
        if (CommaSeparatedSelectionHelper.IsNoneValue(stored, noneValue))
            return Array.Empty<string>();

        var parts = stored!
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in parts)
        {
            var label = NormalizeDisplay(part);
            if (string.IsNullOrEmpty(label) || CommaSeparatedSelectionHelper.IsNoneValue(label, noneValue))
                continue;
            if (!seen.Add(FoldKey(label)))
                continue;
            result.Add(label);
        }

        return result;
    }

    /// <summary>Join chips for BorderZone (default none Ýok) or WorkPermit (empty none).</summary>
    public static string FormatChips(IEnumerable<string>? chips, string? noneValue = null) =>
        CommaSeparatedSelectionHelper.FormatSelected(
            chips?.Select(NormalizeDisplay).Where(c => !string.IsNullOrEmpty(c)),
            noneValue);
}