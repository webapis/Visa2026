using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Invitation + Work Permit cancellation letter for App_Cancel_Inv_WP (Group D).
    /// Template: Visa2026.Module.Resources.App_Cancel_Inv_WP_Letter.docx
    /// </summary>
    public class AppCancelInvWPLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Cancel_Inv_WP" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"CakylykIsRugsatYatyrylmak_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

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
                ["CancelInvCount"]                     = application.CancelInvCount,
                ["CancelInvCountText"]                 = application.CancelInvCountText              ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = application.Application_CompanyHead_PositionTm  ?? string.Empty,
                ["Application_CompanyHead_FullName"]   = application.Application_CompanyHead_FullName           ?? string.Empty,
            };

            var asm = typeof(AppCancelInvWPLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Cancel_Inv_WP_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
