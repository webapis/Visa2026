#nullable enable

using System.Globalization;
using System.Text;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Review Add-placeholder list: remaining library codes, optional search (including
/// <c>CompanySignatory</c>), grouped by related BO.
/// </summary>
public static class ScanPlaceholderChoiceList
{
    public static IReadOnlyList<UserReportPlaceholderCatalogGroup> RemainingGroups(
        IEnumerable<UserReportPlaceholderCatalogEntry> allowed,
        IEnumerable<string> hideShortCodes,
        string? search = null)
    {
        ArgumentNullException.ThrowIfNull(allowed);

        var hide = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (hideShortCodes != null)
        {
            foreach (var code in hideShortCodes)
            {
                if (!string.IsNullOrWhiteSpace(code))
                    hide.Add(code.Trim());
            }
        }

        IEnumerable<UserReportPlaceholderCatalogEntry> remaining = allowed.Where(e =>
            !hide.Contains(e.ShortCode));

        if (!string.IsNullOrWhiteSpace(search))
            remaining = remaining.Where(e => MatchesSearch(e, search));

        return UserReportPlaceholderRelatedBoCatalog.Group(remaining);
    }

    public static bool MatchesSearch(UserReportPlaceholderCatalogEntry entry, string? search)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var term = (search ?? string.Empty).Trim();
        if (term.Length == 0)
            return true;

        foreach (var variant in ExpandTerms(term))
        {
            if (MatchesFields(entry, variant))
                return true;
        }

        return false;
    }

    private static bool MatchesFields(UserReportPlaceholderCatalogEntry entry, string term) =>
        Contains(entry.ShortCode, term)
        || Contains(entry.CanonicalPath, term)
        || Contains(entry.LabelEn, term)
        || Contains(entry.LabelTk, term)
        || Contains(entry.LabelRu, term)
        || Contains(entry.LabelTr, term)
        || Contains(entry.ExampleValue, term)
        || Contains(entry.Pack.ToString(), term)
        || Contains(entry.RelatedBo.ToString(), term)
        || Contains(UserReportPlaceholderRelatedBoCatalog.DisplayNameEn(entry.RelatedBo), term);

    private static IEnumerable<string> ExpandTerms(string term)
    {
        yield return term;
        if (term.Contains("speciality", StringComparison.OrdinalIgnoreCase))
            yield return term.Replace("speciality", "specialty", StringComparison.OrdinalIgnoreCase);
        if (term.Contains("specialities", StringComparison.OrdinalIgnoreCase))
            yield return term.Replace("specialities", "specialties", StringComparison.OrdinalIgnoreCase);
        if (term.Contains("birthplace", StringComparison.OrdinalIgnoreCase))
            yield return term.Replace("birthplace", "birth place", StringComparison.OrdinalIgnoreCase);
        if (term.Contains("place of birth", StringComparison.OrdinalIgnoreCase))
            yield return "birth place";
        if (term.Contains("hereket", StringComparison.OrdinalIgnoreCase)
            || term.Contains("work permitted", StringComparison.OrdinalIgnoreCase))
            yield return "work permitted locations";
        if (term.Contains("cakylyk", StringComparison.OrdinalIgnoreCase)
            || term.Contains("çakylyk", StringComparison.OrdinalIgnoreCase)
            || term.Contains("cancel invitation", StringComparison.OrdinalIgnoreCase))
        {
            yield return "cancel invitation count";
            yield return "cancel invitation as numbers";
            yield return "cancel invitation issued dates";
            yield return "cancel invitation expiration dates";
        }
        if (term.Contains("resmilesdirilen", StringComparison.OrdinalIgnoreCase)
            || term.Contains("resmileşdirilen", StringComparison.OrdinalIgnoreCase))
            yield return "cancel invitation issued dates";
    }

    private static bool Contains(string? value, string term)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(term))
            return false;
        if (value.Contains(term, StringComparison.OrdinalIgnoreCase))
            return true;

        var foldedValue = Fold(value);
        var foldedTerm = Fold(term);
        if (foldedValue.Contains(foldedTerm, StringComparison.OrdinalIgnoreCase))
            return true;

        var compactValue = Compact(foldedValue);
        var compactTerm = Compact(foldedTerm);
        return compactTerm.Length > 0
            && compactValue.Contains(compactTerm, StringComparison.OrdinalIgnoreCase);
    }

    private static string Fold(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        return sb.ToString();
    }

    private static string Compact(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (!char.IsWhiteSpace(ch) && ch is not '-' and not '_')
                sb.Append(ch);
        }

        return sb.ToString();
    }
}