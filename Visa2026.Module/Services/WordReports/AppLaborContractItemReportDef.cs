using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Per-person Zähmet şertnamasy (labor contract) — cross-cutting for all application types.
    /// One page per ApplicationItem. Template: App_Labor_Contract_Item.docx.
    /// Visible when application has at least one ApplicationItem.
    /// </summary>
    public class AppLaborContractItemReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => Array.Empty<string>(); // Cross-cutting: visible for all application types

        public bool IsApplicable(Application application) =>
            application?.ApplicationItems?.Any() == true; // Visible if application has any items

        public string GetFileName(Application application) =>
            $"ZahmetSertnamasy_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var header = new Dictionary<string, object>();

            var rows = (application.ApplicationItems ?? Enumerable.Empty<ApplicationItem>())
                .Select(item => (IDictionary<string, object>)new Dictionary<string, object>
                {
                    ["Person_FullName"]                  = item.Person_FullName                  ?? string.Empty,
                    ["Position_PositionTm"]              = item.Position_PositionTm              ?? string.Empty,
                    ["Passport_Number"]                  = item.Passport_Number                  ?? string.Empty,
                    ["Application_SponsorName"]          = item.Application_SponsorName          ?? string.Empty,
                    ["Application_SponsorSignatory"]     = item.Application_SponsorSignatory     ?? string.Empty,
                    ["Application_CompanyAddress"]       = item.Application_CompanyAddress       ?? string.Empty,
                    ["Contract_StartDateText"]           = item.Contract_StartDateText           ?? string.Empty,
                    ["Contract_ExpirationDateText"]      = item.Contract_ExpirationDateText      ?? string.Empty,
                    ["Contract_PeriodFallbackText"]      = item.Contract_PeriodFallbackText      ?? string.Empty,
                    ["Contract_SalaryText"]              = item.Contract_SalaryText              ?? string.Empty,
                    ["Salary_CurrencyCode"]              = item.Salary_CurrencyCode              ?? string.Empty,
                });

            var asm = typeof(AppLaborContractItemReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Labor_Contract_Item.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillListForm(templateStream, outputStream, header, rows);
            return Task.CompletedTask;
        }
    }
}
