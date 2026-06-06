using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.WordReports;

public sealed class ApplicationWordReportGeneratedFile
{
    public required string FileName { get; init; }

    public required byte[] Content { get; init; }

    public required string ContentType { get; init; }
}

/// <summary>Generates one or many Resminamalar reports by stable catalog <see cref="ApplicationWordReportPackageCatalogEntry.EntryKey"/>.</summary>
public sealed class ApplicationWordReportEntryGenerator
{
    private readonly IServiceProvider serviceProvider;

    public ApplicationWordReportEntryGenerator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task<ApplicationWordReportGeneratedFile?> TryGenerateSingleAsync(
        IObjectSpace objectSpace,
        Application application,
        string entryKey,
        CancellationToken cancellationToken = default)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (application == null)
            throw new ArgumentNullException(nameof(application));
        if (string.IsNullOrWhiteSpace(entryKey))
            return null;

        var catalogService = serviceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var catalog = catalogService.Build(objectSpace, application);
        if (!catalog.Entries.Any(entry => string.Equals(entry.EntryKey, entryKey, StringComparison.Ordinal)))
            return null;

        await using var stream = await GenerateEntryStreamAsync(objectSpace, application, entryKey, cancellationToken)
            .ConfigureAwait(false);
        if (stream == null)
            return null;

        stream.Position = 0;
        var bytes = stream.ToArray();
        if (bytes.Length == 0)
            return null;

        var fileName = ResolveDownloadFileName(entryKey, catalog.Entries);
        return new ApplicationWordReportGeneratedFile
        {
            FileName = fileName,
            Content = bytes,
            ContentType = GetContentType(fileName)
        };
    }

    public async Task<IReadOnlyList<(string FileName, MemoryStream Stream)>> GenerateManyAsync(
        IObjectSpace objectSpace,
        Application application,
        IReadOnlySet<string>? selectedEntryKeys,
        CancellationToken cancellationToken = default)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (application == null)
            throw new ArgumentNullException(nameof(application));

        var catalogService = serviceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var catalog = catalogService.Build(objectSpace, application);
        var keys = selectedEntryKeys == null || selectedEntryKeys.Count == 0
            ? catalog.Entries.Select(entry => entry.EntryKey).ToList()
            : catalog.Entries
                .Where(entry => selectedEntryKeys.Contains(entry.EntryKey))
                .Select(entry => entry.EntryKey)
                .ToList();

        var results = new List<(string FileName, MemoryStream Stream)>();
        foreach (var entryKey in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = await GenerateEntryStreamAsync(objectSpace, application, entryKey, cancellationToken)
                .ConfigureAwait(false);
            if (stream == null)
                continue;

            stream.Position = 0;
            var fileName = ResolveDownloadFileName(entryKey, catalog.Entries);
            results.Add((fileName, stream));
        }

        return results;
    }

    private async Task<MemoryStream?> GenerateEntryStreamAsync(
        IObjectSpace objectSpace,
        Application application,
        string entryKey,
        CancellationToken cancellationToken)
    {
        if (entryKey.StartsWith("system:", StringComparison.Ordinal))
            return await GenerateSystemEntryAsync(objectSpace, application, entryKey, cancellationToken).ConfigureAwait(false);

        if (entryKey.StartsWith("user:", StringComparison.Ordinal)
            && Guid.TryParse(entryKey.AsSpan(5), out var templateId))
        {
            return await GenerateUserEntryAsync(objectSpace, application, templateId, cancellationToken)
                .ConfigureAwait(false);
        }

        return null;
    }

    private async Task<MemoryStream?> GenerateSystemEntryAsync(
        IObjectSpace objectSpace,
        Application application,
        string entryKey,
        CancellationToken cancellationToken)
    {
        var definition = serviceProvider
            .GetServices<IWordReportDefinition>()
            .FirstOrDefault(def =>
                string.Equals(ApplicationWordReportPackageCatalogService.BuildSystemEntryKey(def), entryKey, StringComparison.Ordinal)
                && ApplicationWordReportApplicability.IsDefinitionApplicable(def, application));

        if (definition == null)
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var wordService = serviceProvider.GetRequiredService<IWordFormFillerService>();
        var ms = new MemoryStream();
        await definition.GenerateAsync(application, wordService, ms).ConfigureAwait(false);
        return ms;
    }

    private async Task<MemoryStream?> GenerateUserEntryAsync(
        IObjectSpace objectSpace,
        Application application,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var visibilityService = serviceProvider.GetService<IUserReportVisibilityService>();
        if (visibilityService == null)
            return null;

        var template = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.ApplicableTypeLinks)
                .ThenInclude(l => l.ApplicationType)
            .Include(t => t.ApplicableProjectContractLinks)
                .ThenInclude(l => l.ProjectContract)
            .Include(t => t.TemplateFile)
            .Include(t => t.Placeholders)
            .FirstOrDefault(t => t.ID == templateId && t.IsActive);

        if (template == null || !visibilityService.IsTemplateVisible(template, application))
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var ms = new MemoryStream();
        if (template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
        {
            var excelReportGenerator = serviceProvider.GetService<IExcelReportGenerator>();
            if (excelReportGenerator == null)
                return null;

            var applicationItems = UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application);
            await excelReportGenerator
                .GenerateAsync(template, application, ms, applicationItems)
                .ConfigureAwait(false);
        }
        else
        {
            var userReportGenerator = serviceProvider.GetService<IUserReportGenerator>();
            if (userReportGenerator == null)
                return null;

            var applicationItems = UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application);
            await userReportGenerator
                .GenerateAsync(template, application, ms, applicationItems)
                .ConfigureAwait(false);
        }

        return ms;
    }

    private static string ResolveDownloadFileName(
        string entryKey,
        IReadOnlyList<ApplicationWordReportPackageCatalogEntry> catalogEntries)
    {
        var catalogEntry = catalogEntries.FirstOrDefault(entry =>
            string.Equals(entry.EntryKey, entryKey, StringComparison.Ordinal));

        if (catalogEntry?.OutputFileName is { Length: > 0 } outputFileName)
            return outputFileName;

        if (catalogEntry?.Kind == ApplicationWordReportPackageEntryKind.UserExcel)
        {
            return ZipEntryFileNameSanitizer.BuildReportEntryName(
                catalogEntry.DisplayName,
                ".xlsx");
        }

        if (catalogEntry != null)
        {
            return ZipEntryFileNameSanitizer.BuildReportEntryName(
                catalogEntry.DisplayName,
                ".docx");
        }

        return $"report_{DateTime.Now:yyyyMMdd}.docx";
    }

    private static string GetContentType(string fileName)
    {
        if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase))
        {
            return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }

        return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    }
}
