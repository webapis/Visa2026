using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Model;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Configuration → Wiza berilmeli möhlet opens the one settings form (creates the row if missing).
/// </summary>
public sealed class VisaIssueWindowSettingsNavigationController : WindowController
{
    private ShowNavigationItemController? navigationController;

    public VisaIssueWindowSettingsNavigationController()
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
        if (!IsVisaIssueWindowNavigation(e.ActionArguments.SelectedChoiceActionItem))
            return;

        var objectSpace = Application.CreateObjectSpace(typeof(VisaIssueWindowSettings));
        var settings = VisaIssueWindowSettings.GetOrCreateInstance(objectSpace);
        objectSpace.CommitChanges();
        e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateDetailView(objectSpace, settings, true);
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }

    private static bool IsVisaIssueWindowNavigation(DevExpress.ExpressApp.Actions.ChoiceActionItem? item)
    {
        if (item == null)
            return false;
        if (item.Id is "VisaIssueWindowSettings" or "VisaIssueWindowSettings_ListView")
            return true;
        if (item.Data is IModelNavigationItem model
            && model.View?.Id is string viewId
            && viewId.StartsWith("VisaIssueWindowSettings", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
