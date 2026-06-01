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
using Visa2026.Module.BusinessObjects.Feedback;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate
{
    public class Updater : ModuleUpdater
    {
        /// <summary>Read, Write, and Create without Delete (Users role education lookups).</summary>
        private static readonly string ReadWriteCreateWithoutDelete =
            $"{SecurityOperations.Read};{SecurityOperations.Write};{SecurityOperations.Create}";

        /// <summary>Read, Write, Create, and Delete (user-managed catalogs in multi-select popup).</summary>
        private static readonly string ReadWriteCreateDelete =
            $"{SecurityOperations.Read};{SecurityOperations.Write};{SecurityOperations.Create};{SecurityOperations.Delete}";

        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            // Orphan cleanup again if EF added columns; FK trust is handled by EF after it recreates constraints.
            RunVisaApplicationItemOrphanCleanupSql();

            base.UpdateDatabaseAfterUpdateSchema();

            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();
            var userRole = CreateUserRole();
            EnsurePreferredCultureSelfWritePermission(defaultRole);

            ObjectSpace.CommitChanges();

            UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null)
            {
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) =>
                {
                    user.Roles.Add(defaultRole);
                    user.Roles.Add(userRole);
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

            var existingUser = userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User");
            if (existingUser != null && existingUser.Roles.All(r => r.Name != "Users"))
            {
                existingUser.Roles.Add(userRole);
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

            RunVisaApplicationItemOrphanCleanupSql();
            // Drop all ApplicationItems → Visas FKs so EF can recreate with stable HasConstraintName values (avoids legacy/ambiguous names like FK_ApplicationItems_Visas_ID).
            DropAllApplicationItemsForeignKeysToVisas();
        }

        /// <summary>
        /// Clears FK columns that point at missing rows (dynamic SQL from metadata — see fix for Invalid column name batch compile).
        /// Includes disabled FK definitions so we still clear known orphan columns.
        /// </summary>
        private void RunVisaApplicationItemOrphanCleanupSql()
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL OR OBJECT_ID(N'dbo.Visas', N'U') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);

SELECT @sql = STRING_AGG(
    CAST(
        N'UPDATE ai SET ' + QUOTENAME(c.name) + N' = NULL FROM dbo.ApplicationItems ai WHERE ai.' + QUOTENAME(c.name)
        + N' IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Visas v WHERE v.ID = ai.' + QUOTENAME(c.name) + N')'
        AS nvarchar(max)),
    N'; ')
WITHIN GROUP (ORDER BY c.name)
FROM sys.columns c
INNER JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
INNER JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.ApplicationItems')
  AND fk.referenced_object_id = OBJECT_ID(N'dbo.Visas');

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

IF OBJECT_ID(N'dbo.Visas', N'U') IS NULL OR OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL
    RETURN;

SELECT @sql = STRING_AGG(
    CAST(
        N'UPDATE v SET ' + QUOTENAME(c.name) + N' = NULL FROM dbo.Visas v WHERE v.' + QUOTENAME(c.name)
        + N' IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ApplicationItems ai WHERE ai.ID = v.' + QUOTENAME(c.name) + N')'
        AS nvarchar(max)),
    N'; ')
WITHIN GROUP (ORDER BY c.name)
FROM sys.columns c
INNER JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
INNER JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Visas')
  AND fk.referenced_object_id = OBJECT_ID(N'dbo.ApplicationItems');

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;
";
            ExecuteNonQueryCommand(sql, false);
        }

        /// <summary>
        /// Drops every ApplicationItems → Visas FK before schema sync so migrations are not blocked by disabled,
        /// untrusted, or ambiguously named constraints (e.g. FK_ApplicationItems_Visas_ID from older EF snapshots).
        /// </summary>
        private void DropAllApplicationItemsForeignKeysToVisas()
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.ApplicationItems', N'U') IS NULL OR OBJECT_ID(N'dbo.Visas', N'U') IS NULL
    RETURN;

DECLARE @sql nvarchar(max);

SELECT @sql = STRING_AGG(
    CAST(N'ALTER TABLE dbo.ApplicationItems DROP CONSTRAINT ' + QUOTENAME(fk.name) AS nvarchar(max)),
    N'; ')
WITHIN GROUP (ORDER BY fk.name)
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.ApplicationItems')
  AND fk.referenced_object_id = OBJECT_ID(N'dbo.Visas');

IF @sql IS NOT NULL AND LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;
";
            ExecuteNonQueryCommand(sql, false);
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
        // Diplomas / file copies live on Education (+ aggregated EducationDocument); not always covered by Person recursive grants alone (same pattern as Passport).
        userRole.AddTypePermissionsRecursively<Education>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        // Medical records on Person (+ aggregated document/image rows + FileData); same gap as EducationDocument.
        userRole.AddTypePermissionsRecursively<MedicalRecord>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MedicalRecordDocument>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MedicalRecordImage>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Invitation>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<InvitationItem>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<EducationInstitution>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Specialty>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Lodging>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Rejection>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<RejectionItem>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<WorkPermit>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<WorkPermitItem>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        // File uploads persist FileData as its own row; EF Core Security does not treat it as covered by Person/Passport recursive grants alone.
        userRole.AddTypePermissionsRecursively<FileData>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);

        // =====================================================================
        // USER-DEFINED REPORT TEMPLATES — Users with Report role can create templates
        // =====================================================================
        userRole.AddTypePermissionsRecursively<UserReportTemplate>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<UserReportPlaceholder>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<UserReportTemplateApplicationType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<UserReportTemplateProjectContract>(SecurityOperations.Read, SecurityPermissionState.Allow);

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
        userRole.AddTypePermissionsRecursively<Position>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ActualPosition>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<PurposeOfTravel>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Region>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Relationship>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Subcontractor>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Urgency>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ValidityDuration>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaCategory>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaIssuedPlace>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaPeriod>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<WorkPermitLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MovementPermitLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BorderZoneLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BorderZoneName>(ReadWriteCreateDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<WorkPermittedLocationName>(ReadWriteCreateDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Ministry>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ProjectContract>(SecurityOperations.Read, SecurityPermissionState.Allow);
        // Application number generation reads prefix/format/seed on save; officers must not create or edit org settings.
        userRole.AddTypePermissionsRecursively<ApplicationNumberingProfile>(SecurityOperations.Read, SecurityPermissionState.Allow);
        // Per-BO expiration alert thresholds — officers edit day counts; rows are seeded (no create/delete).
        userRole.AddTypePermissionsRecursively<ExpirationAlertRule>(SecurityOperations.Read, SecurityPermissionState.Allow);
        {
            var expirationAlertPerm = userRole.TypePermissions.First(p => p.TargetType == typeof(ExpirationAlertRule));
            expirationAlertPerm.WriteState = SecurityPermissionState.Allow;
            expirationAlertPerm.CreateState = SecurityPermissionState.Deny;
            expirationAlertPerm.DeleteState = SecurityPermissionState.Deny;
        }

        // =====================================================================
        // NAVIGATION — Only explicitly allowed items are visible
        // Everything not listed here is denied by default.
        // =====================================================================

        // Application group — only list views, no Progress or BusinessTrip
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/Application_ViaMinistries", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/Application_DirectMigration", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/Application", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/ApplicationItem", SecurityPermissionState.Allow);

        // Explicitly DENY Application Progress, Business Trip and Pdf Generation Batch
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/ApplicationProgress", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/BusinessTrip", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/PdfGenerationBatch", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/WordReportGenerationBatch", SecurityPermissionState.Deny);

        // Rejection group (separate from Application)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Rejection", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Rejection/Items/Rejection", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Rejection/Items/RejectionItem", SecurityPermissionState.Allow);

        // Invitation group
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Invitation", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Invitation/Items/Invitation", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Invitation/Items/InvitationItem", SecurityPermissionState.Allow);

        // Operations — state notification inbox (UI prototype)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Operations", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Operations/Items/StateNotifications", SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BusinessObjects.StateNotifications.BoStateNotificationInboxHost>(
            SecurityOperations.Read, SecurityPermissionState.Allow);

        // User feedback — officers: create via header; read own rows under Operations (see EnsureUserFeedbackOfficerPermissions).

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
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/Role", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/AuthorizedRepresentative", SecurityPermissionState.Deny);

        // Explicitly DENY entire Documents group (screenshot 1)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Documents", SecurityPermissionState.Deny);

        // Explicitly DENY entire Employee group (screenshot 1)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Employee", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Employee/Items/EmployeePositionHistory", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Employee/Items/EmployeeContract", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Employee/Items/EmployeeSalary", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Employee/Items/LocalEmployee", SecurityPermissionState.Deny);

        // Explicitly DENY entire Images group (screenshot 1)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Images", SecurityPermissionState.Deny);

        // Explicitly DENY entire Lookup group — admin-only navigation (including all its sub-groups)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Lookup", SecurityPermissionState.Deny);

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

    // Resminamalar user templates (seeded from Resources/Templates) — existing "Users" roles need read access too.
    EnsureReadWriteCreatePermission<UserReportTemplate>(userRole);
    EnsureReadOnlyPermission<UserReportPlaceholder>(userRole);
    EnsureReadWriteCreatePermission<UserReportTemplateApplicationType>(userRole);
    EnsureReadWriteCreatePermission<UserReportTemplateProjectContract>(userRole);

    // PDF filling relies on database-driven mappings (PdfFormMapping). Users must be able to read them.
    EnsureReadOnlyPermission<PdfFormMapping>(userRole);

    // Keep People navigation available even for existing "Users" roles created before this rule.
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/People", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/People/Items/Employees", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/People/Items/FamilyMembers", SecurityPermissionState.Allow);

    // Users: EducationInstitution, Specialty, Position & Lodging — read/write/create only (no delete), including existing roles.
    EnsureReadWriteCreatePermission<EducationInstitution>(userRole);
    EnsureReadWriteCreatePermission<Specialty>(userRole);
    EnsureReadWriteCreatePermission<Position>(userRole);
    EnsureReadWriteCreatePermission<ActualPosition>(userRole);
    // Person.Subcontractor lookup: officers create/select subcontractors without Lookup navigation.
    EnsureReadWriteCreatePermission<Subcontractor>(userRole);
    EnsureReadWriteCreatePermission<Lodging>(userRole);
    EnsureReadWriteCreatePermission<Rejection>(userRole);
    EnsureReadWriteCreatePermission<RejectionItem>(userRole);
    EnsureReadWriteCreatePermission<Invitation>(userRole);
    EnsureReadWriteCreatePermission<InvitationItem>(userRole);
    EnsureReadWriteCreatePermission<WorkPermit>(userRole);
    EnsureReadWriteCreatePermission<WorkPermitItem>(userRole);
    EnsureReadWriteCreatePermission<FileData>(userRole);
    // Comma-separated multi-select popup catalogs — existing "Users" roles need CRUD (not only on first role create).
    EnsureCatalogManagePermission<BorderZoneName>(userRole);
    EnsureCatalogManagePermission<WorkPermittedLocationName>(userRole);
    // Visa family manual editor (Person.VisaApplicationFamilyMembersText): combo sources + employee save.
    EnsureFullAccessRecursivePermission<Person>(userRole);
    // Existing "Users" roles: allow diploma rows and aggregated documents (EducationDocument + File) like Passport.
    EnsureFullAccessRecursivePermission<Education>(userRole);
    // Same for medical records under Person (MedicalRecordDocument / FileData not always covered by Person recursive grants in EF security).
    EnsureFullAccessRecursivePermission<MedicalRecord>(userRole);
    EnsureFullAccessRecursivePermission<MedicalRecordDocument>(userRole);
    EnsureFullAccessRecursivePermission<MedicalRecordImage>(userRole);

    // Users: explicitly deny Lookup group (admin-only), including existing roles.
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Lookup", SecurityPermissionState.Deny);

    // Users: explicitly deny Employee group navigation items, including existing roles.
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Employee", SecurityPermissionState.Deny);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Employee/Items/EmployeePositionHistory", SecurityPermissionState.Deny);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Employee/Items/EmployeeContract", SecurityPermissionState.Deny);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Employee/Items/EmployeeSalary", SecurityPermissionState.Deny);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Employee/Items/LocalEmployee", SecurityPermissionState.Deny);

    // Users: Application list split by progress route (ministry vs direct migration).
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Application/Items/Application_ViaMinistries", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Application/Items/Application_DirectMigration", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Application/Items/Application", SecurityPermissionState.Deny);

    // Users: explicitly deny Application sub-items that should not be visible.
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Application/Items/ApplicationProgress", SecurityPermissionState.Deny);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Application/Items/BusinessTrip", SecurityPermissionState.Deny);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Application/Items/PdfGenerationBatch", SecurityPermissionState.Deny);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Application/Items/WordReportGenerationBatch", SecurityPermissionState.Deny);

    // Users: WorkPermit group (separate from Lookup)
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/WorkPermit", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/WorkPermit/Items/WorkPermit", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/WorkPermit/Items/WorkPermitItem", SecurityPermissionState.Allow);

    // Users: Rejection group (separate from Application)
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Rejection", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Rejection/Items/Rejection", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Rejection/Items/RejectionItem", SecurityPermissionState.Allow);

    // Users: Invitation group (separate from Lookup)
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Invitation", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Invitation/Items/Invitation", SecurityPermissionState.Allow);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/Invitation/Items/InvitationItem", SecurityPermissionState.Allow);

    // Users: lookup types — read only (explicit deny on Write/Create/Delete), including existing roles.
    EnsureReadOnlyPermission<EducationLevel>(userRole);
    EnsureReadOnlyPermission<Country>(userRole);
    EnsureReadOnlyPermission<Relationship>(userRole);
    EnsureReadOnlyPermission<Gender>(userRole);
    EnsureReadOnlyPermission<MaritalStatus>(userRole);
    EnsureReadOnlyPermission<PassportType>(userRole);
    EnsureReadOnlyPermission<VisaType>(userRole);
    EnsureReadOnlyPermission<VisaCategory>(userRole);
    EnsureReadOnlyPermission<VisaIssuedPlace>(userRole);
    EnsureReadOnlyPermission<ApplicationTypeFilter>(userRole);
    EnsureReadOnlyPermission<ApplicationType>(userRole);
    EnsureReadOnlyPermission<Urgency>(userRole);
    EnsureReadOnlyPermission<ApplicationNumberingProfile>(userRole);
    EnsureReadWriteOnlyPermission<ExpirationAlertRule>(userRole);
    EnsureNavigationPermission(userRole, @"Application/NavigationItems/Items/System/Items/ExpirationAlertRule", SecurityPermissionState.Allow);

    EnsureUserFeedbackOfficerPermissions(userRole);

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

        private static void EnsureReadOnlyPermission<T>(PermissionPolicyRole role) where T : class
        {
            var targetType = typeof(T);
            var existingPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
            if (existingPerm != null)
            {
                existingPerm.ReadState = SecurityPermissionState.Allow;
                existingPerm.WriteState = SecurityPermissionState.Deny;
                existingPerm.CreateState = SecurityPermissionState.Deny;
                existingPerm.DeleteState = SecurityPermissionState.Deny;
            }
            else
            {
                role.AddTypePermissionsRecursively<T>(SecurityOperations.Read, SecurityPermissionState.Allow);
                var newPerm = role.TypePermissions.First(p => p.TargetType == typeof(T));
                newPerm.WriteState = SecurityPermissionState.Deny;
                newPerm.CreateState = SecurityPermissionState.Deny;
                newPerm.DeleteState = SecurityPermissionState.Deny;
            }
        }

        private static void EnsureReadWriteOnlyPermission<T>(PermissionPolicyRole role) where T : class
        {
            var targetType = typeof(T);
            var existingPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
            if (existingPerm != null)
            {
                existingPerm.ReadState = SecurityPermissionState.Allow;
                existingPerm.WriteState = SecurityPermissionState.Allow;
                existingPerm.CreateState = SecurityPermissionState.Deny;
                existingPerm.DeleteState = SecurityPermissionState.Deny;
            }
            else
            {
                role.AddTypePermissionsRecursively<T>(SecurityOperations.Read, SecurityPermissionState.Allow);
                var newPerm = role.TypePermissions.First(p => p.TargetType == targetType);
                newPerm.WriteState = SecurityPermissionState.Allow;
                newPerm.CreateState = SecurityPermissionState.Deny;
                newPerm.DeleteState = SecurityPermissionState.Deny;
            }
        }

        private static void EnsureReadWriteCreatePermission<T>(PermissionPolicyRole role) where T : class
        {
            var targetType = typeof(T);
            var existingPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
            if (existingPerm != null)
            {
                existingPerm.ReadState = SecurityPermissionState.Allow;
                existingPerm.WriteState = SecurityPermissionState.Allow;
                existingPerm.CreateState = SecurityPermissionState.Allow;
                existingPerm.DeleteState = null;
            }
            else
            {
                role.AddTypePermissionsRecursively<T>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
            }
        }

        /// <summary>Read, Write, Create, Delete for user-managed comma-separated catalog BOs (multi-select popup).</summary>
        private static void EnsureCatalogManagePermission<T>(PermissionPolicyRole role) where T : class
        {
            var targetType = typeof(T);
            var existingPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
            if (existingPerm != null)
            {
                existingPerm.ReadState = SecurityPermissionState.Allow;
                existingPerm.WriteState = SecurityPermissionState.Allow;
                existingPerm.CreateState = SecurityPermissionState.Allow;
                existingPerm.DeleteState = SecurityPermissionState.Allow;
            }
            else
            {
                role.AddTypePermissionsRecursively<T>(ReadWriteCreateDelete, SecurityPermissionState.Allow);
            }
        }

        /// <summary>Matches new-role grants for <see cref="Passport"/> / <see cref="Visa"/> — full recursive access for existing roles.</summary>
        private static void EnsureFullAccessRecursivePermission<T>(PermissionPolicyRole role) where T : class
        {
            var targetType = typeof(T);
            var existingPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
            if (existingPerm != null)
            {
                existingPerm.ReadState = SecurityPermissionState.Allow;
                existingPerm.WriteState = SecurityPermissionState.Allow;
                existingPerm.CreateState = SecurityPermissionState.Allow;
                existingPerm.DeleteState = SecurityPermissionState.Allow;
            }
            else
            {
                role.AddTypePermissionsRecursively<T>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
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
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "PreferredCulture", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
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

        static void EnsurePreferredCultureSelfWritePermission(PermissionPolicyRole defaultRole)
        {
            if (defaultRole == null)
            {
                return;
            }

            const string memberName = nameof(ApplicationUser.PreferredCulture);
            bool alreadyGranted = defaultRole.TypePermissions
                .SelectMany(tp => tp.MemberPermissions)
                .Any(mp => string.Equals(mp.Members, memberName, StringComparison.Ordinal));
            if (alreadyGranted)
            {
                return;
            }

            defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(
                SecurityOperations.Write,
                memberName,
                cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(),
                SecurityPermissionState.Allow);
        }

        /// <summary>
        /// Officers submit feedback from the header dialog; they may read only their own rows (read-only list/detail).
        /// Administrators retain full access via <see cref="PermissionPolicyRole.IsAdministrative"/>.
        /// </summary>
        static void EnsureUserFeedbackOfficerPermissions(PermissionPolicyRole role)
        {
            if (role == null)
                return;

            var targetType = typeof(UserFeedback);
            var typePerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
            if (typePerm == null)
            {
                role.AddTypePermissionsRecursively<UserFeedback>(SecurityOperations.Create, SecurityPermissionState.Allow);
                typePerm = role.TypePermissions.First(p => p.TargetType == targetType);
            }
            else
            {
                typePerm.CreateState = SecurityPermissionState.Allow;
            }

            typePerm.ReadState = SecurityPermissionState.Deny;
            typePerm.WriteState = SecurityPermissionState.Deny;
            typePerm.DeleteState = SecurityPermissionState.Deny;

            if (!typePerm.ObjectPermissions.Any(op => op.ReadState == SecurityPermissionState.Allow))
            {
                role.AddObjectPermissionFromLambda<UserFeedback>(
                    SecurityOperations.Read,
                    f => f.SubmittedBy.ID == (Guid)CurrentUserIdOperator.CurrentUserId(),
                    SecurityPermissionState.Allow);
            }

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Operations/Items/UserFeedback", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/UserFeedback", SecurityPermissionState.Deny);
        }
    }
}
