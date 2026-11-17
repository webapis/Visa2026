using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Runtime safety net: typed Person detail views + intake wizard layout after all model layers merge.
/// </summary>
public static class PersonTypedDetailViewConfigurator
{
    public static void Wire(XafApplication application)
    {
        application.SetupComplete += (_, _) => EnsureConfigured(application.Model);
        // User ModelDifference loads after login and can wipe wizard-only layout.
        application.LoggedOn += (_, _) => EnsureConfigured(application.Model);
    }

    public static void EnsureConfigured(IModelApplication modelApplication)
    {
        if (modelApplication?.Views == null)
            return;

        var modelViews = modelApplication.Views;
        PersonTypedDetailViewFactory.SyncTypedDetailViews(modelViews);
        PersonIntakeWizardModelPatch.Apply(modelApplication);

        if (modelViews[PersonDetailViewIds.Employee] is not IModelDetailView employeeDetailView
            || modelViews[PersonDetailViewIds.FamilyMember] is not IModelDetailView familyMemberDetailView
            || modelViews[PersonDetailViewIds.TemporaryVisitor] is not IModelDetailView temporaryVisitorDetailView)
        {
            return;
        }

        WireListViewDetailView(modelViews, "Person_ListView_Employees", employeeDetailView);
        WireListViewDetailView(modelViews, "Person_ListView_FamilyMembers", familyMemberDetailView);
        WireListViewDetailView(modelViews, "Person_ListView_TemporaryVisitors", temporaryVisitorDetailView);
    }

    private static void WireListViewDetailView(IModelViews modelViews, string listViewId, IModelDetailView detailView)
    {
        if (modelViews[listViewId] is not IModelListView listView)
            return;

        listView.DetailView = detailView;
    }
}