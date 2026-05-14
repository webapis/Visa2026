using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Single "Resminamalar" button on the Application detail view.
    /// Discovers all IWordReportDefinition registrations, filters by ApplicationType and data,
    /// generates applicable reports, and downloads them as a single .zip (or plain .docx if only one).
    /// </summary>
    public class WordReportsController : ViewController<DetailView>
    {
        private SimpleAction resminamalarAction;

        public WordReportsController()
        {
            TargetObjectType = typeof(Application);
            TargetViewType = ViewType.DetailView;

            resminamalarAction = new SimpleAction(this, "GenerateWordReports", "Reports");
            resminamalarAction.Caption = "Resminamalar";
            resminamalarAction.ImageName = "BO_FileAttachment";
            resminamalarAction.Execute += ResminamalarAction_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            UpdateActionState();
            View.CurrentObjectChanged += (_, _) => UpdateActionState();
        }

        private void UpdateActionState()
        {
            var application = View?.CurrentObject as Application;
            if (application == null)
            {
                resminamalarAction.Enabled["NoApplicableReports"] = false;
                return;
            }

            // Check system reports
            var definitions = Application.ServiceProvider
                .GetServices<IWordReportDefinition>();
            bool anySystem = definitions.Any(d => IsDefinitionApplicable(d, application));

            // Check user reports
            var visibilityService = Application.ServiceProvider.GetService<IUserReportVisibilityService>();
            bool anyUser = false;
            if (visibilityService != null)
            {
                var userTemplates = ObjectSpace.GetObjects<UserReportTemplate>().Where(t => t.IsActive);
                anyUser = userTemplates.Any(t => visibilityService.IsTemplateVisible(t, application));
            }

            resminamalarAction.Enabled["NoApplicableReports"] = anySystem || anyUser;
        }

        private async void ResminamalarAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var application = (Application)e.CurrentObject;

            var wordService = Application.ServiceProvider.GetRequiredService<IWordFormFillerService>();
            var fileDownloader = Application.ServiceProvider.GetRequiredService<IFileDownloader>();
            var userReportGenerator = Application.ServiceProvider.GetService<IUserReportGenerator>();
            var visibilityService = Application.ServiceProvider.GetService<IUserReportVisibilityService>();

            // Get system reports
            var definitions = Application.ServiceProvider
                .GetServices<IWordReportDefinition>()
                .Where(d => IsDefinitionApplicable(d, application))
                .ToList();

            // Get user reports
            var userTemplates = new List<UserReportTemplate>();
            if (visibilityService != null)
            {
                userTemplates = ObjectSpace.GetObjects<UserReportTemplate>()
                    .Where(t => t.IsActive && visibilityService.IsTemplateVisible(t, application))
                    .ToList();
            }

            if (definitions.Count == 0 && userTemplates.Count == 0)
            {
                Application.ShowViewStrategy.ShowMessage(
                    "No applicable Word reports for this application type.",
                    InformationType.Warning);
                return;
            }

            // Generate each report into a named MemoryStream
            var results = new List<(string FileName, MemoryStream Stream)>();

            // Generate system reports
            foreach (var def in definitions)
            {
                var ms = new MemoryStream();
                await def.GenerateAsync(application, wordService, ms);
                ms.Position = 0;
                results.Add((def.GetFileName(application), ms));
            }

            // Generate user reports
            if (userReportGenerator != null)
            {
                foreach (var template in userTemplates)
                {
                    var ms = new MemoryStream();
                    await userReportGenerator.GenerateAsync(template, application, ms);
                    ms.Position = 0;
                    var fileName = $"{template.TemplateName}_{application.FullApplicationNumber}.docx";
                    results.Add((fileName, ms));
                }
            }

            if (results.Count == 1)
            {
                // Single report — download as plain .docx
                var (fileName, stream) = results[0];
                await fileDownloader.DownloadAsync(fileName, stream);
            }
            else
            {
                // Multiple reports — zip them all
                using var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var (fileName, stream) in results)
                    {
                        var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                        using var entryStream = entry.Open();
                        stream.CopyTo(entryStream);
                    }
                }

                foreach (var (_, stream) in results) stream.Dispose();

                string zipName = $"Resminamalar_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.zip";
                zipStream.Position = 0;
                await fileDownloader.DownloadAsync(zipName, zipStream);
            }

            Application.ShowViewStrategy.ShowMessage(
                $"{results.Count} Word report(s) generated.",
                InformationType.Success);
        }

        private static bool IsDefinitionApplicable(IWordReportDefinition def, Application application)
        {
            var names = def.ApplicableApplicationTypeNames;
            if (names != null && names.Length > 0)
            {
                var appTypeName = application.ApplicationType?.Name;
                if (appTypeName == null || !names.Contains(appTypeName, StringComparer.Ordinal))
                    return false;
            }

            return def.IsApplicable(application);
        }
    }
}
