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
}

public sealed class ApplicationWordReportPackageCatalog
{
    public required IReadOnlyList<ApplicationWordReportPackageCatalogEntry> Entries { get; init; }

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
                    UserReportTemplateId = template.ID
                });
            }
        }

        return new ApplicationWordReportPackageCatalog { Entries = entries };
    }

    internal static string BuildUserEntryKey(UserReportTemplate template) =>
        $"user:{template.ID:D}";

    internal static bool TryParseUserTemplateId(string entryKey, out Guid templateId)
    {
        templateId = Guid.Empty;
        if (!entryKey.StartsWith("user:", StringComparison.Ordinal))
            return false;

        return Guid.TryParse(entryKey.AsSpan(5), out templateId) && templateId != Guid.Empty;
    }
}
