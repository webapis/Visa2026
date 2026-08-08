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
 
            // Legacy-style top-level person lists (not nested under People).
            // Captions must be set explicitly — without them XAF falls back to the Person class caption.
            if (navigationItems["People"] is IModelNavigationItem legacyPeopleGroup)
                legacyPeopleGroup.Remove();

            EnsureTopLevelPersonNavigation(
                navigationItems,
                modelViews,
                navItemId: "Employees",
                listViewId: "Person_ListView_Employees",
                criteria: PersonRoleHelper.EmployeeCriteria,
                caption: "Employees",
                imageName: "BO_Employee",
                index: 0);

            EnsureTopLevelPersonNavigation(
                navigationItems,
                modelViews,
                navItemId: "FamilyMembers",
                listViewId: "Person_ListView_FamilyMembers",
                criteria: PersonRoleHelper.FamilyMemberCriteria,
                caption: "Family Members",
                imageName: "BO_Contact",
                index: 1);

            EnsureTopLevelPersonNavigation(
                navigationItems,
                modelViews,
                navItemId: "TemporaryVisitors",
                listViewId: "Person_ListView_TemporaryVisitors",
                criteria: PersonRoleHelper.TemporaryVisitorCriteria,
                caption: "Temporary visitor",
                imageName: "BO_Person",
                index: 2);

            ConfigureApplicationProgressRouteNavigation(navigationItems, modelViews);
            RemoveLegacyLookupOperationalNavigation(navigationItems);
            RemoveStaleInvitationBorderZoneNavigation(navigationItems);
            RemoveStaleApplicationProfileListNavigation(navigationItems);
        }

        /// <summary>
        /// Officers use the custom Application Profile catalog host; strip native BO list nav if model diffs re-add it.
        /// </summary>
        private static void RemoveStaleApplicationProfileListNavigation(IModelNavigationItems navigationItems)
        {
            if (navigationItems["Configuration"] is not IModelNavigationItem configurationGroup)
                return;

            if (configurationGroup.Items["ApplicationProfile"] is IModelNavigationItem legacyProfileList)
                legacyProfileList.Remove();
        }

        private static void EnsureTopLevelPersonNavigation(
            IModelNavigationItems navigationItems,
            IModelViews modelViews,
            string navItemId,
            string listViewId,
            string criteria,
            string caption,
            string imageName,
            int index)
        {
            var listView = EnsureListView(modelViews, listViewId, "Person_ListView", criteria);
            if (listView == null)
                return;

            listView.Caption = caption;

            var navItem = navigationItems[navItemId] ?? navigationItems.AddNode<IModelNavigationItem>(navItemId);
            navItem.View = listView;
            navItem.Caption = caption;
            navItem.ImageName = imageName;
            navItem.Index = index;
        }

        /// <summary>
        /// Border zone permits live under the top-level <c>BorderZone</c> nav group; strip stale nodes under <c>Invitation</c>.
        /// </summary>
        private static void RemoveStaleInvitationBorderZoneNavigation(IModelNavigationItems navigationItems)
        {
            if (navigationItems["Invitation"] is not IModelNavigationItem invitationGroup)
                return;

            if (invitationGroup.Items["BorderZone"] is IModelNavigationItem legacyBorderZone)
                legacyBorderZone.Remove();
            if (invitationGroup.Items["BorderZoneItem"] is IModelNavigationItem legacyBorderZoneItem)
                legacyBorderZoneItem.Remove();
        }

        /// <summary>
        /// Person, Passport, Visa, etc. use <see cref="NavigationItemAttribute"/>(false); strip stale nodes if the generator re-added them.
        /// Officers use top-level Employees / Family Members / Temporary Visitors instead of a flat Person list.
        /// </summary>
        private static void RemoveLegacyLookupOperationalNavigation(IModelNavigationItems navigationItems)
        {
            if (navigationItems["Lookup"] is not IModelNavigationItem lookupGroup)
                return;

            RemoveNavItemIfPresent(lookupGroup, "Person", "Person");
            RemoveNavItemIfPresent(lookupGroup, "Person", "AddressOfResidence");
            RemoveNavItemIfPresent(lookupGroup, "Education", "Education");
            RemoveNavItemIfPresent(lookupGroup, "Housing", "Lodging");
            RemoveNavItemIfPresent(lookupGroup, "Housing", "Hotel");
            RemoveNavItemIfPresent(lookupGroup, "Housing", "Hospital");
            RemoveNavItemIfPresent(lookupGroup, "Housing", "OtherSite");
            RemoveNavItemIfPresent(lookupGroup, "Medical", "MedicalRecord");
            RemoveNavItemIfPresent(lookupGroup, "Passport", "Passport");
            RemoveNavItemIfPresent(lookupGroup, "Visa", "Visa");
            RemoveNavItemIfPresent(lookupGroup, "Organization", "Ministry");
            RemoveNavItemIfPresent(lookupGroup, "Invitation", "BorderZone");
            RemoveNavItemIfPresent(lookupGroup, "Invitation", "BorderZoneItem");
        }

        private static void RemoveNavItemIfPresent(IModelNavigationItem parentGroup, string subgroupId, string itemId)
        {
            if (parentGroup.Items[subgroupId]?.Items[itemId] is IModelNavigationItem legacyItem)
                legacyItem.Remove();
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
                var viaItem = applicationGroup.Items[ApplicationProgressRouteNavigation.NavItemViaMinistries]
                    ?? applicationGroup.Items.AddNode<IModelNavigationItem>(ApplicationProgressRouteNavigation.NavItemViaMinistries);
                viaItem.View = viaMinistriesView;
                viaItem.ImageName = "BO_Organization";
            }

            var directView = EnsureListView(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewDirectMigration,
                "Application_ListView",
                ApplicationProgressRouteNavigation.CriteriaDirectMigration);
            if (modelViews[ApplicationProgressRouteNavigation.ListViewDirectMigration] is IModelListView directMigrationListView)
            {
                // Direct migration has no ministry approval SLA; hide both SLA deadline columns.
                SetColumnVisibility(directMigrationListView, nameof(BusinessObjects.Application.ProgressSlaStatement), false);
                SetColumnVisibility(directMigrationListView, nameof(BusinessObjects.Application.MigrationSlaStatement), false);
            }
            if (directView != null)
            {
                var directItem = applicationGroup.Items[ApplicationProgressRouteNavigation.NavItemDirectMigration]
                    ?? applicationGroup.Items.AddNode<IModelNavigationItem>(ApplicationProgressRouteNavigation.NavItemDirectMigration);
                directItem.View = directView;
                directItem.ImageName = "BO_Localization";
            }

            RemoveApplicationItemRouteNavItems(applicationGroup);
            // Remove the node if another generator re-added it (Administrators ignore nav Deny).
            if (applicationGroup.Items["Application"] is IModelNavigationItem legacyApplicationItem)
                legacyApplicationItem.Remove();

            if (applicationGroup.Items["ApplicationItem"] is IModelNavigationItem legacyApplicationItemsItem)
                legacyApplicationItemsItem.Remove();
        }

        private static void RemoveApplicationItemRouteNavItems(IModelNavigationItem applicationGroup)
        {
            foreach (var routeNavId in new[]
                     {
                         ApplicationProgressRouteNavigation.NavItemViaMinistries,
                         ApplicationProgressRouteNavigation.NavItemDirectMigration,
                     })
            {
                if (applicationGroup.Items[routeNavId] is not IModelNavigationItem routeNavItem)
                    continue;

                if (routeNavItem.Items[ApplicationProgressRouteNavigation.NavItemItemsViaMinistries]
                    is IModelNavigationItem viaItemsNav)
                    viaItemsNav.Remove();

                if (routeNavItem.Items[ApplicationProgressRouteNavigation.NavItemItemsDirectMigration]
                    is IModelNavigationItem directItemsNav)
                    directItemsNav.Remove();
            }
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

        private static void SetColumnVisibility(IModelListView view, string propertyName, bool visible)
            => ModelListViewColumnVisibility.Set(view, propertyName, visible);
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
                employeeListView.Caption = "Employees";

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
                familyMemberListView.Caption = "Family Members";

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
                existingFamilyMemberListView.Caption = "Family Members";
                SetColumnVisibility(existingFamilyMemberListView, "Subcontractor", true);
                if (modelViews[PersonDetailViewIds.FamilyMember] is IModelDetailView familyDetailView)
                    existingFamilyMemberListView.DetailView = familyDetailView;
            }

            if (modelViews["Person_ListView_Employees"] is IModelListView existingEmployeeListView)
            {
                existingEmployeeListView.Caption = "Employees";
                if (modelViews[PersonDetailViewIds.Employee] is IModelDetailView employeeDetailView)
                    existingEmployeeListView.DetailView = employeeDetailView;
            }

            if (modelViews["Person_ListView_TemporaryVisitors"] == null)
            {
                var visitorListView = modelViews.AddNode<IModelListView>("Person_ListView_TemporaryVisitors");
                visitorListView.Id = "Person_ListView_TemporaryVisitors";
                visitorListView.ModelClass = originalListView.ModelClass;
                visitorListView.Criteria = PersonRoleHelper.TemporaryVisitorCriteria;
                visitorListView.Caption = "Temporary visitor";

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
                existingVisitorListView.Caption = "Temporary visitor";
                if (modelViews[PersonDetailViewIds.TemporaryVisitor] is IModelDetailView visitorDetailViewForList)
                    existingVisitorListView.DetailView = visitorDetailViewForList;
            }

            CloneApplicationListViewIfMissing(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewViaMinistries,
                ApplicationProgressRouteNavigation.CriteriaViaMinistries);
            if (modelViews[ApplicationProgressRouteNavigation.ListViewViaMinistries] is IModelListView viaMinistriesListView)
            {
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.Application.Urgency), true);
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.Application.ApprovalLegProfile), true);
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.Application.VisaPeriod), true);
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.Application.VisaType), true);
            }
            CloneApplicationListViewIfMissing(
                modelViews,
                ApplicationProgressRouteNavigation.ListViewDirectMigration,
                ApplicationProgressRouteNavigation.CriteriaDirectMigration);

            HideCurrentRejectionItemColumn(modelViews, "Person_ListView_Employees");
            HideCurrentRejectionItemColumn(modelViews, "Person_ListView_FamilyMembers");
            HideCurrentRejectionItemColumn(modelViews, "Person_ListView_TemporaryVisitors");

            if (node is ModelNode viewsNode && viewsNode.Root is IModelApplication modelApplication)
                PersonTypedDetailViewConfigurator.EnsureConfigured(modelApplication);
        }

        private static void HideCurrentRejectionItemColumn(IModelViews modelViews, string viewId)
        {
            if (modelViews[viewId] is IModelListView listView)
                SetColumnVisibility(listView, "CurrentRejectionItem", false);
        }

        private static void CloneApplicationListViewIfMissing(
            IModelViews modelViews,
            string targetViewId,
            string criteria)
        {
            if (modelViews[targetViewId] != null)
                return;

            if (modelViews["Application_ListView"] is not IModelListView sourceView)
                return;

            var targetView = modelViews.AddNode<IModelListView>(targetViewId);
            targetView.Id = targetViewId;
            targetView.ModelClass = sourceView.ModelClass;
            targetView.Criteria = criteria;
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
            => ModelListViewColumnVisibility.Set(view, propertyName, visible);
    }

    /// <summary>
    /// Shared ListView column Index hide/show helper for navigation and view-cloner updaters.
    /// </summary>
    file static class ModelListViewColumnVisibility
    {
        public static void Set(IModelListView view, string propertyName, bool visible)
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