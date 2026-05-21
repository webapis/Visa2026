using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using Visa2026.Blazor.Server.Localization;
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
            CustomizeLanguage += Visa2026BlazorApplication_CustomizeLanguage;
        }

        void Visa2026BlazorApplication_CustomizeLanguage(object sender, CustomizeLanguageEventArgs e)
        {
            // Align XAF Application Model language with ASP.NET request culture (cookie / default en-US).
            if (VisaLocalization.TryNormalizeCulture(
                    System.Globalization.CultureInfo.CurrentUICulture.Name,
                    out string fromRequest))
            {
                e.LanguageName = fromRequest;
                return;
            }

            if (VisaLocalization.TryNormalizeCulture(e.LanguageName, out string fromXaf))
            {
                e.LanguageName = fromXaf;
                return;
            }

            e.LanguageName = VisaLocalization.DefaultCultureName;
        }
        protected override void OnSetupStarted()
        {
            base.OnSetupStarted();
            // DatabaseUpdateMode is set in Startup.AddBuildStep (UpdateOldDatabase by default; FORCE_XAF_DB_UPDATE for one-shot full update).
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
