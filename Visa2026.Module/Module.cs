using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.DomainLogics;
using Visa2026.Module.Controllers;
using Visa2026.Module.Model;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.MigrationImport;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.ReportsV2;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.Appearance;
using DevExpress.ExpressApp.Office;
using System.Reflection;

namespace Visa2026.Module
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ModuleBase.
    public sealed class Visa2026Module : ModuleBase
    {
        public static string Version => typeof(Visa2026Module).Assembly.GetName().Version?.ToString() ?? "Unknown";

        public static string VersionDisplay
        {
            get
            {
                var asm = typeof(Visa2026Module).Assembly;
                var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                return string.IsNullOrWhiteSpace(info) ? Version : info;
            }
        }
        public Visa2026Module()
        {
            //
            // Visa2026Module
            //
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationUser));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyRole));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.ModelDifference));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.ModelDifferenceAspect));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Security.SecurityModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.AuditTrail.EFCore.AuditTrailModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.CloneObject.CloneObjectModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Notifications.NotificationsModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Office.OfficeModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ReportsV2.ReportsModuleV2));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Validation.ValidationModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ViewVariantsModule.ViewVariantsModule));
            DevExpress.ExpressApp.Security.SecurityModule.UsedExportedTypes = DevExpress.Persistent.Base.UsedExportedTypes.Custom;
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileData));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileAttachment));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.HCategory));
            if (MailMergeFeature.Enabled)
                AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.RichTextMailMergeData));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.BoStateSnapshot));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.StateChangeRule));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.StateChangeLog));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.PdfBatchEnqueueOptions));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.PersonIncompleteMarkOptions));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationReportPackageListHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.StateNotifications.BoStateNotificationInboxHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.Operations.ImportReimportHistoryHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ReportDashboard.ReportDashboardHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationWorkspace.ApplicationWorkspaceHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationProfileWizard.ApplicationProfileWizardHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationProfilePicker.ApplicationProfilePickerHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationProfileOverview.ApplicationProfileOverviewHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationProfileCatalog.ApplicationProfileCatalogHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.OfficerShell.OfficerShellHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.Feedback.UserFeedback));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.Operations.ApplicationRuntimeLog));
        }
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB)
        {
            // PostgreSQL-only: ObjectSpace seed/config + dual schema helpers + Report Dashboard PG views.
            // (Former SQL Server T-SQL ModuleUpdaters are not registered.)
            return new ModuleUpdater[]
            {
                new DatabaseUpdate.ApplicationProfileInstanceCutoverSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.Updater(objectSpace, versionFromDB),
                new DatabaseUpdate.TenantUserSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.SyncRulesUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.SystemSettingsUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.OrganizationSingletonSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.StateChangeRulesUpdater(objectSpace, versionFromDB),

                new DatabaseUpdate.LookupBaseNameTmBackfillUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.LookupCatalogSyncUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ProjectContractTitleDescriptionMergeUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.LookupLocalizationKeyUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.UrgencyDuplicateCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationTypeSelectionCodeUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationTypeCapabilityFlagsSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PersonIncompleteDataSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.CompanyProfileRegistrationDateSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PersonPreviousWorkplacesInTurkmenistanSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PersonExportBatchSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.InvitationLegacyShapeSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationProfileInstanceProgressProcessNumberSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationProfileSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.VisaIssuingApplicationProfileInstanceSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.VisaIssuingInvitationItemSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.VisaProcessNumberSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationTypeConfigurationUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationTypeGroupSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationTypeGroupSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationMigrationSlaProfileDropSchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationProfileSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationProfileTenantCatalogSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationProfileNestedTemplateTenantCatalogSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApprovalLegProfileSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationProfileApprovalLegVersionTenantCatalogSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.IssuedDocumentStatusColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ReportDashboardPostgresViewsUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ProjectContractApprovalLegProfileLinkUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.VisaFamilyManualFromFamilyMembersMigrationUpdater(objectSpace, versionFromDB)
            };
        }
        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters)
        {
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new CustomNavigationUpdater());
            updaters.Add(new CustomViewClonerUpdater());
            updaters.Add(new PersonTypedDetailViewModelUpdater());
            updaters.Add(new LookupLocalizationModelUpdater());
            updaters.Add(new LookupLocalizationLookupListViewUpdater());
            updaters.Add(new LookupBaseDetailViewModelUpdater());
            updaters.Add(new ApprovalLegProfileMinistryLegViewsUpdater());
            updaters.Add(new ApplicationProfileInstanceProgressHistoryViewsUpdater());
            updaters.Add(new PersonNestedListViewsUpdater());
            updaters.Add(new ApplicationProfileInstanceChildNestedListViewsUpdater());
            updaters.Add(new ApplicationProfileInstanceHideDeprecatedTypeColumnUpdater());
            updaters.Add(new ExpirationAlertRuleViewsUpdater());
            updaters.Add(new ListViewShowFindPanelModelUpdater());
            updaters.Add(new DatabaseUpdate.HistoryDashboardViewItemUpdater());
            updaters.Add(new DatabaseUpdate.BoStateNotificationInboxModelUpdater());
            updaters.Add(new DatabaseUpdate.BoStateNotificationInboxDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ImportReimportHistoryModelUpdater());
            updaters.Add(new DatabaseUpdate.ImportReimportHistoryDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ReportDashboardModelUpdater());
            updaters.Add(new DatabaseUpdate.ReportDashboardDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.PersonDossierDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationWorkspaceDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationWorkspaceLayoutUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfileWizardDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfileWizardLayoutUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfilePickerDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfilePickerLayoutUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfileOverviewDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfileOverviewLayoutUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfileCatalogDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfileCatalogLayoutUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationProfileCatalogModelUpdater());
            updaters.Add(new DatabaseUpdate.OfficerShellDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.OfficerShellLayoutUpdater());
            updaters.Add(new DatabaseUpdate.OfficerShellModelUpdater());
            updaters.Add(new DatabaseUpdate.UserFeedbackModelUpdater());
            updaters.Add(new DatabaseUpdate.UserFeedbackViewsUpdater());
            updaters.Add(new DatabaseUpdate.UserFeedbackDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationRuntimeLogModelUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationRuntimeLogViewsUpdater());
            updaters.Add(new DatabaseUpdate.PersonListViewActionColumnsUpdater());
            updaters.Add(new DatabaseUpdate.HeaderDocumentCopiesListViewColumnUpdater());
        }
        protected override IEnumerable<Type> GetRegularTypes()
        {
            return base.GetRegularTypes().Where(t => !t.ContainsGenericParameters);
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo)
        {
            base.CustomizeTypesInfo(typesInfo);
            BoStateRowAppearanceRegistration.Register(typesInfo);
            OptionalDetailFieldsAppearanceRegistration.Register(typesInfo);
            if (!MailMergeFeature.Enabled)
                MailMergeFeatureRegistration.HideFromApplicationModel(typesInfo);
        }

        public override void Setup(XafApplication application)
        {
            base.Setup(application);
            application.ObjectSpaceCreated += Application_ObjectSpaceCreated;
        }

        private static void Application_ObjectSpaceCreated(object? sender, ObjectSpaceCreatedEventArgs e)
        {
            ApprovalLegProfileMinistryLegObjectSpaceHooks.Subscribe(e.ObjectSpace);
            ApplicationProfileConfigLockObjectSpaceHooks.Subscribe(e.ObjectSpace);
            PersonVisaFamilyManualDefaultsObjectSpaceHooks.Subscribe(e.ObjectSpace);
            MigrationImportAuditTrailObjectSpaceHooks.ApplyIfNeeded(e.ObjectSpace);
        }
    }
}
