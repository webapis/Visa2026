using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Address-change re-registration letter for App_Reg_Info_Change_Address.
    /// "... ýaşaýan salgysyny çalyşandygy sebäpli täze öý salgysyna hasaba almagyňyzy Sizden haýyş edýäris."
    /// Template: Visa2026.Module.Resources.App_Reg_Info_Change_Address_Letter.docx
    /// </summary>
    public class AppRegInfoChangeAddressLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Reg_Info_Change_Address" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"SalgyUytgemek_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]              = application.FullApplicationNumber          ?? string.Empty,
                ["ApplicationDate"]                    = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["MigrationService_NameTm"]            = application.MigrationService_NameTm        ?? string.Empty,
                ["TotalPersonCount"]                   = application.TotalPersonCount,
                ["TotalPersonCountText"]               = application.TotalPersonCountText            ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = application.CompanyHead?.Position?.NameTm  ?? string.Empty,
                ["Application_CompanyHead_FullName"]   = application.CompanyHead?.FullName           ?? string.Empty,
            };

            var asm = typeof(AppRegInfoChangeAddressLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Reg_Info_Change_Address_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
