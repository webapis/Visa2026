using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Blazor.Server.Services;

public sealed class ApplicationRuntimeLogNavigationHelper
{
    public void OpenRuntimeErrors(XafApplication application)
    {
        if (application == null)
            return;

        if (TryShowViaNavigation(application))
            return;

        ShowListDirect(application);
    }

    private static bool TryShowViaNavigation(XafApplication application)
    {
        var window = application.MainWindow;
        if (window == null)
            return false;

        var showNav = window.GetController<ShowNavigationItemController>();
        if (showNav == null)
            return false;

        var navItem = FindRuntimeErrorsItem(showNav.ShowNavigationItemAction.Items);
        if (navItem == null)
            return false;

        showNav.ShowNavigationItemAction.DoExecute(navItem);
        return true;
    }

    private static void ShowListDirect(XafApplication application)
    {
        var window = application.MainWindow;
        if (window == null)
            return;

        var objectSpace = application.CreateObjectSpace(typeof(ApplicationRuntimeLog));
        var listView = application.CreateListView(objectSpace, typeof(ApplicationRuntimeLog), true);

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(listView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(window, null));
    }

    private static ChoiceActionItem? FindRuntimeErrorsItem(ChoiceActionItemCollection items)
    {
        var direct = items.Find("ApplicationRuntimeLog");
        if (direct != null)
            return direct;

        var operations = items.Find("Operations");
        return operations?.Items.Find("ApplicationRuntimeLog");
    }
}
