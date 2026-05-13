using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Exit visa request letter for App_Exit_Visa (Group A — Ministry recipient).
    /// Template: Visa2026.Module.Resources.App_Exit_Visa_Letter.docx
    /// </summary>
    public class AppExitVisaLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Exit_Visa" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"CykysWizasy_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]                    = application.FullApplicationNumber                        ?? string.Empty,
                ["ApplicationDate"]                          = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["ProjectContract_Ministry_RecipientBlock"]  = application.ProjectContract_Ministry_RecipientBlock      ?? string.Empty,
                ["Urgency_NameTm"]                           = application.Urgency_NameTm                               ?? string.Empty,
                ["ProjectContract_Ministry_FormOfAddress"]   = application.ProjectContract_Ministry_FormOfAddress       ?? string.Empty,
                ["ProjectContract_Description"]              = application.ProjectContract_Description                  ?? string.Empty,
                ["Company_Name"]                             = application.Company?.Name                                ?? string.Empty,
                ["TotalPersonCount"]                         = application.TotalPersonCount,
                ["TotalPersonCountText"]                     = application.TotalPersonCountText                         ?? string.Empty,
                ["VisaPeriod_NameTm"]                        = application.VisaPeriod_NameTm                            ?? string.Empty,
                ["Application_CompanyHead_PositionTm"]       = application.CompanyHead?.Position?.NameTm               ?? string.Empty,
                ["Application_CompanyHead_FullName"]         = application.CompanyHead?.FullName                       ?? string.Empty,
            };

            var asm = typeof(AppExitVisaLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Exit_Visa_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
