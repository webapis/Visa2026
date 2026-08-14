using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ApplicationProfileCatalog;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.ApplicationProfileCatalog;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the Application Profile catalog DetailView when Application Profiles → Application Profile Templates is selected.
/// </summary>
public sealed class ApplicationProfileCatalogNavigationController : WindowController
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
        if (!IsCatalogNavigation(e.ActionArguments.SelectedChoiceActionItem))
            return;

        var catalogView = ApplicationProfileCatalogOpenHelper.CreateCatalogView(Application);
        if (catalogView == null)
            return;

        e.ActionArguments.ShowViewParameters.CreatedView = catalogView;
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }

    private static bool IsCatalogNavigation(ChoiceActionItem? item)
    {
        if (item == null)
            return false;

        if (item.Id is ApplicationProfileCatalogModelUpdater.NavItemId
            or "ApplicationProfileCatalogHost")
        {
            return true;
        }

        if (item.Data is IModelNavigationItem modelNav
            && string.Equals(modelNav.View?.Id, ApplicationProfileCatalogViewIds.DetailView, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(item.Caption, ApplicationProfileInstanceProgressRouteNavigation.CaptionTemplates, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.ParentItem?.Id, "Application", StringComparison.Ordinal))
        {
            return true;
        }

        // Leftover Configuration node from before the catalog moved into Application Profiles.
        if (string.Equals(item.Caption, "Application Profile", StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.ParentItem?.Id, "Configuration", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}