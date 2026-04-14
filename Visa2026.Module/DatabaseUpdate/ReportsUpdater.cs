using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ReportsV2;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Reports;

namespace Visa2026.Module.DatabaseUpdate
{
    public class ReportsUpdater : PredefinedReportsUpdater
    {
        public ReportsUpdater(XafApplication application, IObjectSpace objectSpace, Version currentDBVersion) :
            base(application, objectSpace, currentDBVersion)
        {
            // Register reports in the application
            AddPredefinedReport<ApplicationReport>("Application Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<ApplicationVisaExtEmp>("Application For Employee's Visa Extension Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<ApplicationLetterReport>("Application Letter Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<ApplicationItemReport>("ApplicationItem Report", typeof(ApplicationItem), isInplaceReport: true);

            // ApplicationItem-level personnel list reports (Inv group — 14 columns)
            AddPredefinedReport<AppInvItemReport>("App Inv Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppInvAndWPItemReport>("App Inv And WP Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppInvFMItemReport>("App Inv FM Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppVisaExtFMItemReport>("App Visa Ext FM Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppVisaAndWPExtItemReport>("App Visa And WP Ext Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppAdditionalWPLocationItemReport>("App Additional WP Location Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppCancelVisaItemReport>("App Cancel Visa Item Report", typeof(ApplicationItem), isInplaceReport: true);

            // Reg group item reports are handled by RegistrationListReport (typeof Registration, no criteria)
            AddPredefinedReport<RegistrationListReport>("Registration List Report", typeof(Registration), isInplaceReport: true);
            AddPredefinedReport<AppRegCheckInReport>("App Reg Check In Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppInvReport>("App Inv Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppInvFMReport>("App Inv FM Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppInvAndWPReport>("App Inv And WP Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppRegCheckOutReport>("App Reg Check Out Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppRegCheckInInternalReport>("App Reg Check In Internal Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppRegCheckOutInternalReport>("App Reg Check Out Internal Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppRegExtReport>("App Reg Ext Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppRegInfoChangeAddressReport>("App Reg Info Change Address Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppRegInfoChangePassportReport>("App Reg Info Change Passport Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppCancelVisaReport>("App Cancel Visa Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppVisaAndWPExtReport>("App Visa And WP Ext Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppVisaExtFMReport>("App Visa Ext FM Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppAdditionalWPLocationReport>("App Additional WP Location Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppChangeInvReport>("App Change Inv Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppChangePassportReport>("App Change Passport Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppCancelVisaAndWPReport>("App Cancel Visa And WP Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppCancelInvWPReport>("App Cancel Inv WP Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppBorderZonePermissionReport>("App Border Zone Permission Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppBorderZonePermissionItemReport>("App Border Zone Permission Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppCancelInvWPItemReport>("App Cancel Inv WP Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppCancelVisaAndWPItemReport>("App Cancel Visa And WP Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppChangeInvItemReport>("App Change Inv Item Report", typeof(ApplicationItem), isInplaceReport: true);
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            // 1. Rule for the "Visa Extension" report: Only visible for specific application types
            CreateReportVisibility(
                reportName: "Application For Employee's Visa Extension Report",
                displayName: "Application For Employee's Visa Extension",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] In ('Wiza we Iş Rugsatnamasyny Uzaltmak (IŞG)', 'Another Application Type Name')"
            );



            // ApplicationItem generic report — always visible for all application types
            CreateReportVisibility(
                reportName: "ApplicationItem Report",
                displayName: "Application Item Details",
                targetType: typeof(ApplicationItem),
                criteria: ""
            );

            // ApplicationItem personnel list reports — Inv group (14-column)
            CreateReportVisibility(
                reportName: "App Inv Item Report",
                displayName: "Çakylyk — Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Inv'"
            );
            CreateReportVisibility(
                reportName: "App Inv And WP Item Report",
                displayName: "Çakylyk we Iş Rugsatnamasy — Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Inv_And_WP'"
            );
            CreateReportVisibility(
                reportName: "App Inv FM Item Report",
                displayName: "Çakylyk FM — Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Inv_FM'"
            );
            CreateReportVisibility(
                reportName: "App Visa Ext FM Item Report",
                displayName: "Wiza Möhletini Uzaltmak FM — Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Visa_Ext_FM'"
            );
            CreateReportVisibility(
                reportName: "App Visa And WP Ext Item Report",
                displayName: "Wiza we Iş Rugsatnamasyny Uzaltmak — Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Visa_and_WP_Ext'"
            );
            CreateReportVisibility(
                reportName: "App Additional WP Location Item Report",
                displayName: "Goşmaça hereket çägi — Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Additional_WP_location'"
            );
            CreateReportVisibility(
                reportName: "App Cancel Visa Item Report",
                displayName: "Wizany Ýatyrmak — Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Cancel_Visa'"
            );

            // Reg group item reports: RegistrationListReport handles all Registration types — no per-type rule needed

            // // 4. Rule for "Application Letter Report": Visible for all applications except Draft
            // CreateReportVisibility(
            //     reportName: "Application Letter Report",
            //     displayName: "Application Processing Letter",
            //     targetType: typeof(Visa2026.Module.BusinessObjects.Application),
            //     criteria: null
            // );

            // 5. Rule for "Registration List Report": Always visible for Registrations
            CreateReportVisibility(
                reportName: "Registration List Report",
                displayName: "Registration Personnel List",
                targetType: typeof(Registration),
                criteria: "" // Empty criteria means it's always visible for this target type
            );

            // 6. App_Reg_Check_In — Application-level cover letter to Migration Service
            CreateReportVisibility(
                reportName: "App Reg Check In Report",
                displayName: "Hasaba Almak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_In'"
            );

            // 7. App_Inv — Application-level invitation letter to Ministry
            CreateReportVisibility(
                reportName: "App Inv Report",
                displayName: "Çakylyk — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Inv'"
            );

            // 8. App_Reg_Ext — Application-level registration extension letter
            CreateReportVisibility(
                reportName: "App Reg Ext Report",
                displayName: "Hasaba Alyş Möhletini Uzaltmak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_ext'"
            );

            // 9. App_Reg_Check_Out_Internal — Application-level internal movement check-out letter
            CreateReportVisibility(
                reportName: "App Reg Check Out Internal Report",
                displayName: "Hasapdan Çykarmak (Içerki) — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_Out_Internal'"
            );

            // 9. App_Reg_Check_In_Internal — Application-level internal movement check-in letter
            CreateReportVisibility(
                reportName: "App Reg Check In Internal Report",
                displayName: "Hasaba Almak (Içerki) — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_In_Internal'"
            );

            // 9. App_Reg_Check_Out — Application-level check-out letter to Migration Service
            CreateReportVisibility(
                reportName: "App Reg Check Out Report",
                displayName: "Hasapdan Çykarmak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_Out'"
            );

            // 9. App_Inv_And_WP — Application-level invitation + work permit letter
            CreateReportVisibility(
                reportName: "App Inv And WP Report",
                displayName: "Çakylyk we Iş Rugsatnamasy — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Inv_And_WP'"
            );

            // 9. App_Inv_FM — Application-level invitation letter for family members
            CreateReportVisibility(
                reportName: "App Inv FM Report",
                displayName: "Çakylyk — FM Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Inv_FM'"
            );

            // 10. App_Reg_Info_Change_Address — Address change re-registration letter
            CreateReportVisibility(
                reportName: "App Reg Info Change Address Report",
                displayName: "Salgy Üýtgemegi — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Info_Change_Address'"
            );

            // 11. App_Reg_Info_Change_Passport — Passport change re-registration letter
            CreateReportVisibility(
                reportName: "App Reg Info Change Passport Report",
                displayName: "Pasport Üýtgemegi — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Info_Change_Passport'"
            );

            // 12. App_Cancel_Visa — Visa cancellation letter to national Migration Service head
            CreateReportVisibility(
                reportName: "App Cancel Visa Report",
                displayName: "Wizany Ýatyrmak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Cancel_Visa'"
            );

            // 13. App_Visa_and_WP_Ext — Visa + work permit extension request to Ministry
            CreateReportVisibility(
                reportName: "App Visa And WP Ext Report",
                displayName: "Wiza we Iş Rugsatnamasyny Uzaltmak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Visa_and_WP_Ext'"
            );

            // 14. App_Visa_Ext_FM — FM visa extension request to Ministry
            CreateReportVisibility(
                reportName: "App Visa Ext FM Report",
                displayName: "Wiza Möhletini Uzaltmak FM — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Visa_Ext_FM'"
            );

            // 15. App_Additional_WP_location — Additional work permit location request to Ministry
            CreateReportVisibility(
                reportName: "App Additional WP Location Report",
                displayName: "Goşmaça hereket çägi — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Additional_WP_location'"
            );

            // 16. App_Change_Inv — Change of invitation letter to national Migration Service head
            CreateReportVisibility(
                reportName: "App Change Inv Report",
                displayName: "\u00C7akylygy \u00FC\u00FDtgetmek \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Change_Inv'"
            );

            // 17. App_Change_Passport — Visa transfer to new passport letter to national Migration Service head
            CreateReportVisibility(
                reportName: "App Change Passport Report",
                displayName: "Wizan\u00FD Ge\u00E7irmek \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Change_Passport'"
            );

            // 18. App_Cancel_Visa_and_WP — Visa and work permit cancellation letter to national Migration Service head
            CreateReportVisibility(
                reportName: "App Cancel Visa And WP Report",
                displayName: "Wiza we I\u015F Rugsat\u00E7ynamany \u00DDatyrmak \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Cancel_Visa_and_WP'"
            );

            // 20. App_Border_Zone_Permission — Border zone visa registration request letter to Ministry head
            CreateReportVisibility(
                reportName: "App Border Zone Permission Report",
                displayName: "Serhet \u00DDaka Rugsatnama \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Border_Zone_Permission'"
            );

            // 22. App_Cancel_Inv_WP Item — Personnel list for invitation + work permit cancellation
            CreateReportVisibility(
                reportName: "App Cancel Inv WP Item Report",
                displayName: "\u00C7akylyk we I\u015F Rugsat\u00E7ynamany \u00DDatyrmak \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Cancel_Inv_WP'"
            );

            // 21. App_Border_Zone_Permission Item — Personnel list for border zone permission
            CreateReportVisibility(
                reportName: "App Border Zone Permission Item Report",
                displayName: "Serhet \u00DDaka Rugsatnama \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Border_Zone_Permission'"
            );

            // 23. App_Cancel_Visa_and_WP Item — Personnel list for visa + work permit cancellation
            CreateReportVisibility(
                reportName: "App Cancel Visa And WP Item Report",
                displayName: "Wiza we I\u015F Rugsat\u00E7ynamany \u00DDatyrmak \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Cancel_Visa_and_WP'"
            );

            // 24. App_Change_Inv Item — Personnel list for invitation change
            CreateReportVisibility(
                reportName: "App Change Inv Item Report",
                displayName: "\u00C7akylyk\u00FD \u00DC\u00FDtgetmek \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Change_Inv'"
            );

            // 19. App_Cancel_Inv_WP — Invitation and work permit cancellation letter to national Migration Service head
            CreateReportVisibility(
                reportName: "App Cancel Inv WP Report",
                displayName: "\u00C7akylyk we I\u015F Rugsat\u00E7ynamany \u00DDatyrmak \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Cancel_Inv_WP'"
            );

            // CRITICAL: Changes made within the ModuleUpdater must be committed to the database.
            ObjectSpace.CommitChanges();
        }

        private void CreateReportVisibility(string reportName, string displayName, Type targetType, string criteria)
        {
            // Try to find the existing rule by ReportName to avoid duplicates
            var visibility = ObjectSpace.FirstOrDefault<ReportVisibility>(v => v.ReportName == reportName)
                             ?? ObjectSpace.CreateObject<ReportVisibility>();
            
            visibility.ReportName = reportName;
            visibility.ReportDisplayName = displayName;
            visibility.TargetTypeFullName = targetType.FullName;
            visibility.VisibilityCriteria = criteria;
        }
    }
}
