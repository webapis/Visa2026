using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using System.Threading.Tasks;

namespace Visa2026.Module.Controllers
{
    public class ApplicationItemPdfController : ViewController
    {
        private SimpleAction generatePdfAction;

        public ApplicationItemPdfController()
        {
            TargetObjectType = typeof(ApplicationItem);
            TargetViewType = ViewType.Any;

            generatePdfAction = new SimpleAction(this, "GenerateApplicationPdf", "View");
            generatePdfAction.Caption = "Generate PDF";
            generatePdfAction.ImageName = "ExportToPDF";
            generatePdfAction.SelectionDependencyType = SelectionDependencyType.Independent;
            generatePdfAction.Execute += GeneratePdfAction_Execute;
        }

        private async void GeneratePdfAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var selectedItems = e.SelectedObjects?.OfType<ApplicationItem>().ToList()
                ?? new List<ApplicationItem>();

            // Fallback for DetailView invocation
            if (!selectedItems.Any() && e.CurrentObject is ApplicationItem currentItem)
                selectedItems.Add(currentItem);

            selectedItems = selectedItems
                .Where(i => i != null && i.Application != null && !i.IsDeleted)
                .Distinct()
                .ToList();

            if (!selectedItems.Any())
                return;

            // 1. Get Configuration
            var configuration = Application.ServiceProvider.GetRequiredService<IConfiguration>();
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

            // 2. Get Service
            var pdfFillerService = Application.ServiceProvider.GetRequiredService<IPdfFormFillerService>();
            var fileDownloader = Application.ServiceProvider.GetRequiredService<IFileDownloader>();

            // 3. Prepare mappings once
            var mappings = PdfMappingHelper.GetMappings(View.ObjectSpace);

            try
            {
                // 4. Generate output:
                // - single selection: download one PDF
                // - multi selection: download a ZIP containing one PDF per item (no merge)
                if (selectedItems.Count == 1)
                {
                    var only = selectedItems[0];
                    var data = new Dictionary<string, object>();
                    PdfMappingHelper.MapApplicationData(data, only.Application, only, View.ObjectSpace, null, mappings);

                    string personName = only.Person != null ? $"{only.Person.FirstName}_{only.Person.LastName}" : "Unknown";
                    string outputFileName = $"Visa_{personName}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                    using var ms = new MemoryStream();
                    pdfFillerService.FillForm(templatePath, ms, data);
                    ms.Position = 0;
                    await fileDownloader.DownloadAsync(outputFileName, ms);

                    Application.ShowViewStrategy.ShowMessage(
                        $"PDF generated and downloaded: {outputFileName}",
                        InformationType.Success);
                    return;
                }

                string zipName = $"Visa_Selected_{selectedItems.Count}_{DateTime.Now:yyyyMMddHHmmss}.zip";
                using (var zipStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        int idx = 1;
                        foreach (var item in selectedItems)
                        {
                            var data = new Dictionary<string, object>();
                            PdfMappingHelper.MapApplicationData(data, item.Application, item, View.ObjectSpace, null, mappings);

                            string personName = item.Person != null ? $"{item.Person.FirstName}_{item.Person.LastName}" : "Unknown";
                            string entryName = $"{idx:00}_Visa_{personName}.pdf";
                            idx++;

                            using var pdfStream = new MemoryStream();
                            pdfFillerService.FillForm(templatePath, pdfStream, data);
                            pdfStream.Position = 0;

                            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                            using var entryStream = entry.Open();
                            pdfStream.CopyTo(entryStream);
                        }
                    }

                    zipStream.Position = 0;
                    await fileDownloader.DownloadAsync(zipName, zipStream);
                }

                Application.ShowViewStrategy.ShowMessage(
                    $"ZIP generated and downloaded: {zipName}",
                    InformationType.Success);
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException($"Error generating PDF: {ex.Message}");
            }
        }

    }
}