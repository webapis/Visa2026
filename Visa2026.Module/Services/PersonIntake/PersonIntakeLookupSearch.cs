using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Search, rank, and browse for person intake wizard lookup fields (same fold rules as
/// <see cref="CaseAddressAutocomplete"/>, keyed by lookup record id instead of text).
/// </summary>
public static class PersonIntakeLookupSearch
{
    public const int MaxResults = 6;

    /// <summary>Lists with this many entries or fewer open in full on click or focus.</summary>
    public const int ShortListThreshold = 15;

    /// <summary>Separator between a leading code and the name, e.g. "VIR — ABŞ-nyň Wirgin adalary".</summary>
    public const string CodeSeparator = " — ";

    public sealed record Entry(Guid Id, string Text, int UseCount);

    public static string WithCode(string code, string name) => code.Trim() + CodeSeparator + name;

    /// <summary>Text after a leading code, or the whole text when there is no code.</summary>
    public static string NamePart(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var at = text.IndexOf(CodeSeparator, StringComparison.Ordinal);
        return at < 0 ? text : text[(at + CodeSeparator.Length)..];
    }

    public sealed record Result(IReadOnlyList<Entry> Items, int Total);

    public static bool IsShortList(int count) => count > 0 && count <= ShortListThreshold;

    public static Result Search(string? query, IEnumerable<Entry> entries, Guid? excludeId = null)
    {
        var tokens = Tokens(query);
        if (tokens.Count == 0 || entries == null)
            return new Result(Array.Empty<Entry>(), 0);

        var first = tokens[0];
        var hits = entries
            .Where(entry => entry != null && entry.Id != excludeId)
            .Select(entry => (
                Entry: entry,
                Key: CaseAddressAutocomplete.FoldKey(entry.Text),
                NameKey: CaseAddressAutocomplete.FoldKey(NamePart(entry.Text))))
            .Where(item => tokens.All(token => item.Key.Contains(token, StringComparison.Ordinal)))
            .OrderByDescending(item =>
                item.Key.StartsWith(first, StringComparison.Ordinal)
                || item.NameKey.StartsWith(first, StringComparison.Ordinal) ? 1 : 0)
            .ThenByDescending(item => item.Entry.UseCount)
            .ThenBy(item => item.Entry.Text, StringComparer.CurrentCultureIgnoreCase)
            .Select(item => item.Entry)
            .ToList();

        return new Result(hits.Take(MaxResults).ToList(), hits.Count);
    }

    /// <summary>Whole list for short lookups, most used first.</summary>
    public static IReadOnlyList<Entry> Browse(IEnumerable<Entry> entries, Guid? excludeId = null)
    {
        if (entries == null)
            return Array.Empty<Entry>();

        return entries
            .Where(entry => entry != null && entry.Id != excludeId)
            .OrderByDescending(entry => entry.UseCount)
            .ThenBy(entry => entry.Text, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Entry whose folded text equals the query, or null.</summary>
    public static Entry? ExactMatch(string? text, IEnumerable<Entry> entries)
    {
        var key = CaseAddressAutocomplete.FoldKey(text);
        if (string.IsNullOrEmpty(key) || entries == null)
            return null;

        return entries.FirstOrDefault(entry =>
                   entry != null && string.Equals(CaseAddressAutocomplete.FoldKey(entry.Text), key, StringComparison.Ordinal))
               ?? entries.FirstOrDefault(entry =>
                   entry != null && string.Equals(CaseAddressAutocomplete.FoldKey(NamePart(entry.Text)), key, StringComparison.Ordinal));
    }

    private static List<string> Tokens(string? query) =>
        CaseAddressAutocomplete.FoldKey(query)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
}
