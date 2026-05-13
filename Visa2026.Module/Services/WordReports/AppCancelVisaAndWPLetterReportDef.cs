using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Visa + Work Permit cancellation letter for App_Cancel_Visa_and_WP (Group D).
    /// Template: Visa2026.Module.Resources.App_Cancel_Visa_And_WP_Letter.docx
    /// </summary>
    public class AppCancelVisaAndWPLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Cancel_Visa_and_WP" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"WizaIsRugsatYatyrylmak_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]              = application.FullApplicationNumber          ?? string.Empty,
                ["ApplicationDate"]                    = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["CancelPersonCount"]                  = application.CancelPersonCount,
                ["CancelPersonCountText"]              = application.CancelPersonCountText           ?? string.Empty,
                ["CancelWPCount"]                      = application.CancelWPCount,
                ["CancelWPCountText"]                  = application.CancelWPCountText               ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = application.CompanyHead?.Position?.NameTm  ?? string.Empty,
                ["Application_CompanyHead_FullName"]   = application.CompanyHead?.FullName           ?? string.Empty,
            };

            var asm = typeof(AppCancelVisaAndWPLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Cancel_Visa_And_WP_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
