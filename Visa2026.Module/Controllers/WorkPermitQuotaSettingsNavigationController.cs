using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Configuration → Work permit quota opens the one settings form (creates the row if missing).
/// </summary>
public sealed class WorkPermitQuotaSettingsNavigationController : WindowController
{
    private ShowNavigationItemController? navigationController;

    public WorkPermitQuotaSettingsNavigationController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        navigationController = Frame.GetController<ShowNavigationItemController>();
        if (navigationController != null)
            navigationController.CustomShowNavigationItem += OnCustomShowNavigationItem;
    }

    protected override void OnDeactivated()
    {
        if (navigationController != null)
            navigationController.CustomShowNavigationItem -= OnCustomShowNavigationItem;
        navigationController = null;
        base.OnDeactivated();
    }

    private void OnCustomShowNavigationItem(object sender, CustomShowNavigationItemEventArgs e)
    {
        if (!IsWorkPermitQuotaNavigation(e.ActionArguments.SelectedChoiceActionItem))
            return;

        var objectSpace = Application.CreateObjectSpace(typeof(WorkPermitQuotaSettings));
        var settings = WorkPermitQuotaSettings.GetOrCreateInstance(objectSpace);
        objectSpace.CommitChanges();
        e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateDetailView(objectSpace, settings, true);
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }

    private static bool IsWorkPermitQuotaNavigation(ChoiceActionItem? item)
    {
        if (item == null)
            return false;
        if (item.Id is "WorkPermitQuotaSettings" or "WorkPermitQuotaSettings_ListView")
            return true;
        if (item.Data is IModelNavigationItem model
            && model.View?.Id is string viewId
            && viewId.StartsWith("WorkPermitQuotaSettings", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
