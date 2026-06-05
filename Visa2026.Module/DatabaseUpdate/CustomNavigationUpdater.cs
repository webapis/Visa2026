using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using System.Linq;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    public class CustomNavigationUpdater : ModelNodesGeneratorUpdater<NavigationItemNodeGenerator>
    {
        public override void UpdateNode(ModelNode node)
        {
            var rootNode = (IModelRootNavigationItems)node;
            var navigationItems = rootNode.Items;
            var modelViews = rootNode.Application.Views;
 
            // Create a new "People" group directly at the root level
            var peopleGroup = navigationItems["People"] ?? navigationItems.AddNode<IModelNavigationItem>("People");
            peopleGroup.ImageName = "BO_User";

            var employeeListView = EnsureListView(modelViews, "Person_ListView_Employees", "Person_ListView", PersonRoleHelper.EmployeeCriteria);
            if (employeeListView != null)
            {
                var employeeItem = peopleGroup.Items["Employees"] ?? peopleGroup.Items.AddNode<IModelNavigationItem>("Employees");
                employeeItem.View = employeeListView;
                employeeItem.ImageName = "BO_Employee";
            }

            var familyMemberListView = EnsureListView(modelViews, "Person_ListView_FamilyMembers", "Person_ListView", PersonRoleHelper.FamilyMemberCriteria);
            if (familyMemberListView != null)
            {
                var familyMemberItem = peopleGroup.Items["FamilyMembers"] ?? peopleGroup.Items.AddNode<IModelNavigationItem>("FamilyMembers");
                familyMemberItem.View = familyMemberListView;
                familyMemberItem.ImageName = "BO_Contact";
            }

            var temporaryVisitorListView = EnsureListView(
                modelViews,
                "Person_ListView_TemporaryVisitors",
                "Person_ListView",
                PersonRoleHelper.TemporaryVisitorCriteria);
            if (temporaryVisitorListView != null)
            {
                var visitorItem = peopleGroup.Items["TemporaryVisitors"]
                    ?? peopleGroup.Items.AddNode<IModelNavigationItem>("TemporaryVisitors");
                visitorItem.View = temporaryVisitorListView;
                visitorItem.ImageName = "BO_Person";
            }

            ConfigureApplicationProgressRouteNavigation(navigationItems, modelViews);
        }

        private static void ConfigureApplicationProgressRouteNavigation(
            IModelNavigationItems navigationItems,
            IModelViews modelViews)
        {
            // Application and ApplicationItem use [NavigationItem(false)]; create the group explicitly
            // (previously ApplicationItem anchored this folder).
            var applicationGroup = navigationItems["Application"]
                ?? navigationItems.AddNode<IModelNavigationItem>("Application");
            applicationGroup.ImageName ??= "BO_FileAttachment";

            var viaMinistriesView = EnsureListView(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewViaMinistries,
                "Application_ListView",
                ApplicationProgressRouteNavigation.CriteriaViaMinistries);
            if (viaMinistriesView != null)
            {
                viaMinistriesView.Caption = "Applications (via ministries)";
                var viaItem = applicationGroup.Items[ApplicationProgressRouteNavigation.NavItemViaMinistries]
                    ?? applicationGroup.Items.AddNode<IModelNavigationItem>(ApplicationProgressRouteNavigation.NavItemViaMinistries);
                viaItem.View = viaMinistriesView;
                viaItem.ImageName = "BO_Organization";
                AttachApplicationItemNavigation(viaItem, modelViews, routeViaMinistries: true);
            }

            var directView = EnsureListView(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewDirectMigration,
                "Application_ListView",
                ApplicationProgressRouteNavigation.CriteriaDirectMigration);
            if (directView != null)
            {
                directView.Caption = "Applications (direct to migration)";
                var directItem = applicationGroup.Items[ApplicationProgressRouteNavigation.NavItemDirectMigration]
                    ?? applicationGroup.Items.AddNode<IModelNavigationItem>(ApplicationProgressRouteNavigation.NavItemDirectMigration);
                directItem.View = directView;
                directItem.ImageName = "BO_Localization";
                AttachApplicationItemNavigation(directItem, modelViews, routeViaMinistries: false);
            }

            // Default ListView for Application is hidden via [NavigationItem(false)] on the BO.
            // Remove the node if another generator re-added it (Administrators ignore nav Deny).
            if (applicationGroup.Items["Application"] is IModelNavigationItem legacyApplicationItem)
                legacyApplicationItem.Remove();

            if (applicationGroup.Items["ApplicationItem"] is IModelNavigationItem legacyApplicationItemsItem)
                legacyApplicationItemsItem.Remove();
        }

        private static void AttachApplicationItemNavigation(
            IModelNavigationItem routeNavItem,
            IModelViews modelViews,
            bool routeViaMinistries)
        {
            var listViewId = routeViaMinistries
                ? ApplicationProgressRouteNavigation.ListViewItemsViaMinistries
                : ApplicationProgressRouteNavigation.ListViewItemsDirectMigration;
            var criteria = routeViaMinistries
                ? ApplicationProgressRouteNavigation.CriteriaItemsViaMinistries
                : ApplicationProgressRouteNavigation.CriteriaItemsDirectMigration;
            var navItemId = routeViaMinistries
                ? ApplicationProgressRouteNavigation.NavItemItemsViaMinistries
                : ApplicationProgressRouteNavigation.NavItemItemsDirectMigration;
            var caption = routeViaMinistries
                ? "Application items (via ministries)"
                : "Application items (direct to migration)";

            var itemsView = EnsureListView(modelViews, listViewId, "ApplicationItem_ListView", criteria);
            if (itemsView == null)
                return;

            itemsView.Caption = caption;
            var itemsNavItem = routeNavItem.Items[navItemId]
                ?? routeNavItem.Items.AddNode<IModelNavigationItem>(navItemId);
            itemsNavItem.View = itemsView;
            itemsNavItem.ImageName = "BO_Order_Item";
        }

        private static IModelListView? EnsureListView(IModelViews views, string newViewId, string sourceViewId, string criteria)
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
                employeeListView.Criteria = PersonRoleHelper.EmployeeCriteria;

                CopyColumns(originalListView, employeeListView);

                // Customize columns for Employees
                SetColumnVisibility(employeeListView, "SponsoringEmployee", false);
                SetColumnVisibility(employeeListView, "Relationship", false);
                SetColumnVisibility(employeeListView, "Company", false);
                SetColumnVisibility(employeeListView, "Email", true);
                SetColumnVisibility(employeeListView, "CurrentPositionHistory", true);

                if (modelViews[PersonDetailViewIds.Employee] is IModelDetailView employeeDetailViewForList)
                    employeeListView.DetailView = employeeDetailViewForList;
            }

            // Create Family Member ListView if it doesn't exist
            if (modelViews["Person_ListView_FamilyMembers"] == null)
            {
                var familyMemberListView = modelViews.AddNode<IModelListView>("Person_ListView_FamilyMembers");
                familyMemberListView.Id = "Person_ListView_FamilyMembers";
                familyMemberListView.ModelClass = originalListView.ModelClass;
                familyMemberListView.Criteria = PersonRoleHelper.FamilyMemberCriteria;

                CopyColumns(originalListView, familyMemberListView);

                // Customize columns for Family Members
                SetColumnVisibility(familyMemberListView, "Company", false);
                SetColumnVisibility(familyMemberListView, "Subcontractor", true);
                SetColumnVisibility(familyMemberListView, "CurrentWorkPermitItem", false);
                SetColumnVisibility(familyMemberListView, "CurrentPositionHistory", false);
                SetColumnVisibility(familyMemberListView, "HireDate", false);
                
                SetColumnVisibility(familyMemberListView, "SponsoringEmployee", true);
                SetColumnVisibility(familyMemberListView, "Relationship", true);

                if (modelViews[PersonDetailViewIds.FamilyMember] is IModelDetailView familyDetailView)
                    familyMemberListView.DetailView = familyDetailView;
            }
            else if (modelViews["Person_ListView_FamilyMembers"] is IModelListView existingFamilyMemberListView)
            {
                SetColumnVisibility(existingFamilyMemberListView, "Subcontractor", true);
                if (modelViews[PersonDetailViewIds.FamilyMember] is IModelDetailView familyDetailView)
                    existingFamilyMemberListView.DetailView = familyDetailView;
            }

            if (modelViews["Person_ListView_Employees"] is IModelListView existingEmployeeListView
                && modelViews[PersonDetailViewIds.Employee] is IModelDetailView employeeDetailView)
            {
                existingEmployeeListView.DetailView = employeeDetailView;
            }

            if (modelViews["Person_ListView_TemporaryVisitors"] == null)
            {
                var visitorListView = modelViews.AddNode<IModelListView>("Person_ListView_TemporaryVisitors");
                visitorListView.Id = "Person_ListView_TemporaryVisitors";
                visitorListView.ModelClass = originalListView.ModelClass;
                visitorListView.Criteria = PersonRoleHelper.TemporaryVisitorCriteria;

                CopyColumns(originalListView, visitorListView);

                SetColumnVisibility(visitorListView, "SponsoringEmployee", false);
                SetColumnVisibility(visitorListView, "Relationship", false);
                SetColumnVisibility(visitorListView, "Company", false);
                SetColumnVisibility(visitorListView, "Email", false);
                SetColumnVisibility(visitorListView, "HireDate", false);
                SetColumnVisibility(visitorListView, "CurrentWorkPermitItem", false);
                SetColumnVisibility(visitorListView, "CurrentPositionHistory", false);
                SetColumnVisibility(visitorListView, "Subcontractor", true);
                SetColumnVisibility(visitorListView, "ProjectContract", true);

                if (modelViews[PersonDetailViewIds.TemporaryVisitor] is IModelDetailView visitorDetailView)
                    visitorListView.DetailView = visitorDetailView;
            }
            else if (modelViews["Person_ListView_TemporaryVisitors"] is IModelListView existingVisitorListView)
            {
                if (modelViews[PersonDetailViewIds.TemporaryVisitor] is IModelDetailView visitorDetailViewForList)
                    existingVisitorListView.DetailView = visitorDetailViewForList;
            }

            CloneApplicationListViewIfMissing(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewViaMinistries,
                ApplicationProgressRouteNavigation.CriteriaViaMinistries,
                "Applications (via ministries)");
            CloneApplicationListViewIfMissing(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewDirectMigration,
                ApplicationProgressRouteNavigation.CriteriaDirectMigration,
                "Applications (direct to migration)");
            CloneApplicationItemListViewIfMissing(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewItemsViaMinistries,
                ApplicationProgressRouteNavigation.CriteriaItemsViaMinistries,
                "Application items (via ministries)");
            CloneApplicationItemListViewIfMissing(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewItemsDirectMigration,
                ApplicationProgressRouteNavigation.CriteriaItemsDirectMigration,
                "Application items (direct to migration)");

            if (node is ModelNode viewsNode && viewsNode.Root is IModelApplication modelApplication)
                PersonTypedDetailViewConfigurator.EnsureConfigured(modelApplication);
        }

        private static void CloneApplicationItemListViewIfMissing(
            IModelViews modelViews,
            string targetViewId,
            string criteria,
            string caption)
        {
            if (modelViews[targetViewId] != null)
                return;

            if (modelViews["ApplicationItem_ListView"] is not IModelListView sourceView)
                return;

            var targetView = modelViews.AddNode<IModelListView>(targetViewId);
            targetView.Id = targetViewId;
            targetView.ModelClass = sourceView.ModelClass;
            targetView.Criteria = criteria;
            targetView.Caption = caption;
            CopyColumns(sourceView, targetView);
        }

        private static void CloneApplicationListViewIfMissing(
            IModelViews modelViews,
            string targetViewId,
            string criteria,
            string caption)
        {
            if (modelViews[targetViewId] != null)
                return;

            if (modelViews["Application_ListView"] is not IModelListView sourceView)
                return;

            var targetView = modelViews.AddNode<IModelListView>(targetViewId);
            targetView.Id = targetViewId;
            targetView.ModelClass = sourceView.ModelClass;
            targetView.Criteria = criteria;
            targetView.Caption = caption;
            CopyColumns(sourceView, targetView);
        }

        private static void CopyColumns(IModelListView source, IModelListView target)
        {
            foreach (var sourceColumn in source.Columns)
            {
                var targetColumn = target.Columns[sourceColumn.Id] ?? target.Columns.AddNode<IModelColumn>(sourceColumn.Id);
                targetColumn.PropertyName = sourceColumn.PropertyName;
                targetColumn.Index = sourceColumn.Index;
                targetColumn.Caption = sourceColumn.Caption;
                targetColumn.Width = sourceColumn.Width;
                targetColumn.SortIndex = sourceColumn.SortIndex;
                targetColumn.SortOrder = sourceColumn.SortOrder;
            }
        }

        private static void SetColumnVisibility(IModelListView view, string propertyName, bool visible)
        {
            var column = view.Columns.FirstOrDefault(c => c.PropertyName == propertyName);
            if (column == null && visible)
            {
                column = view.Columns.AddNode<IModelColumn>(propertyName);
                column.PropertyName = propertyName;
            }

            if (column != null)
            {
                column.Index = visible ? (column.Index == -1 ? 100 : column.Index) : -1;
            }
        }
    }
}