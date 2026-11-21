using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

/// <summary>
/// Fold / token match / rank / highlight ranges for case Address 1–2 autocomplete.
/// </summary>
public static class CaseAddressAutocomplete
{
    public const int MaxResults = 6;
    public const int MaxLength = 500;

    public sealed record SavedEntry(string Text, int UseCount, DateTime UsedAtUtc);

    public sealed record SearchResult(IReadOnlyList<SavedEntry> Items, int Total);

    public static string NormalizeDisplay(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        var collapsed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length > MaxLength)
            collapsed = collapsed[..MaxLength];
        return collapsed;
    }

    public static string FoldKey(string? text)
    {
        // Length-preserving fold (spec §6.1) so highlight indices map to original text.
        var folded = ApplySpecMap(NormalizeDisplay(text)).ToLowerInvariant();
        if (folded.Length > 450)
            folded = folded[..450];
        return folded;
    }

    public static SearchResult Search(string? query, IEnumerable<SavedEntry> store)
    {
        var q = NormalizeDisplay(query);
        if (string.IsNullOrEmpty(q))
            return new SearchResult(Array.Empty<SavedEntry>(), 0);

        var tokens = Tokens(q);
        if (tokens.Count == 0)
            return new SearchResult(Array.Empty<SavedEntry>(), 0);

        var first = tokens[0];
        var hits = store
            .Where(item =>
            {
                var f = FoldKey(item.Text);
                return tokens.All(t => f.Contains(t, StringComparison.Ordinal));
            })
            .OrderByDescending(item => FoldKey(item.Text).StartsWith(first, StringComparison.Ordinal) ? 1 : 0)
            .ThenByDescending(item => item.UseCount)
            .ThenByDescending(item => item.UsedAtUtc)
            .ToList();

        return new SearchResult(hits.Take(MaxResults).ToList(), hits.Count);
    }

    public static bool IsExactSaved(string? text, IEnumerable<SavedEntry> store)
    {
        var key = FoldKey(text);
        if (string.IsNullOrEmpty(key))
            return false;
        return store.Any(item => string.Equals(FoldKey(item.Text), key, StringComparison.Ordinal));
    }

    /// <summary>Merged highlight ranges in original text (fold length-preserving).</summary>
    public static IReadOnlyList<(int Start, int End)> HighlightRanges(string text, string? query)
    {
        var tokens = Tokens(query ?? string.Empty);
        if (tokens.Count == 0 || string.IsNullOrEmpty(text))
            return Array.Empty<(int, int)>();

        var folded = FoldKey(text);
        // Fold may change length vs FormD stripping — for highlight use FormD-safe fold on same string.
        // PersonSearchTextNormalizer.Fold can change length; use index on folded and map 1:1 only when lengths match.
        if (folded.Length != text.Length)
        {
            // Fallback: highlight on folded indices clamped to text length (best-effort).
            folded = PersonSearchTextNormalizer.Fold(text);
        }

        var ranges = new List<(int, int)>();
        foreach (var token in tokens)
        {
            if (token.Length == 0)
                continue;
            var i = 0;
            while (i <= folded.Length - token.Length)
            {
                var at = folded.IndexOf(token, i, StringComparison.Ordinal);
                if (at < 0)
                    break;
                ranges.Add((at, at + token.Length));
                i = at + token.Length;
            }
        }

        if (ranges.Count == 0)
            return Array.Empty<(int, int)>();

        ranges.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        var merged = new List<(int, int)>();
        foreach (var r in ranges)
        {
            if (merged.Count > 0 && r.Item1 <= merged[^1].Item2)
            {
                var last = merged[^1];
                merged[^1] = (last.Item1, Math.Max(last.Item2, r.Item2));
            }
            else
                merged.Add(r);
        }

        // Clamp to original text length.
        return merged
            .Select(r => (Math.Min(r.Item1, text.Length), Math.Min(r.Item2, text.Length)))
            .Where(r => r.Item2 > r.Item1)
            .ToList();
    }

    private static List<string> Tokens(string query)
    {
        var folded = FoldKey(query);
        return folded.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static string ApplySpecMap(string folded)
    {
        if (string.IsNullOrEmpty(folded))
            return string.Empty;
        var sb = new StringBuilder(folded.Length);
        foreach (var ch in folded)
        {
            sb.Append(ch switch
            {
                'ý' or 'Ý' => 'y',
                'ň' or 'Ň' => 'n',
                'ž' or 'Ž' => 'z',
                'ä' or 'Ä' => 'a',
                'ö' or 'Ö' => 'o',
                'ü' or 'Ü' => 'u',
                'ğ' or 'Ğ' => 'g',
                'ç' or 'Ç' => 'c',
                'ş' or 'Ş' => 's',
                'ı' or 'I' => 'i',
                'İ' => 'i',
                _ => ch
            });
        }
        return sb.ToString();
    }
}