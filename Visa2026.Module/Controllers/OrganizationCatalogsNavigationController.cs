using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.OrganizationCatalogs;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.OrganizationCatalogs;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the Organization catalogs DetailView when Configuration → Organization catalogs is selected.
/// </summary>
public sealed class OrganizationCatalogsNavigationController : WindowController
{
    private ShowNavigationItemController? _navigationController;

    protected override void OnActivated()
    {
        base.OnActivated();
        _navigationController = Frame.GetController<ShowNavigationItemController>();
        if (_navigationController != null)
            _navigationController.CustomShowNavigationItem += OnCustomShowNavigationItem;
    }

    protected override void OnDeactivated()
    {
        if (_navigationController != null)
            _navigationController.CustomShowNavigationItem -= OnCustomShowNavigationItem;
        _navigationController = null;
        base.OnDeactivated();
    }

    private void OnCustomShowNavigationItem(object sender, CustomShowNavigationItemEventArgs e)
    {
        if (!IsCatalogsNavigation(e.ActionArguments.SelectedChoiceActionItem))
            return;

        var catalogView = OrganizationCatalogsOpenHelper.CreateCatalogView(Application);
        if (catalogView == null)
            return;

        e.ActionArguments.ShowViewParameters.CreatedView = catalogView;
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }

    private static bool IsCatalogsNavigation(ChoiceActionItem? item)
    {
        if (item == null)
            return false;

        if (item.Id is OrganizationCatalogsModelUpdater.NavItemId or "OrganizationCatalogsHost")
            return true;

        if (item.Data is IModelNavigationItem modelNav
            && string.Equals(modelNav.View?.Id, OrganizationCatalogsViewIds.DetailView, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(item.Caption, OrganizationCatalogsViewIds.Caption, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.ParentItem?.Id, "Configuration", StringComparison.Ordinal);
    }
}
