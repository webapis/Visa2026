using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Invitation + Work Permit letter for App_Inv_And_WP (Group A — Ministry recipient).
    /// Template: Visa2026.Module.Resources.App_Inv_And_WP_Letter.docx
    /// </summary>
    public class AppInvAndWPLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Inv_And_WP" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"CakylykIsRugsatnamasy_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var rawRecipient = application.ProjectContract_Ministry_RecipientBlock ?? string.Empty;
            var (recipientLine1, recipientLine2) = MinistryRecipientBlockFormatter.SplitIntoAddressLines(rawRecipient);

            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]                    = application.FullApplicationNumber                        ?? string.Empty,
                ["ApplicationDate"]                          = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["ProjectContract_Ministry_RecipientBlock"]  = rawRecipient,
                ["ProjectContract_Ministry_RecipientBlock_Line1"] = recipientLine1,
                ["ProjectContract_Ministry_RecipientBlock_Line2"] = recipientLine2 ?? string.Empty,
                ["ProjectContract_Ministry_RecipientBlock_HasLine2"] = !string.IsNullOrEmpty(recipientLine2),
                ["Urgency_NameTm"]                           = application.Urgency_NameTm                               ?? string.Empty,
                ["ProjectContract_Ministry_FormOfAddress"]   = application.ProjectContract_Ministry_FormOfAddress       ?? string.Empty,
                ["ProjectContract_Description"]              = application.ProjectContract_Description                  ?? string.Empty,
                ["Company_Name"]                             = application.Application_Company_Name                                ?? string.Empty,
                ["TotalPersonCount"]                         = application.TotalPersonCount,
                ["TotalPersonCountText"]                     = application.TotalPersonCountText                         ?? string.Empty,
                ["VisaPeriod_NameTm"]                        = application.VisaPeriod_NameTm                            ?? string.Empty,
                ["VisaCategory_NameTm"]                      = application.VisaCategory_NameTm                         ?? string.Empty,
                ["Application_CompanyHead_PositionTm"]       = application.Application_CompanyHead_PositionTm               ?? string.Empty,
                ["Application_CompanyHead_FullName"]         = application.Application_CompanyHead_FullName                       ?? string.Empty,
            };

            var asm = typeof(AppInvAndWPLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Inv_And_WP_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
