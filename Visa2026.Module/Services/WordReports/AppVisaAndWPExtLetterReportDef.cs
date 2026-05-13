using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Visa + Work Permit extension request letter for App_Visa_and_WP_Ext (Group C).
    /// Template: Visa2026.Module.Resources.App_Visa_And_WP_Ext_Letter.docx
    /// </summary>
    public class AppVisaAndWPExtLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Visa_and_WP_Ext" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"WizaIsRugsatUzatmak_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]                    = application.FullApplicationNumber                        ?? string.Empty,
                ["ApplicationDate"]                          = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["ProjectContract_Ministry_RecipientBlock"]  = application.ProjectContract_Ministry_RecipientBlock      ?? string.Empty,
                ["ProjectContract_Ministry_FormOfAddress"]   = application.ProjectContract_Ministry_FormOfAddress       ?? string.Empty,
                ["ProjectContract_Description"]              = application.ProjectContract_Description                  ?? string.Empty,
                ["Company_Name"]                             = application.Company?.Name                                ?? string.Empty,
                ["TotalPersonCount"]                         = application.TotalPersonCount,
                ["TotalPersonCountText"]                     = application.TotalPersonCountText                         ?? string.Empty,
                ["VisaPeriod_NameTm"]                        = application.VisaPeriod_NameTm                            ?? string.Empty,
                ["VisaCategory_NameTm"]                      = application.VisaCategory_NameTm                         ?? string.Empty,
                ["Application_CompanyHead_PositionTm"]       = application.CompanyHead?.Position?.NameTm               ?? string.Empty,
                ["Application_CompanyHead_FullName"]         = application.CompanyHead?.FullName                       ?? string.Empty,
            };

            var asm = typeof(AppVisaAndWPExtLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Visa_And_WP_Ext_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
