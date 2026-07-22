namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Normalizes EmployeePositionHistory.ActualPosition titles for VISA2014 import/cleanup.
/// Numeric codes, punctuation-only strings, and empty values are not real titles — use "-".
/// </summary>
internal static class Visa2014ActualPositionNormalizer
{
    internal const string DashName = "-";

    /// <summary>
    /// True when <paramref name="name"/> contains at least one Unicode letter (a real word → likely a title).
    /// </summary>
    internal static bool ContainsLetter(string? name) =>
        !string.IsNullOrEmpty(name) && name.Any(char.IsLetter);

    /// <summary>
    /// Empty / whitespace / no alphabetic letter → "-"; otherwise trimmed original.
    /// Covers Position.Code-style values ("617-", "1 216", "209550-8-1-1226") and punctuation-only (".", "..").
    /// </summary>
    internal static string Normalize(string? raw)
    {
        var trimmed = raw?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return DashName;

        if (string.Equals(trimmed, DashName, StringComparison.Ordinal))
            return DashName;

        return ContainsLetter(trimmed) ? trimmed : DashName;
    }

    /// <summary>True when the value should be collapsed to "-" (not a usable position title).</summary>
    internal static bool IsNonTitlePlaceholder(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return true;
        if (string.Equals(trimmed, DashName, StringComparison.Ordinal))
            return false;
        return !ContainsLetter(trimmed);
    }
}