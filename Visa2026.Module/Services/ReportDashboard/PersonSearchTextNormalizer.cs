using System.Globalization;
using System.Text;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Folds Turkish and common Latin diacritics so person search matches ASCII typed terms
/// against accented names (e.g. typing "u" matches "u with diaeresis").
/// Applied to query tokens and to <c>vw_rd_person_search.SearchText</c> (SQL translate).
/// </summary>
public static class PersonSearchTextNormalizer
{
    /// <summary>
    /// Characters passed to SQL <c>translate</c> after <c>lower</c>. Keep in sync with <see cref="Fold"/>.
    /// Length of <see cref="SqlFoldFrom"/> must equal <see cref="SqlFoldTo"/>.
    /// </summary>
    public const string SqlFoldFrom =
        "\u00e0\u00e1\u00e2\u00e3\u00e4\u00e5\u00e8\u00e9\u00ea\u00eb\u00ec\u00ed\u00ee\u00ef" +
        "\u00f2\u00f3\u00f4\u00f5\u00f6\u00f9\u00fa\u00fb\u00fc\u00fd\u00ff\u00f1\u00e7" +
        "\u011f\u0131\u015f";

    public const string SqlFoldTo =
        "aaaaaaeeeeiiiiooooouuuuyync" +
        "gis";

    public static string Fold(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (char ch in value.Normalize(NormalizationForm.FormD))
        {
            char mapped = ch switch
            {
                '\u0130' => 'i', // Turkish capital I with dot
                '\u0131' => 'i', // Turkish dotless i
                '\u011e' => 'g', // G-breve
                '\u011f' => 'g',
                '\u015e' => 's', // S-cedilla
                '\u015f' => 's',
                '\u00c7' => 'c',
                '\u00e7' => 'c',
                _ => ch
            };

            if (CharUnicodeInfo.GetUnicodeCategory(mapped) == UnicodeCategory.NonSpacingMark)
                continue;

            sb.Append(char.ToLowerInvariant(mapped));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}