using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Base for all 14-column landscape "Daşary ýurt raýatlarynyň sanawy" personnel list reports.
    /// Derived classes only need to supply ApplicableApplicationTypeNames and GetFileName.
    /// Template: Visa2026.Module.Resources.App_Sanawy_Letter.docx
    /// </summary>
    public abstract class AppSanawyLetterReportDefBase : IWordReportDefinition
    {
        public abstract string[] ApplicableApplicationTypeNames { get; }

        public bool IsApplicable(Application application) => true;

        public abstract string GetFileName(Application application);

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var header = new Dictionary<string, object>
            {
                ["Application_CompanyHead_PositionTm"] = application.CompanyHead?.Position?.NameTm ?? string.Empty,
                ["Application_CompanyHead_FullName"]   = application.CompanyHead?.FullName           ?? string.Empty,
            };

            var rows = (application.ApplicationItems ?? Enumerable.Empty<ApplicationItem>())
                .Select((item, idx) => (IDictionary<string, object>)new Dictionary<string, object>
                {
                    ["RowNo"]                                  = idx + 1,
                    ["Person_LastName"]                        = item.Person_LastName                       ?? string.Empty,
                    ["Person_FirstName"]                       = item.Person_FirstName                      ?? string.Empty,
                    ["Person_DateOfBirthText"]                 = item.Person_DateOfBirthText                ?? string.Empty,
                    ["Person_CountryOfBirthTm"]                = item.Person_CountryOfBirthTm               ?? string.Empty,
                    ["Person_BirthPlace"]                      = item.Person_BirthPlace                     ?? string.Empty,
                    ["Person_GenderTm"]                        = item.Person_GenderTm                       ?? string.Empty,
                    ["Person_NationalityCode"]                 = item.Person_NationalityCode                ?? string.Empty,
                    ["Passport_Number"]                        = item.Passport_Number                       ?? string.Empty,
                    ["Passport_ExpirationDateText"]            = item.Passport_ExpirationDateText           ?? string.Empty,
                    ["Education_LevelTm"]                      = item.Education_LevelTm                     ?? string.Empty,
                    ["Education_InstitutionName"]              = item.Education_InstitutionName             ?? string.Empty,
                    ["Education_SpecialtyTm"]                  = item.Education_SpecialtyTm                 ?? string.Empty,
                    ["Position_PositionTm"]                    = item.Position_PositionTm                   ?? string.Empty,
                    ["Application_VisaPeriod_NameTm"]          = item.Application_VisaPeriod_NameTm         ?? string.Empty,
                    ["Application_VisaCategory_NameTm"]        = item.Application_VisaCategory_NameTm       ?? string.Empty,
                    ["Address_FullAddress"]                    = item.Address_FullAddress                   ?? string.Empty,
                    ["Person_ForeignAddress"]                  = item.Person_ForeignAddress                 ?? string.Empty,
                    ["Application_BorderZoneLocation_NameTm"]  = item.Application_BorderZoneLocation_NameTm ?? string.Empty,
                });

            var asm = typeof(AppSanawyLetterReportDefBase).Assembly;
            const string res = "Visa2026.Module.Resources.App_Sanawy_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillListForm(templateStream, outputStream, header, rows);
            return Task.CompletedTask;
        }
    }
}
