using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.StateNotifications;
using Visa2026.Module.Services.StateNotifications;

namespace Visa2026.Blazor.Server.Services;

public sealed class BoStateNotificationNavigationHelper
{
    private readonly BoStateNotificationInboxFilterService _filterService;

    public BoStateNotificationNavigationHelper(BoStateNotificationInboxFilterService filterService) =>
        _filterService = filterService;

    public void OpenStateNotifications(XafApplication application, bool criticalOnly = false)
    {
        if (application == null)
            return;

        if (criticalOnly)
            _filterService.SetPendingCriticalOnly();

        if (TryShowViaNavigation(application))
            return;

        ShowInboxDirect(application);
    }

    private static bool TryShowViaNavigation(XafApplication application)
    {
        var window = application.MainWindow;
        if (window == null)
            return false;

        var showNav = window.GetController<ShowNavigationItemController>();
        if (showNav == null)
            return false;

        var navItem = FindNavigationItem(showNav.ShowNavigationItemAction.Items, "StateNotifications");
        if (navItem == null)
            return false;

        showNav.ShowNavigationItemAction.DoExecute(navItem);
        return true;
    }

    private static void ShowInboxDirect(XafApplication application)
    {
        var objectSpace = application.CreateObjectSpace(typeof(BoStateNotificationInboxHost));
        var host = objectSpace.CreateObject<BoStateNotificationInboxHost>();
        var detailView = application.CreateDetailView(objectSpace, host);
        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(null, null));
    }

    private static ChoiceActionItem FindNavigationItem(ChoiceActionItemCollection items, string id)
    {
        foreach (ChoiceActionItem item in items)
        {
            if (string.Equals(item.Id, id, StringComparison.Ordinal))
                return item;

            var nested = FindNavigationItem(item.Items, id);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
