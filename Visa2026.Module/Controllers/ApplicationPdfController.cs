using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    public class ApplicationPdfController : ViewController<DetailView>
    {
        private SimpleAction downloadAllAction;

        public ApplicationPdfController()
        {
            TargetObjectType = typeof(Application);
            TargetViewType = ViewType.DetailView;

            downloadAllAction = new SimpleAction(this, "DownloadAllApplicationItemsAsPdf", "View");
            downloadAllAction.Caption = "Download All as PDF";
            downloadAllAction.ImageName = "ExportToPDF";
            downloadAllAction.Execute += DownloadAllAction_Execute;
        }

        private async void DownloadAllAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var application = (Application)e.CurrentObject;
            if (application == null || !application.ApplicationItems.Any(i => !i.IsDeleted))
            {
                Application.ShowViewStrategy.ShowMessage("There are no active items to generate a PDF for.", InformationType.Warning);
                return;
            }

            var configuration = Application.ServiceProvider.GetRequiredService<IConfiguration>();
            var pdfFillerService = Application.ServiceProvider.GetRequiredService<IPdfFormFillerService>();
            var fileDownloader = Application.ServiceProvider.GetRequiredService<IFileDownloader>();

            string relativeTemplatePath = configuration["PdfSettings:TemplatePath"];

            if (string.IsNullOrEmpty(relativeTemplatePath))
            {
                throw new UserFriendlyException("PDF Template Path is not configured. Please check 'PdfSettings:TemplatePath' in appsettings.json.");
            }

            var templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeTemplatePath));
            if (!File.Exists(templatePath))
            {
                // Try embedded resource from the Module assembly
                var asm = typeof(ApplicationItemPdfController).Assembly; // or typeof(SomeModuleType).Assembly
                string resourceName = "Visa2026.Module.Resources.Visa_Application_TM_QR_08.pdf";
                using var resStream = asm.GetManifestResourceStream(resourceName);
                if (resStream == null) throw new UserFriendlyException($"PDF template not found as file or embedded resource: {relativeTemplatePath}");
                // write temp file
                string tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
                using (var fs = File.Create(tmp)) resStream.CopyTo(fs);
                templatePath = tmp;
            }

            var individualPdfStreams = new List<MemoryStream>();

            try
            {
                // Fetch dynamic mappings once for the batch
                var mappings = PdfMappingHelper.GetMappings(View.ObjectSpace);

                foreach (var item in application.ApplicationItems.Where(i => !i.IsDeleted))
                {
                    var data = new Dictionary<string, object>();
                    PdfMappingHelper.MapApplicationData(data, application, item, View.ObjectSpace, null, mappings);

                    var memoryStream = new MemoryStream();
                    pdfFillerService.FillForm(templatePath, memoryStream, data);
                    individualPdfStreams.Add(memoryStream);
                }

                if (individualPdfStreams.Any())
                {
                    using (var mergedStream = new MemoryStream())
                    {
                        individualPdfStreams.ForEach(s => s.Position = 0);
                        pdfFillerService.MergePdfs(individualPdfStreams.ToArray(), mergedStream);

                        string outputFileName = $"Application_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                        mergedStream.Position = 0;
                        await fileDownloader.DownloadAsync(outputFileName, mergedStream);

                        Application.ShowViewStrategy.ShowMessage($"PDF for {individualPdfStreams.Count} items generated and downloaded.", InformationType.Success);
                    }
                }
            }
            finally
            {
                foreach (var stream in individualPdfStreams)
                {
                    stream.Dispose();
                }
            }
        }
    }
}