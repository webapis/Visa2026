using System;
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
            AddPredefinedReport<ApplicationItemReport>("ApplicationItem Report", typeof(ApplicationItem), isInplaceReport: true);
        }
    }
}