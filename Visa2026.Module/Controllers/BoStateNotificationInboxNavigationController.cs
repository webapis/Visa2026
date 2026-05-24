using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.StateNotifications;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the inbox detail view with a non-persistent host object (nav cannot pass an object key).
/// </summary>
public class BoStateNotificationInboxNavigationController : WindowController
{
    private ShowNavigationItemController _navigationController;

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
        if (e.ActionArguments.SelectedChoiceActionItem?.Id != "StateNotifications")
            return;

        var objectSpace = Application.CreateObjectSpace(typeof(BoStateNotificationInboxHost));
        var host = objectSpace.CreateObject<BoStateNotificationInboxHost>();
        e.ActionArguments.ShowViewParameters.CreatedView = Application.CreateDetailView(objectSpace, host);
        e.ActionArguments.ShowViewParameters.TargetWindow = TargetWindow.Current;
        e.Handled = true;
    }
}
