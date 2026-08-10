using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.OfficerShell;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the officer shell DetailView when Application → Application Profiles is selected.
/// </summary>
public sealed class OfficerShellNavigationController : WindowController
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
        if (!IsShellNavigation(e.ActionArguments.SelectedChoiceActionItem))
            return;

        var shellView = OfficerShellOpenHelper.CreateShellView(Application);
        if (shellView == null)
            return;

        e.ActionArguments.ShowViewParameters.CreatedView = shellView;
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }

    private static bool IsShellNavigation(ChoiceActionItem? item)
    {
        if (item == null)
            return false;

        if (item.Id is OfficerShellModelUpdater.NavItemId or "OfficerShellHost")
            return true;

        if (item.Data is IModelNavigationItem modelNav
            && string.Equals(modelNav.View?.Id, OfficerShellViewIds.DetailView, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(item.Caption, "Application Profiles", StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.ParentItem?.Id, "Application", StringComparison.Ordinal);
    }
}
