using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EF;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.Persistent.BaseImpl.EFCore.AuditTrail;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate
{
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();
            var userRole = CreateUserRole();

            ObjectSpace.CommitChanges();

            UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null)
            {
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) =>
                {
                    user.Roles.Add(defaultRole);
                });
            }

            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "Admin") == null)
            {
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "Admin", EmptyPassword, (user) =>
                {
                    user.Roles.Add(adminRole);
                });
            }

            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "StandardUser") == null)
            {
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "StandardUser", EmptyPassword, (user) =>
                {
                    user.Roles.Add(defaultRole);
                    user.Roles.Add(userRole);
                });
            }

            ObjectSpace.CommitChanges();
        }

        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();

            if (CurrentDBVersion < new Version("1.1.0.5"))
            {
                // ExecuteNonQueryCommand("EXEC sp_rename 'MyTable.OldColumnName', 'NewColumnName', 'COLUMN'", true);
            }
        }

        PermissionPolicyRole CreateAdminRole()
        {
            PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
            if (adminRole == null)
            {
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
                adminRole.IsAdministrative = true;
            }
            return adminRole;
        }

     PermissionPolicyRole CreateUserRole()
{
    PermissionPolicyRole userRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Users");
    if (userRole == null)
    {
        userRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
        userRole.Name = "Users";

        // =====================================================================
        // FULL ACCESS — Core operational objects
        // =====================================================================
        userRole.AddTypePermissionsRecursively<Person>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Application>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationItem>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Passport>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Visa>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Registration>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Invitation>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<InvitationItem>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<EducationInstitution>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Specialty>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);

        // =====================================================================
        // READ ONLY — Lookup objects (can be referenced but not modified)
        // =====================================================================
        userRole.AddTypePermissionsRecursively<ApplicationTypeFilter>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationState>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<CheckPoint>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Country>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Department>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<EducationLevel>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Gender>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MaritalStatus>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MigrationService>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<OrganizationType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<PassportType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Position>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<PurposeOfTravel>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Region>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Relationship>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Subcontractor>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Urgency>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ValidityDuration>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaCategory>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaIssuedPlace>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaPeriod>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<WorkPermitLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MovementPermitLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BorderZoneLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Company>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Ministry>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ProjectContract>(SecurityOperations.Read, SecurityPermissionState.Allow);

        // =====================================================================
        // NAVIGATION — Only explicitly allowed items are visible
        // Everything not listed here is denied by default.
        // =====================================================================

        // Application group — only list views, no Progress or BusinessTrip
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/Application", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/ApplicationItem", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/Rejection", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/RejectionItem", SecurityPermissionState.Allow);

        // Explicitly DENY Application Progress and Business Trip (visible in screenshot 2)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/ApplicationProgress", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/BusinessTrip", SecurityPermissionState.Deny);

        // Invitation group
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Invitation", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Invitation/Items/Invitation", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Invitation/Items/InvitationItem", SecurityPermissionState.Allow);

        // MyDetails only from Default group
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);

        // Explicitly DENY everything else in Default group (screenshot 3)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/AddressOfResidenceDocument", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/BorderZoneItem", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/BusinessTripAddress", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/BusinessTripPlan", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/AuthorizedSignatory", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/ContractTemplate", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/EmployeeContractDocument", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/SchedulerEvent", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/Role", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/AuthorizedRepresentative", SecurityPermissionState.Deny);

        // Explicitly DENY entire Documents group (screenshot 1)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Documents", SecurityPermissionState.Deny);

        // Explicitly DENY entire Employee group (screenshot 1)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Employee", SecurityPermissionState.Deny);

        // Explicitly DENY entire Images group (screenshot 1)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Images", SecurityPermissionState.Deny);

        // Explicitly DENY all Lookup navigation groups
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/Application/Config", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/Education/Config", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/General/Geography", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/Organization/Config", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/Passport/Config", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/Person/Config", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/Visa/Config", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/WorkPermit/Config", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup/Invitation", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Auth", SecurityPermissionState.Deny);
    }

    // Keep report read permission available even for existing "Users" roles created before this rule.
    EnsureTypePermission<ReportDataV2>(userRole, SecurityOperations.Read, SecurityPermissionState.Allow);
    EnsureTypePermission<ReportVisibility>(userRole, SecurityOperations.Read, SecurityPermissionState.Allow);

    // Keep People navigation available even for existing "Users" roles created before this rule.
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/People", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/People/Items/Employees", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/People/Items/FamilyMembers", SecurityPermissionState.Allow);

    // Upgrade existing "Users" roles that had read-only EducationInstitution or weaker Specialty permissions.
    PermissionSettingHelper.SetTypePermission<EducationInstitution>(userRole, SecurityOperations.FullAccess, SecurityPermissionState.Allow);
    PermissionSettingHelper.SetTypePermission<Specialty>(userRole, SecurityOperations.FullAccess, SecurityPermissionState.Allow);

    return userRole;
}

        private static void EnsureNavigationPermission(PermissionPolicyRole role, string itemPath, SecurityPermissionState state)
        {
            var existingPermission = role.NavigationPermissions.FirstOrDefault(p => p.ItemPath == itemPath);
            if (existingPermission == null)
            {
                role.AddNavigationPermission(itemPath, state);
            }
            else
            {
                existingPermission.NavigateState = state;
            }
        }

        private static void EnsureTypePermission<T>(PermissionPolicyRole role, string operation, SecurityPermissionState state) where T : class
        {
            var targetType = typeof(T);
            var existingPermission = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
            if (existingPermission == null)
            {
                role.AddTypePermission<T>(operation, state);
            }
        }

        PermissionPolicyRole CreateDefaultRole()
        {
            PermissionPolicyRole defaultRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(role => role.Name == "Default");
            if (defaultRole == null)
            {
                defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                defaultRole.Name = "Default";

                defaultRole.AddObjectPermissionFromLambda<ApplicationUser>(SecurityOperations.Read, cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "ChangePasswordOnFirstLogon", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "StoredPassword", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddObjectPermission<ModelDifference>(SecurityOperations.ReadWriteAccess, "UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddObjectPermission<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, "Owner.UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddTypePermission<AuditDataItemPersistent>(SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddObjectPermissionFromLambda<AuditDataItemPersistent>(SecurityOperations.Read, a => a.UserObject.Key == CurrentUserIdOperator.CurrentUserId().ToString(), SecurityPermissionState.Allow);
                defaultRole.AddTypePermission<AuditEFCoreWeakReference>(SecurityOperations.Read, SecurityPermissionState.Allow);
            }
            return defaultRole;
        }
    }
}
