using System.Collections.Generic;
using DevExpress.Data.Filtering;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

/// <summary>
/// FullTextSearch extras for Application Profile Instance ListViews: linked people
/// first/last/middle names.
/// </summary>
public static class ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
{
    public static CriteriaOperator? BuildLinkedPeopleIdentityCriteria(string searchText)
    {
        var tokens = ReportDashboardCatalog.PersonSearchTokens(searchText);
        if (tokens.Length == 0)
            return null;

        var innerParts = new List<string>();
        var operands = new List<object>();
        foreach (var token in tokens)
        {
            innerParts.Add(
                $"({PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[FirstName]")} Or {PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[LastName]")} Or {PersonSearchTextNormalizer.FoldedLowerContainsCriteria("[MiddleName]")})");
            operands.Add(token);
            operands.Add(token);
            operands.Add(token);
        }

        return CriteriaOperator.Parse($"[People][{string.Join(" And ", innerParts)}]", operands.ToArray());
    }
}