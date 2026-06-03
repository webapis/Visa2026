using System.Globalization;
using System.Text;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

/// <summary>
/// Normalized key comparison for catalog sync so re-runs match rows stored with ASCII-folded Turkmen titles.
/// </summary>
internal static class LookupCatalogMatchHelper
{
    public static bool KeysEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        var normalizedLeft = NormalizeKey(left);
        var normalizedRight = NormalizeKey(right);
        return normalizedLeft.Length > 0
            && string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
    }

    /// <summary>
    /// Lowercase, Turkmen ASCII fold, and strip combining marks (e.g. <c>Aýal</c> and <c>Ayal</c>).
    /// </summary>
    public static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var folded = FoldTurkmenChars(value.Trim());
        var decomposed = folded.Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            buffer.Append(char.ToLowerInvariant(ch));
        }

        return buffer.ToString();
    }

    private static string FoldTurkmenChars(string value)
    {
        if (value.Length == 0)
            return value;

        var buffer = new StringBuilder(value.Length);
        foreach (var ch in value)
            buffer.Append(FoldTurkmenChar(ch));

        return buffer.ToString();
    }

    private static char FoldTurkmenChar(char ch) => ch switch
    {
        'ý' or 'Ý' => 'y',
        'ä' or 'Ä' => 'a',
        'ö' or 'Ö' => 'o',
        'ü' or 'Ü' => 'u',
        'ç' or 'Ç' => 'c',
        'ş' or 'Ş' => 's',
        'ň' or 'Ň' => 'n',
        'ž' or 'Ž' => 'z',
        'î' or 'Î' => 'i',
        _ => ch,
    };
}
