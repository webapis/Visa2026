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
            AddPredefinedReport<ApplicationReport>("Application Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<ApplicationReport>("Application For Employee's Visa Extension Report", typeof(Visa2026.Module.BusinessObjects.Application), isInplaceReport: true);
            AddPredefinedReport<ApplicationItemReport>("ApplicationItem Report", typeof(ApplicationItem), isInplaceReport: true);
           // AddPredefinedReport<EmployeeContractReport>("Employee Contract", typeof(EmployeeContract), isInplaceReport: true);
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            // Seed the visibility rule for the Visa Extension report
            CreateReportVisibility(
                reportName: "Application For Employee's Visa Extension Report",
                displayName: "Application For Employee's Visa Extension",
                targetType: typeof(Visa2026.Module.BusinessObjects.Application),
                criteria: "[ApplicationType.Name] In ('Wiza we Iş Rugsatnamasyny Uzaltmak (IŞG)', 'Another Application Type Name', 'Third Type')"
            );
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