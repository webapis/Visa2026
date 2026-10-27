using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.Email;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Configuration → Mail server opens the one settings form (creates the row if the list is empty).
/// </summary>
public sealed class EmailSmtpSettingsNavigationController : WindowController
{
    private ShowNavigationItemController? navigationController;

    public EmailSmtpSettingsNavigationController()
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
        if (!IsMailServerNavigation(e.ActionArguments.SelectedChoiceActionItem))
            return;

        var objectSpace = Application.CreateObjectSpace(typeof(EmailSmtpSettings));
        var settings = EmailSmtpSettings.GetOrCreateInstance(objectSpace);
        objectSpace.CommitChanges();
        e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateDetailView(objectSpace, settings, true);
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }

    private static bool IsMailServerNavigation(ChoiceActionItem? item)
    {
        if (item == null)
            return false;
        if (item.Id is "EmailSmtpSettings" or "EmailSmtpSettings_ListView")
            return true;
        if (item.Data is IModelNavigationItem model
            && model.View?.Id is string viewId
            && viewId.StartsWith("EmailSmtpSettings", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
