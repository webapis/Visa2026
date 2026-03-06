using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
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
            generatePdfAction.SelectionDependencyType = SelectionDependencyType.RequireSingleObject;
            generatePdfAction.Execute += GeneratePdfAction_Execute;
        }

        private async void GeneratePdfAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var applicationItem = (ApplicationItem)e.CurrentObject;
            if (applicationItem == null || applicationItem.Application == null) return;

            // 1. Get Configuration
            var configuration = Application.ServiceProvider.GetRequiredService<IConfiguration>();
            string relativeTemplatePath = configuration["PdfSettings:TemplatePath"];

            if (string.IsNullOrEmpty(relativeTemplatePath))
            {
                throw new UserFriendlyException("PDF Template Path is not configured. Please check 'PdfSettings:TemplatePath' in appsettings.json.");
            }

            string templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeTemplatePath));

            if (!File.Exists(templatePath))
            {
                throw new UserFriendlyException($"The PDF template file was not found at the configured path: {templatePath}. Ensure the file's 'Copy to Output Directory' property is set to 'Copy if newer' or 'Copy always'.");
            }

            // 2. Get Service
            var pdfFillerService = Application.ServiceProvider.GetRequiredService<IPdfFormFillerService>();

            // 3. Prepare Data
            var data = new Dictionary<string, object>();
            PdfMappingHelper.MapApplicationData(data, applicationItem.Application, applicationItem);

            // 4. Generate PDF
            string personName = applicationItem.Person != null ? $"{applicationItem.Person.FirstName}_{applicationItem.Person.LastName}" : "Unknown";
            string outputFileName = $"Visa_{personName}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            try 
            {
                using (var memoryStream = new MemoryStream())
                {
                    pdfFillerService.FillForm(templatePath, memoryStream, data);
                    
                    // Reset stream position for reading
                    memoryStream.Position = 0;

                    var fileDownloader = Application.ServiceProvider.GetRequiredService<IFileDownloader>();
                    await fileDownloader.DownloadAsync(outputFileName, memoryStream);
                }
                
                Application.ShowViewStrategy.ShowMessage($"PDF Generated and downloaded: {outputFileName}", InformationType.Success);
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException($"Error generating PDF: {ex.Message}");
            }
        }

    }
}