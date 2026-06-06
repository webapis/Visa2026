using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ReportsV2;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>
    /// Legacy Reports V2 (XtraReports) registration removed — Resminamalar uses user report templates only.
    /// </summary>
    public class ReportsUpdater : PredefinedReportsUpdater
    {
        public ReportsUpdater(XafApplication application, IObjectSpace objectSpace, Version currentDBVersion) :
            base(application, objectSpace, currentDBVersion)
        {
        }
    }
}
