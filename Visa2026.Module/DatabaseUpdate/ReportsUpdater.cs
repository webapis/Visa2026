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
            AddPredefinedReport<ApplicationItemReport>("ApplicationItem Report", typeof(ApplicationItem), isInplaceReport: true);
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
