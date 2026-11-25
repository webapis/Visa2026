using DevExpress.ExpressApp;
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

            EnsurePersonRosterGroupNavigation(
                navigationItems,
                modelViews,
                groupNavId: PersonRosterListViewIds.NavEmployees,
                allListViewId: PersonRosterListViewIds.Employees,
                incompleteListViewId: PersonRosterListViewIds.EmployeesIncomplete,
                roleCriteria: PersonRoleHelper.EmployeeCriteria,
                incompleteCriteria: PersonRosterListViewIds.EmployeesIncompleteCriteria,
                groupCaption: "Employees",
                incompleteCaption: "Incomplete",
                imageName: "BO_Employee",
                index: 0);

            EnsurePersonRosterGroupNavigation(
                navigationItems,
                modelViews,
                groupNavId: PersonRosterListViewIds.NavFamilyMembers,
                allListViewId: PersonRosterListViewIds.FamilyMembers,
                incompleteListViewId: PersonRosterListViewIds.FamilyMembersIncomplete,
                roleCriteria: PersonRoleHelper.FamilyMemberCriteria,
                incompleteCriteria: PersonRosterListViewIds.FamilyMembersIncompleteCriteria,
                groupCaption: "Family Members",
                incompleteCaption: "Incomplete",
                imageName: "BO_Contact",
                index: 1);

            EnsureInvitationsGroupNavigation(navigationItems, modelViews);
            EnsureWorkPermitsGroupNavigation(navigationItems, modelViews);

            EnsureTopLevelPersonNavigation(
                navigationItems,
                modelViews,
                navItemId: "TemporaryVisitors",
                listViewId: "Person_ListView_TemporaryVisitors",
                criteria: PersonRoleHelper.TemporaryVisitorCriteria,
                caption: "Temporary visitor",
                imageName: "BO_Person",
                index: 4);

            ConfigureApplicationProfileInstanceProgressRouteNavigation(navigationItems, modelViews);
            RemoveLegacyLookupOperationalNavigation(navigationItems);
            RemoveStaleInvitationBorderZoneNavigation(navigationItems);
            RemoveStaleApplicationProfileListNavigation(navigationItems);
            HideTemplateOwnedConfigurationNavigation(navigationItems);
            HideRetiredLeftNavigation(navigationItems);
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

        /// <summary>
        /// Shared catalogs are edited from Choose Organization and case Organization (inline New/Edit).
        /// Hide leftover native BO nav and the Organization catalogs Configuration item.
        /// Ministry SLA days also live on the profile; keep the singleton for snapshot fallback.
        /// </summary>
        private static void HideTemplateOwnedConfigurationNavigation(IModelNavigationItems navigationItems)
        {
            if (navigationItems["Configuration"] is not IModelNavigationItem configurationGroup)
                return;

            HideNavItemIfPresent(configurationGroup, "MinistryReviewSlaSettings");
            HideNavItemIfPresent(configurationGroup, "ApprovalLegProfile");
            HideNavItemIfPresent(configurationGroup, "ApprovingMinistry");
            HideNavItemIfPresent(configurationGroup, "AuthorizedSignatory");
            HideNavItemIfPresent(configurationGroup, "AuthorizedRepresentative");
            HideNavItemIfPresent(configurationGroup, "CompanyProfile");
            HideNavItemIfPresent(configurationGroup, "OrganizationCatalogs");
            HideNavItemIfPresent(configurationGroup, "OrganizationCatalogsHost");
        }

        private static void HideNavItemIfPresent(IModelNavigationItem parentGroup, string itemId)
        {
            if (parentGroup.Items[itemId] is IModelNavigationItem item)
                item.Visible = false;
        }

        /// <summary>
        /// Invitation, work permit headers/items, the WorkPermit folder, visa tracking, the Visa Extension folder,
        /// work-permit tracking, temporary visitors, rejection, and border zone stay off the left menu.
        /// Records remain reachable from a person or an application.
        /// </summary>
        private static void HideRetiredLeftNavigation(IModelNavigationItems navigationItems)
        {
            HideTopLevelIfPresent(navigationItems, "TemporaryVisitors");
            HideTopLevelIfPresent(navigationItems, "Invitation");
            HideTopLevelIfPresent(navigationItems, "Rejection");
            HideTopLevelIfPresent(navigationItems, "BorderZone");
            HideTopLevelIfPresent(navigationItems, "Visa Tracking");
            HideTopLevelIfPresent(navigationItems, "Work Permit Tracking");
            HideTopLevelIfPresent(navigationItems, "VisaExt");
            HideTopLevelIfPresent(navigationItems, "WorkPermit");

            if (navigationItems["WorkPermit"] is IModelNavigationItem workPermitGroup)
            {
                HideNavItemIfPresent(workPermitGroup, "WorkPermit");
                HideNavItemIfPresent(workPermitGroup, "WorkPermitItem");
                HideNavItemIfPresent(workPermitGroup, "ForeignWorkerMaglumat");
                HideNavItemIfPresent(workPermitGroup, "WorkPermitLocationGrant");
                HideNavItemIfPresent(workPermitGroup, "WorkPermitLocationGrantItem");
            }

            if (navigationItems["People"] is IModelNavigationItem peopleGroup)
                HideNavItemIfPresent(peopleGroup, "TemporaryVisitors");

            if (navigationItems["Default"] is IModelNavigationItem defaultGroup)
            {
                foreach (var itemId in new[]
                         {
                             "Invitation", "InvitationItem",
                             "WorkPermit", "WorkPermitItem",
                             "Rejection", "RejectionItem",
                             "BorderZone", "BorderZoneItem",
                             "VisaExtensionStatus", "VisaExtensionTracking", "VisaTransferStatus",
                             "VisaCancelExtStatus", "VisaCancellationStatus",
                             "WorkPermitExtensionStatus", "WorkPermitExtensionTracking",
                         })
                {
                    HideNavItemIfPresent(defaultGroup, itemId);
                }
            }
        }

        private static void HideTopLevelIfPresent(IModelNavigationItems navigationItems, string itemId)
        {
            if (navigationItems[itemId] is IModelNavigationItem item)
                item.Visible = false;
        }

        private static void EnsurePersonRosterGroupNavigation(
            IModelNavigationItems navigationItems,
            IModelViews modelViews,
            string groupNavId,
            string allListViewId,
            string incompleteListViewId,
            string roleCriteria,
            string incompleteCriteria,
            string groupCaption,
            string incompleteCaption,
            string imageName,
            int index)
        {
            var allView = EnsureListView(modelViews, allListViewId, "Person_ListView", roleCriteria);
            // Incomplete ListViews: PersonIncompleteRosterListViewSync (ModelMember-safe columns).
            var incompleteView = modelViews[incompleteListViewId] as IModelListView;
            if (allView == null || incompleteView == null)
                return;

            allView.Caption = groupCaption;
            incompleteView.Caption = incompleteCaption;
            incompleteView.Criteria = incompleteCriteria;
            if (allView.DetailView != null)
                incompleteView.DetailView = allView.DetailView;

            var group = navigationItems[groupNavId] ?? navigationItems.AddNode<IModelNavigationItem>(groupNavId);
            group.Caption = groupCaption;
            group.ImageName = imageName;
            group.Index = index;
            group.View = allView;
            if (group.Items[PersonRosterListViewIds.NavAll] is IModelNavigationItem allNav)
                allNav.Remove();
            if (group.Items[PersonRosterListViewIds.NavIncomplete] is IModelNavigationItem incompleteNav)
                incompleteNav.Remove();
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
        /// Officers use top-level Employees / Family Members. Temporary visitors stay off the left navigation.
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

        private static void ConfigureApplicationProfileInstanceProgressRouteNavigation(
            IModelNavigationItems navigationItems,
            IModelViews modelViews)
        {
            // ApplicationProfileInstance and ApplicationRosterMergeLine use [NavigationItem(false)]; create the group explicitly
            // (previously ApplicationRosterMergeLine anchored this folder).
            var applicationGroup = navigationItems["Application"]
                ?? navigationItems.AddNode<IModelNavigationItem>("Application");
            applicationGroup.Caption = ApplicationProfileInstanceProgressRouteNavigation.CaptionGroup;
            applicationGroup.ImageName ??= "BO_FileAttachment";

            EnsureApplicationListView(
                modelViews,
                ApplicationProfileInstanceProgressRouteNavigation.ListViewStaged,
                ApplicationProfileInstanceProgressRouteNavigation.CriteriaStaged,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionStaged);
            HideApplicationListNavItem(
                applicationGroup,
                ApplicationProfileInstanceProgressRouteNavigation.NavItemStaged);

            EnsureApplicationListView(
                modelViews,
                ApplicationProfileInstanceProgressRouteNavigation.ListViewInProcess,
                ApplicationProfileInstanceProgressRouteNavigation.CriteriaInProcess,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionInProcess);
            HideApplicationListNavItem(
                applicationGroup,
                ApplicationProfileInstanceProgressRouteNavigation.NavItemInProcess);

            var viaMinistriesView = EnsureApplicationListView(
                modelViews,
                ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries,
                ApplicationProfileInstanceProgressRouteNavigation.CriteriaViaMinistries,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionViaMinistries);
            EnsureApplicationListNavItem(
                applicationGroup,
                viaMinistriesView,
                ApplicationProfileInstanceProgressRouteNavigation.NavItemViaMinistries,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionViaMinistries,
                "BO_Organization",
                index: 3);

            var directView = EnsureApplicationListView(
                modelViews,
                ApplicationProfileInstanceProgressRouteNavigation.ListViewDirectMigration,
                ApplicationProfileInstanceProgressRouteNavigation.CriteriaDirectMigration,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionDirectMigration);
            if (modelViews[ApplicationProfileInstanceProgressRouteNavigation.ListViewDirectMigration] is IModelListView directMigrationListView)
            {
                // Direct migration has no ministry approval SLA; hide both SLA deadline columns.
                SetColumnVisibility(directMigrationListView, nameof(BusinessObjects.ApplicationProfileInstance.ProgressSlaStatement), false);
                SetColumnVisibility(directMigrationListView, nameof(BusinessObjects.ApplicationProfileInstance.MigrationSlaStatement), false);
                SetColumnVisibility(directMigrationListView, nameof(BusinessObjects.ApplicationProfileInstance.MigrationService), true);
            }
            EnsureApplicationListNavItem(
                applicationGroup,
                directView,
                ApplicationProfileInstanceProgressRouteNavigation.NavItemDirectMigration,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionDirectMigration,
                "BO_Localization",
                index: 4);

            HideApplicationListNavItem(applicationGroup, InvitationItemCakylyklarNavigation.LegacyNavItemId);

            RemoveApplicationItemRouteNavItems(applicationGroup);
            // Remove the node if another generator re-added it (Administrators ignore nav Deny).
            if (applicationGroup.Items["Application"] is IModelNavigationItem legacyApplicationItem)
                legacyApplicationItem.Remove();

            if (applicationGroup.Items["ApplicationRosterMergeLine"] is IModelNavigationItem legacyApplicationItemsItem)
                legacyApplicationItemsItem.Remove();
        }

        private static void EnsureApplicationListNavItem(
            IModelNavigationItem applicationGroup,
            IModelListView? view,
            string navItemId,
            string caption,
            string imageName,
            int index)
        {
            if (view == null)
                return;

            var navItem = applicationGroup.Items[navItemId]
                ?? applicationGroup.Items.AddNode<IModelNavigationItem>(navItemId);
            navItem.View = view;
            navItem.Caption = caption;
            navItem.ImageName = imageName;
            navItem.Index = index;
        }

        /// <summary>
        /// Top-level Invitations group (near Employees / Family Members): Employee and Family member registries.
        /// TemporaryVisitor invitation lines are excluded from both lists.
        /// </summary>
        private static void EnsureInvitationsGroupNavigation(
            IModelNavigationItems navigationItems,
            IModelViews modelViews)
        {
            var employeesView = EnsureInvitationItemCakylyklarListView(
                modelViews,
                InvitationItemCakylyklarNavigation.ListViewEmployeesId,
                InvitationItemCakylyklarNavigation.CriteriaEmployees,
                InvitationItemCakylyklarNavigation.CaptionEmployee);
            var familyView = EnsureInvitationItemCakylyklarListView(
                modelViews,
                InvitationItemCakylyklarNavigation.ListViewFamilyMembersId,
                InvitationItemCakylyklarNavigation.CriteriaFamilyMembers,
                InvitationItemCakylyklarNavigation.CaptionFamilyMember);
            if (employeesView == null || familyView == null)
                return;

            var group = navigationItems[InvitationItemCakylyklarNavigation.GroupNavId]
                ?? navigationItems.AddNode<IModelNavigationItem>(InvitationItemCakylyklarNavigation.GroupNavId);
            group.Caption = InvitationItemCakylyklarNavigation.CaptionGroup;
            group.ImageName = InvitationItemCakylyklarNavigation.ImageName;
            group.Index = InvitationItemCakylyklarNavigation.GroupNavIndex;
            group.View = null;

            var employeeNav = group.Items[InvitationItemCakylyklarNavigation.NavItemEmployee]
                ?? group.Items.AddNode<IModelNavigationItem>(InvitationItemCakylyklarNavigation.NavItemEmployee);
            employeeNav.View = employeesView;
            employeeNav.Caption = InvitationItemCakylyklarNavigation.CaptionEmployee;
            employeeNav.ImageName = "BO_Employee";
            employeeNav.Index = 0;

            var familyNav = group.Items[InvitationItemCakylyklarNavigation.NavItemFamilyMember]
                ?? group.Items.AddNode<IModelNavigationItem>(InvitationItemCakylyklarNavigation.NavItemFamilyMember);
            familyNav.View = familyView;
            familyNav.Caption = InvitationItemCakylyklarNavigation.CaptionFamilyMember;
            familyNav.ImageName = "BO_Contact";
            familyNav.Index = 1;
        }

        /// <summary>
        /// Top-level Work Permits group: Employee registry only.
        /// </summary>
        private static void EnsureWorkPermitsGroupNavigation(
            IModelNavigationItems navigationItems,
            IModelViews modelViews)
        {
            var employeesView = EnsureWorkPermitItemRegistryListView(
                modelViews,
                WorkPermitItemRegistryNavigation.ListViewEmployeesId,
                WorkPermitItemRegistryNavigation.CriteriaEmployees,
                WorkPermitItemRegistryNavigation.CaptionEmployee);
            if (employeesView == null)
                return;

            var group = navigationItems[WorkPermitItemRegistryNavigation.GroupNavId]
                ?? navigationItems.AddNode<IModelNavigationItem>(WorkPermitItemRegistryNavigation.GroupNavId);
            group.Caption = WorkPermitItemRegistryNavigation.CaptionGroup;
            group.ImageName = WorkPermitItemRegistryNavigation.ImageName;
            group.Index = WorkPermitItemRegistryNavigation.GroupNavIndex;
            group.View = null;

            var employeeNav = group.Items[WorkPermitItemRegistryNavigation.NavItemEmployee]
                ?? group.Items.AddNode<IModelNavigationItem>(WorkPermitItemRegistryNavigation.NavItemEmployee);
            employeeNav.View = employeesView;
            employeeNav.Caption = WorkPermitItemRegistryNavigation.CaptionEmployee;
            employeeNav.ImageName = "BO_Employee";
            employeeNav.Index = 0;
        }

        private static IModelListView? EnsureWorkPermitItemRegistryListView(
            IModelViews views,
            string listViewId,
            string criteria,
            string caption)
        {
            var sourceView = views[WorkPermitItemRegistryNavigation.SourceListViewId] as IModelListView;
            var view = views[listViewId] as IModelListView;
            if (view == null && sourceView != null)
            {
                view = views.AddNode<IModelListView>(listViewId);
                view.ModelClass = sourceView.ModelClass;
            }

            if (view == null)
                return null;

            if (view.ModelClass == null && sourceView != null)
                view.ModelClass = sourceView.ModelClass;

            view.Criteria = criteria;
            view.Caption = caption;
            view.DataAccessMode = CollectionSourceDataAccessMode.Client;
            ModelListViewColumnVisibility.ConfigureWorkPermitRegistryColumns(view);
            return view;
        }

        private static IModelListView? EnsureInvitationItemCakylyklarListView(
            IModelViews views,
            string listViewId,
            string criteria,
            string caption)
        {
            var sourceView = views[InvitationItemCakylyklarNavigation.SourceListViewId] as IModelListView;
            var view = views[listViewId] as IModelListView;
            if (view == null && sourceView != null)
            {
                view = views.AddNode<IModelListView>(listViewId);
                view.ModelClass = sourceView.ModelClass;
            }

            if (view == null)
                return null;

            if (view.ModelClass == null && sourceView != null)
                view.ModelClass = sourceView.ModelClass;

            view.Criteria = criteria;
            view.Caption = caption;
            view.DataAccessMode = CollectionSourceDataAccessMode.Client;
            ModelListViewColumnVisibility.ConfigureCakylyklarColumns(view);
            return view;
        }

        private static void HideApplicationListNavItem(IModelNavigationItem applicationGroup, string navItemId)
        {
            if (applicationGroup.Items[navItemId] is IModelNavigationItem navItem)
                navItem.Visible = false;
        }

        private static void RemoveApplicationItemRouteNavItems(IModelNavigationItem applicationGroup)
        {
            foreach (var routeNavId in new[]
                     {
                         ApplicationProfileInstanceProgressRouteNavigation.NavItemViaMinistries,
                         ApplicationProfileInstanceProgressRouteNavigation.NavItemDirectMigration,
                     })
            {
                if (applicationGroup.Items[routeNavId] is not IModelNavigationItem routeNavItem)
                    continue;

                if (routeNavItem.Items[ApplicationProfileInstanceProgressRouteNavigation.NavItemItemsViaMinistries]
                    is IModelNavigationItem viaItemsNav)
                    viaItemsNav.Remove();

                if (routeNavItem.Items[ApplicationProfileInstanceProgressRouteNavigation.NavItemItemsDirectMigration]
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

        private static IModelListView? EnsureApplicationListView(
            IModelViews views,
            string newViewId,
            string criteria,
            string? caption = null)
        {
            var sourceView = ResolveSourceApplicationListView(views);
            var view = views[newViewId] as IModelListView;
            if (view == null && sourceView != null)
            {
                view = views.AddNode<IModelListView>(newViewId);
                view.ModelClass = sourceView.ModelClass;
            }

            if (view != null)
            {
                if (view.ModelClass == null && sourceView != null)
                    view.ModelClass = sourceView.ModelClass;
                view.Criteria = criteria;
                if (!string.IsNullOrEmpty(caption))
                    view.Caption = caption;
            }

            return view;
        }

        private static IModelListView? ResolveSourceApplicationListView(IModelViews views) =>
            views[ApplicationProfileInstanceProgressRouteNavigation.SourceListView] as IModelListView
            ?? views[ApplicationProfileInstanceProgressRouteNavigation.LegacySourceListView] as IModelListView;

        private static void SetColumnVisibility(IModelListView view, string propertyName, bool visible)
            => ModelListViewColumnVisibility.Set(view, propertyName, visible);
    }

    public class CustomViewClonerUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
    {
        public override void UpdateNode(ModelNode node)
        {
            var modelViews = (IModelViews)node;
            CloneApplicationRouteListViews(modelViews);

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

            PersonIncompleteRosterListViewSync.EnsureAll(modelViews);

            HideCurrentRejectionItemColumn(modelViews, "Person_ListView_Employees");
            HideCurrentRejectionItemColumn(modelViews, "Person_ListView_FamilyMembers");
            HideCurrentRejectionItemColumn(modelViews, PersonRosterListViewIds.EmployeesIncomplete);
            HideCurrentRejectionItemColumn(modelViews, PersonRosterListViewIds.FamilyMembersIncomplete);
            HideCurrentRejectionItemColumn(modelViews, "Person_ListView_TemporaryVisitors");

            if (node is ModelNode viewsNode && viewsNode.Root is IModelApplication modelApplication)
                PersonTypedDetailViewConfigurator.EnsureConfigured(modelApplication);
        }

        private static void HideCurrentRejectionItemColumn(IModelViews modelViews, string viewId)
        {
            if (modelViews[viewId] is IModelListView listView)
                SetColumnVisibility(listView, "CurrentRejectionItem", false);
        }

        private static void CloneApplicationRouteListViews(IModelViews modelViews)
        {
            CloneApplicationListViewIfMissing(
                modelViews,
                ApplicationProfileInstanceProgressRouteNavigation.ListViewStaged,
                ApplicationProfileInstanceProgressRouteNavigation.CriteriaStaged,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionStaged);
            CloneApplicationListViewIfMissing(
                modelViews,
                ApplicationProfileInstanceProgressRouteNavigation.ListViewInProcess,
                ApplicationProfileInstanceProgressRouteNavigation.CriteriaInProcess,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionInProcess);
            CloneApplicationListViewIfMissing(
                modelViews,
                ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries,
                ApplicationProfileInstanceProgressRouteNavigation.CriteriaViaMinistries,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionViaMinistries);
            if (modelViews[ApplicationProfileInstanceProgressRouteNavigation.ListViewViaMinistries] is IModelListView viaMinistriesListView)
            {
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.ApplicationProfileInstance.Urgency), true);
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.ApplicationProfileInstance.ApprovalLegProfile), false);
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.ApplicationProfileInstance.ProjectContract), true);
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.ApplicationProfileInstance.VisaPeriod), true);
                SetColumnVisibility(viaMinistriesListView, nameof(BusinessObjects.ApplicationProfileInstance.VisaType), true);
            }
            CloneApplicationListViewIfMissing(
                modelViews,
                ApplicationProfileInstanceProgressRouteNavigation.ListViewDirectMigration,
                ApplicationProfileInstanceProgressRouteNavigation.CriteriaDirectMigration,
                ApplicationProfileInstanceProgressRouteNavigation.CaptionDirectMigration);
            if (modelViews[ApplicationProfileInstanceProgressRouteNavigation.ListViewDirectMigration] is IModelListView directMigrationListView)
            {
                SetColumnVisibility(directMigrationListView, nameof(BusinessObjects.ApplicationProfileInstance.ProgressSlaStatement), false);
                SetColumnVisibility(directMigrationListView, nameof(BusinessObjects.ApplicationProfileInstance.MigrationSlaStatement), false);
                SetColumnVisibility(directMigrationListView, nameof(BusinessObjects.ApplicationProfileInstance.MigrationService), true);
            }

            CloneInvitationItemCakylyklarListViewIfMissing(modelViews);
            CloneWorkPermitItemRegistryListViewIfMissing(modelViews);
        }


        private static void CloneWorkPermitItemRegistryListViewIfMissing(IModelViews modelViews)
        {
            CloneOneWorkPermitRegistryListViewIfMissing(
                modelViews,
                WorkPermitItemRegistryNavigation.ListViewEmployeesId,
                WorkPermitItemRegistryNavigation.CriteriaEmployees,
                WorkPermitItemRegistryNavigation.CaptionEmployee);
        }

        private static void CloneOneWorkPermitRegistryListViewIfMissing(
            IModelViews modelViews,
            string listViewId,
            string criteria,
            string caption)
        {
            var sourceView = modelViews[WorkPermitItemRegistryNavigation.SourceListViewId] as IModelListView;
            if (modelViews[listViewId] is not IModelListView targetView)
            {
                if (sourceView == null)
                    return;

                targetView = modelViews.AddNode<IModelListView>(listViewId);
                targetView.Id = listViewId;
                targetView.ModelClass = sourceView.ModelClass;
                CopyColumns(sourceView, targetView);
            }

            if (targetView.ModelClass == null && sourceView != null)
                targetView.ModelClass = sourceView.ModelClass;

            targetView.Criteria = criteria;
            targetView.Caption = caption;
            targetView.DataAccessMode = CollectionSourceDataAccessMode.Client;
            ModelListViewColumnVisibility.ConfigureWorkPermitRegistryColumns(targetView);
        }

        private static void CloneInvitationItemCakylyklarListViewIfMissing(IModelViews modelViews)
        {
            CloneOneCakylyklarListViewIfMissing(
                modelViews,
                InvitationItemCakylyklarNavigation.ListViewEmployeesId,
                InvitationItemCakylyklarNavigation.CriteriaEmployees,
                InvitationItemCakylyklarNavigation.CaptionEmployee);
            CloneOneCakylyklarListViewIfMissing(
                modelViews,
                InvitationItemCakylyklarNavigation.ListViewFamilyMembersId,
                InvitationItemCakylyklarNavigation.CriteriaFamilyMembers,
                InvitationItemCakylyklarNavigation.CaptionFamilyMember);
        }

        private static void CloneOneCakylyklarListViewIfMissing(
            IModelViews modelViews,
            string listViewId,
            string criteria,
            string caption)
        {
            var sourceView = modelViews[InvitationItemCakylyklarNavigation.SourceListViewId] as IModelListView;
            if (modelViews[listViewId] is not IModelListView targetView)
            {
                if (sourceView == null)
                    return;

                targetView = modelViews.AddNode<IModelListView>(listViewId);
                targetView.Id = listViewId;
                targetView.ModelClass = sourceView.ModelClass;
                CopyColumns(sourceView, targetView);
            }

            if (targetView.ModelClass == null && sourceView != null)
                targetView.ModelClass = sourceView.ModelClass;

            targetView.Criteria = criteria;
            targetView.Caption = caption;
            targetView.DataAccessMode = CollectionSourceDataAccessMode.Client;
            ModelListViewColumnVisibility.ConfigureCakylyklarColumns(targetView);
        }

        private static void CloneApplicationListViewIfMissing(
            IModelViews modelViews,
            string targetViewId,
            string criteria,
            string? caption = null)
        {
            var sourceView = ResolveSourceApplicationListView(modelViews);
            if (modelViews[targetViewId] is not IModelListView targetView)
            {
                if (sourceView == null)
                    return;

                targetView = modelViews.AddNode<IModelListView>(targetViewId);
                targetView.Id = targetViewId;
                targetView.ModelClass = sourceView.ModelClass;
                CopyColumns(sourceView, targetView);
            }
            else if (targetView.ModelClass == null && sourceView != null)
            {
                targetView.ModelClass = sourceView.ModelClass;
                if (targetView.Columns.Count == 0)
                    CopyColumns(sourceView, targetView);
            }

            targetView.Criteria = criteria;
            if (!string.IsNullOrEmpty(caption))
                targetView.Caption = caption;
        }

        private static IModelListView? ResolveSourceApplicationListView(IModelViews views) =>
            views[ApplicationProfileInstanceProgressRouteNavigation.SourceListView] as IModelListView
            ?? views[ApplicationProfileInstanceProgressRouteNavigation.LegacySourceListView] as IModelListView;

        private static void CopyColumns(IModelListView source, IModelListView target)
        {
            foreach (var sourceColumn in source.Columns)
            {
                if (string.IsNullOrWhiteSpace(sourceColumn.PropertyName))
                    continue;

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

        public static void SetIndex(IModelListView view, string propertyName, int index)
        {
            var column = view.Columns.FirstOrDefault(c => c.PropertyName == propertyName);
            if (column == null)
            {
                column = view.Columns.AddNode<IModelColumn>(propertyName);
                column.PropertyName = propertyName;
            }

            column.Index = index;
        }

        /// <summary>
        /// Çakylyklar columns: State last; Invitation № before State (preview link).
        /// </summary>
        /// <summary>
        /// Work Permits (Employee) registry columns: State last.
        /// </summary>
        public static void ConfigureWorkPermitRegistryColumns(IModelListView view)
        {
            Set(view, "WorkPermitItemName", false);
            Set(view, "Person", false);
            Set(view, "Passport", false);
            Set(view, "WorkPermit", false);
            Set(view, "CurrentPositionHistory", false);
            Set(view, "ASNumber", false);
            Set(view, "WorkPermittedLocations", false);
            Set(view, "DocumentCopiesListLink", false);
            Set(view, "ShowOptionalFields", false);

            Set(view, nameof(WorkPermitItem.RegistryPersonDisplay), true);
            Set(view, nameof(WorkPermitItem.RegistryPassportDisplay), true);
            Set(view, nameof(WorkPermitItem.RegistryDepartmentDisplay), true);
            Set(view, nameof(WorkPermitItem.RegistryWorkPermitNumberDisplay), true);
            Set(view, nameof(WorkPermitItem.StartDate), true);
            Set(view, nameof(WorkPermitItem.ExpirationDate), true);
            Set(view, nameof(WorkPermitItem.RegistryDaysRemaining), true);
            Set(view, nameof(WorkPermitItem.RegistryProcessNumberDisplay), true);
            Set(view, nameof(WorkPermitItem.RegistryLinkedDetailDisplay), true);
            Set(view, nameof(WorkPermitItem.RegistryStateDisplay), true);

            SetIndex(view, nameof(WorkPermitItem.RegistryPersonDisplay), 0);
            SetIndex(view, nameof(WorkPermitItem.RegistryPassportDisplay), 1);
            SetIndex(view, nameof(WorkPermitItem.RegistryDepartmentDisplay), 2);
            SetIndex(view, nameof(WorkPermitItem.RegistryWorkPermitNumberDisplay), 3);
            SetIndex(view, nameof(WorkPermitItem.StartDate), 4);
            SetIndex(view, nameof(WorkPermitItem.ExpirationDate), 5);
            SetIndex(view, nameof(WorkPermitItem.RegistryDaysRemaining), 6);
            SetIndex(view, nameof(WorkPermitItem.RegistryProcessNumberDisplay), 7);
            SetIndex(view, nameof(WorkPermitItem.RegistryLinkedDetailDisplay), 8);
            SetIndex(view, nameof(WorkPermitItem.RegistryStateDisplay), 9);
        }
        public static void ConfigureCakylyklarColumns(IModelListView view)
        {
            Set(view, "InvitationItemName", false);
            Set(view, "Person", false);
            Set(view, "Passport", false);
            Set(view, "Invitation", false);
            Set(view, "ApplicationProfileInstance", false);
            Set(view, "IssuedVisa", false);
            Set(view, nameof(InvitationItem.DocumentCopiesListLink), false);

            Set(view, nameof(InvitationItem.CakylyklarPersonDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarPassportDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarDepartmentDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarWorkPermitDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarVisaTypeDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarVisaCategoryDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarVisaPeriodDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarStateDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarProcessNumberDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarLinkedDetailDisplay), true);
            Set(view, nameof(InvitationItem.ExpirationDate), true);
            Set(view, nameof(InvitationItem.CakylyklarDaysRemaining), true);
            Set(view, nameof(InvitationItem.CakylyklarInvitationNumberDisplay), true);
            Set(view, nameof(InvitationItem.CakylyklarApplicationDisplay), true);

            // Person → … → Linked → Expiration → Days remaining → Invitation № → State
            SetIndex(view, nameof(InvitationItem.CakylyklarPersonDisplay), 0);
            SetIndex(view, nameof(InvitationItem.CakylyklarPassportDisplay), 1);
            SetIndex(view, nameof(InvitationItem.CakylyklarDepartmentDisplay), 2);
            SetIndex(view, nameof(InvitationItem.CakylyklarWorkPermitDisplay), 3);
            SetIndex(view, nameof(InvitationItem.CakylyklarVisaTypeDisplay), 4);
            SetIndex(view, nameof(InvitationItem.CakylyklarVisaCategoryDisplay), 5);
            SetIndex(view, nameof(InvitationItem.CakylyklarVisaPeriodDisplay), 6);
            SetIndex(view, nameof(InvitationItem.CakylyklarProcessNumberDisplay), 7);
            SetIndex(view, nameof(InvitationItem.CakylyklarLinkedDetailDisplay), 8);
            SetIndex(view, nameof(InvitationItem.ExpirationDate), 9);
            SetIndex(view, nameof(InvitationItem.CakylyklarDaysRemaining), 10);
            SetIndex(view, nameof(InvitationItem.CakylyklarInvitationNumberDisplay), 11);
            SetIndex(view, nameof(InvitationItem.CakylyklarStateDisplay), 12);
            SetIndex(view, nameof(InvitationItem.CakylyklarApplicationDisplay), 13);
        }
    }
}