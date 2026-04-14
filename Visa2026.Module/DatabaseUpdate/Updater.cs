﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
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
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }
        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

#if !EASYTEST
           
            
#endif
            //string name = "MyName";
            //EntityObject1 theObject = ObjectSpace.FirstOrDefault<EntityObject1>(u => u.Name == name);
            //if(theObject == null) {
            //    theObject = ObjectSpace.CreateObject<EntityObject1>();
            //    theObject.Name = name;
            //}

            // The code below creates users and roles for testing purposes only.
            // In production code, you can create users and assign roles to them automatically, as described in the following help topic:
            // https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication

            // If a role doesn't exist in the database, create this role
            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();
            var userRole = CreateUserRole();

            ObjectSpace.CommitChanges(); //This line persists created object(s), including SystemSettings if new.

            UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

            // If a user named 'User' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) =>
                {
                    // Add the Users role to the user
                    user.Roles.Add(defaultRole);
                });
            }

            // If a user named 'Admin' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "Admin") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "Admin", EmptyPassword, (user) =>
                {
                    // Add the Administrators role to the user
                    user.Roles.Add(adminRole);
                });
            }

            // Create the 'StandardUser' account and assign the Standard User permissions
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "StandardUser") == null)
            {
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "StandardUser", EmptyPassword, (user) =>
                {
                    user.Roles.Add(defaultRole); // Grants access to Navigation and My Details
                    user.Roles.Add(userRole);    // Grants access to Visas, Persons, and Applications
                });
            }

            ObjectSpace.CommitChanges(); //This line persists created object(s).


#if DEBUG
#if !EASYTEST
           
#endif
#endif

        }
        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();
            
            // The 'CurrentDBVersion' property holds the version of this module as recorded in the database.
            // The application's assembly version is the new version. The updater runs when the new version is higher.
            // Use this property to run data migration scripts for specific version upgrades.
            
            // Example: This script will only run if you are upgrading from any version older than 1.1.0.5.
            // After this runs, the database version will be updated to the application's current version,
            // so this block will not execute again on subsequent startups.
            if (CurrentDBVersion < new Version("1.1.0.5"))
            {
                // Use ExecuteNonQueryCommand for schema changes like renaming columns or tables.
                // This is a safe way to preserve data when renaming a property in your C# code.
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

                // Full Access to core operational data
                userRole.AddTypePermissionsRecursively<Person>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Application>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<ApplicationItem>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Passport>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Visa>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Registration>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Invitation>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<InvitationItem>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);

                // Read-Only access to static/lookup data
                userRole.AddTypePermissionsRecursively<Company>(SecurityOperations.Read, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Ministry>(SecurityOperations.Read, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<ProjectContract>(SecurityOperations.Read, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Position>(SecurityOperations.Read, SecurityPermissionState.Allow);
                userRole.AddTypePermissionsRecursively<Department>(SecurityOperations.Read, SecurityPermissionState.Allow);
            }
            return userRole;
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