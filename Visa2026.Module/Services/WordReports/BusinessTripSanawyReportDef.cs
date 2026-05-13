using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// "Daşary ýurt raýatlarynyň sanawy" — 11-column tabular list of BusinessTrips.
    /// Applies to App_Business_Trip_Arrival and App_Business_Trip_Departure.
    /// Template: Visa2026.Module.Resources.BusinessTrip_Sanawy.docx
    /// </summary>
    public class BusinessTripSanawyReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[]
        {
            "App_Business_Trip_Arrival",
            "App_Business_Trip_Departure"
        };

        public bool IsApplicable(Application application) =>
            application?.BusinessTrips?.Any(bt => bt != null && !bt.IsDeleted) == true;

        public string GetFileName(Application application) =>
            $"Sanawy_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var trips = application.BusinessTrips
                .Where(bt => bt != null && !bt.IsDeleted)
                .ToList();

            var header = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]              = application.FullApplicationNumber ?? string.Empty,
                ["ApplicationDate"]                    = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["Application_CompanyHead_FullName"]   = trips[0].Application_CompanyHead_FullName   ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = trips[0].Application_CompanyHead_PositionTm ?? string.Empty,
            };

            var rows = trips.Select((bt, idx) => (IDictionary<string, object>)new Dictionary<string, object>
            {
                ["RowNumber"]                       = idx + 1,
                ["Person_LastName"]                 = bt.Person_LastName                  ?? string.Empty,
                ["Person_FirstName"]                = bt.Person_FirstName                 ?? string.Empty,
                ["Person_DateOfBirthText"]          = bt.Person_DateOfBirthText           ?? string.Empty,
                ["Person_BirthPlace"]               = bt.Person_BirthPlace                ?? string.Empty,
                ["Person_GenderTm"]                 = bt.Person_GenderTm                  ?? string.Empty,
                ["Person_NationalityCode"]          = bt.Person_NationalityCode           ?? string.Empty,
                ["Passport_Number"]                 = bt.Passport_Number                  ?? string.Empty,
                ["Passport_ExpirationDateText"]     = bt.Passport_ExpirationDateText      ?? string.Empty,
                ["Position_NameTm"]                 = bt.Position_NameTm                  ?? string.Empty,
                ["Visa_NumberAndType"]              = bt.Visa_NumberAndType               ?? string.Empty,
                ["Visa_StartDateText"]              = bt.Visa_StartDateText               ?? string.Empty,
                ["Visa_ExpirationDateText"]         = bt.Visa_ExpirationDateText          ?? string.Empty,
                ["Address_FullAddress"]             = bt.Address_FullAddress              ?? string.Empty,
                ["BusinessTripAddress_FullAddress"] = bt.BusinessTripAddress_FullAddress  ?? string.Empty,
            }).ToList();

            var asm = typeof(BusinessTripSanawyReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.BusinessTrip_Sanawy.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException(
                    $"Embedded template not found: {res}. Ensure it is registered as EmbeddedResource in the .csproj.");

            wordService.FillListForm(templateStream, outputStream, header, rows);
            return Task.CompletedTask;
        }
    }
}
