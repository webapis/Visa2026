using System.Globalization;
using DevExpress.Data.Filtering;

namespace Visa2026.Module.Services;

/// <summary>
/// Filter tabs on the Employees and Family Members lists: All, Search, and Incomplete.
/// Search shows every match for the role. Incomplete is the incomplete subset.
/// </summary>
public static class PersonRosterColorTab
{
    public const string CriteriaKey = "RosterColorTab";

    public const string All = "all";
    public const string Search = "search";
    public const string Incomplete = "incomplete";

    public readonly record struct TabInfo(string Key, string Label, string BackgroundHex, int Count);

    public static bool IsSearch(string? key) =>
        string.Equals(key?.Trim(), Search, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? key)
    {
        if (IsSearch(key))
            return Search;
        if (string.Equals(key?.Trim(), Incomplete, StringComparison.OrdinalIgnoreCase))
            return Incomplete;
        return All;
    }

    public static string Label(string key)
    {
        var name = CultureInfo.CurrentUICulture.Name ?? string.Empty;
        var turkish = name.StartsWith("tr", StringComparison.OrdinalIgnoreCase);
        var turkmen = name.StartsWith("tk", StringComparison.OrdinalIgnoreCase);
        var russian = name.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        if (IsSearch(key))
            return turkish ? "Arama" : turkmen ? "Gözleg" : russian ? "Поиск" : "Search";
        if (string.Equals(Normalize(key), Incomplete, StringComparison.Ordinal))
            return turkish ? "Eksik" : turkmen ? "Doly däl" : russian ? "Неполные" : "Incomplete";
        return turkish ? "Tümü" : turkmen ? "Hemmesi" : russian ? "Все" : "All";
    }

    public static string BackgroundHex(string key) =>
        string.Equals(Normalize(key), Incomplete, StringComparison.Ordinal) ? "#fdba74" : "#94a3b8";

    public static CriteriaOperator? CriteriaFor(string? key) =>
        string.Equals(Normalize(key), Incomplete, StringComparison.Ordinal)
            ? CriteriaOperator.Parse("[IsDataIncomplete] = True")
            : null;

    public static TabInfo Describe(string key, int count) =>
        new(Normalize(key), Label(key), IsSearch(key) ? string.Empty : BackgroundHex(key), count);
}
