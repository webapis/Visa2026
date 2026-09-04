using System.Collections.Generic;
using DevExpress.Data.Filtering;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

/// <summary>
/// FullTextSearch extras for Application Profile Instance ListViews: linked people
/// first/last/middle names and passport numbers (person booklets or instance links).
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
                "(Contains(Lower([FirstName]), ?) Or Contains(Lower([LastName]), ?) Or Contains(Lower([MiddleName]), ?))");
            operands.Add(token);
            operands.Add(token);
            operands.Add(token);
        }

        return CriteriaOperator.Parse($"[People][{string.Join(" And ", innerParts)}]", operands.ToArray());
    }

    public static CriteriaOperator? BuildLinkedPeoplePassportCriteria(string searchText)
    {
        var tokens = ReportDashboardCatalog.PersonSearchTokens(searchText);
        if (tokens.Length == 0)
            return null;

        var peopleInner = new List<string>();
        var peopleOperands = new List<object>();
        var instanceInner = new List<string>();
        var instanceOperands = new List<object>();
        foreach (var token in tokens)
        {
            peopleInner.Add("[Passports][Contains(Lower([PassportNumber]), ?)]");
            peopleOperands.Add(token);
            instanceInner.Add("Contains(Lower([PassportNumber]), ?)");
            instanceOperands.Add(token);
        }

        var onPeople = CriteriaOperator.Parse(
            $"[People][{string.Join(" And ", peopleInner)}]",
            peopleOperands.ToArray());
        var onInstance = CriteriaOperator.Parse(
            $"[Passports][{string.Join(" And ", instanceInner)}]",
            instanceOperands.ToArray());
        return PersonListViewFullTextSearchCriteriaBuilder.CombineOr(onPeople, onInstance);
    }
}