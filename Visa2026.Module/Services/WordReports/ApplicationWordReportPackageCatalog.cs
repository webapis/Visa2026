using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.WordReports;

public enum ApplicationWordReportPackageEntryKind
{
    SystemWord = 0,
    UserWord = 1,
    UserExcel = 2
}

public sealed class ApplicationWordReportPackageCatalogEntry
{
    public required string EntryKey { get; init; }

    public required string DisplayName { get; init; }

    public string? OutputFileName { get; init; }

    public ApplicationWordReportPackageEntryKind Kind { get; init; }

    public ApplicationWordReportPackageEntrySource Source { get; init; }

    public ApplicationWordReportPackageReadinessLevel Readiness { get; init; }

    public string? ReadinessMessageKey { get; init; }

    public IReadOnlyList<ApplicationWordReportPackageReadinessHint> ReadinessHints { get; init; } =
        Array.Empty<ApplicationWordReportPackageReadinessHint>();
}

public sealed class ApplicationWordReportPackageCatalog
{
    public required IReadOnlyList<ApplicationWordReportPackageCatalogEntry> Entries { get; init; }

    public int TotalCount => Entries.Count;

    public ApplicationWordReportPackageReadinessSummary ReadinessSummary =>
        ApplicationWordReportPackageReadinessSummary.Compute(Entries);
}

/// <summary>
/// Lists Word/Excel reports that <see cref="WordReportBundleBuilder"/> would include for an application.
/// </summary>
public sealed class ApplicationWordReportPackageCatalogService
{
    private readonly IServiceProvider serviceProvider;

    public ApplicationWordReportPackageCatalogService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public ApplicationWordReportPackageCatalog Build(IObjectSpace objectSpace, Application application) =>
        Build(objectSpace, application, WordReportGenerationContext.ForApplication());

    public ApplicationWordReportPackageCatalog Build(
        IObjectSpace objectSpace,
        Application application,
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

        var definitions = serviceProvider
            .GetServices<IWordReportDefinition>()
            .Where(d => ApplicationWordReportApplicability.IsDefinitionApplicable(d, application))
            .Where(d => WordReportDefinitionScopeHelper.GetPackageScope(d) == context.Scope)
            .ToList();

        foreach (var def in definitions)
        {
            var systemHints = ApplicationWordReportPackageDryRunEvaluator.CollectSystemReportHints(
                objectSpace, application, def, selectedItems);
            var (level, messageKey) = ApplicationWordReportPackageReadinessEvaluator.EvaluateSystemReport(
                objectSpace, application, def, systemHints);

            entries.Add(new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = BuildSystemEntryKey(def),
                DisplayName = Path.GetFileName(def.GetFileName(application)),
                OutputFileName = def.GetFileName(application),
                Kind = ApplicationWordReportPackageEntryKind.SystemWord,
                Source = ApplicationWordReportPackageEntrySource.System,
                Readiness = level,
                ReadinessMessageKey = messageKey,
                ReadinessHints = systemHints
            });
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
                    Source = ApplicationWordReportPackageEntrySource.User,
                    Readiness = level,
                    ReadinessMessageKey = messageKey,
                    ReadinessHints = dryRunHints
                });
            }
        }

        return new ApplicationWordReportPackageCatalog { Entries = entries };
    }

    internal static string BuildSystemEntryKey(IWordReportDefinition definition) =>
        $"system:{definition.GetType().FullName}";

    internal static string BuildUserEntryKey(UserReportTemplate template) =>
        $"user:{template.ID:D}";
}
