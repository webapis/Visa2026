using System;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Controller responsible for invalidating the Report Visibility cache 
    /// whenever a ReportVisibility configuration record is changed or removed.
    /// </summary>
    public class ReportVisibilityCacheController : ViewController
    {
        public ReportVisibilityCacheController()
        {
            // Target only the ReportVisibility configuration object
            TargetObjectType = typeof(ReportVisibility);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            // Subscribe to the Committed event to catch any save or delete operation
            ObjectSpace.Committed += ObjectSpace_Committed;
        }

        private void ObjectSpace_Committed(object sender, EventArgs e)
        {
            var cacheService = Application.ServiceProvider.GetService<IReportVisibilityCacheService>();
            // Clear the cache so the next request fetches fresh rules from the DB
            cacheService?.ClearCache();
            Application.ShowViewStrategy.ShowMessage("Report Visibility cache refreshed successfully.", InformationType.Success);
        }

        protected override void OnDeactivated()
        {
            ObjectSpace.Committed -= ObjectSpace_Committed;
            base.OnDeactivated();
        }
    }
}