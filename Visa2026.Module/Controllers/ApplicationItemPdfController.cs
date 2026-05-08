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
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DevExpress.ExpressApp.Security;

namespace Visa2026.Module.Controllers
{
    public class ApplicationItemPdfController : ViewController
    {
        private SimpleAction generatePdfAction;
        private SimpleAction generatePdfBatchAction;
        private SimpleAction myPdfJobsAction;

        public ApplicationItemPdfController()
        {
            TargetObjectType = typeof(ApplicationItem);
            TargetViewType = ViewType.Any;

            generatePdfAction = new SimpleAction(this, "GenerateApplicationPdf", "View");
            generatePdfAction.Caption = "Generate PDF";
            generatePdfAction.ImageName = "ExportToPDF";
            generatePdfAction.SelectionDependencyType = SelectionDependencyType.Independent;
            generatePdfAction.Execute += GeneratePdfAction_Execute;

            generatePdfBatchAction = new SimpleAction(this, "GenerateApplicationPdfBatch", "View");
            generatePdfBatchAction.Caption = "Generate PDF (Background)";
            generatePdfBatchAction.ImageName = "Action_Workflow";
            generatePdfBatchAction.SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects;
            generatePdfBatchAction.Execute += GeneratePdfBatchAction_Execute;

            myPdfJobsAction = new SimpleAction(this, "ShowMyPdfBatches", "View");
            myPdfJobsAction.Caption = "My PDF Jobs";
            myPdfJobsAction.ImageName = "BO_List";
            myPdfJobsAction.SelectionDependencyType = SelectionDependencyType.Independent;
            myPdfJobsAction.Execute += MyPdfJobsAction_Execute;
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
            var logger = Application.ServiceProvider.GetService<ILogger<ApplicationItemPdfController>>();

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
                string zipFolder = Path.GetFileNameWithoutExtension(zipName);
                // For multi-selection, avoid buffering the whole ZIP in memory (can be large and cause high RAM usage).
                // Write to a temp file, then stream it to the downloader.
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"visa_{Guid.NewGuid():N}.zip");
                try
                {
                    using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 64 * 1024, FileOptions.SequentialScan))
                    using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        int idx = 1;
                        foreach (var item in selectedItems)
                        {
                            var data = new Dictionary<string, object>();
                            PdfMappingHelper.MapApplicationData(data, item.Application, item, View.ObjectSpace, null, mappings);

                            string personName = item.Person != null ? $"{item.Person.FirstName}_{item.Person.LastName}" : "Unknown";
                            string entryName = $"{zipFolder}/{idx:00}_Visa_{personName}.pdf";
                            idx++;

                            using var pdfStream = new MemoryStream();
                            pdfFillerService.FillForm(templatePath, pdfStream, data);
                            pdfStream.Position = 0;

                            var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                            using var entryStream = entry.Open();
                            pdfStream.CopyTo(entryStream);
                        }
                    }

                    using var readStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
                    await fileDownloader.DownloadAsync(zipName, readStream);
                }
                finally
                {
                    try { File.Delete(tempZipPath); } catch { }
                }

                Application.ShowViewStrategy.ShowMessage(
                    $"ZIP generated and downloaded: {zipName}",
                    InformationType.Success);
            }
            catch (OperationCanceledException)
            {
                // Blazor circuits/requests can cancel long-running work; report cleanly instead of crashing the circuit.
                logger?.LogWarning("PDF generation canceled. SelectedItems={Count}", selectedItems.Count);
                Application.ShowViewStrategy.ShowMessage(
                    "PDF generation was canceled (likely due to timeout or connection loss). Try fewer items per batch.",
                    InformationType.Warning);
            }
            catch (Exception ex)
            {
                // Never throw from an async void action handler; it can crash the Blazor circuit.
                logger?.LogError(ex, "Error generating PDF/ZIP. SelectedItems={Count}", selectedItems.Count);
                Application.ShowViewStrategy.ShowMessage(
                    $"Error generating PDF: {ex.Message}",
                    InformationType.Error);
            }
        }

        private async void GeneratePdfBatchAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var selectedItems = e.SelectedObjects?.OfType<ApplicationItem>().ToList()
                ?? new List<ApplicationItem>();

            selectedItems = selectedItems
                .Where(i => i != null && i.Application != null && !i.IsDeleted)
                .Distinct()
                .ToList();

            if (selectedItems.Count < 2)
            {
                Application.ShowViewStrategy.ShowMessage(
                    "Select at least 2 items to run background PDF generation.",
                    InformationType.Warning);
                return;
            }

            var keys = selectedItems
                .Select(i => View.ObjectSpace.GetKeyValue(i))
                .Where(k => k != null)
                .ToList();

            if (keys.Count < 2)
                return;

            var keyType = keys.First().GetType();
            var keyStrings = keys.Select(k => Convert.ToString(k, System.Globalization.CultureInfo.InvariantCulture)).ToList();

            var batch = View.ObjectSpace.CreateObject<PdfGenerationBatch>();
            batch.RequestedBy = SecuritySystem.CurrentUserName;
            batch.ItemKeyType = keyType.AssemblyQualifiedName ?? keyType.FullName ?? keyType.Name;
            batch.ItemKeysJson = JsonSerializer.Serialize(keyStrings);
            batch.TotalItems = keyStrings.Count;
            batch.Status = PdfGenerationBatchStatus.Queued;

            View.ObjectSpace.CommitChanges();

            Application.ShowViewStrategy.ShowMessage(
                $"PDF generation started in background ({batch.TotalItems} item(s)). Open 'My PDF Jobs' to download when ready.",
                InformationType.Success);
        }

        private void MyPdfJobsAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var os = Application.CreateObjectSpace(typeof(PdfGenerationBatch));
            var listView = Application.CreateListView(os, typeof(PdfGenerationBatch), true);
            listView.CollectionSource.Criteria["RequestedBy"] = new DevExpress.Data.Filtering.BinaryOperator(
                nameof(PdfGenerationBatch.RequestedBy),
                SecuritySystem.CurrentUserName);

            var svp = new ShowViewParameters(listView)
            {
                TargetWindow = TargetWindow.NewModalWindow
            };
            Application.ShowViewStrategy.ShowView(svp, new ShowViewSource(Frame, null));
        }

    }
}