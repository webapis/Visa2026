using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// FM visa extension request letter for App_Visa_Ext_FM (Group B).
    /// Template: Visa2026.Module.Resources.App_Visa_Ext_FM_Letter.docx
    /// </summary>
    public class AppVisaExtFMLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Visa_Ext_FM" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"WizaUzatmaFM_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

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
                ["Company_Name"]                             = application.Application_Company_Name                                ?? string.Empty,
                ["TotalPersonCount"]                         = application.TotalPersonCount,
                ["TotalPersonCountText"]                     = application.TotalPersonCountText                         ?? string.Empty,
                ["FamilyMember_Relationship_NameTm"]         = application.FamilyMember_Relationship_NameTm             ?? string.Empty,
                ["SponsoringEmployee_FullName"]              = application.SponsoringEmployee_FullName                  ?? string.Empty,
                ["SponsoringEmployee_PositionTm"]            = application.SponsoringEmployee_PositionTm                ?? string.Empty,
                ["VisaCategory_NameTm"]                      = application.VisaCategory_NameTm                         ?? string.Empty,
                ["Application_CompanyHead_PositionTm"]       = application.Application_CompanyHead_PositionTm               ?? string.Empty,
                ["Application_CompanyHead_FullName"]         = application.Application_CompanyHead_FullName                       ?? string.Empty,
            };

            var asm = typeof(AppVisaExtFMLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Visa_Ext_FM_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
