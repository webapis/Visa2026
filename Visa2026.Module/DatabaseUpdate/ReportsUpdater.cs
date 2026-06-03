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
            // Legacy Reports V2 (XtraReports) — disabled for now. Word reports / user templates are used instead.
            // Uncomment AddPredefinedReport blocks below to re-register on deploy (existing ReportDataV2 rows may remain in DB).
            /*
            AddPredefinedReport<ApplicationVisaExtEmp>("Application For Employee's Visa Extension Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);

            AddPredefinedReport<AppInvItemReport>("App Inv Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppInvAndWPItemReport>("App Inv And WP Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppInvAndWPBorcnamaItemReport>("Borcnama Item", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppInvFMItemReport>("App Inv FM Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppVisaExtFMItemReport>("App Visa Ext FM Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppVisaAndWPExtItemReport>("App Visa And WP Ext Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppAdditionalWPLocationItemReport>("App Additional WP Location Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppCancelVisaItemReport>("App Cancel Visa Item Report", typeof(ApplicationItem), isInplaceReport: true);

            AddPredefinedReport<RegistrationListReport>("Registration List Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<RegistrationForm16Report>("Registration Form 16 Report", typeof(ApplicationItem), isInplaceReport: true);
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
            AddPredefinedReport<AppChangePassportItemReport>("App Change Passport Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppBusinessTripArrivalReport>("App Business Trip Arrival Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppBusinessTripDepartureReport>("App Business Trip Departure Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppBusinessTripSanawReport>("App Business Trip Sanaw Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppExitVisaReport>("App Exit Visa Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<AppExitVisaItemReport>("App Exit Visa Item Report", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<AppLaborContractItemReportV2>("App Labor Contract Item Report V2", typeof(ApplicationItem), isInplaceReport: true);
            AddPredefinedReport<WorkPermitListReport>("Work Permit List Report", typeof(WorkPermitItem), isInplaceReport: true);
            */
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            // Legacy ReportVisibility seeding — disabled while predefined reports are commented out above.
            /*
            CreateReportVisibility(
                reportName: "Application For Employee's Visa Extension Report",
                displayName: "Application For Employee's Visa Extension",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] In ('Wiza we Iş Rugsatnamasyny Uzaltmak (IŞG)', 'Another Application Type Name')"
            );

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
                reportName: "Borcnama Item",
                displayName: "Borcnama Item",
                targetType: typeof(ApplicationItem),
                criteria: ""
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

            CreateReportVisibility(
                reportName: "Registration List Report",
                displayName: "Hasaba Almak Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.ShowRegistrations]"
            );

            CreateReportVisibility(
                reportName: "Registration Form 16 Report",
                displayName: "Bellige Alyş Namasy (16)",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.ShowRegistrations]"
            );

            CreateReportVisibility(
                reportName: "App Reg Check In Report",
                displayName: "Hasaba Almak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_In'"
            );

            CreateReportVisibility(
                reportName: "App Inv Report",
                displayName: "Çakylyk — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Inv'"
            );

            CreateReportVisibility(
                reportName: "App Reg Ext Report",
                displayName: "Hasaba Alyş Möhletini Uzaltmak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_ext'"
            );

            CreateReportVisibility(
                reportName: "App Reg Check Out Internal Report",
                displayName: "Hasapdan Çykarmak (Içerki) — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_Out_Internal'"
            );

            CreateReportVisibility(
                reportName: "App Reg Check In Internal Report",
                displayName: "Hasaba Almak (Içerki) — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_In_Internal'"
            );

            CreateReportVisibility(
                reportName: "App Reg Check Out Report",
                displayName: "Hasapdan Çykarmak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Check_Out'"
            );

            CreateReportVisibility(
                reportName: "App Inv And WP Report",
                displayName: "Çakylyk we Iş Rugsatnamasy — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Inv_And_WP'"
            );

            CreateReportVisibility(
                reportName: "App Inv FM Report",
                displayName: "Çakylyk — FM Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Inv_FM'"
            );

            CreateReportVisibility(
                reportName: "App Reg Info Change Address Report",
                displayName: "Salgy Üýtgemegi — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Info_Change_Address'"
            );

            CreateReportVisibility(
                reportName: "App Reg Info Change Passport Report",
                displayName: "Pasport Üýtgemegi — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Reg_Info_Change_Passport'"
            );

            CreateReportVisibility(
                reportName: "App Cancel Visa Report",
                displayName: "Wizany Ýatyrmak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Cancel_Visa'"
            );

            CreateReportVisibility(
                reportName: "App Visa And WP Ext Report",
                displayName: "Wiza we Iş Rugsatnamasyny Uzaltmak — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Visa_and_WP_Ext'"
            );

            CreateReportVisibility(
                reportName: "App Visa Ext FM Report",
                displayName: "Wiza Möhletini Uzaltmak FM — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Visa_Ext_FM'"
            );

            CreateReportVisibility(
                reportName: "App Additional WP Location Report",
                displayName: "Goşmaça hereket çägi — Ýüztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Additional_WP_location'"
            );

            CreateReportVisibility(
                reportName: "App Change Inv Report",
                displayName: "\u00C7akylygy \u00FC\u00FDtgetmek \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Change_Inv'"
            );

            CreateReportVisibility(
                reportName: "App Change Passport Report",
                displayName: "Wizan\u00FD Ge\u00E7irmek \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Change_Passport'"
            );

            CreateReportVisibility(
                reportName: "App Cancel Visa And WP Report",
                displayName: "Wiza we I\u015F Rugsat\u00E7ynamany \u00DDatyrmak \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Cancel_Visa_and_WP'"
            );

            CreateReportVisibility(
                reportName: "App Border Zone Permission Report",
                displayName: "Serhet \u00DDaka Rugsatnama \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Border_Zone_Permission'"
            );

            CreateReportVisibility(
                reportName: "App Cancel Inv WP Item Report",
                displayName: "\u00C7akylyk we I\u015F Rugsat\u00E7ynamany \u00DDatyrmak \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Cancel_Inv_WP'"
            );

            CreateReportVisibility(
                reportName: "App Border Zone Permission Item Report",
                displayName: "Serhet \u00DDaka Rugsatnama \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Border_Zone_Permission'"
            );

            CreateReportVisibility(
                reportName: "App Cancel Visa And WP Item Report",
                displayName: "Wiza we I\u015F Rugsat\u00E7ynamany \u00DDatyrmak \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Cancel_Visa_and_WP'"
            );

            CreateReportVisibility(
                reportName: "App Change Inv Item Report",
                displayName: "\u00C7akylyk\u00FD \u00DC\u00FDtgetmek \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Change_Inv'"
            );

            CreateReportVisibility(
                reportName: "App Change Passport Item Report",
                displayName: "Pasport \u00DC\u00FDtgemegi \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Change_Passport'"
            );

            CreateReportVisibility(
                reportName: "App Cancel Inv WP Report",
                displayName: "\u00C7akylyk we I\u015F Rugsat\u00E7ynamany \u00DDatyrmak \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Cancel_Inv_WP'"
            );

            CreateReportVisibility(
                reportName: "App Business Trip Arrival Report",
                displayName: "Iş Sapary — Geliş",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Business_Trip_Arrival'"
            );

            CreateReportVisibility(
                reportName: "App Business Trip Departure Report",
                displayName: "Iş Sapary — Gidiş",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Business_Trip_Departure'"
            );

            CreateReportVisibility(
                reportName: "App Business Trip Sanaw Report",
                displayName: "Iş Sapary — Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] In ('App_Business_Trip_Arrival', 'App_Business_Trip_Departure')"
            );

            CreateReportVisibility(
                reportName: "App Exit Visa Item Report",
                displayName: "\u00C7yk\u00FD\u015F Wiza \u2014 Sanawy",
                targetType: typeof(ApplicationItem),
                criteria: "[Application.ApplicationType.Name] = 'App_Exit_Visa'"
            );

            CreateReportVisibility(
                reportName: "App Labor Contract Item Report",
                displayName: "Z\u00E4hmet \u015Fertnamasy \u2014 \u015Eahsy",
                targetType: typeof(ApplicationItem),
                criteria: ""
            );

            CreateReportVisibility(
                reportName: "App Exit Visa Report",
                displayName: "\u00C7yk\u00FD\u015F Wiza \u2014 \u00DD\u00FCztutma",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] = 'App_Exit_Visa'"
            );

            CreateReportVisibility(
                reportName: "Work Permit List Report",
                displayName: "I\u015F Rugsat\u00E7ynama \u2014 Sanawy",
                targetType: typeof(WorkPermitItem),
                criteria: ""
            );
            */
        }

        private void CreateReportVisibility(string reportName, string displayName, Type targetType, string criteria)
        {
            var visibility = ObjectSpace.FirstOrDefault<ReportVisibility>(v => v.ReportName == reportName)
                             ?? ObjectSpace.CreateObject<ReportVisibility>();

            visibility.ReportName = reportName;
            visibility.ReportDisplayName = displayName;
            visibility.TargetTypeFullName = targetType.FullName;
            visibility.VisibilityCriteria = criteria;
        }
    }
}
