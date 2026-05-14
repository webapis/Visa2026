using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// GT-15 Çalık branch letter to Döwlet migrasiýa gullugy (branch letterhead layout).
    /// Template: Visa2026.Module.Resources.App_Visa_And_WP_Ext_GT15_Calik_Migration_Letter.docx
    /// </summary>
    public class AppVisaWPExtGt15CalikMigrationLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Visa_and_WP_Ext" };

        public bool IsApplicable(Application application) =>
            application.ProjectContract?.NameTm is { } nameTm
            && nameTm.Contains("GT-15", StringComparison.OrdinalIgnoreCase);

        public string GetFileName(Application application) =>
            $"WizaUzat_GT15_Calik_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["FullApplicationNumber"] = application.FullApplicationNumber ?? string.Empty,
                ["ApplicationDate"] = application.ApplicationDate.ToString("dd.MM.yyyy"),
                ["MigrationService_NameTm"] = application.MigrationService_NameTm ?? string.Empty,
                ["ApplicationType_ShowUrgency"] = application.ApplicationType?.ShowUrgency ?? false,
                ["Urgency_NameTm"] = application.Urgency_NameTm ?? string.Empty,
                ["ProjectContract_Description"] = application.ProjectContract_Description ?? string.Empty,
                ["CancelPersonCount"] = application.CancelPersonCount,
                ["CancelPersonCountText"] = application.CancelPersonCountText ?? string.Empty,
                ["VisaPeriod_NameTm"] = application.VisaPeriod_NameTm ?? string.Empty,
                ["VisaCategory_NameTm"] = application.VisaCategory_NameTm ?? string.Empty,
                ["Application_CompanyHead_PositionTm"] = application.CompanyHead?.Position?.NameTm ?? string.Empty,
                ["Application_CompanyHead_FullName"] = application.CompanyHead?.FullName ?? string.Empty,
            };

            var asm = typeof(AppVisaWPExtGt15CalikMigrationLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Visa_And_WP_Ext_GT15_Calik_Migration_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
