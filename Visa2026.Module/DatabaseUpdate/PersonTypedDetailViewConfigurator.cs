using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Runtime safety net: typed Person detail views + intake wizard layout after all model layers merge.
/// Also remirrors Incomplete roster ListViews so user ModelDifference cannot leave DxGrid-breaking columns.
/// </summary>
public static class PersonTypedDetailViewConfigurator
{
    public static void Wire(XafApplication application)
    {
        application.SetupComplete += (_, _) => EnsureConfigured(application.Model);
        // User ModelDifference loads after login and can wipe wizard-only layout / Incomplete columns.
        application.LoggedOn += (_, _) => EnsureConfigured(application.Model);
    }

    public static void EnsureConfigured(IModelApplication modelApplication)
    {
        if (modelApplication?.Views == null)
            return;

        var modelViews = modelApplication.Views;
        PersonTypedDetailViewFactory.SyncTypedDetailViews(modelViews);
        PersonIntakeWizardModelPatch.Apply(modelApplication);

        // After user diffs merge: drop calculated roster columns, then clone Incomplete from All.
        if (modelViews[PersonRosterListViewIds.Employees] is IModelListView employees)
            PersonListViewActionColumnsUpdater.KeepFullNameAndDossier(employees);
        if (modelViews[PersonRosterListViewIds.FamilyMembers] is IModelListView familyMembers)
            PersonListViewActionColumnsUpdater.KeepFullNameAndDossier(familyMembers);
        PersonIncompleteRosterListViewSync.EnsureAll(modelViews);
        FlattenRosterNavigation(modelApplication);

        if (modelViews[PersonDetailViewIds.Employee] is not IModelDetailView employeeDetailView
            || modelViews[PersonDetailViewIds.FamilyMember] is not IModelDetailView familyMemberDetailView
            || modelViews[PersonDetailViewIds.TemporaryVisitor] is not IModelDetailView temporaryVisitorDetailView)
        {
            return;
        }

        WireListViewDetailView(modelViews, PersonRosterListViewIds.Employees, employeeDetailView);
        WireListViewDetailView(modelViews, PersonRosterListViewIds.EmployeesIncomplete, employeeDetailView);
        WireListViewDetailView(modelViews, PersonRosterListViewIds.FamilyMembers, familyMemberDetailView);
        WireListViewDetailView(modelViews, PersonRosterListViewIds.FamilyMembersIncomplete, familyMemberDetailView);
        WireListViewDetailView(modelViews, PersonRosterListViewIds.TemporaryVisitors, temporaryVisitorDetailView);
    }

    private static void FlattenRosterNavigation(IModelApplication modelApplication)
    {
        if (modelApplication is not IModelApplicationNavigationItems navigation)
            return;

        FlattenRosterItem(navigation.NavigationItems?.Items, PersonRosterListViewIds.NavEmployees, PersonRosterListViewIds.Employees, modelApplication.Views);
        FlattenRosterItem(navigation.NavigationItems?.Items, PersonRosterListViewIds.NavFamilyMembers, PersonRosterListViewIds.FamilyMembers, modelApplication.Views);
    }

    private static void FlattenRosterItem(IModelNavigationItems? items, string navId, string listViewId, IModelViews views)
    {
        if (items?[navId] is not IModelNavigationItem item)
            return;

        if (views[listViewId] is IModelListView listView)
            item.View = listView;

        if (item.Items[PersonRosterListViewIds.NavAll] is IModelNavigationItem all)
            all.Remove();
        if (item.Items[PersonRosterListViewIds.NavIncomplete] is IModelNavigationItem incomplete)
            incomplete.Remove();
    }

    private static void WireListViewDetailView(IModelViews modelViews, string listViewId, IModelDetailView detailView)
    {
        if (modelViews[listViewId] is not IModelListView listView)
            return;

        listView.DetailView = detailView;
    }
}