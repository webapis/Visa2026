﻿using System.IO;
using System.Reflection;
using System.Text.Json;
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
            CreateCountries();
            CreateGenders();
            CreateMaritalStatuses();
            CreateUrgencies();
            CreateVisaCategories();
            CreateVisaPeriods();
            //string name = "MyName";
            //EntityObject1 theObject = ObjectSpace.FirstOrDefault<EntityObject1>(u => u.Name == name);
            //if(theObject == null) {
            //    theObject = ObjectSpace.CreateObject<EntityObject1>();
            //    theObject.Name = name;
            //}

            // The code below creates users and roles for testing purposes only.
            // In production code, you can create users and assign roles to them automatically, as described in the following help topic:
            // https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication
#if !RELEASE
            // If a role doesn't exist in the database, create this role
            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();

            ObjectSpace.CommitChanges(); //This line persists created object(s).

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

            ObjectSpace.CommitChanges(); //This line persists created object(s).
#endif
        }
        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();
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

        private void CreateCountries()
        {
            SeedData<Country, CountryData>("countries.json",
                (data) => ObjectSpace.FirstOrDefault<Country>(c => c.Code == data.Code),
                (country, data) =>
                {
                    country.Code = data.Code;
                    country.Name = data.Name;
                    country.DialingCode = data.DialingCode;
                });
        }

        private void CreateGenders()
        {
            SeedData<Gender, NameData>("genders.json",
                (data) => ObjectSpace.FirstOrDefault<Gender>(g => g.Name == data.Name),
                (gender, data) => gender.Name = data.Name);
        }

        private void CreateMaritalStatuses()
        {
            SeedData<MaritalStatus, NameData>("maritalstatuses.json",
                (data) => ObjectSpace.FirstOrDefault<MaritalStatus>(m => m.Name == data.Name),
                (status, data) => status.Name = data.Name);
        }

        private void CreateUrgencies()
        {
            SeedData<Urgency, UrgencyData>("urgencies.json",
                (data) => ObjectSpace.FirstOrDefault<Urgency>(u => u.Name == data.Name),
                (urgency, data) =>
                {
                    urgency.Name = data.Name;
                    urgency.Code = data.Code;
                    urgency.Priority = data.Priority;
                });
        }

        private void CreateVisaCategories()
        {
            SeedData<VisaCategory, NameData>("visacategories.json",
                (data) => ObjectSpace.FirstOrDefault<VisaCategory>(c => c.Name == data.Name),
                (category, data) => category.Name = data.Name);
        }

        private void CreateVisaPeriods()
        {
            SeedData<VisaPeriod, NameData>("visaperiods.json",
                (data) => ObjectSpace.FirstOrDefault<VisaPeriod>(p => p.Name == data.Name),
                (period, data) => period.Name = data.Name);
        }

        private void SeedData<TEntity, TData>(string jsonFileName, Func<TData, TEntity> findExisting, Action<TEntity, TData> mapData) where TEntity : class
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Visa2026.Module.DatabaseUpdate.{jsonFileName}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    // Log or handle missing resource if necessary, or just return.
                    // For now, we can return to avoid crashing if a file is missing during development.
                    return; 
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var items = JsonSerializer.Deserialize<List<TData>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            var entity = findExisting(item);
                            if (entity == null)
                            {
                                entity = ObjectSpace.CreateObject<TEntity>();
                                mapData(entity, item);
                            }
                        }
                        ObjectSpace.CommitChanges();
                    }
                }
            }
        }

        private class CountryData
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string DialingCode { get; set; }
        }

        private class NameData
        {
            public string Name { get; set; }
        }

        private class UrgencyData
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public int? Priority { get; set; }
        }
    }
}
