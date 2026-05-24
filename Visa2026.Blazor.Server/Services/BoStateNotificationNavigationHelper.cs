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

        var navItem = FindStateNotificationsItem(showNav.ShowNavigationItemAction.Items);
        if (navItem == null)
            return false;

        showNav.ShowNavigationItemAction.DoExecute(navItem);
        return true;
    }

    private static void ShowInboxDirect(XafApplication application)
    {
        var window = application.MainWindow;
        if (window == null)
            return;

        var objectSpace = application.CreateObjectSpace(typeof(BoStateNotificationInboxHost));
        var host = objectSpace.CreateObject<BoStateNotificationInboxHost>();
        var detailView = application.CreateDetailView(objectSpace, host);

        // Blazor ShowViewInCurrentWindow requires a non-null Frame on ShowViewSource (not null,null).
        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(window, null));
    }

    private static ChoiceActionItem FindStateNotificationsItem(ChoiceActionItemCollection items)
    {
        var direct = items.Find("StateNotifications");
        if (direct != null)
            return direct;

        var operations = items.Find("Operations");
        return operations?.Items.Find("StateNotifications");
    }
}
