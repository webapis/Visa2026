using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Runtime safety net: ensures typed Person detail views and list <see cref="IModelListView.DetailView"/> wiring after model merge.
/// </summary>
public static class PersonTypedDetailViewConfigurator
{
    public static void EnsureConfigured(IModelApplication modelApplication)
    {
        if (modelApplication?.Views == null)
            return;

        var modelViews = modelApplication.Views;
        PersonTypedDetailViewFactory.SyncTypedDetailViews(modelViews);

        if (modelViews[PersonDetailViewIds.Employee] is not IModelDetailView employeeDetailView
            || modelViews[PersonDetailViewIds.FamilyMember] is not IModelDetailView familyMemberDetailView)
        {
            return;
        }

        WireListViewDetailView(modelViews, "Person_ListView_Employees", employeeDetailView);
        WireListViewDetailView(modelViews, "Person_ListView_FamilyMembers", familyMemberDetailView);
    }

    private static void WireListViewDetailView(IModelViews modelViews, string listViewId, IModelDetailView detailView)
    {
        if (modelViews[listViewId] is not IModelListView listView)
            return;

        listView.DetailView = detailView;
    }
}
