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
            application?.ApplicationItems?.Any(i => i != null) == true;

        public string GetFileName(Application application) =>
            $"Sanawy_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream) =>
            GenerateForItemsAsync(application, wordService, outputStream, null);

        public Task GenerateForItemsAsync(
            Application application,
            IWordFormFillerService wordService,
            Stream outputStream,
            IList<ApplicationItem>? items)
        {
            var sourceItems = (items ?? application.ApplicationItems ?? Enumerable.Empty<ApplicationItem>())
                .Where(i => i != null)
                .ToList();

            var header = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]              = application.FullApplicationNumber ?? string.Empty,
                ["ApplicationDate"]                    = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["Application_CompanyHead_FullName"]   = sourceItems.FirstOrDefault()?.Application_CompanyHead_FullName   ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = sourceItems.FirstOrDefault()?.Application_CompanyHead_PositionTm ?? string.Empty,
            };

            var rows = sourceItems.Select((item, idx) => (IDictionary<string, object>)new Dictionary<string, object>
            {
                ["RowNumber"]                       = idx + 1,
                ["Person_LastName"]                 = item.Person_LastName                  ?? string.Empty,
                ["Person_FirstName"]                = item.Person_FirstName                 ?? string.Empty,
                ["Person_DateOfBirthText"]          = item.Person_DateOfBirthText           ?? string.Empty,
                ["Person_BirthPlace"]               = item.Person_BirthPlace                ?? string.Empty,
                ["Person_GenderTm"]                 = item.Person_GenderTm                  ?? string.Empty,
                ["Person_NationalityCode"]          = item.Person_NationalityCode           ?? string.Empty,
                ["Passport_Number"]                 = item.Passport_Number                  ?? string.Empty,
                ["Passport_ExpirationDateText"]     = item.Passport_ExpirationDateText      ?? string.Empty,
                ["Position_NameTm"]                 = item.Position_NameTm                  ?? string.Empty,
                ["Visa_NumberAndType"]              = item.Visa_NumberAndType               ?? string.Empty,
                ["Visa_StartDateText"]              = item.Visa_StartDateText               ?? string.Empty,
                ["Visa_ExpirationDateText"]         = item.Visa_ExpirationDateText          ?? string.Empty,
                ["Address_FullAddress"]             = item.Address_FullAddress              ?? string.Empty,
                ["BusinessTripAddress_FullAddress"] = item.BusinessTripAddress_FullAddress  ?? string.Empty,
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
