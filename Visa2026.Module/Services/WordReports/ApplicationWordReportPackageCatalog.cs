using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.WordReports;

public enum ApplicationWordReportPackageEntryKind
{
    UserWord = 0,
    UserExcel = 1
}

public sealed class ApplicationWordReportPackageCatalogEntry
{
    public required string EntryKey { get; init; }

    public required string DisplayName { get; init; }

    public string? OutputFileName { get; init; }

    public ApplicationWordReportPackageEntryKind Kind { get; init; }

    public ApplicationWordReportPackageReadinessLevel Readiness { get; init; }

    public string? ReadinessMessageKey { get; init; }

    public IReadOnlyList<ApplicationWordReportPackageReadinessHint> ReadinessHints { get; init; } =
        Array.Empty<ApplicationWordReportPackageReadinessHint>();

    public Guid? UserReportTemplateId { get; init; }

    public Guid? ApplicationProfileTemplateId { get; init; }

    /// <summary>Officer-created this-profile nested row — catalog Delete moves it to Recycle Bin.</summary>
    public bool CanMoveToRecycleBin { get; init; }

    public DateTime? CreatedOnUtc { get; init; }

    public string? CreatedByUserName { get; init; }

    public DateTime? ModifiedOnUtc { get; init; }

    public string? ModifiedByUserName { get; init; }

    public DateTime? RecycledAtUtc { get; init; }

    public string? RecycledByUserName { get; init; }

    public ApplicationProfileTemplateCatalogScope CatalogScope { get; init; }

    public ApplicationProfileTemplateDataScope DataScope { get; init; }

    public int SortOrder { get; init; }

    public string? CategoryKey { get; init; }

    /// <summary>Shared library row that is included for this profile (ON).</summary>
    public bool IsSharedIncluded { get; init; }

    public bool ShowsSharedChip =>
        ApplicationProfileWizardTemplateScopeHelper.IsShared(CatalogScope);
}

public sealed class ApplicationWordReportPackageCatalog
{
    public required IReadOnlyList<ApplicationWordReportPackageCatalogEntry> Entries { get; init; }

    public IReadOnlyList<ApplicationWordReportPackageCatalogEntry> RecycleBinEntries { get; init; } =
        Array.Empty<ApplicationWordReportPackageCatalogEntry>();

    /// <summary>
    /// Full shared library for the Shared tab (included and not). Empty when the catalog
    /// is the seeded fallback list (no nested profile templates).
    /// </summary>
    public IReadOnlyList<ApplicationWordReportPackageCatalogEntry> SharedEntries { get; init; } =
        Array.Empty<ApplicationWordReportPackageCatalogEntry>();

    /// <summary>
    /// Profile has Word/Excel nested templates (live or recycled). Show Recycle Bin tab;
    /// do not fall back to the seeded user-template catalog.
    /// </summary>
    public bool HasProfileNestedCatalog { get; init; }

    public int TotalCount => Entries.Count;

    public ApplicationWordReportPackageReadinessSummary ReadinessSummary =>
        ApplicationWordReportPackageReadinessSummary.Compute(Entries);
}

/// <summary>
/// Lists user-defined Word/Excel report templates visible for an application (Resminamalar catalog).
/// </summary>
public sealed class ApplicationWordReportPackageCatalogService
{
    private readonly IServiceProvider serviceProvider;

    public ApplicationWordReportPackageCatalogService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public ApplicationWordReportPackageCatalog Build(IObjectSpace objectSpace, ApplicationProfileInstance application) =>
        Build(objectSpace, application, WordReportGenerationContext.ForApplication());

    public ApplicationWordReportPackageCatalog Build(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        WordReportGenerationContext context)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (application == null)
            throw new ArgumentNullException(nameof(application));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var selectedItems = context.ResolveApplicationItems(objectSpace, application);
        var entries = new List<ApplicationWordReportPackageCatalogEntry>();

        if (ApplicationProfileNestedTemplateCatalogHelper.UsesProfileNestedCatalog(application, objectSpace))
        {
            entries.AddRange(BuildProfileNestedEntries(objectSpace, application, selectedItems));
            return new ApplicationWordReportPackageCatalog
            {
                Entries = entries,
                RecycleBinEntries = BuildRecycleBinEntries(objectSpace, application).ToList(),
                SharedEntries = BuildSharedLibraryEntries(objectSpace, entries),
                HasProfileNestedCatalog = true,
            };
        }

        var visibilityService = serviceProvider.GetService<IUserReportVisibilityService>();
        if (visibilityService != null)
        {
            var userTemplates = UserReportTemplateVisibilityHelper.GetVisibleActiveTemplates(
                objectSpace, visibilityService, application)
                .Where(template => WordReportDefinitionScopeHelper.MatchesUserTemplateScope(
                    template.RootBoType, context.Scope));

            foreach (var template in userTemplates.OrderBy(t => t.SortOrder).ThenBy(t => t.TemplateName))
            {
                var loadedTemplate = objectSpace.GetObjectsQuery<UserReportTemplate>()
                    .Include(t => t.Placeholders)
                    .Include(t => t.TemplateFile)
                    .FirstOrDefault(t => t.ID == template.ID)
                    ?? template;

                var (level, messageKey) = ApplicationWordReportPackageReadinessEvaluator.EvaluateUserTemplate(
                    objectSpace, application, loadedTemplate, selectedItems);
                var dryRunHints = ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
                    objectSpace, application, loadedTemplate, selectedItems);
                (level, messageKey) = ApplicationWordReportPackageReadinessEvaluator.ApplyDryRunHints(
                    level, messageKey, dryRunHints);

                entries.Add(new ApplicationWordReportPackageCatalogEntry
                {
                    EntryKey = BuildUserEntryKey(template),
                    DisplayName = template.TemplateName ?? string.Empty,
                    OutputFileName = template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel
                        ? ZipEntryFileNameSanitizer.BuildReportEntryName(template.TemplateName, ".xlsx")
                        : ZipEntryFileNameSanitizer.BuildReportEntryName(template.TemplateName, ".docx"),
                    Kind = template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel
                        ? ApplicationWordReportPackageEntryKind.UserExcel
                        : ApplicationWordReportPackageEntryKind.UserWord,
                    Readiness = level,
                    ReadinessMessageKey = messageKey,
                    ReadinessHints = dryRunHints,
                    UserReportTemplateId = template.ID,
                    CreatedOnUtc = template.CreatedOnUtc,
                    CreatedByUserName = template.CreatedByUserName,
                    ModifiedOnUtc = template.ModifiedOnUtc,
                    ModifiedByUserName = template.ModifiedByUserName
                });
            }
        }

        return new ApplicationWordReportPackageCatalog
        {
            Entries = entries,
            HasProfileNestedCatalog = false,
        };
    }

    private IEnumerable<ApplicationWordReportPackageCatalogEntry> BuildProfileNestedEntries(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        IList<ApplicationRosterMergeLine> selectedItems)
    {
        var profileTemplates = ApplicationProfileNestedTemplateCatalogHelper
            .GetOrderedTemplates(application, objectSpace);
        var userByName = ApplicationProfileNestedTemplateCatalogHelper.LoadActiveUserTemplatesByName(
            objectSpace,
            profileTemplates.Select(template => template.TemplateName));

        foreach (var profileTemplate in profileTemplates)
        {
            userByName.TryGetValue(profileTemplate.TemplateName ?? string.Empty, out var userTemplate);

            var (level, messageKey) = EvaluateProfileTemplateReadiness(
                objectSpace,
                application,
                profileTemplate,
                userTemplate,
                selectedItems);

            var dryRunHints = userTemplate != null
                ? ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
                    objectSpace, application, userTemplate, selectedItems)
                : Array.Empty<ApplicationWordReportPackageReadinessHint>();

            (level, messageKey) = ApplicationWordReportPackageReadinessEvaluator.ApplyDryRunHints(
                level, messageKey, dryRunHints);

            yield return new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = ApplicationProfileNestedTemplateCatalogHelper.BuildEntryKey(profileTemplate),
                DisplayName = profileTemplate.TemplateName ?? string.Empty,
                OutputFileName = ApplicationProfileNestedTemplateCatalogHelper.ResolveOutputFileName(
                    profileTemplate,
                    userTemplate),
                Kind = ApplicationProfileNestedTemplateCatalogHelper.ResolveEntryKind(
                    profileTemplate,
                    userTemplate),
                Readiness = level,
                ReadinessMessageKey = messageKey,
                ReadinessHints = dryRunHints,
                UserReportTemplateId = userTemplate?.ID,
                ApplicationProfileTemplateId = profileTemplate.ID,
                CanMoveToRecycleBin = ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(
                    profileTemplate),
                CreatedOnUtc = profileTemplate.CreatedOnUtc ?? userTemplate?.CreatedOnUtc,
                CreatedByUserName = profileTemplate.CreatedByUserName ?? userTemplate?.CreatedByUserName,
                ModifiedOnUtc = profileTemplate.ModifiedOnUtc ?? userTemplate?.ModifiedOnUtc,
                ModifiedByUserName = profileTemplate.ModifiedByUserName ?? userTemplate?.ModifiedByUserName,
                CatalogScope = profileTemplate.CatalogScope,
                DataScope = profileTemplate.DataScope,
                SortOrder = profileTemplate.SortOrder,
                CategoryKey = profileTemplate.CategoryKey,
                IsSharedIncluded = ApplicationProfileWizardTemplateScopeHelper.IsShared(
                    profileTemplate.CatalogScope),
            };
        }
    }

    private IReadOnlyList<ApplicationWordReportPackageCatalogEntry> BuildSharedLibraryEntries(
        IObjectSpace objectSpace,
        IReadOnlyList<ApplicationWordReportPackageCatalogEntry> thisProfileEntries)
    {
        var sharedRows = ApplicationProfileWizardTemplateCatalog.Build(objectSpace).Shared;
        if (sharedRows.Count == 0)
            return Array.Empty<ApplicationWordReportPackageCatalogEntry>();

        var includedByName = thisProfileEntries
            .Where(entry => entry.ShowsSharedChip)
            .GroupBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var list = new List<ApplicationWordReportPackageCatalogEntry>(sharedRows.Count);
        foreach (var row in sharedRows)
        {
            if (includedByName.TryGetValue(row.Name, out var included))
            {
                list.Add(included);
                continue;
            }

            var kind = row.Kind == ApplicationProfileTemplateKind.Excel
                ? ApplicationWordReportPackageEntryKind.UserExcel
                : ApplicationWordReportPackageEntryKind.UserWord;
            var extension = kind == ApplicationWordReportPackageEntryKind.UserExcel ? ".xlsx" : ".docx";
            list.Add(new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = BuildUserEntryKey(row.UserReportTemplateId),
                DisplayName = row.Name,
                OutputFileName = ZipEntryFileNameSanitizer.BuildReportEntryName(row.Name, extension),
                Kind = kind,
                Readiness = ApplicationWordReportPackageReadinessLevel.Ready,
                UserReportTemplateId = row.UserReportTemplateId,
                CatalogScope = row.Scope,
                DataScope = row.DataScope,
                SortOrder = row.SortOrder,
                CategoryKey = row.CategoryKeys.FirstOrDefault(),
                IsSharedIncluded = false,
            });
        }

        return list;
    }

    private IEnumerable<ApplicationWordReportPackageCatalogEntry> BuildRecycleBinEntries(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application)
    {
        var recycled = ApplicationProfileNestedTemplateCatalogHelper.GetRecycledTemplates(application, objectSpace);
        var userByName = ApplicationProfileNestedTemplateCatalogHelper.LoadActiveUserTemplatesByName(
            objectSpace,
            recycled.Select(template => template.TemplateName));

        foreach (var profileTemplate in recycled)
        {
            userByName.TryGetValue(profileTemplate.TemplateName ?? string.Empty, out var userTemplate);

            yield return new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = ApplicationProfileNestedTemplateCatalogHelper.BuildEntryKey(profileTemplate),
                DisplayName = profileTemplate.TemplateName ?? string.Empty,
                OutputFileName = ApplicationProfileNestedTemplateCatalogHelper.ResolveOutputFileName(
                    profileTemplate,
                    userTemplate),
                Kind = ApplicationProfileNestedTemplateCatalogHelper.ResolveEntryKind(
                    profileTemplate,
                    userTemplate),
                Readiness = ApplicationWordReportPackageReadinessLevel.Warning,
                ReadinessMessageKey = "ApplicationReportPackage.RecycleBin.InBin",
                UserReportTemplateId = userTemplate?.ID,
                ApplicationProfileTemplateId = profileTemplate.ID,
                RecycledAtUtc = profileTemplate.RecycledAtUtc,
                RecycledByUserName = profileTemplate.RecycledByUserName,
                CreatedOnUtc = profileTemplate.CreatedOnUtc ?? userTemplate?.CreatedOnUtc,
                CreatedByUserName = profileTemplate.CreatedByUserName ?? userTemplate?.CreatedByUserName,
                ModifiedOnUtc = profileTemplate.ModifiedOnUtc ?? userTemplate?.ModifiedOnUtc,
                ModifiedByUserName = profileTemplate.ModifiedByUserName ?? userTemplate?.ModifiedByUserName,
            };
        }
    }

    private static (ApplicationWordReportPackageReadinessLevel Level, string? MessageKey)
        EvaluateProfileTemplateReadiness(
            IObjectSpace objectSpace,
            ApplicationProfileInstance application,
            ApplicationProfileTemplate profileTemplate,
            UserReportTemplate? userTemplate,
            IList<ApplicationRosterMergeLine> selectedItems)
    {
        if (userTemplate == null)
        {
            return (ApplicationWordReportPackageReadinessLevel.Warning,
                "ApplicationReportPackage.Readiness.ProfileTemplateUnlinked");
        }

        var fileLoaded = profileTemplate.TemplateFile != null || userTemplate.TemplateFile != null;
        if (fileLoaded
            && !ApplicationProfileNestedTemplateCatalogHelper.HasMergeableFile(profileTemplate, userTemplate))
        {
            return (ApplicationWordReportPackageReadinessLevel.Warning,
                "ApplicationReportPackage.Readiness.NoTemplateFile");
        }

        return ApplicationWordReportPackageReadinessEvaluator.EvaluateUserTemplate(
            objectSpace,
            application,
            userTemplate,
            selectedItems,
            requireLoadedTemplateFile: fileLoaded);
    }

    internal static string BuildUserEntryKey(UserReportTemplate template) =>
        BuildUserEntryKey(template.ID);

    internal static string BuildUserEntryKey(Guid templateId) =>
        $"user:{templateId:D}";

    internal static bool TryParseUserTemplateId(string entryKey, out Guid templateId)
    {
        templateId = Guid.Empty;
        if (!entryKey.StartsWith("user:", StringComparison.Ordinal))
            return false;

        return Guid.TryParse(entryKey.AsSpan(5), out templateId) && templateId != Guid.Empty;
    }
}
