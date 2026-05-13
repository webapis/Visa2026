using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Application-level arrival notification letter for App_Business_Trip_Arrival.
    /// "Daşary ýurt raýatlynyň ... iş saparyna gelendigini size habar berýäris."
    /// Template: Visa2026.Module.Resources.BusinessTrip_Arrival_Letter.docx
    /// </summary>
    public class BusinessTripArrivalLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[]
        {
            "App_Business_Trip_Arrival"
        };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"GelisHaty_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]              = application.FullApplicationNumber           ?? string.Empty,
                ["ApplicationDate"]                    = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["MigrationService_NameTm"]            = application.MigrationService_NameTm         ?? string.Empty,
                ["TotalPersonCount"]                   = application.TotalPersonCount,
                ["TotalPersonCountText"]               = application.TotalPersonCountText             ?? string.Empty,
                ["BusinessTripStartDateText"]          = application.BusinessTripStartDateText        ?? string.Empty,
                ["BusinessTripEndDateText"]            = application.BusinessTripEndDateText          ?? string.Empty,
                ["BusinessTripDurationDays"]           = application.BusinessTripDurationDays?.ToString() ?? string.Empty,
                ["ToRegionName_Genitive"]              = application.ToRegionName_Genitive            ?? string.Empty,
                ["ToCityName_Dative"]                  = application.ToCityName_Dative               ?? string.Empty,
                ["BusinessTripPurpose_NameTm"]         = application.BusinessTripPurpose_NameTm       ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = application.CompanyHead?.Position?.NameTm   ?? string.Empty,
                ["Application_CompanyHead_FullName"]   = application.CompanyHead?.FullName             ?? string.Empty,
            };

            var asm = typeof(BusinessTripArrivalLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.BusinessTrip_Arrival_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
