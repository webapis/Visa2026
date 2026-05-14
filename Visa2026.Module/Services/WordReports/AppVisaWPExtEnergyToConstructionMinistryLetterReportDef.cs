using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Ministry of Energy → Ministry of Construction letter (GT-15 / Çalık sample scan).
    /// Template: Visa2026.Module.Resources.App_Visa_WP_Ext_Energy_To_Construction_Ministry_Letter.docx
    /// Dynamic slots: foreign-citizen count + Turkmen cardinal; visa/work permit period phrase from <see cref="VisaPeriod"/>.
    /// </summary>
    public class AppVisaWPExtEnergyToConstructionMinistryLetterReportDef : IWordReportDefinition
    {
        public string[] ApplicableApplicationTypeNames => new[] { "App_Visa_and_WP_Ext" };

        public bool IsApplicable(Application application) =>
            application.ProjectContract?.NameTm is { } nameTm
            && nameTm.Contains("GT-15", StringComparison.OrdinalIgnoreCase);

        public string GetFileName(Application application) =>
            $"Hat_Energetika_Gurlushyk_{application.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";

        public Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream)
        {
            var data = new Dictionary<string, object>
            {
                ["CancelPersonCount"] = application.CancelPersonCount,
                ["CancelPersonCountText"] = application.CancelPersonCountText ?? string.Empty,
                ["VisaPeriod_NameTm"] = application.VisaPeriod_NameTm ?? string.Empty,
            };

            var asm = typeof(AppVisaWPExtEnergyToConstructionMinistryLetterReportDef).Assembly;
            const string res = "Visa2026.Module.Resources.App_Visa_WP_Ext_Energy_To_Construction_Ministry_Letter.docx";

            using var templateStream = asm.GetManifestResourceStream(res)
                ?? throw new InvalidOperationException($"Embedded template not found: {res}.");

            wordService.FillForm(templateStream, outputStream, data);
            return Task.CompletedTask;
        }
    }
}
