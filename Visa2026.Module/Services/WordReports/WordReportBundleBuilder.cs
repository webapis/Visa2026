using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.WordReports;

public sealed class WordReportBundleBuilder : IWordReportBundleBuilder
{
    private readonly IServiceProvider serviceProvider;

    public WordReportBundleBuilder(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task<WordReportBundleResult> BuildZipAsync(
        Application application,
        IObjectSpace objectSpace,
        Stream zipStream,
        CancellationToken cancellationToken = default)
    {
        if (application == null)
            throw new ArgumentNullException(nameof(application));
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (zipStream == null)
            throw new ArgumentNullException(nameof(zipStream));

        var wordService = serviceProvider.GetRequiredService<IWordFormFillerService>();
        var userReportGenerator = serviceProvider.GetService<IUserReportGenerator>();
        var excelReportGenerator = serviceProvider.GetService<IExcelReportGenerator>();
        var visibilityService = serviceProvider.GetService<IUserReportVisibilityService>();

        var definitions = serviceProvider
            .GetServices<IWordReportDefinition>()
            .Where(d => IsDefinitionApplicable(d, application))
            .ToList();

        var userTemplates = new List<UserReportTemplate>();
        if (visibilityService != null)
        {
            userTemplates = UserReportTemplateVisibilityHelper.GetVisibleActiveTemplates(
                objectSpace, visibilityService, application);
        }

        if (definitions.Count == 0 && userTemplates.Count == 0)
            throw new InvalidOperationException("No applicable reports for this application.");

        var results = new List<(string FileName, MemoryStream Stream)>();

        try
        {
            foreach (var def in definitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ms = new MemoryStream();
                await def.GenerateAsync(application, wordService, ms).ConfigureAwait(false);
                ms.Position = 0;
                results.Add((def.GetFileName(application), ms));
            }

            foreach (var template in userTemplates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ms = new MemoryStream();
                string extension;
                if (template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
                {
                    if (excelReportGenerator == null)
                        continue;

                    var applicationItems = UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application);
                    await excelReportGenerator
                        .GenerateAsync(template, application, ms, applicationItems)
                        .ConfigureAwait(false);
                    extension = ".xlsx";
                }
                else
                {
                    if (userReportGenerator == null)
                        continue;

                    var applicationItems = UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application);
                    await userReportGenerator
                        .GenerateAsync(template, application, ms, applicationItems)
                        .ConfigureAwait(false);
                    extension = ".docx";
                }

                ms.Position = 0;
                var fileName = ZipEntryFileNameSanitizer.BuildReportEntryName(
                    template.TemplateName,
                    application.FullApplicationNumber,
                    extension);
                results.Add((fileName, ms));
            }

            var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (fileName, stream) in results)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string entryName = ZipEntryFileNameSanitizer.EnsureUnique(fileName, usedEntryNames);
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    stream.Position = 0;
                    await stream.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
                }
            }

            string zipName = ZipEntryFileNameSanitizer.Sanitize(
                $"Resminamalar_{ZipEntryFileNameSanitizer.FlattenApplicationNumber(application.FullApplicationNumber)}_{DateTime.Now:yyyyMMdd}.zip",
                maxLength: 180);
            if (!zipName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                zipName += ".zip";

            return new WordReportBundleResult
            {
                ReportCount = results.Count,
                ZipFileName = zipName
            };
        }
        finally
        {
            foreach (var (_, stream) in results)
                stream.Dispose();
        }
    }

    private static bool IsDefinitionApplicable(IWordReportDefinition def, Application application)
    {
        var names = def.ApplicableApplicationTypeNames;
        if (names != null && names.Length > 0)
        {
            var appTypeName = application.ApplicationType?.Name;
            if (appTypeName == null || !names.Contains(appTypeName, StringComparer.Ordinal))
                return false;
        }

        return def.IsApplicable(application);
    }
}
