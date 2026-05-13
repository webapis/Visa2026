using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Visa transfer (old→new passport) letter for App_Change_Passport (Group D).
    /// Template: Visa2026.Module.Resources.App_Change_Passport_Letter.docx
    /// </summary>
    public class AppChangePassportLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Change_Passport" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"WizaGecirilmek_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]              = application.FullApplicationNumber          ?? string.Empty,
                ["ApplicationDate"]                    = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["TotalPersonCount"]                   = application.TotalPersonCount,
                ["TotalPersonCountText"]               = application.TotalPersonCountText            ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = application.CompanyHead?.Position?.NameTm  ?? string.Empty,
                ["Application_CompanyHead_FullName"]   = application.CompanyHead?.FullName           ?? string.Empty,
            };

            var asm = typeof(AppChangePassportLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Change_Passport_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
