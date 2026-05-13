using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Per-person Borçnama (commitment) form for App_Inv_And_WP.
    /// One page per ApplicationItem. Template: App_Inv_And_WP_Borcnama_Item.docx.
    /// </summary>
    public class AppInvAndWPBorcnamaItemReportDef : IWordReportDefinition
    {
        // Cross-cutting: available for all application types (like Labor Contract)
        public string[] ApplicableApplicationTypeNames => Array.Empty<string>();

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"Borcnama_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var header = new Dictionary<string, object>();

            var rows = (application.ApplicationItems ?? Enumerable.Empty<ApplicationItem>())
                .Select(item => (IDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Person_FullName"]                      = item.Person_FullName                      ?? string.Empty,
                    ["Person_DateOfBirthText"]               = item.Person_DateOfBirthText               ?? string.Empty,
                    ["Application_SponsorName"]              = item.Application_SponsorName              ?? string.Empty,
                    ["Application_SponsorSignatory"]         = item.Application_SponsorSignatory         ?? string.Empty,
                    ["Application_CompanyRegistryAddressLine"] = item.Application_CompanyRegistryAddressLine ?? string.Empty,
                    ["CompanyHead_FullName"]                 = item.CompanyHead_FullName                 ?? string.Empty,
                    ["CompanyHead_PassportLine"]             = item.CompanyHead_PassportLine             ?? string.Empty,
                    ["Representative_FullName"]              = item.Representative_FullName              ?? string.Empty,
                    ["Representative_PassportLine"]          = item.Representative_PassportLine          ?? string.Empty,
                });

            var asm = typeof(AppInvAndWPBorcnamaItemReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Inv_And_WP_Borcnama_Item.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillListForm(templateStream, outputStream, header, rows);
            return Task.CompletedTask;
        }
    }
}
