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

            // Commit changes after creating all employees to avoid circular dependency issues during intermediate saves if any
            // Actually, the SeedData method commits changes inside the loop if we are not careful, but here SeedData calls mapData then commits.
            // The issue is likely that when we add Education to Employee, and Employee has CurrentEducation, EF Core gets confused with the circular relationship if both are new.
            // However, in our seeding logic, we are adding Education to employee.Educations collection.
            // The SingleActiveBaseObject logic might be setting CurrentEducation on the Person (Employee).
            // If we commit the Employee first, then add Education, it might solve it.
            // But SeedData commits at the end of the loop over all items.
            // Let's try to commit the Employee first before adding children that might reference it back as "Current".
            // But we are inside mapData which is inside SeedData which commits after the loop.
            // Wait, SeedData implementation:
            // foreach (var item in items) { ... mapData(entity, item); } ObjectSpace.CommitChanges();
            // So it commits once at the end.
            // The error says: Education [Added] <- ForeignKeyConstraint { 'CurrentEducationID' } Employee [Added] <- ForeignKeyConstraint { 'PersonID' } Education [Added]
            // This is because Employee has a CurrentEducation FK to Education, and Education has a Person FK to Employee.
            // When both are added at the same time, EF Core can't resolve the dependency cycle.
            // We need to save the Employee first (without CurrentEducation), then save Education, then update Employee with CurrentEducation.
            // But SingleActiveBaseObject logic sets CurrentEducation immediately when we add the Education object and set its IsActive (which defaults to true).

            // To fix this in seeding:
            // We can modify SeedData to commit the Employee first before adding children.
            // But SeedData is generic.
            // We can modify the mapData action to commit changes if needed, but we need to be careful.
            // Or we can disable the automatic "Current" setting during seeding, but that's hard because it's in OnSaving/OnCreated of the business object.

            // Alternative: In CreateEmployees, we can commit the employee creation first, then add children.
            // But mapData is called when the object is created or found.
            // If it's created, it's new.
            // We can try to commit inside mapData after setting basic properties.
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
                                // We need to commit the entity here if it's an Employee to generate ID and avoid circular dependency with children
                                // But we can't easily know if it's Employee here or if it needs immediate commit.
                                // However, for Employee, we can do it inside the mapData action?
                                // No, mapData receives the entity.

                                // Let's try to commit inside the mapData for Employee specifically.
                                // But we need to handle the transaction.
                                // If we commit inside mapData, the outer CommitChanges will just commit nothing or remaining changes.

                                mapData(entity, item);
                            }
                        }
                        ObjectSpace.CommitChanges();
                    }
                }
            }
        }
