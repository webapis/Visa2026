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
            !hide.Contains(e.ShortCode)
            && !IsHiddenJoinedCode(e.ShortCode, search));

        if (!string.IsNullOrWhiteSpace(search))
            remaining = remaining.Where(e => MatchesSearch(e, search));

        return UserReportPlaceholderRelatedBoCatalog.Group(remaining);
    }

    /// <summary>
    /// Joined catalog codes stay for old templates, but Review Add offers the
    /// parts separately. Type the short code to reach the joined token.
    /// </summary>
    private static bool IsHiddenJoinedCode(string shortCode, string? search)
    {
        var term = (search ?? string.Empty).Trim();
        if (shortCode.Equals("EGIY", StringComparison.OrdinalIgnoreCase))
        {
            return !term.Equals("EGIY", StringComparison.OrdinalIgnoreCase)
                && !term.Contains("level and institution", StringComparison.OrdinalIgnoreCase)
                && !term.Contains("education +", StringComparison.OrdinalIgnoreCase);
        }

        if (shortCode.Equals("VNAT", StringComparison.OrdinalIgnoreCase))
        {
            return !term.Equals("VNAT", StringComparison.OrdinalIgnoreCase)
                && !term.Contains("number and type", StringComparison.OrdinalIgnoreCase);
        }

        return false;
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
        if (term.Contains("work permit item", StringComparison.OrdinalIgnoreCase)
            || term.Contains("wp item", StringComparison.OrdinalIgnoreCase)
            || term.Contains("valid to", StringComparison.OrdinalIgnoreCase)
            || term.Contains("tassyknama", StringComparison.OrdinalIgnoreCase)
            || term.Contains("iş rugsatnama", StringComparison.OrdinalIgnoreCase)
            || term.Contains("is rugsatnama", StringComparison.OrdinalIgnoreCase))
        {
            yield return "work permit number";
            yield return "work permit AS number";
            yield return "work permit start date";
            yield return "work permit valid to";
            yield return "work permitted locations";
        }
        if (term.Contains("gosulmaly", StringComparison.OrdinalIgnoreCase)
            || term.Contains("goşulmaly", StringComparison.OrdinalIgnoreCase)
            || term.Contains("work permit location", StringComparison.OrdinalIgnoreCase))
            yield return "work permit location";
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
        if (term.Contains("is sapary", StringComparison.OrdinalIgnoreCase)
            || term.Contains("iş sapary", StringComparison.OrdinalIgnoreCase)
            || term.Contains("business trip", StringComparison.OrdinalIgnoreCase))
        {
            yield return "business trip start date";
            yield return "business trip duration";
            yield return "business trip address";
            yield return "from region";
            yield return "from city";
            yield return "to region";
            yield return "to city";
            yield return "Purpose";
            yield return "iş saparynda boljak salgysy";
            yield return "BTAD";
        }
        if (term.Contains("boljak salgy", StringComparison.OrdinalIgnoreCase)
            || term.Contains("boljak salgysy", StringComparison.OrdinalIgnoreCase)
            || term.Contains("baryan yer", StringComparison.OrdinalIgnoreCase)
            || term.Contains("barýan ýer", StringComparison.OrdinalIgnoreCase)
            || term.Contains("business trip address", StringComparison.OrdinalIgnoreCase)
            || term.Contains("business trip adress", StringComparison.OrdinalIgnoreCase)
            || term.Contains("trip address", StringComparison.OrdinalIgnoreCase)
            || term.Contains("trip adress", StringComparison.OrdinalIgnoreCase)
            || term.Contains("BusinessTripAddress", StringComparison.OrdinalIgnoreCase)
            || term.Equals("BTAD", StringComparison.OrdinalIgnoreCase))
        {
            yield return "business trip address";
            yield return "iş saparynda boljak salgysy";
            yield return "BTAD";
            yield return "BusinessTripAddress_FullAddress";
        }
        // Common typo + ADRS-shaped search should still surface BTAD (same region+city+street shape).
        if (term.Contains("adress", StringComparison.OrdinalIgnoreCase)
            && !term.Contains("address", StringComparison.OrdinalIgnoreCase))
            yield return term.Replace("adress", "address", StringComparison.OrdinalIgnoreCase);
        if (term.Contains("Address_FullAddress", StringComparison.OrdinalIgnoreCase)
            || term.Contains("FullAddress", StringComparison.OrdinalIgnoreCase))
        {
            yield return "BTAD";
            yield return "BusinessTripAddress_FullAddress";
            yield return "ADRS";
            yield return "Address_FullAddress";
        }
        if (term.Contains("from region", StringComparison.OrdinalIgnoreCase)
            || term.Contains("fromregion", StringComparison.OrdinalIgnoreCase))
            yield return "From Region";
        if (term.Contains("from city", StringComparison.OrdinalIgnoreCase)
            || term.Contains("fromcity", StringComparison.OrdinalIgnoreCase))
            yield return "From City";
        if (term.Contains("to region", StringComparison.OrdinalIgnoreCase)
            || term.Contains("toregion", StringComparison.OrdinalIgnoreCase))
            yield return "To Region";
        if (term.Contains("to city", StringComparison.OrdinalIgnoreCase)
            || term.Contains("tocity", StringComparison.OrdinalIgnoreCase))
            yield return "To City";
        if (term.Contains("maksady", StringComparison.OrdinalIgnoreCase)
            || term.Contains("purpose", StringComparison.OrdinalIgnoreCase))
            yield return "Purpose";
        if (term.Contains("visa start", StringComparison.OrdinalIgnoreCase)
            || term.Contains("start date", StringComparison.OrdinalIgnoreCase)
            || term.Contains("baslanyan", StringComparison.OrdinalIgnoreCase)
            || term.Equals("VSTD", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Visa start date";
            yield return "VSTD";
        }
        if (term.Contains("visa number", StringComparison.OrdinalIgnoreCase)
            || term.Equals("VNUM", StringComparison.OrdinalIgnoreCase))
            yield return "Visa number";
        if (term.Contains("visa type", StringComparison.OrdinalIgnoreCase)
            || term.Equals("VTYP", StringComparison.OrdinalIgnoreCase))
            yield return "Visa type";
        if (term.Contains("foreign address country", StringComparison.OrdinalIgnoreCase)
            || term.Equals("PFAC", StringComparison.OrdinalIgnoreCase)
            || term.Contains("ForeignAddressCountry", StringComparison.OrdinalIgnoreCase)
            || term.Contains("dasary salgy yurd", StringComparison.OrdinalIgnoreCase))
        {
            yield return "PFAC";
            yield return "Foreign address country";
            yield return "Person_ForeignAddressCountryCode";
        }
        if (term.Contains("foreign address", StringComparison.OrdinalIgnoreCase)
            || term.Equals("PFAD", StringComparison.OrdinalIgnoreCase)
            || term.Equals("PFWC", StringComparison.OrdinalIgnoreCase))
        {
            yield return "PFAC";
            yield return "PFAD";
            yield return "PFWC";
            yield return "Foreign address country";
        }
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