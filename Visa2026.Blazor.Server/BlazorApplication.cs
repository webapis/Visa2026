using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server
{
    public class Visa2026BlazorApplication : BlazorApplication
    {
        public Visa2026BlazorApplication()
        {
            ApplicationName = "Visa2026";
            CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
            DatabaseVersionMismatch += Visa2026BlazorApplication_DatabaseVersionMismatch;
        }
        protected override void OnSetupStarted()
        {
            base.OnSetupStarted();
            // DatabaseUpdateMode is set in Startup.AddBuildStep (UpdateOldDatabase by default for fast restarts).
        }
        void Visa2026BlazorApplication_DatabaseVersionMismatch(object sender, DatabaseVersionMismatchEventArgs e)
        {
            try
            {
                e.Updater.Update();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == -2 || ex.Message.Contains("Timeout"))
            {
                // Stale MSBuild processes (from EF scaffolding during build) can hold SQL connections
                // that block the startup schema check. Kill them and retry once.
                foreach (var p in System.Diagnostics.Process.GetProcessesByName("MSBuild"))
                {
                    try { p.Kill(entireProcessTree: true); } catch { }
                }
                System.Threading.Thread.Sleep(2000);
                e.Updater.Update();
            }
            e.Handled = true;
        }
    }
}
