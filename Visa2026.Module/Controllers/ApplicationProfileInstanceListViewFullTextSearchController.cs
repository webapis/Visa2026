using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Filtering;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Extends Application Profile Instance ListView FullTextSearch so officers can find
/// cases by linked person first/last name and passport number. Those members are not
/// ListView columns, so DxGrid / default FullTextSearch skip them.
/// </summary>
public sealed class ApplicationProfileInstanceListViewFullTextSearchController
    : ObjectViewController<ListView, ApplicationProfileInstance>
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

        var identityCriteria = ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildLinkedPeopleIdentityCriteria(e.SearchText);
        var passportCriteria = ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildLinkedPeoplePassportCriteria(e.SearchText);
        var combinedCriteria = PersonListViewFullTextSearchCriteriaBuilder.CombineOr(
            defaultCriteria,
            identityCriteria,
            passportCriteria);

        if (ReferenceEquals(combinedCriteria, null))
            return;

        e.Criteria = combinedCriteria;
        e.Handled = true;
    }
}