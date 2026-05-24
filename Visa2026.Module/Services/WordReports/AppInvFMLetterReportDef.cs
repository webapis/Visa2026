using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Family Member invitation letter for App_Inv_FM (Group B — Ministry recipient, static intro paragraphs).
    /// Template: Visa2026.Module.Resources.App_Inv_FM_Letter.docx
    /// </summary>
    public class AppInvFMLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Inv_FM" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"CakylykFM_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

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
                ["VisaPeriod_NameTm"]                        = application.VisaPeriod_NameTm                            ?? string.Empty,
                ["VisaCategory_NameTm"]                      = application.VisaCategory_NameTm                         ?? string.Empty,
                ["Application_CompanyHead_PositionTm"]       = application.Application_CompanyHead_PositionTm               ?? string.Empty,
                ["Application_CompanyHead_FullName"]         = application.Application_CompanyHead_FullName                       ?? string.Empty,
            };

            var asm = typeof(AppInvFMLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Inv_FM_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
