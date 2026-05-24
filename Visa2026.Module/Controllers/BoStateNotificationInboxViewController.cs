using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.StateNotifications;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Hosts the prototype inbox: ensures an object exists and hides standard CRUD chrome.
/// </summary>
public class BoStateNotificationInboxViewController : ObjectViewController<DetailView, BoStateNotificationInboxHost>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        EnsureCurrentObject();
        View.AllowDelete.SetItemValue("BoStateNotificationInbox", false);
        View.AllowNew.SetItemValue("BoStateNotificationInbox", false);

        var modifications = Frame.GetController<ModificationsController>();
        if (modifications != null)
        {
            modifications.SaveAction.Active.SetItemValue("BoStateNotificationInbox", false);
            modifications.SaveAndCloseAction.Active.SetItemValue("BoStateNotificationInbox", false);
            modifications.SaveAndNewAction.Active.SetItemValue("BoStateNotificationInbox", false);
        }

        var deleteController = Frame.GetController<DeleteObjectsViewController>();
        deleteController?.DeleteAction.Active.SetItemValue("BoStateNotificationInbox", false);

        var refresh = Frame.GetController<RefreshController>()?.RefreshAction;
        refresh?.Active.SetItemValue("BoStateNotificationInbox", false);
    }

    private void EnsureCurrentObject()
    {
        if (View.CurrentObject is BoStateNotificationInboxHost)
            return;

        View.CurrentObject = ObjectSpace.CreateObject<BoStateNotificationInboxHost>();
    }
}
