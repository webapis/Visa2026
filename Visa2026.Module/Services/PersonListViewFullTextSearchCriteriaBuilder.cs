using System;
using System.Collections.Generic;
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
                CriteriaOperator.Parse("Contains(Lower([FirstName]), ?)", token),
                CriteriaOperator.Parse("Contains(Lower([MiddleName]), ?)", token),
                CriteriaOperator.Parse("Contains(Lower([LastName]), ?)", token),
                CriteriaOperator.Parse("Contains(Lower([PersonalNumber]), ?)", token));

            result = ReferenceEquals(result, null)
                ? tokenCriteria
                : GroupOperator.Combine(GroupOperatorType.And, result, tokenCriteria);
        }

        return result;
    }

    /// <summary>
    /// Match folded tokens against any related passport number (AND across tokens).
    /// </summary>
    public static CriteriaOperator? BuildPassportNumberCriteria(string searchText)
    {
        var tokens = ReportDashboardCatalog.PersonSearchTokens(searchText);
        if (tokens.Length == 0)
            return null;

        CriteriaOperator? result = null;
        foreach (var token in tokens)
        {
            var tokenCriteria = CriteriaOperator.Parse(
                "[Passports][Contains(Lower([PassportNumber]), ?)]",
                token);
            result = ReferenceEquals(result, null)
                ? tokenCriteria
                : GroupOperator.Combine(GroupOperatorType.And, result, tokenCriteria);
        }

        return result;
    }
}
