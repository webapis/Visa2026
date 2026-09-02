#nullable enable

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

        return Contains(entry.ShortCode, term)
            || Contains(entry.CanonicalPath, term)
            || Contains(entry.LabelEn, term)
            || Contains(entry.LabelTk, term)
            || Contains(entry.LabelRu, term)
            || Contains(entry.LabelTr, term)
            || Contains(entry.RelatedBo.ToString(), term)
            || Contains(UserReportPlaceholderRelatedBoCatalog.DisplayNameEn(entry.RelatedBo), term);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value)
        && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}