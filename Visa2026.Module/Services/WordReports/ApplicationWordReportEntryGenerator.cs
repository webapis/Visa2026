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

    public Task<ApplicationWordReportGeneratedFile?> TryGenerateSingleAsync(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        string entryKey,
        CancellationToken cancellationToken = default) =>
        TryGenerateSingleAsync(objectSpace, application, entryKey, WordReportGenerationContext.ForApplication(), cancellationToken);

    public async Task<ApplicationWordReportGeneratedFile?> TryGenerateSingleAsync(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        string entryKey,
        WordReportGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (application == null)
            throw new ArgumentNullException(nameof(application));
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(entryKey))
            return null;

        var catalogService = serviceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var catalog = catalogService.Build(objectSpace, application, context);
        if (!catalog.Entries.Any(entry => string.Equals(entry.EntryKey, entryKey, StringComparison.Ordinal)))
            return null;

        var outputs = await GenerateEntryOutputsAsync(objectSpace, application, entryKey, context, catalog.Entries, cancellationToken)
            .ConfigureAwait(false);
        var first = outputs.FirstOrDefault();
        if (first.Stream == null)
            return null;

        await using (first.Stream)
        {
            first.Stream.Position = 0;
            var bytes = first.Stream.ToArray();
            if (bytes.Length == 0)
                return null;

            return new ApplicationWordReportGeneratedFile
            {
                FileName = first.FileName,
                Content = bytes,
                ContentType = GetContentType(first.FileName)
            };
        }
    }

    public Task<IReadOnlyList<(string FileName, MemoryStream Stream)>> GenerateManyAsync(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        IReadOnlySet<string>? selectedEntryKeys,
        CancellationToken cancellationToken = default) =>
        GenerateManyAsync(objectSpace, application, selectedEntryKeys, WordReportGenerationContext.ForApplication(), cancellationToken);

    public async Task<IReadOnlyList<(string FileName, MemoryStream Stream)>> GenerateManyAsync(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        IReadOnlySet<string>? selectedEntryKeys,
        WordReportGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (application == null)
            throw new ArgumentNullException(nameof(application));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var catalogService = serviceProvider.GetRequiredService<ApplicationWordReportPackageCatalogService>();
        var catalog = catalogService.Build(objectSpace, application, context);
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
            var outputs = await GenerateEntryOutputsAsync(objectSpace, application, entryKey, context, catalog.Entries, cancellationToken)
                .ConfigureAwait(false);
            results.AddRange(outputs);
        }

        return results;
    }

    private async Task<List<(string FileName, MemoryStream Stream)>> GenerateEntryOutputsAsync(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        string entryKey,
        WordReportGenerationContext context,
        IReadOnlyList<ApplicationWordReportPackageCatalogEntry> catalogEntries,
        CancellationToken cancellationToken)
    {
        if (ApplicationProfileNestedTemplateCatalogHelper.TryParseEntryKey(entryKey, out var profileTemplateId))
        {
            return await GenerateProfileEntryOutputsAsync(
                    objectSpace,
                    application,
                    profileTemplateId,
                    context,
                    catalogEntries,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (entryKey.StartsWith("user:", StringComparison.Ordinal)
            && Guid.TryParse(entryKey.AsSpan(5), out var templateId))
        {
            return await GenerateUserEntryOutputsAsync(objectSpace, application, templateId, context, catalogEntries, cancellationToken)
                .ConfigureAwait(false);
        }

        return new List<(string, MemoryStream)>();
    }

    private async Task<List<(string FileName, MemoryStream Stream)>> GenerateProfileEntryOutputsAsync(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Guid profileTemplateId,
        WordReportGenerationContext context,
        IReadOnlyList<ApplicationWordReportPackageCatalogEntry> catalogEntries,
        CancellationToken cancellationToken)
    {
        var profileTemplate = ApplicationProfileNestedTemplateCatalogHelper.LoadProfileTemplate(
            objectSpace,
            profileTemplateId);
        if (profileTemplate == null
            || application.ApplicationProfile == null
            || profileTemplate.ApplicationProfileId != application.ApplicationProfile.ID)
        {
            return new List<(string, MemoryStream)>();
        }

        var userTemplate = ApplicationProfileNestedTemplateCatalogHelper.TryResolveMergeTemplate(
            objectSpace,
            profileTemplate);
        if (userTemplate == null)
            return new List<(string, MemoryStream)>();

        return await GenerateUserEntryOutputsAsync(
                objectSpace,
                application,
                userTemplate.ID,
                context,
                catalogEntries,
                cancellationToken,
                skipVisibilityCheck: true)
            .ConfigureAwait(false);
    }

    private async Task<List<(string FileName, MemoryStream Stream)>> GenerateUserEntryOutputsAsync(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Guid templateId,
        WordReportGenerationContext context,
        IReadOnlyList<ApplicationWordReportPackageCatalogEntry> catalogEntries,
        CancellationToken cancellationToken,
        bool skipVisibilityCheck = false)
    {
        var visibilityService = serviceProvider.GetService<IUserReportVisibilityService>();
        if (visibilityService == null && !skipVisibilityCheck)
            return new List<(string, MemoryStream)>();

        var template = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.ApplicableTypeLinks)
                .ThenInclude(l => l.ApplicationType)
            .Include(t => t.ApplicableGroupLinks)
                .ThenInclude(l => l.ApplicationTypeGroup)
                    .ThenInclude(g => g.Members)
            .Include(t => t.ApplicableProjectContractLinks)
                .ThenInclude(l => l.ProjectContract)
            .Include(t => t.TemplateFile)
            .Include(t => t.Placeholders)
            .FirstOrDefault(t => t.ID == templateId && t.IsActive);

        if (template == null
            || (visibilityService != null
                && !skipVisibilityCheck
                && !visibilityService.IsTemplateVisible(template, application)))
            return new List<(string, MemoryStream)>();

        var defaultFileName = ResolveDownloadFileName(template, catalogEntries);

        if (!UsesPerItemWordOutput(template, context))
        {
            var stream = await GenerateUserEntryAsync(objectSpace, application, template, context, cancellationToken)
                .ConfigureAwait(false);
            return stream == null
                ? new List<(string, MemoryStream)>()
                : new List<(string, MemoryStream)> { (defaultFileName, stream) };
        }

        var selectedItems = context.ResolveApplicationItems(objectSpace, application);
        var outputs = new List<(string FileName, MemoryStream Stream)>();
        foreach (var item in selectedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ms = await GenerateUserEntryForItemAsync(template, item, cancellationToken).ConfigureAwait(false);
            if (ms == null)
                continue;

            var fileName = BuildPerItemUserTemplateFileName(template, item);
            outputs.Add((fileName, ms));
        }

        return outputs;
    }

    private async Task<MemoryStream?> GenerateUserEntryAsync(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        UserReportTemplate template,
        WordReportGenerationContext context,
        CancellationToken cancellationToken)
    {
        var selectedItems = context.ResolveApplicationItems(objectSpace, application);
        if (UsesPerItemWordOutput(template, context))
        {
            var firstItem = selectedItems.FirstOrDefault();
            return firstItem == null
                ? null
                : await GenerateUserEntryForItemAsync(template, firstItem, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var ms = new MemoryStream();
        if (template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
        {
            var excelReportGenerator = serviceProvider.GetService<IExcelReportGenerator>();
            if (excelReportGenerator == null)
                return null;

            await excelReportGenerator
                .GenerateAsync(template, application, ms, selectedItems)
                .ConfigureAwait(false);
        }
        else
        {
            var userReportGenerator = serviceProvider.GetService<IUserReportGenerator>();
            if (userReportGenerator == null)
                return null;

            await userReportGenerator
                .GenerateAsync(template, application, ms, selectedItems)
                .ConfigureAwait(false);
        }

        return ms;
    }

    private async Task<MemoryStream?> GenerateUserEntryForItemAsync(
        UserReportTemplate template,
        ApplicationRosterMergeLine applicationItem,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ms = new MemoryStream();
        if (template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
        {
            var excelReportGenerator = serviceProvider.GetService<IExcelReportGenerator>();
            if (excelReportGenerator == null)
                return null;

            await excelReportGenerator.GenerateAsync(template, applicationItem, ms).ConfigureAwait(false);
        }
        else
        {
            var userReportGenerator = serviceProvider.GetService<IUserReportGenerator>();
            if (userReportGenerator == null)
                return null;

            await userReportGenerator.GenerateAsync(template, applicationItem, ms).ConfigureAwait(false);
        }

        return ms;
    }

    private static bool UsesPerItemWordOutput(UserReportTemplate template, WordReportGenerationContext context) =>
        context.Scope == WordReportPackageScope.ApplicationRosterMergeLine
        && template.GetEffectiveOutputFormat() == TemplateOutputFormat.Word
        && !UserReportMergeDataHelper.UsesSingleDocumentItemList(template)
        && (template.RootBoType is UserReportBoType.ApplicationItem or UserReportBoType.Person
            || UserReportMergeDataHelper.IsSahsyKagyzUserReportTemplate(template)
            || UserReportMergeDataHelper.TemplateUsesSahsyKagyzRowPlaceholders(template, template.Placeholders));

    private static string BuildPerItemUserTemplateFileName(UserReportTemplate template, ApplicationRosterMergeLine item)
    {
        var extension = template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel ? ".xlsx" : ".docx";
        var personPart = $"{item.Person_LastName}_{item.Person_FirstName}".Trim('_');
        if (string.IsNullOrWhiteSpace(personPart))
            personPart = item.ID.ToString("N")[..8];

        var label = $"{template.TemplateName}_{personPart}";
        return ZipEntryFileNameSanitizer.BuildReportEntryName(label, extension);
    }

    /// <summary>
    /// Nested profile catalogs use <c>profile:{id}</c> keys, not <c>user:{id}</c>. Matching only the
    /// user key made Excel merges download as <c>report_yyyyMMdd.docx</c>, so Preview ran Word PDF
    /// on spreadsheet bytes and showed a blank page.
    /// </summary>
    internal static string ResolveDownloadFileName(
        UserReportTemplate template,
        IReadOnlyList<ApplicationWordReportPackageCatalogEntry> catalogEntries)
    {
        ArgumentNullException.ThrowIfNull(template);

        var extension = template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel
            ? ".xlsx"
            : ".docx";

        catalogEntries ??= Array.Empty<ApplicationWordReportPackageCatalogEntry>();

        var catalogEntry = catalogEntries.FirstOrDefault(entry =>
                template.ID != Guid.Empty && entry.UserReportTemplateId == template.ID)
            ?? catalogEntries.FirstOrDefault(entry =>
                string.Equals(
                    entry.EntryKey,
                    ApplicationWordReportPackageCatalogService.BuildUserEntryKey(template),
                    StringComparison.Ordinal));

        if (catalogEntry?.OutputFileName is { Length: > 0 } outputFileName)
        {
            if (outputFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return outputFileName;

            return ZipEntryFileNameSanitizer.BuildReportEntryName(
                Path.GetFileNameWithoutExtension(outputFileName),
                extension);
        }

        var displayName = catalogEntry?.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = template.TemplateName;

        return ZipEntryFileNameSanitizer.BuildReportEntryName(displayName, extension);
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
