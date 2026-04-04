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
            AddPredefinedReport<RegistrationListReport>("Registration List Report", typeof(Registration), isInplaceReport: true);
            AddPredefinedReport<AppRegCheckInReport>("App Reg Check In Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
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

            // 2. Rule for the "General Application Report": Visible for all applications where status is not 'Draft'
            CreateReportVisibility(
                reportName: "Application Report",
                displayName: "General Application Report",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[CurrentState.State] <> 'Draft'"
            );

            // 3. Rule for "ApplicationItem Report": Always visible for ApplicationItems
            CreateReportVisibility(
                reportName: "ApplicationItem Report",
                displayName: "Application Item Details",
                targetType: typeof(ApplicationItem),
                criteria: "" // Empty criteria means it's always visible for this target type
            );

            // 4. Rule for "Application Letter Report": Visible for all applications except Draft
            CreateReportVisibility(
                reportName: "Application Letter Report",
                displayName: "Application Processing Letter",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: null
            );

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
