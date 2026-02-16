using System.IO;
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
            CreateVisaTypes();
            CreateEducationLevels();
            CreatePurposeOfTravels();
            CreateCheckPoints();
            CreateVisaIssuedPlaces();
            CreateBorderZones();
            CreateMinistries();
            CreateWorkPermitLocations();
            CreateRegions();
            CreateDepartments();
            CreatePositions();
            CreateSpecialties();
            CreatePassportTypes();
            CreateEducationInstitutions();
            CreateProjectContracts();
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

#if DEBUG
            CreateEmployees();
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

        private void CreateVisaTypes()
        {
            SeedData<VisaType, VisaTypeData>("visatypes.json",
                (data) => ObjectSpace.FirstOrDefault<VisaType>(t => t.Code == data.Code),
                (type, data) =>
                {
                    type.Name = data.Name;
                    type.Code = data.Code;
                    // Description can be added here if it's in the JSON
                });
        }

        private void CreateEducationLevels()
        {
            SeedData<EducationLevel, NameData>("educationlevels.json",
                (data) => ObjectSpace.FirstOrDefault<EducationLevel>(l => l.Name == data.Name),
                (level, data) => level.Name = data.Name);
        }

        private void CreatePurposeOfTravels()
        {
            SeedData<PurposeOfTravel, NameData>("purposeoftravels.json",
                (data) => ObjectSpace.FirstOrDefault<PurposeOfTravel>(p => p.Name == data.Name),
                (purpose, data) => purpose.Name = data.Name);
        }

        private void CreateCheckPoints()
        {
            SeedData<CheckPoint, NameData>("checkpoints.json",
                (data) => ObjectSpace.FirstOrDefault<CheckPoint>(c => c.Name == data.Name),
                (checkpoint, data) => checkpoint.Name = data.Name);
        }

        private void CreateVisaIssuedPlaces()
        {
            SeedData<VisaIssuedPlace, NameData>("visaissuedplaces.json",
                (data) => ObjectSpace.FirstOrDefault<VisaIssuedPlace>(v => v.Name == data.Name),
                (place, data) => place.Name = data.Name);
        }

        private void CreateBorderZones()
        {
            SeedData<BorderZone, NameData>("borderzones.json",
                (data) => ObjectSpace.FirstOrDefault<BorderZone>(b => b.Name == data.Name),
                (zone, data) => zone.Name = data.Name);
        }

        private void CreateMinistries()
        {
            SeedData<Ministry, NameData>("ministries.json",
                (data) => ObjectSpace.FirstOrDefault<Ministry>(m => m.Name == data.Name),
                (ministry, data) => ministry.Name = data.Name);
        }

        private void CreateWorkPermitLocations()
        {
            SeedData<WorkPermitLocation, NameData>("workpermitlocations.json",
                (data) => ObjectSpace.FirstOrDefault<WorkPermitLocation>(w => w.Name == data.Name),
                (location, data) => location.Name = data.Name);
        }

        private void CreateRegions()
        {
            SeedData<Region, NameData>("regions.json",
                (data) => ObjectSpace.FirstOrDefault<Region>(r => r.Name == data.Name),
                (region, data) => region.Name = data.Name);
        }

        private void CreateDepartments()
        {
            SeedData<Department, NameData>("departments.json",
                (data) => ObjectSpace.FirstOrDefault<Department>(d => d.Name == data.Name),
                (department, data) => department.Name = data.Name);
        }

        private void CreatePositions()
        {
            SeedData<Position, NameData>("positions.json",
                (data) => ObjectSpace.FirstOrDefault<Position>(p => p.Name == data.Name),
                (position, data) => position.Name = data.Name);
        }

        private void CreateSpecialties()
        {
            SeedData<Specialty, NameData>("specialties.json",
                (data) => ObjectSpace.FirstOrDefault<Specialty>(s => s.Name == data.Name),
                (specialty, data) => specialty.Name = data.Name);
        }

        private void CreatePassportTypes()
        {
            SeedData<PassportType, NameData>("passporttypes.json",
                (data) => ObjectSpace.FirstOrDefault<PassportType>(p => p.Name == data.Name),
                (passportType, data) => passportType.Name = data.Name);
        }

        private void CreateEducationInstitutions()
        {
            SeedData<EducationInstitution, NameData>("educationinstitutions.json",
                (data) => ObjectSpace.FirstOrDefault<EducationInstitution>(e => e.Name == data.Name),
                (institution, data) => institution.Name = data.Name);
        }

        private void CreateProjectContracts()
        {
            SeedData<ProjectContract, NameData>("projectcontracts.json",
                (data) => ObjectSpace.FirstOrDefault<ProjectContract>(p => p.Name == data.Name),
                (contract, data) => contract.Name = data.Name);
        }

        private void CreateEmployees()
        {
            SeedData<Employee, EmployeeData>("employees.json",
                (data) => ObjectSpace.FirstOrDefault<Employee>(e => e.Email == data.Email),
                (employee, data) =>
                {
                    employee.FirstName = data.FirstName;
                    employee.LastName = data.LastName;
                    employee.Email = data.Email;
                    employee.HireDate = data.HireDate;
                    employee.ProjectContract = ObjectSpace.FirstOrDefault<ProjectContract>(p => p.Name == data.ProjectContract);

                    // Mapping simple properties
                    employee.Gender = ObjectSpace.FirstOrDefault<Gender>(g => g.Name == data.Gender);
                    employee.Position = ObjectSpace.FirstOrDefault<Position>(p => p.Name == data.Position);
                    employee.Department = ObjectSpace.FirstOrDefault<Department>(d => d.Name == data.Department);

                    if (!string.IsNullOrEmpty(data.Nationality))
                    {
                        employee.Nationality = ObjectSpace.FirstOrDefault<Country>(c => c.Code == data.Nationality);
                    }

                    // Commit the employee first to avoid circular dependency with children (CurrentEducation <-> Employee)
                    ObjectSpace.CommitChanges();

                    // Handle Position Histories
                    if (data.PositionHistories != null)
                    {
                        foreach (var phData in data.PositionHistories)
                        {
                            var position = ObjectSpace.FirstOrDefault<Position>(p => p.Name == phData.Position);
                            var department = ObjectSpace.FirstOrDefault<Department>(d => d.Name == phData.Department);

                            var existingHistory = employee.PositionHistory.FirstOrDefault(h => h.StartDate == phData.StartDate && h.Position == position && h.Department == department);
                            if (existingHistory == null)
                            {
                                var history = ObjectSpace.CreateObject<EmployeePositionHistory>();
                                history.StartDate = phData.StartDate;
                                history.EndDate = phData.EndDate;
                                history.Position = position;
                                history.Department = department;
                                history.Employee = employee;
                                employee.PositionHistory.Add(history);
                            }
                        }
                    }

                    // Handle Passports
                    if (data.Passports != null)
                    {
                        foreach (var pData in data.Passports)
                        {
                            var passport = ObjectSpace.FirstOrDefault<Passport>(p => p.PassportNumber == pData.PassportNumber);
                            if (passport == null)
                            {
                                passport = ObjectSpace.CreateObject<Passport>();
                                passport.PassportNumber = pData.PassportNumber;
                                passport.Person = employee;
                                employee.Passports.Add(passport);
                            }

                            passport.PassportType = ObjectSpace.FirstOrDefault<PassportType>(pt => pt.Name == pData.PassportType);
                            passport.IssueDate = pData.IssueDate;
                            passport.ExpirationDate = pData.ExpirationDate;
                            passport.Authority = pData.Authority;

                            // Handle Visas within Passport
                            if (pData.Visas != null)
                            {
                                foreach (var vData in pData.Visas)
                                {
                                    var visa = ObjectSpace.FirstOrDefault<Visa>(v => v.VisaNumber == vData.VisaNumber);
                                    if (visa == null)
                                    {
                                        visa = ObjectSpace.CreateObject<Visa>();
                                        visa.VisaNumber = vData.VisaNumber;
                                        visa.Person = employee;
                                        employee.Visas.Add(visa);
                                    }
                                    visa.StartDate = vData.StartDate;
                                    visa.ExpirationDate = vData.ExpirationDate;
                                }
                            }
                        }
                    }

                    // Handle Educations
                    if (data.Educations != null)
                    {
                        foreach (var eData in data.Educations)
                        {
                            var level = ObjectSpace.FirstOrDefault<EducationLevel>(l => l.Name == eData.EducationLevel);
                            var institution = ObjectSpace.FirstOrDefault<EducationInstitution>(i => i.Name == eData.EducationInstitution);

                            var existingEducation = employee.Educations.FirstOrDefault(e => e.EducationLevel == level && e.EducationInstitution == institution);

                            if (existingEducation == null)
                            {
                                var education = ObjectSpace.CreateObject<Education>();
                                education.Person = employee;
                                employee.Educations.Add(education);
                                existingEducation = education;
                            }

                            existingEducation.EducationLevel = level;
                            existingEducation.EducationInstitution = institution;
                            existingEducation.EducationCountry = ObjectSpace.FirstOrDefault<Country>(c => c.Code == eData.EducationCountry || c.Name == eData.EducationCountry);
                            existingEducation.Specialty = ObjectSpace.FirstOrDefault<Specialty>(s => s.Name == eData.Specialty);
                            existingEducation.HasEducationPeriod = eData.HasEducationPeriod;
                            existingEducation.EducationStartDate = eData.EducationStartDate;
                            existingEducation.EducationEndDate = eData.EducationEndDate;
                        }
                    }
                });
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
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return;
                    }
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

        private class VisaTypeData
        {
            public string Name { get; set; }
            public string Code { get; set; }
        }

        private class EmployeeData
        {
            public string EmployeeId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Gender { get; set; }
            public DateTime HireDate { get; set; }
            public string Position { get; set; }
            public string Department { get; set; }
            public string Email { get; set; }
            public string ProjectContract { get; set; }
            public string Nationality { get; set; }
            public List<PositionHistoryData> PositionHistories { get; set; }
            public List<PassportData> Passports { get; set; }
            public List<EducationData> Educations { get; set; }
        }

        private class PositionHistoryData
        {
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string Position { get; set; }
            public string Department { get; set; }
        }

        private class PassportData
        {
            public string PassportNumber { get; set; }
            public string PassportType { get; set; }
            public DateTime IssueDate { get; set; }
            public DateTime ExpirationDate { get; set; }
            public string Authority { get; set; }
            public List<VisaData> Visas { get; set; }
        }

        private class VisaData
        {
            public string VisaNumber { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime ExpirationDate { get; set; }
        }

        private class EducationData
        {
            public string EducationLevel { get; set; }
            public string EducationInstitution { get; set; }
            public string EducationCountry { get; set; }
            public string Specialty { get; set; }
            public bool HasEducationPeriod { get; set; }
            public DateTime? EducationStartDate { get; set; }
            public DateTime? EducationEndDate { get; set; }
        }
    }
}
