using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Change-of-invitation letter for App_Change_Inv.
    /// Fixed recipient: national migration chief. Lists all invitations in a repeating table row.
    /// Template: Visa2026.Module.Resources.App_Change_Inv_Letter.docx
    /// </summary>
    public class AppChangeInvLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Change_Inv" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"CakylykUytgemek_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var header = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]              = application.FullApplicationNumber          ?? string.Empty,
                ["ApplicationDate"]                    = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["TotalPersonCount"]                   = application.TotalPersonCount,
                ["TotalPersonCountText"]               = application.TotalPersonCountText            ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = application.CompanyHead?.Position?.NameTm  ?? string.Empty,
                ["Application_CompanyHead_FullName"]   = application.CompanyHead?.FullName           ?? string.Empty,
            };

            var rows = (application.Invitations ?? Enumerable.Empty<Invitation>())
                .Select((inv, idx) => (IDictionary<string, object>)new Dictionary<string, object>
                {
                    ["RowNo"]            = idx + 1,
                    ["InvitationNumber"] = inv.InvitationNumber                                    ?? string.Empty,
                    ["StartDate"]        = inv.StartDate.ToString("dd.MM.yyyy"),
                    ["ExpirationDate"]   = inv.ExpirationDate.HasValue
                                            ? inv.ExpirationDate.Value.ToString("dd.MM.yyyy")
                                            : string.Empty,
                });

            var asm = typeof(AppChangeInvLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Change_Inv_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillListForm(templateStream, outputStream, header, rows);
            return Task.CompletedTask;
        }
    }
}
