using System.Text;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Comparison keys for matching document literals against instance values.
/// Casing always folds with the invariant culture: <c>tr-TR</c> rules would map <c>I</c>/<c>ı</c>
/// inconsistently and corrupt Turkmen and Turkish comparisons.
/// </summary>
public static class TemplateTextNormalizer
{
    /// <summary>Shortest literal worth matching. Below this, hits are noise ("1", "Mary").</summary>
    public const int MinimumMatchLength = 3;

    private static readonly Dictionary<char, char> FoldedCharacters = new()
    {
        ['ä'] = 'a',
        ['ç'] = 'c',
        ['ž'] = 'z',
        ['ň'] = 'n',
        ['ö'] = 'o',
        ['ş'] = 's',
        ['ü'] = 'u',
        ['ý'] = 'y',
        ['ı'] = 'i',
        ['ğ'] = 'g',
        ['â'] = 'a',
        ['î'] = 'i',
        ['û'] = 'u',
        ['é'] = 'e',
    };

    /// <summary>Trim, collapse internal whitespace, invariant lowercase.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        var lastWasWhitespace = false;
        foreach (var ch in value.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasWhitespace)
                    builder.Append(' ');
                lastWasWhitespace = true;
                continue;
            }

            lastWasWhitespace = false;
            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    /// <summary><see cref="Normalize"/> plus Turkmen and Turkish diacritic folding.</summary>
    public static string NormalizeFolded(string? value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
            return normalized;

        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
            builder.Append(Fold(ch));

        return builder.ToString();
    }

    /// <summary>Single-character fold, so callers that track offsets stay 1:1 with the source text.</summary>
    public static char Fold(char ch) =>
        FoldedCharacters.TryGetValue(ch, out var folded) ? folded : ch;

    public static bool IsIdentifierSeparator(char ch) =>
        ch is ' ' or '-' or '.' or '/' or '\\' or '_' or '#';

    /// <summary>Folded form without spaces or separators, so <c>T 12345-678</c> matches <c>T12345678</c>.</summary>
    public static string NormalizeIdentifier(string? value)
    {
        var folded = NormalizeFolded(value);
        if (folded.Length == 0)
            return folded;

        var builder = new StringBuilder(folded.Length);
        foreach (var ch in folded)
        {
            if (IsIdentifierSeparator(ch))
                continue;
            builder.Append(ch);
        }

        return builder.ToString();
    }

    public static bool IsMatchable(string? value) =>
        NormalizeFolded(value).Length >= MinimumMatchLength;
}
