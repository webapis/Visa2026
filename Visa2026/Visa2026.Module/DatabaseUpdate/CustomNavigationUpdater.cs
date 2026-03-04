using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using System.Linq;

namespace Visa2026.Module.DatabaseUpdate
{
    public class CustomNavigationUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
    {
        public override void UpdateNode(ModelNode node)
        {
            var rootNode = (IModelRootNavigationItems)node;
            var navigationItems = rootNode.Items;
            var modelViews = rootNode.Application.Views;
 
            // Find the 'Default' group. If it doesn't exist, we'll add our group to the root.
            var defaultGroup = navigationItems["Default"];
            IModelNavigationItems targetCollection;
 
            if (defaultGroup != null) {
                targetCollection = defaultGroup.Items;
            }
            else {
                targetCollection = navigationItems;
            }
 
            // Create a new "People" group inside the target collection
            var peopleGroup = targetCollection["People"] ?? targetCollection.AddNode<IModelNavigationItem>("People");
            // Create a new "People" group directly at the root level
            var peopleGroup = navigationItems["People"] ?? navigationItems.AddNode<IModelNavigationItem>("People");
            peopleGroup.Caption = "People";
            peopleGroup.ImageName = "BO_User";
 
            var employeeListView = modelViews["Person_ListView_Employees"] as IModelListView;
            var employeeListView = EnsureListView(modelViews, "Person_ListView_Employees", "Person_ListView", "[IsEmployee] = true");
            if (employeeListView != null)
            {
                var employeeItem = peopleGroup.Items["Employees"] ?? peopleGroup.Items.AddNode<IModelNavigationItem>("Employees");
                employeeItem.Caption = "Employees";
                employeeItem.View = employeeListView;
                employeeItem.ImageName = "BO_Employee";
            }
 
            var familyMemberListView = modelViews["Person_ListView_FamilyMembers"] as IModelListView;
            var familyMemberListView = EnsureListView(modelViews, "Person_ListView_FamilyMembers", "Person_ListView", "[IsEmployee] = false");
            if (familyMemberListView != null)
            {
                var familyMemberItem = peopleGroup.Items["FamilyMembers"] ?? peopleGroup.Items.AddNode<IModelNavigationItem>("FamilyMembers");
                familyMemberItem.Caption = "Family Members";
                familyMemberItem.View = familyMemberListView;
                familyMemberItem.ImageName = "BO_Contact";
            }
        }

        private IModelListView EnsureListView(IModelViews views, string newViewId, string sourceViewId, string criteria)
        {
            var view = views[newViewId] as IModelListView;
            if (view == null)
            {
                var sourceView = views[sourceViewId] as IModelListView;
                if (sourceView != null)
                {
                    view = views.AddNode<IModelListView>(newViewId);
                    view.ModelClass = sourceView.ModelClass;
                    view.Criteria = criteria;
                }
            }
            return view;
        }
    }

    public class CustomViewClonerUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
    {
        public override void UpdateNode(ModelNode node)
        {
            var modelViews = (IModelViews)node;
            var originalListView = modelViews["Person_ListView"] as IModelListView;
            if (originalListView == null) return;

            // Create Employee ListView if it doesn't exist
            if (modelViews["Person_ListView_Employees"] == null)
            {
                var employeeListView = modelViews.AddNode<IModelListView>("Person_ListView_Employees");
                employeeListView.Id = "Person_ListView_Employees";
                employeeListView.ModelClass = originalListView.ModelClass;
                employeeListView.Criteria = "[IsEmployee] = true";
            }

            // Create Family Member ListView if it doesn't exist
            if (modelViews["Person_ListView_FamilyMembers"] == null)
            {
                var familyMemberListView = modelViews.AddNode<IModelListView>("Person_ListView_FamilyMembers");
                familyMemberListView.Id = "Person_ListView_FamilyMembers";
                familyMemberListView.ModelClass = originalListView.ModelClass;
                familyMemberListView.Criteria = "[IsEmployee] = false";
            }
        }
    }
}