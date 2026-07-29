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
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.BusinessObjects.StateNotifications;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
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
            _ = CreateUsersReadOnlyRole();
            var visaOfficeRole = CreateVisaOfficeRole();
            EnsurePreferredCultureSelfWritePermission(defaultRole);
            ApplicationUserThemePreferencePermissions.EnsureSelfWrite(defaultRole);

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

            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "VisaOffice") == null)
            {
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "VisaOffice", EmptyPassword, (user) =>
                {
                    user.Roles.Add(defaultRole);
                    user.Roles.Add(visaOfficeRole);
                });
            }

            var existingUser = userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User");
            if (existingUser != null && existingUser.Roles.All(r => r.Name != "Users"))
            {
                existingUser.Roles.Add(userRole);
            }

            TenantUserCatalogSync.Sync(ObjectSpace, userManager);

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
            if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
                return;

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
            if (DatabaseProviderDetector.IsPostgreSql(ObjectSpace))
                return;

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

        /// <summary>Super administrator — full access bypass (<see cref="PermissionPolicyRole.IsAdministrative"/>).</summary>
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

        /// <summary>
        /// Visa office configuration: organization singletons, project contracts, ministries, and Resminamalar templates.
        /// Complements <see cref="CreateUserRole"/> (case officers). Assign together with <see cref="CreateDefaultRole"/>.
        /// </summary>
        PermissionPolicyRole CreateVisaOfficeRole()
        {
            PermissionPolicyRole visaOfficeRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "VisaOffice");
            if (visaOfficeRole == null)
            {
                visaOfficeRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                visaOfficeRole.Name = "VisaOffice";
            }

            EnsureVisaOfficeConfigurationPermissions(visaOfficeRole);
            EnsureUserReportTemplateOfficerPermissions(visaOfficeRole);
            EnsureVisaOfficeNavigationPermissions(visaOfficeRole);
            EnsureReportDashboardOfficerPermissions(visaOfficeRole);
            EnsureAdminOnlyOperationsDeny(visaOfficeRole);

            return visaOfficeRole;
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
        // Dashboard Open ListView for Visa Extension / Extension Result (vw_rd_visa_app_progress).
        userRole.AddTypePermissionsRecursively<VwRdVisaAppProgress>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaByPeriod>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaActiveByProject>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaActiveByPeriodCategoryType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaOnExtension>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaOnExtensionByPeriodCategoryType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaExtensionResult>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaExtensionResultByPeriodCategoryType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaExtensionRequired>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VwRdVisaByDaysRemaining>(SecurityOperations.Read, SecurityPermissionState.Allow);
        // Diplomas / file copies live on Education (+ aggregated EducationDocument); not always covered by Person recursive grants alone (same pattern as Passport).
        userRole.AddTypePermissionsRecursively<Education>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        // Medical records on Person (+ aggregated document/image rows + FileData); same gap as EducationDocument.
        userRole.AddTypePermissionsRecursively<MedicalRecord>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MedicalRecordDocument>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MedicalRecordImage>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Invitation>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<InvitationItem>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BorderZone>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BorderZoneItem>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<EducationInstitution>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Specialty>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Lodging>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Hotel>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Hospital>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<OtherSite>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
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
        // Extract placeholders replaces all rows — delete required (read/write/create alone is not enough).
        userRole.AddTypePermissionsRecursively<UserReportPlaceholder>(ReadWriteCreateDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<UserReportTemplateApplicationType>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<UserReportTemplateApplicationTypeGroup>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<UserReportTemplateProjectContract>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationTypeGroup>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationTypeGroupMember>(SecurityOperations.Read, SecurityPermissionState.Allow);

        // =====================================================================
        // READ ONLY — Lookup objects (can be referenced but not modified)
        // =====================================================================
        userRole.AddTypePermissionsRecursively<ApplicationTypeFilter>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        // Migration deadline ListView column resolves ApplicationType.MigrationSlaProfile (MaxDays / labels).
        userRole.AddTypePermissionsRecursively<ApplicationMigrationSlaProfile>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationState>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<CheckPoint>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Country>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Department>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<EducationLevel>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<Gender>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MaritalStatus>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MigrationService>(SecurityOperations.Read, SecurityPermissionState.Allow);
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
        userRole.AddTypePermissionsRecursively<VisaPeriod>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<VisaType>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<WorkPermitLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<MovementPermitLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BorderZoneLocation>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BorderZoneName>(ReadWriteCreateDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<WorkPermittedLocationName>(ReadWriteCreateDelete, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ProjectContract>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApprovingMinistry>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApprovalLegProfile>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApprovalLegProfileMinistryLeg>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ProjectContractApprovalLegProfile>(SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<ApplicationApprovalLegSnapshot>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
        // Application number generation reads prefix/format/seed on save; officers must not create or edit org settings.
        userRole.AddTypePermissionsRecursively<ApplicationNumberingProfile>(SecurityOperations.Read, SecurityPermissionState.Allow);
        // Per-BO expiration alert thresholds — read at runtime; configuration UI is VisaOffice only.
        userRole.AddTypePermissionsRecursively<ExpirationAlertRule>(SecurityOperations.Read, SecurityPermissionState.Allow);
        {
            var expirationAlertPerm = userRole.TypePermissions.First(p => p.TargetType == typeof(ExpirationAlertRule));
            expirationAlertPerm.WriteState = SecurityPermissionState.Deny;
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
        userRole.AddNavigationPermission(
            @"Application/NavigationItems/Items/Application/Items/Application_ViaMinistries/Items/ApplicationItem_ViaMinistries",
            SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(
            @"Application/NavigationItems/Items/Application/Items/Application_DirectMigration/Items/ApplicationItem_DirectMigration",
            SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/Application", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Application/Items/ApplicationItem", SecurityPermissionState.Deny);

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

        // BorderZone group (separate from Invitation)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/BorderZone", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/BorderZone/Items/BorderZone", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/BorderZone/Items/BorderZoneItem", SecurityPermissionState.Allow);

        // Operations — UserFeedback only (see EnsureUserFeedbackOfficerPermissions); runtime log + state inbox are admin-only.
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Operations", SecurityPermissionState.Allow);

        // Reports — user-defined Word/Excel templates (Resminamalar custom templates + Edit template link)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Reports", SecurityPermissionState.Allow);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Reports/Items/UserReportTemplate", SecurityPermissionState.Allow);

        userRole.AddTypePermissionsRecursively<BusinessObjects.ApplicationItemDocumentCopiesListHost>(
            SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BusinessObjects.ApplicationReportPackageListHost>(
            SecurityOperations.Read, SecurityPermissionState.Allow);
        userRole.AddTypePermissionsRecursively<BusinessObjects.ApplicationItemReportPackageListHost>(
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
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/Role", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/AuthorizedRepresentative", SecurityPermissionState.Deny);

        // Explicitly DENY entire Documents group (screenshot 1)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Documents", SecurityPermissionState.Deny);

        // Explicitly DENY entire Employee group (screenshot 1)
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Employee", SecurityPermissionState.Deny);
        userRole.AddNavigationPermission(@"Application/NavigationItems/Items/Employee/Items/EmployeePositionHistory", SecurityPermissionState.Deny);
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

    // Resminamalar user templates (seeded from Resources/Templates) — existing "Users" roles need read/write/create + navigation too.
    EnsureUserReportTemplateOfficerPermissions(userRole);

    // PDF filling relies on database-driven mappings (PdfFormMapping). Users must be able to read them.
    EnsureReadOnlyPermission<PdfFormMapping>(userRole);

    EnsureUsersOfficerNavigationPermissions(userRole);
    EnsureReportDashboardOfficerPermissions(userRole);

    // Users: EducationInstitution, Specialty, Position & Lodging — read/write/create only (no delete), including existing roles.
    EnsureReadWriteCreatePermission<EducationInstitution>(userRole);
    EnsureReadWriteCreatePermission<Specialty>(userRole);
    EnsureReadWriteCreatePermission<Position>(userRole);
    EnsureReadWriteCreatePermission<ActualPosition>(userRole);
    // Person.Subcontractor lookup: officers create/select subcontractors without Lookup navigation.
    EnsureReadWriteCreatePermission<Subcontractor>(userRole);
    EnsureReadWriteCreatePermission<Lodging>(userRole);
    EnsureReadWriteCreatePermission<Hotel>(userRole);
    EnsureReadWriteCreatePermission<Hospital>(userRole);
    EnsureReadWriteCreatePermission<OtherSite>(userRole);
    EnsureReadWriteCreatePermission<Rejection>(userRole);
    EnsureReadWriteCreatePermission<RejectionItem>(userRole);
    EnsureReadWriteCreatePermission<Invitation>(userRole);
    EnsureReadWriteCreatePermission<InvitationItem>(userRole);
    EnsureReadWriteCreatePermission<BorderZone>(userRole);
    EnsureReadWriteCreatePermission<BorderZoneItem>(userRole);
    EnsureReadWriteCreatePermission<WorkPermit>(userRole);
    EnsureReadWriteCreatePermission<WorkPermitItem>(userRole);
    EnsureReadOnlyPermission<ForeignWorkerMaglumat>(userRole);
    EnsureReadWriteCreatePermission<FileData>(userRole);
    // Application.VisaPeriod lookup popup — officers add/edit periods without Lookup navigation.
    EnsureReadWriteCreatePermission<VisaPeriod>(userRole);
    // Address of residence: allow adding supporting documents inline (no Documents navigation group access needed).
    EnsureFullAccessRecursivePermission<AddressOfResidence>(userRole);
    EnsureFullAccessRecursivePermission<AddressOfResidenceDocument>(userRole);
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
    EnsureReadOnlyPermission<ExpirationAlertRule>(userRole);

    // Application ListView process-tracking columns (status, approval/migration SLA).
    EnsureApplicationProcessTrackingReadPermissions(userRole, officerCanWriteProgress: true);

    EnsureUserFeedbackOfficerPermissions(userRole);
    EnsureAdminOnlyOperationsDeny(userRole);

    return userRole;
}

        /// <summary>
        /// Parallel-period officers: same navigation as <see cref="CreateUserRole"/> but read-only on business data.
        /// Used while legacy VISA2015 remains system of record (see ON_PREM_IIS_MIGRATION_RUNBOOK).
        /// </summary>
        PermissionPolicyRole CreateUsersReadOnlyRole()
        {
            PermissionPolicyRole role = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "UsersReadOnly");
            if (role == null)
            {
                role = ObjectSpace.CreateObject<PermissionPolicyRole>();
                role.Name = "UsersReadOnly";
            }

            EnsureUsersOfficerNavigationPermissions(role);
            EnsureReportDashboardOfficerPermissions(role);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Operations", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Reports", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration", SecurityPermissionState.Deny);
            EnsureUsersReadOnlyTypePermissions(role);
            EnsureAdminOnlyOperationsDeny(role);

            return role;
        }

        /// <summary>Report Dashboard home — readable by all officer roles.</summary>
        static void EnsureReportDashboardOfficerPermissions(PermissionPolicyRole role)
        {
            if (role == null)
                return;

            EnsureTypePermission<BusinessObjects.ReportDashboard.ReportDashboardHost>(
                role, SecurityOperations.Read, SecurityPermissionState.Allow);
            // Open ListView for Visa Extension / Extension Result (vw_rd_visa_app_progress).
            EnsureTypePermission<VwRdVisaAppProgress>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaByPeriod>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaActiveByProject>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaActiveByPeriodCategoryType>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaOnExtension>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaOnExtensionByPeriodCategoryType>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaExtensionResult>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaExtensionResultByPeriodCategoryType>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaExtensionRequired>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<VwRdVisaByDaysRemaining>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Home", SecurityPermissionState.Allow);
            EnsureNavigationPermission(
                role,
                @"Application/NavigationItems/Items/Home/Items/ReportDashboard",
                SecurityPermissionState.Allow);
        }

        /// <summary>Shared case-officer navigation for <c>Users</c> and <c>UsersReadOnly</c> roles.</summary>
        static void EnsureUsersOfficerNavigationPermissions(PermissionPolicyRole role)
        {
            if (role == null)
                return;

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Home", SecurityPermissionState.Allow);
            EnsureNavigationPermission(
                role,
                @"Application/NavigationItems/Items/Home/Items/ReportDashboard",
                SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application/Items/Application_ViaMinistries", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application/Items/Application_DirectMigration", SecurityPermissionState.Allow);
            EnsureNavigationPermission(
                role,
                @"Application/NavigationItems/Items/Application/Items/Application_ViaMinistries/Items/ApplicationItem_ViaMinistries",
                SecurityPermissionState.Allow);
            EnsureNavigationPermission(
                role,
                @"Application/NavigationItems/Items/Application/Items/Application_DirectMigration/Items/ApplicationItem_DirectMigration",
                SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application/Items/Application", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application/Items/ApplicationItem", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application/Items/ApplicationProgress", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application/Items/BusinessTrip", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application/Items/PdfGenerationBatch", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application/Items/WordReportGenerationBatch", SecurityPermissionState.Deny);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Rejection", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Rejection/Items/Rejection", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Rejection/Items/RejectionItem", SecurityPermissionState.Allow);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Invitation", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Invitation/Items/Invitation", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Invitation/Items/InvitationItem", SecurityPermissionState.Allow);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/BorderZone", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/BorderZone/Items/BorderZone", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/BorderZone/Items/BorderZoneItem", SecurityPermissionState.Allow);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/WorkPermit", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/WorkPermit/Items/WorkPermit", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/WorkPermit/Items/WorkPermitItem", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/WorkPermit/Items/ForeignWorkerMaglumat", SecurityPermissionState.Allow);

            // Top-level person lists (legacy-style; not under People).
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Employees", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/FamilyMembers", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/TemporaryVisitors", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/People", SecurityPermissionState.Deny);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Operations", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Reports", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Reports/Items/UserReportTemplate", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/AddressOfResidenceDocument", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/BorderZoneItem", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/BusinessTripAddress", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/BusinessTripPlan", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/AuthorizedSignatory", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/ContractTemplate", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/Role", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Default/Items/AuthorizedRepresentative", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Documents", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Employee", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Employee/Items/EmployeePositionHistory", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Employee/Items/EmployeeSalary", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Employee/Items/LocalEmployee", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Images", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Application/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Education/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/General/Geography", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Organization/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Passport/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Person/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Visa/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/WorkPermit/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Invitation", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Auth", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/System/Items/ExpirationAlertRule", SecurityPermissionState.Deny);
        }

        static void EnsureUsersReadOnlyTypePermissions(PermissionPolicyRole role)
        {
            if (role == null)
                return;

            EnsureReadOnlyPermission<Person>(role);
            EnsureReadOnlyPermission<Application>(role);
            EnsureReadOnlyPermission<ApplicationItem>(role);
            EnsureReadOnlyPermission<Passport>(role);
            EnsureReadOnlyPermission<Visa>(role);
            EnsureReadOnlyPermission<Education>(role);
            EnsureReadOnlyPermission<MedicalRecord>(role);
            EnsureReadOnlyPermission<MedicalRecordDocument>(role);
            EnsureReadOnlyPermission<MedicalRecordImage>(role);
            EnsureReadOnlyPermission<Invitation>(role);
            EnsureReadOnlyPermission<InvitationItem>(role);
            EnsureReadOnlyPermission<BorderZone>(role);
            EnsureReadOnlyPermission<BorderZoneItem>(role);
            EnsureReadOnlyPermission<EducationInstitution>(role);
            EnsureReadOnlyPermission<Specialty>(role);
            EnsureReadOnlyPermission<Lodging>(role);
            EnsureReadOnlyPermission<Hotel>(role);
            EnsureReadOnlyPermission<Hospital>(role);
            EnsureReadOnlyPermission<OtherSite>(role);
            EnsureReadOnlyPermission<Rejection>(role);
            EnsureReadOnlyPermission<RejectionItem>(role);
            EnsureReadOnlyPermission<WorkPermit>(role);
            EnsureReadOnlyPermission<WorkPermitItem>(role);
            EnsureReadOnlyPermission<ForeignWorkerMaglumat>(role);
            EnsureReadOnlyPermission<FileData>(role);
            EnsureReadOnlyPermission<Position>(role);
            EnsureReadOnlyPermission<ActualPosition>(role);
            EnsureReadOnlyPermission<Subcontractor>(role);
            EnsureReadOnlyPermission<AddressOfResidence>(role);
            EnsureReadOnlyPermission<AddressOfResidenceDocument>(role);
            EnsureReadOnlyPermission<BorderZoneName>(role);
            EnsureReadOnlyPermission<WorkPermittedLocationName>(role);
            EnsureReadOnlyPermission<UserReportTemplate>(role);
            EnsureReadOnlyPermission<UserReportPlaceholder>(role);
            EnsureReadOnlyPermission<UserReportTemplateApplicationType>(role);
            EnsureReadOnlyPermission<UserReportTemplateApplicationTypeGroup>(role);
            EnsureReadOnlyPermission<UserReportTemplateProjectContract>(role);
            EnsureReadOnlyPermission<ApplicationTypeGroup>(role);
            EnsureReadOnlyPermission<ApplicationTypeGroupMember>(role);
            EnsureReadOnlyPermission<PdfGenerationBatch>(role);
            EnsureReadOnlyPermission<WordReportGenerationBatch>(role);

            EnsureReadOnlyPermission<ApplicationTypeFilter>(role);
            EnsureReadOnlyPermission<ApplicationType>(role);
            EnsureReadOnlyPermission<CheckPoint>(role);
            EnsureReadOnlyPermission<Country>(role);
            EnsureReadOnlyPermission<Department>(role);
            EnsureReadOnlyPermission<EducationLevel>(role);
            EnsureReadOnlyPermission<Gender>(role);
            EnsureReadOnlyPermission<MaritalStatus>(role);
            EnsureReadOnlyPermission<PassportType>(role);
            EnsureReadOnlyPermission<PurposeOfTravel>(role);
            EnsureReadOnlyPermission<Region>(role);
            EnsureReadOnlyPermission<Relationship>(role);
            EnsureReadOnlyPermission<Urgency>(role);
            EnsureReadOnlyPermission<ValidityDuration>(role);
            EnsureReadOnlyPermission<VisaCategory>(role);
            EnsureReadOnlyPermission<VisaIssuedPlace>(role);
            EnsureReadOnlyPermission<VisaPeriod>(role);
            EnsureReadOnlyPermission<VisaType>(role);
            EnsureReadOnlyPermission<WorkPermitLocation>(role);
            EnsureReadOnlyPermission<MovementPermitLocation>(role);
            EnsureReadOnlyPermission<BorderZoneLocation>(role);
            EnsureReadOnlyPermission<ApplicationNumberingProfile>(role);
            EnsureReadOnlyPermission<ExpirationAlertRule>(role);
            EnsureReadOnlyPermission<PdfFormMapping>(role);

            // Same process-tracking reads as Users (status + approval/migration SLA columns); no write.
            EnsureApplicationProcessTrackingReadPermissions(role, officerCanWriteProgress: false);

            EnsureTypePermission<ReportDataV2>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<ReportVisibility>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<BusinessObjects.ApplicationItemDocumentCopiesListHost>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<BusinessObjects.ApplicationReportPackageListHost>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<BusinessObjects.ApplicationItemReportPackageListHost>(role, SecurityOperations.Read, SecurityPermissionState.Allow);

            EnsureDenyTypeAccess<UserFeedback>(role);
        }

        /// <summary>
        /// Grants types needed to display Application ListView process-tracking columns
        /// (Current status, Approval/Migration working days &amp; deadlines). No Configuration navigation.
        /// Shared by <c>Users</c> and <c>UsersReadOnly</c> (reader officers).
        /// </summary>
        /// <param name="officerCanWriteProgress">
        /// When true (Users): officers may create/update progress and approval snapshots.
        /// When false (UsersReadOnly): progress and snapshots are read-only.
        /// </param>
        static void EnsureApplicationProcessTrackingReadPermissions(PermissionPolicyRole role, bool officerCanWriteProgress)
        {
            if (role == null)
                return;

            // Migration deadline / working days — ApplicationType.MigrationSlaProfile
            EnsureReadOnlyPermission<ApplicationMigrationSlaProfile>(role);
            // Current status labels
            EnsureReadOnlyPermission<ApplicationState>(role);
            EnsureReadOnlyPermission<ApplicationLocation>(role);
            EnsureReadOnlyPermission<MigrationService>(role);
            // Approval deadline / working days — Application.ApprovalLegSnapshots
            EnsureReadOnlyPermission<ApprovingMinistry>(role);
            EnsureReadOnlyPermission<ApprovalLegProfile>(role);
            EnsureReadOnlyPermission<ApprovalLegProfileMinistryLeg>(role);
            EnsureReadOnlyPermission<ProjectContractApprovalLegProfile>(role);
            EnsureReadOnlyPermission<ProjectContract>(role);

            if (officerCanWriteProgress)
            {
                EnsureFullAccessRecursivePermission<ApplicationProgress>(role);
                EnsureReadWriteCreatePermission<ApplicationApprovalLegSnapshot>(role);
            }
            else
            {
                EnsureReadOnlyPermission<ApplicationProgress>(role);
                EnsureReadOnlyPermission<ApplicationApprovalLegSnapshot>(role);
            }
        }

        static void EnsureDenyTypeAccess<T>(PermissionPolicyRole role) where T : class
        {
            if (role == null)
                return;

            var targetType = typeof(T);
            var existing = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
            if (existing != null)
            {
                existing.ReadState = SecurityPermissionState.Deny;
                existing.WriteState = SecurityPermissionState.Deny;
                existing.CreateState = SecurityPermissionState.Deny;
                existing.DeleteState = SecurityPermissionState.Deny;
            }
            else
            {
                role.AddTypePermissionsRecursively<T>(SecurityOperations.Read, SecurityPermissionState.Deny);
            }
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
                existingPerm.NavigateState = SecurityPermissionState.Allow;
            }
            else
            {
                role.AddTypePermissionsRecursively<T>(ReadWriteCreateWithoutDelete, SecurityPermissionState.Allow);
                var newPerm = role.TypePermissions.First(p => p.TargetType == targetType);
                newPerm.NavigateState = SecurityPermissionState.Allow;
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
                ApplicationUserThemePreferencePermissions.EnsureSelfWrite(defaultRole);
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
        /// Tenant JSON organization singletons + project contracts (not global Lookup catalogs).
        /// Read/write only on singletons — deploy sync owns row lifecycle (no create/delete).
        /// </summary>
        static void EnsureVisaOfficeConfigurationPermissions(PermissionPolicyRole role)
        {
            if (role == null)
                return;

            EnsureReadWriteOnlyPermission<CompanyProfile>(role);
            EnsureReadWriteOnlyPermission<AuthorizedSignatory>(role);
            EnsureReadWriteOnlyPermission<AuthorizedRepresentative>(role);
            EnsureReadWriteOnlyPermission<ApplicationNumberingProfile>(role);
            EnsureReadWriteCreatePermission<ProjectContract>(role);
            EnsureReadWriteCreatePermission<ApprovingMinistry>(role);
            EnsureReadWriteCreatePermission<ApprovalLegProfile>(role);
            EnsureReadWriteCreatePermission<FileData>(role);
            EnsureTypePermission<ReportDataV2>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureTypePermission<ReportVisibility>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
            EnsureReadOnlyPermission<PdfFormMapping>(role);
            EnsureFullAccessRecursivePermission<ApplicationMigrationSlaProfile>(role);
            EnsureFullAccessRecursivePermission<SystemSettings>(role);
            EnsureFullAccessRecursivePermission<MinistryReviewSlaSettings>(role);
            EnsureReadWriteOnlyPermission<ExpirationAlertRule>(role);
            // Link / Unlink application types on migration SLA profile detail.
            EnsureTypePermission<ApplicationType>(role, SecurityOperations.Read, SecurityPermissionState.Allow);
        }

        /// <summary>Navigation for visa office — Configuration screens, Resminamalar templates; not case processing.</summary>
        static void EnsureVisaOfficeNavigationPermissions(PermissionPolicyRole role)
        {
            if (role == null)
                return;

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/CompanyProfile", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/AuthorizedSignatory", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/AuthorizedRepresentative", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/ApplicationNumberingProfile", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/ApplicationMigrationSlaProfile", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/MinistryReviewSlaSettings", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/ExpirationAlertRule", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/SystemSettings", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/ProjectContract", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/ApprovingMinistry", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Configuration/Items/ApprovalLegProfile", SecurityPermissionState.Allow);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Education", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Housing", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Medical", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Person", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Passport", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Visa", SecurityPermissionState.Deny);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Application/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Education/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/General/Geography", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Organization/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Passport/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Person/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Visa/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/WorkPermit/Config", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Lookup/Invitation", SecurityPermissionState.Deny);

            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Application", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Employees", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/FamilyMembers", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/TemporaryVisitors", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/People", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Employee", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Documents", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Images", SecurityPermissionState.Deny);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Auth", SecurityPermissionState.Deny);
        }

        /// <summary>
        /// Officers maintain Resminamalar custom templates: read/list, open DetailView (navigate), edit file + placeholders.
        /// </summary>
        static void EnsureUserReportTemplateOfficerPermissions(PermissionPolicyRole role)
        {
            if (role == null)
                return;

            EnsureReadWriteCreatePermission<UserReportTemplate>(role);
            EnsureFullAccessRecursivePermission<UserReportPlaceholder>(role);
            EnsureReadWriteCreatePermission<UserReportTemplateApplicationType>(role);
            EnsureReadWriteCreatePermission<UserReportTemplateApplicationTypeGroup>(role);
            EnsureReadWriteCreatePermission<UserReportTemplateProjectContract>(role);
            EnsureReadOnlyPermission<ApplicationTypeGroup>(role);
            EnsureReadOnlyPermission<ApplicationTypeGroupMember>(role);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Reports", SecurityPermissionState.Allow);
            EnsureNavigationPermission(role, @"Application/NavigationItems/Items/Reports/Items/UserReportTemplate", SecurityPermissionState.Allow);
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

        /// <summary>
        /// Application runtime log, state-notification inbox, and Import reimport history —
        /// super administrators only (<see cref="PermissionPolicyRole.IsAdministrative"/>).
        /// </summary>
        static void EnsureAdminOnlyOperationsDeny(PermissionPolicyRole role)
        {
            if (role == null)
                return;

            DenyTypeRead<ApplicationRuntimeLog>(role);
            DenyTypeRead<BoStateNotificationInboxHost>(role);
            DenyTypeRead<ImportReimportHistoryHost>(role);

            EnsureNavigationPermission(
                role,
                @"Application/NavigationItems/Items/Operations/Items/ApplicationRuntimeLog",
                SecurityPermissionState.Deny);
            EnsureNavigationPermission(
                role,
                @"Application/NavigationItems/Items/Operations/Items/StateNotifications",
                SecurityPermissionState.Deny);
            EnsureNavigationPermission(
                role,
                @"Application/NavigationItems/Items/Operations/Items/ImportReimportHistory",
                SecurityPermissionState.Deny);
        }

        static void DenyTypeRead<T>(PermissionPolicyRole role) where T : class
        {
            var targetType = typeof(T);
            var existing = role.TypePermissions.FirstOrDefault(tp => tp.TargetType == targetType);
            if (existing != null)
                existing.ReadState = SecurityPermissionState.Deny;
            else
                role.AddTypePermissionsRecursively<T>(SecurityOperations.Read, SecurityPermissionState.Deny);
        }
    }
}
