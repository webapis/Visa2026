using System.Globalization;
using System.Security.Cryptography;
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

    /// <summary>Matches <see cref="BusinessObjects.LookupBase.LocalizationKey"/> max length (64).</summary>
    public const int LocalizationKeyMaxLength = 64;

    /// <summary>
    /// Normalizes then fits within <see cref="LocalizationKeyMaxLength"/>; long keys get an 8-char hash suffix.
    /// </summary>
    public static string ToLocalizationKey(string? value)
    {
        var normalized = NormalizeKey(value);
        if (normalized.Length == 0)
            return string.Empty;

        if (normalized.Length <= LocalizationKeyMaxLength)
            return normalized;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))
            .ToLowerInvariant()[..8];
        var prefixLength = LocalizationKeyMaxLength - 1 - hash.Length;
        return normalized[..prefixLength] + "_" + hash;
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
