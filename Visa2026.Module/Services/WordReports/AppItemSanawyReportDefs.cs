using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Base for item-level table reports: iterates ApplicationItems, maps fields, calls FillListForm.
    /// Derived classes supply the template resource name, applicable types, and file name.
    /// </summary>
    public abstract class AppItemSanawyReportDefBase : IWordReportDefinition
    {
        protected abstract string EmbeddedResourceName { get; }
        public abstract string[] ApplicableApplicationTypeNames { get; }
        public bool IsApplicable(Application application) => true;
        public abstract string GetFileName(Application application);

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream) =>
            GenerateForItemsAsync(application, wordService, outputStream, null);

        public Task GenerateForItemsAsync(
            Application application,
            IWordFormFillerService wordService,
            Stream outputStream,
            IList<ApplicationItem>? items)
        {
            var header = new Dictionary<string, object>
            {
                ["Application_CompanyHead_PositionTm"] = application.Application_CompanyHead_PositionTm ?? string.Empty,
                ["Application_CompanyHead_FullName"]   = application.Application_CompanyHead_FullName           ?? string.Empty,
            };

            var sourceItems = (items ?? application.ApplicationItems ?? Enumerable.Empty<ApplicationItem>())
                .Where(item => item != null)
                .ToList();

            var rows = sourceItems
                .Select((item, idx) => (IDictionary<string, object>)new Dictionary<string, object>
                {
                    ["RowNo"]                                   = idx + 1,
                    ["Person_LastName"]                         = item.Person_LastName                        ?? string.Empty,
                    ["Person_FirstName"]                        = item.Person_FirstName                       ?? string.Empty,
                    ["Person_DateOfBirthText"]                  = item.Person_DateOfBirthText                 ?? string.Empty,
                    ["Person_CountryOfBirthTm"]                 = item.Person_CountryOfBirthTm                ?? string.Empty,
                    ["Person_BirthPlace"]                       = item.Person_BirthPlace                      ?? string.Empty,
                    ["Person_GenderTm"]                         = item.Person_GenderTm                        ?? string.Empty,
                    ["Person_NationalityCode"]                  = item.Person_NationalityCode                 ?? string.Empty,
                    ["Person_ForeignAddress"]                   = item.Person_ForeignAddress                  ?? string.Empty,
                    ["Passport_Number"]                         = item.Passport_Number                        ?? string.Empty,
                    ["Passport_ExpirationDateText"]             = item.Passport_ExpirationDateText            ?? string.Empty,
                    ["Education_LevelTm"]                       = item.Education_LevelTm                      ?? string.Empty,
                    ["Education_InstitutionName"]               = item.Education_InstitutionName              ?? string.Empty,
                    ["Education_SpecialtyTm"]                   = item.Education_SpecialtyTm                  ?? string.Empty,
                    ["Position_PositionTm"]                     = item.Position_PositionTm                    ?? string.Empty,
                    ["Address_FullAddress"]                     = item.Address_FullAddress                    ?? string.Empty,
                    ["Application_VisaPeriod_NameTm"]           = item.Application_VisaPeriod_NameTm          ?? string.Empty,
                    ["Application_VisaCategory_NameTm"]         = item.Application_VisaCategory_NameTm        ?? string.Empty,
                    ["Application_BorderZoneLocation_NameTm"]   = item.Application_BorderZoneLocation_NameTm  ?? string.Empty,
                    ["WorkPermit_Number"]                       = item.WorkPermit_Number                      ?? string.Empty,
                    ["WorkPermit_ASNumber"]                     = item.WorkPermit_ASNumber                    ?? string.Empty,
                    ["WorkPermit_StartDateText"]                = item.WorkPermit_StartDateText               ?? string.Empty,
                    ["WorkPermit_ExpirationDateText"]           = item.WorkPermit_ExpirationDateText          ?? string.Empty,
                    ["WorkPermit_WorkPermittedLocations"]       = item.WorkPermit_WorkPermittedLocations      ?? string.Empty,
                    ["Visa_Number"]                             = item.Visa_Number                            ?? string.Empty,
                    ["Visa_StartDateText"]                      = item.Visa_StartDateText                     ?? string.Empty,
                    ["Visa_ExpirationDateText"]                 = item.Visa_ExpirationDateText                ?? string.Empty,
                    ["Invitation_Number"]                       = item.Invitation_Number                      ?? string.Empty,
                    ["Invitation_StartDateText"]                = item.Invitation_StartDateText               ?? string.Empty,
                    ["Invitation_ExpirationDateText"]           = item.Invitation_ExpirationDateText          ?? string.Empty,
                });

            var asm = typeof(AppItemSanawyReportDefBase).Assembly;
            using var templateStream = asm.GetManifestResourceStream(EmbeddedResourceName)
                ?? throw new InvalidOperationException($"Embedded template not found: {EmbeddedResourceName}.");

            wordService.FillListForm(templateStream, outputStream, header, rows);
            return Task.CompletedTask;
        }
    }

    public class AppCancelInvWPItemReportDef : AppItemSanawyReportDefBase
    {
        protected override string EmbeddedResourceName => "Visa2026.Module.Resources.App_Cancel_Inv_WP_Item.docx";
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Cancel_Inv_WP" };
        public override string GetFileName(Application app) =>
            $"Sanawy_CakylykIsYatyr_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppCancelVisaAndWPItemReportDef : AppItemSanawyReportDefBase
    {
        protected override string EmbeddedResourceName => "Visa2026.Module.Resources.App_Cancel_Visa_And_WP_Item.docx";
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Cancel_Visa_and_WP" };
        public override string GetFileName(Application app) =>
            $"Sanawy_WizaIsYatyr_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppChangeInvItemReportDef : AppItemSanawyReportDefBase
    {
        protected override string EmbeddedResourceName => "Visa2026.Module.Resources.App_Change_Inv_Item.docx";
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Change_Inv" };
        public override string GetFileName(Application app) =>
            $"Sanawy_CakylykUytgemek_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppBorderZonePermissionItemReportDef : AppItemSanawyReportDefBase
    {
        protected override string EmbeddedResourceName => "Visa2026.Module.Resources.App_Border_Zone_Permission_Item.docx";
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Border_Zone_Permission" };
        public override string GetFileName(Application app) =>
            $"Sanawy_SerhetYaka_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }
}
