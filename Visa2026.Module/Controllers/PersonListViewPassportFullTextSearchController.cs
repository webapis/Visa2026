using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Filtering;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Extends Person ListView FullTextSearch so officers can find people by name parts,
/// personal number, and related <see cref="Passport.PassportNumber"/> (any passport).
/// Typed list views show <see cref="Person.FullName"/> (non-persistent), which XAF
/// excludes from server-mode search, so identity criteria are built explicitly.
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

        var identityCriteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria(e.SearchText);
        var passportCriteria = PersonListViewFullTextSearchCriteriaBuilder.BuildPassportNumberCriteria(e.SearchText);
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