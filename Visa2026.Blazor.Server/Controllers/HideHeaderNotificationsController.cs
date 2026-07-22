using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Hides the built-in XAF Notifications bell (ShowNotificationsAction) from the main header.
/// </summary>
public sealed class HideHeaderNotificationsController : WindowController
{
    const string HideReason = "Visa2026.HideHeaderNotifications";
    const string ShowNotificationsActionId = "ShowNotifications";

    public HideHeaderNotificationsController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        foreach (Controller controller in Frame.Controllers)
        {
            foreach (ActionBase action in controller.Actions)
            {
                if (string.Equals(action.Id, ShowNotificationsActionId, StringComparison.Ordinal))
                    action.Active.SetItemValue(HideReason, false);
            }
        }
    }

    protected override void OnDeactivated()
    {
        foreach (Controller controller in Frame.Controllers)
        {
            foreach (ActionBase action in controller.Actions)
            {
                if (string.Equals(action.Id, ShowNotificationsActionId, StringComparison.Ordinal))
                    action.Active.RemoveItem(HideReason);
            }
        }

        base.OnDeactivated();
    }
}