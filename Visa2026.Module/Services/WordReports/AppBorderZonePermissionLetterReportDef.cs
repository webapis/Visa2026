using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Border zone permission request letter for App_Border_Zone_Permission (Group C).
    /// Template: Visa2026.Module.Resources.App_Border_Zone_Permission_Letter.docx
    /// </summary>
    public class AppBorderZonePermissionLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Border_Zone_Permission" };

        public bool IsApplicable(Application application) => true;

        public string GetFileName(Application application) =>
            $"SerhetYakaWizasy_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"]                    = application.FullApplicationNumber                        ?? string.Empty,
                ["ApplicationDate"]                          = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["ProjectContract_Ministry_RecipientBlock"]  = application.ProjectContract_Ministry_RecipientBlock      ?? string.Empty,
                ["ProjectContract_Ministry_FormOfAddress"]   = application.ProjectContract_Ministry_FormOfAddress       ?? string.Empty,
                ["ProjectContract_Description"]              = application.ProjectContract_Description                  ?? string.Empty,
                ["Company_Name"]                             = application.Company?.Name                                ?? string.Empty,
                ["TotalPersonCount"]                         = application.TotalPersonCount,
                ["TotalPersonCountText"]                     = application.TotalPersonCountText                         ?? string.Empty,
                ["BorderZoneLocation_NameTm"]                = application.BorderZoneLocation_NameTm                   ?? string.Empty,
                ["Application_CompanyHead_PositionTm"]       = application.CompanyHead?.Position?.NameTm               ?? string.Empty,
                ["Application_CompanyHead_FullName"]         = application.CompanyHead?.FullName                       ?? string.Empty,
            };

            var asm = typeof(AppBorderZonePermissionLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Border_Zone_Permission_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
