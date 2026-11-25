using System.Collections;
using System.Reflection;
using DevExpress.ExpressApp;
using Visa2026.Blazor.Server.Controllers;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Save and Close leaves the wizard and shows the saved person on Employees or Family Members.
/// </summary>
public static class PersonIntakeWizardReturn
{
    public static void ShowListWithPerson(XafApplication application, View wizardView, Person person)
    {
        var listViewId = person.PersonRole == PersonRecordRole.FamilyMember
            ? PersonRosterListViewIds.FamilyMembers
            : PersonRosterListViewIds.Employees;

        var openList = FindOpenList(application, listViewId);
        if (openList != null)
        {
            Activate(application, openList);
            openList.GetController<PersonRosterColorTabsController>()
                ?.RevealPerson(person.ID, person.IsDataIncomplete);
            wizardView.Close();
            return;
        }

        PersonRosterSavedPersonFocus.Pending = new PersonRosterSavedPersonFocus.PendingFocus(
            listViewId,
            person.ID,
            person.IsDataIncomplete);

        var objectSpace = application.CreateObjectSpace(typeof(Person));
        var listView = application.CreateListView(
            listViewId,
            application.CreateCollectionSource(objectSpace, typeof(Person), listViewId),
            isRoot: true);
        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(listView) { TargetWindow = TargetWindow.NewWindow },
            new ShowViewSource(application.MainWindow, null));
        wizardView.Close();
    }

    private static Window? FindOpenList(XafApplication application, string listViewId)
    {
        var strategy = application.ShowViewStrategy;
        if (strategy == null)
            return null;

        var windows = strategy.GetType()
            .GetProperty("Windows", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(strategy) as IEnumerable;
        if (windows == null)
            return null;

        foreach (var item in windows)
        {
            if (item is Window window
                && window.View is ListView list
                && !list.IsDisposed
                && string.Equals(list.Id, listViewId, StringComparison.Ordinal))
                return window;
        }

        return null;
    }

    private static void Activate(XafApplication application, Window window)
    {
        var strategy = application.ShowViewStrategy;
        if (strategy == null)
            return;

        try
        {
            strategy.GetType()
                .GetMethod("SetActiveWindow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new[] { typeof(Window) })
                ?.Invoke(strategy, new object[] { window });
        }
        catch (TargetInvocationException)
        {
        }
    }
}
