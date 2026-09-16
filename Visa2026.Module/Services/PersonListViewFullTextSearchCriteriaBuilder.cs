using System;
using System.Collections.Generic;
using System.Text;
using DevExpress.Data.Filtering;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

/// <summary>
/// Builds Person ListView FullTextSearch criteria. Typed Person list views show
/// <c>FullName</c> (non-persistent) instead of name parts, so the default XAF
/// search skips first/last/middle names in server data access modes.
/// </summary>
public static class PersonListViewFullTextSearchCriteriaBuilder
{
    /// <summary>
    /// Compare a compacted search key to <c>PassportNumber</c> with spaces and hyphens removed
    /// (e.g. typed <c>U86993401</c> matches stored <c>U 86993401</c>).
    /// </summary>
    public const string PassportNumberCompactContainsCriteria =
        "Contains(Lower(Replace(Replace([PassportNumber], ' ', ''), '-', '')), ?)";

    public static CriteriaOperator? CombineOr(params CriteriaOperator?[] parts)
    {
        CriteriaOperator? result = null;
        foreach (var part in parts)
        {
            if (ReferenceEquals(part, null))
                continue;

            result = ReferenceEquals(result, null)
                ? part
                : GroupOperator.Combine(GroupOperatorType.Or, result, part);
        }

        return result;
    }

    /// <summary>
    /// Fold, then drop whitespace and hyphens so passport search is one key, not AND-split tokens.
    /// </summary>
    public static string CompactPassportSearchKey(string? searchText)
    {
        var folded = PersonSearchTextNormalizer.Fold(searchText ?? string.Empty);
        if (folded.Length == 0)
            return string.Empty;

        var builder = new StringBuilder(folded.Length);
        foreach (var ch in folded)
        {
            if (char.IsWhiteSpace(ch) || ch is '-' or '\u2013' or '\u2014')
                continue;
            builder.Append(ch);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Match folded tokens against first/middle/last name and personal number (AND across tokens).
    /// </summary>
    public static CriteriaOperator? BuildPersonIdentityCriteria(string searchText)
    {
        var tokens = ReportDashboardCatalog.PersonSearchTokens(searchText);
        if (tokens.Length == 0)
            return null;

        CriteriaOperator? result = null;
        foreach (var token in tokens)
        {
            var tokenCriteria = GroupOperator.Combine(
                GroupOperatorType.Or,
                CriteriaOperator.Parse(PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[FirstName]"), token),
                CriteriaOperator.Parse(PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[MiddleName]"), token),
                CriteriaOperator.Parse(PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[LastName]"), token),
                CriteriaOperator.Parse("Contains(Lower([PersonalNumber]), ?)", token));

            result = ReferenceEquals(result, null)
                ? tokenCriteria
                : GroupOperator.Combine(GroupOperatorType.And, result, tokenCriteria);
        }

        return result;
    }

    /// <summary>
    /// Match a compacted passport key against any related passport number.
    /// </summary>
    public static CriteriaOperator? BuildPassportNumberCriteria(string searchText)
    {
        var key = CompactPassportSearchKey(searchText);
        if (key.Length == 0)
            return null;

        return CriteriaOperator.Parse(
            $"[Passports][{PassportNumberCompactContainsCriteria}]",
            key);
    }
}
