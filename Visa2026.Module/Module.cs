using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.DomainLogics;
using Visa2026.Module.Model;
using Visa2026.Module.DatabaseUpdate;
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
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationItemDocumentCopiesListHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationReportPackageListHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.ApplicationItemReportPackageListHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.StateNotifications.BoStateNotificationInboxHost));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.Feedback.UserFeedback));
            AdditionalExportedTypes.Add(typeof(Visa2026.Module.BusinessObjects.Operations.ApplicationRuntimeLog));
        }
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB)
        {
            return new ModuleUpdater[]
            {
                new DatabaseUpdate.Updater(objectSpace, versionFromDB),
                new DatabaseUpdate.PersonRoleMigrationUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PersonFamilyRelationDocumentMigrationUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ProjectContractLegacyColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.SyncRulesUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.SystemSettingsUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.OrganizationSingletonSeedUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PersonCurrentColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.EmployeeContractSchemaCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationItemCurrentSalarySchemaUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PersonIsActiveColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PassportCurrentVisaColumnCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationLegacyColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.InvitationHeaderStatusColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.WorkPermitItemStatusColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.WorkPermitApplicationNotRequiredColumnCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.VisaVisibilityToggleColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.OrganizationLegacySchemaCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.OrganizationPdfFormMappingUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PdfFormMappingUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ReportsUpdater(Application, objectSpace, versionFromDB),
                new DatabaseUpdate.UserReportTemplateApplicableTypesMigrationUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.UserReportTemplateUpdater(Application, objectSpace, versionFromDB),
                new DatabaseUpdate.StateChangeRulesUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.SqlViewsUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationLineItemsConsolidationUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationItemMovementFlattenUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.RegistrationTravelHistoryBackfillUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationItemPurposeOfTravelColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.AddressOfResidenceStartDateColumnCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationItemBorderZoneLocationStringUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.WorkPermitItemPermittedLocationsStringUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationItemWorkPermittedLocationsStringUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.VisaBorderZoneLocationStringUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.VisaBorderZoneLocationYokDefaultUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.PdfGenerationBatchRequestedCultureUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.WordReportGenerationBatchSelectedReportKeysUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.WordReportGenerationBatchSelectedApplicationItemIdsUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.EmployeeSalaryAmountStringUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.EducationGraduationYearStringUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.SubcontractorContactColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.SoftDeleteColumnsCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.LookupBaseNameTmBackfillUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.LookupCatalogSyncUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.LookupBaseNameTmPdfFormMappingUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationNumberingProfileMigrationUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.LookupLocalizationKeyUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.UrgencyDuplicateCleanupUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationTypeSelectionCodeUpdater(objectSpace, versionFromDB),
                new DatabaseUpdate.ApplicationTypeConfigurationUpdater(objectSpace, versionFromDB)
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
            updaters.Add(new DatabaseUpdate.HistoryDashboardViewItemUpdater());
            updaters.Add(new DatabaseUpdate.BoStateNotificationInboxModelUpdater());
            updaters.Add(new DatabaseUpdate.BoStateNotificationInboxDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.UserFeedbackModelUpdater());
            updaters.Add(new DatabaseUpdate.UserFeedbackViewsUpdater());
            updaters.Add(new DatabaseUpdate.UserFeedbackDetailViewUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationRuntimeLogModelUpdater());
            updaters.Add(new DatabaseUpdate.ApplicationRuntimeLogViewsUpdater());
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
            // Manage various aspects of the application UI and behavior at the module level.
        }
    }
}
