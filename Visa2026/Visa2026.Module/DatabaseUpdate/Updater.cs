﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
#if !RELEASE
            // If a role doesn't exist in the database, create this role
            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();

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

            ObjectSpace.CommitChanges(); //This line persists created object(s).
#endif

#if DEBUG
#if !EASYTEST
           
#endif
#endif

            SeedPdfFormMappings();
            ObjectSpace.CommitChanges();
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

        private void SeedPdfFormMappings()
        {
            // Application Level
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L01[0]", "Application.ApplicationType.PdfForm_Code", "Visa operation type", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L02[0]", "Application.Urgency.PdfForm_Code", "Urgency (Dropdown)", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._25[0]", "Application.VisaType.PdfForm_Code", "Visa Type (Application Level)", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._27[0]", "Application.VisaPeriod.PdfForm_Count", "Duration of stay (count)", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._271[0]", "Application.VisaPeriod.PdfForm__Code", "Duration of stay (unit)", PdfMappingMode.Property);

            // Company Info
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L10[0]", "Application.Company.Name", "Company Name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L11[0]", "Application.Company.Address", "Company Address", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L13[0]", "Application.Company.PhoneNumber", "Company Phone", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].L12[0]", "Application.Company.Email", "Company Email", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].IP[1].#field[0]", null, "Legal Entity Checkbox", PdfMappingMode.Constant, "true");

            // Person Level
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._01[0]", "Person.LastName", "Last Name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._03[0]", "Person.FirstName", "First Name", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._04[0]", "Person.DateOfBirth", "Date of Birth", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._05[0]", "Person.Gender.Name", "Gender", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._18[0]", "Person.MaritalStatus.PdfForm_Code", "Marital Status", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._08[0]", "Person.BirthPlace", "Birth Place", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._06[0]", "Person.CountryOfBirth.Code", "Country of Birth", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._07[0]", "Person.Nationality.Code", "Citizenship", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._15[0]", null, "Foreign Address (Country + Address)", PdfMappingMode.Expression, "Concat(Person.ForeignAddressCountry.Code, ', ', Person.ForeignAddress)");

            // Education
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._19[0]", "CurrentEducation.EducationLevel.PdfForm_Code", "Education Level", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._20[0]", "CurrentEducation.Specialty.Name", "Specialty", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._21[0]", null, "Education Place (Country + Institution)", PdfMappingMode.Expression, "Concat(CurrentEducation.EducationCountry.Name, ', ', CurrentEducation.EducationInstitution.Name)");

            // Work
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._23[0]", "CurrentPositionHistory.Position.Name", "Work Position", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._22[0]", null, "Work Place and Phone", PdfMappingMode.Expression, "Concat(Person.Company.Name, ', ', Person.Company.PhoneNumber)");

            // Photo
            CreateMappingIfNotExists("topmostSubform[0].Page1[0].ImageField1[0]", "Person.Photo", "Photo", PdfMappingMode.Property);

            // Passport
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._10[0]", "CurrentPassport.PassportType.PdfForm_Code", "Passport Type", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._09[0]", "CurrentPassport.PersonalNumber", "Personal Number", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._11[0]", "CurrentPassport.PassportNumber", "Passport Number", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._12[0]", "CurrentPassport.IssueDate", "Passport Issue Date", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._13[0]", "CurrentPassport.ExpirationDate", "Passport Expiration Date", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page1[0]._14[0]", "CurrentPassport.IssuedCountry.Code", "Passport Issued Country", PdfMappingMode.Property);

            // Visa
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._26[0]", "Application.VisaCategory.PdfForm_Code", "Visa Category", PdfMappingMode.Property);

            // Address of Residence
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._33[0]", "CurrentAddressOfResidence.Region.PdfForm_Code", "Region of stay", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._34[0]", "CurrentAddressOfResidence.City.PdfForm_Code", "District of stay", PdfMappingMode.Property);
            CreateMappingIfNotExists("topmostSubform[0].Page2[0]._35[0]", "CurrentAddressOfResidence.FullAddress", "Stay address", PdfMappingMode.Property);
        }

        private void CreateMappingIfNotExists(string pdfKey, string propertyPath, string description, PdfMappingMode mode, string expressionOrConstant = null)
        {
            var existingMapping = ObjectSpace.FirstOrDefault<PdfFormMapping>(m => m.PdfFieldKey == pdfKey);
            if (existingMapping == null)
            {
                var newMapping = ObjectSpace.CreateObject<PdfFormMapping>();
                newMapping.PdfFieldKey = pdfKey;
                newMapping.Description = description;
                newMapping.MappingMode = mode;

                if (mode == PdfMappingMode.Property)
                {
                    newMapping.PropertyPath = propertyPath;
                }
                else if (mode == PdfMappingMode.Expression)
                {
                    newMapping.Expression = expressionOrConstant;
                }
                else if (mode == PdfMappingMode.Constant)
                {
                    newMapping.ConstantValue = expressionOrConstant;
                }
            }
        }

    }
}
