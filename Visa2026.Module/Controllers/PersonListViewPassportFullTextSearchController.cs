using System;
using System.Collections.Generic;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Filtering;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Extends Person ListView FullTextSearch so officers can find people by related
/// <see cref="Passport.PassportNumber"/> (any passport on the person).
/// Collection paths are not supported via CustomGetFullTextSearchProperties alone,
/// so criteria are built explicitly and OR-ed with the default FullTextSearch criteria.
/// </summary>
public sealed class PersonListViewPassportFullTextSearchController : ObjectViewController<ListView, Person>
{
    private FilterController filterController;

    protected override void OnActivated()
    {
        base.OnActivated();
        filterController = Frame.GetController<FilterController>();
        if (filterController != null)
            filterController.CustomBuildCriteria += OnCustomBuildCriteria;
    }

    protected override void OnDeactivated()
    {
        if (filterController != null)
        {
            filterController.CustomBuildCriteria -= OnCustomBuildCriteria;
            filterController = null;
        }

        base.OnDeactivated();
    }

    private void OnCustomBuildCriteria(object sender, CustomBuildCriteriaEventArgs e)
    {
        if (filterController == null || string.IsNullOrWhiteSpace(e.SearchText))
            return;

        var defaultProperties = filterController.GetFullTextSearchProperties();
        var defaultCriteria = new SearchCriteriaBuilder(
            View.ObjectTypeInfo,
            defaultProperties,
            e.SearchText,
            GroupOperatorType.Or,
            includeNonPersistentMembers: false,
            filterController.FullTextSearchMode).BuildCriteria();

        var passportCriteria = BuildPassportNumberCriteria(e.SearchText);

        if (ReferenceEquals(defaultCriteria, null) && ReferenceEquals(passportCriteria, null))
            return;

        if (ReferenceEquals(defaultCriteria, null))
            e.Criteria = passportCriteria;
        else if (ReferenceEquals(passportCriteria, null))
            e.Criteria = defaultCriteria;
        else
            e.Criteria = GroupOperator.Combine(GroupOperatorType.Or, defaultCriteria, passportCriteria);

        e.Handled = true;
    }

    private static CriteriaOperator BuildPassportNumberCriteria(string searchText)
    {
        CriteriaOperator result = null;
        foreach (var token in SplitSearchTokens(searchText))
        {
            // Exists related passport whose number contains the token (EF translates to EXISTS).
            var tokenCriteria = CriteriaOperator.Parse(
                "[Passports][Contains([PassportNumber], ?)]",
                token);
            result = result == null
                ? tokenCriteria
                : GroupOperator.Combine(GroupOperatorType.And, result, tokenCriteria);
        }

        return result;
    }

    private static IEnumerable<string> SplitSearchTokens(string searchText)
    {
        return searchText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
    }
}