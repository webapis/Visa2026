using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Resminamalar catalog + merge bridge for <see cref="ApplicationProfileTemplate"/> rows (slice 12).
/// </summary>
public static class ApplicationProfileNestedTemplateCatalogHelper
{
    public const string EntryKeyPrefix = "profile:";

    public static bool UsesProfileNestedCatalog(
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace = null) =>
        HasAnyMergeNestedTemplate(application, objectSpace);

    /// <summary>
    /// True when the profile has Word/Excel nested rows, including Recycle Bin.
    /// Keeps Resminamalar on the nested catalog (does not fall back to seeded library rows)
    /// after every officer template has been recycled.
    /// </summary>
    public static bool HasAnyMergeNestedTemplate(
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace = null)
    {
        var profileId = application?.ApplicationProfile?.ID ?? Guid.Empty;
        if (objectSpace != null && profileId != Guid.Empty)
            return HasAnyMergeNestedTemplate(objectSpace, profileId);

        return application?.ApplicationProfile?.NestedTemplates
            ?.Any(t => t != null && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm)
        == true;
    }

    public static IReadOnlyList<ApplicationProfileTemplate> GetOrderedTemplates(
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace = null)
    {
        var source = ResolveMergeTemplates(application, objectSpace, recycled: false);
        if (source.Count == 0)
            return Array.Empty<ApplicationProfileTemplate>();

        return source
            .Where(t => IsVisibleForInstance(t, application))
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.TemplateName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Recycled Word/Excel nested rows for this profile (not filtered by contract).
    /// Newest first.
    /// </summary>
    public static IReadOnlyList<ApplicationProfileTemplate> GetRecycledTemplates(
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace = null)
    {
        var source = ResolveMergeTemplates(application, objectSpace, recycled: true);
        if (source.Count == 0)
            return Array.Empty<ApplicationProfileTemplate>();

        return source
            .OrderByDescending(t => t.RecycledAtUtc)
            .ThenBy(t => t.TemplateName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Nested Word/Excel rows for the profile. Recycle state is taken from the database
    /// (AsNoTracking id query) so a pooled identity map cannot keep RecycledAtUtc stale.
    /// </summary>
    public static IReadOnlyList<ApplicationProfileTemplate> LoadMergeTemplates(
        IObjectSpace objectSpace,
        Guid profileId) =>
        LoadMergeTemplates(objectSpace, profileId, recycled: null);

    private static bool HasAnyMergeNestedTemplate(IObjectSpace objectSpace, Guid profileId)
    {
        if (objectSpace is EFCoreObjectSpace { DbContext: Visa2026EFCoreDbContext db })
        {
            return db.ApplicationProfileTemplates
                .AsNoTracking()
                .Any(t => t.ApplicationProfileId == profileId
                    && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm);
        }

        return objectSpace.GetObjectsQuery<ApplicationProfileTemplate>()
            .Any(t => t.ApplicationProfileId == profileId
                && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm);
    }

    private static IReadOnlyList<ApplicationProfileTemplate> LoadMergeTemplates(
        IObjectSpace objectSpace,
        Guid profileId,
        bool? recycled)
    {
        if (objectSpace == null || profileId == Guid.Empty)
            return Array.Empty<ApplicationProfileTemplate>();

        var ids = QueryMergeTemplateIds(objectSpace, profileId, recycled);
        if (ids.Count == 0)
            return Array.Empty<ApplicationProfileTemplate>();

        var templates = new List<ApplicationProfileTemplate>(ids.Count);
        foreach (var id in ids)
        {
            var template = objectSpace.GetObjectByKey<ApplicationProfileTemplate>(id);
            if (template == null)
                continue;

            RefreshTrackedTemplate(objectSpace, template);
            templates.Add(template);
        }

        return templates;
    }

    private static IReadOnlyList<Guid> QueryMergeTemplateIds(
        IObjectSpace objectSpace,
        Guid profileId,
        bool? recycled)
    {
        if (objectSpace is EFCoreObjectSpace { DbContext: Visa2026EFCoreDbContext db })
        {
            IQueryable<ApplicationProfileTemplate> query = db.ApplicationProfileTemplates
                .AsNoTracking()
                .Where(t => t.ApplicationProfileId == profileId
                    && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm);
            query = ApplyRecycledFilter(query, recycled);
            return query.Select(t => t.ID).ToList();
        }

        IQueryable<ApplicationProfileTemplate> objectsQuery = objectSpace
            .GetObjectsQuery<ApplicationProfileTemplate>()
            .Where(t => t.ApplicationProfileId == profileId
                && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm);
        objectsQuery = ApplyRecycledFilter(objectsQuery, recycled);
        return objectsQuery.Select(t => t.ID).ToList();
    }

    private static IQueryable<ApplicationProfileTemplate> ApplyRecycledFilter(
        IQueryable<ApplicationProfileTemplate> query,
        bool? recycled)
    {
        if (recycled == true)
            return query.Where(t => t.RecycledAtUtc != null);
        if (recycled == false)
            return query.Where(t => t.RecycledAtUtc == null);
        return query;
    }

    private static void RefreshTrackedTemplate(IObjectSpace objectSpace, ApplicationProfileTemplate template)
    {
        if (template == null || objectSpace.IsNewObject(template))
            return;

        if (objectSpace is EFCoreObjectSpace { DbContext: { } dbContext })
        {
            var entry = dbContext.Entry(template);
            if (entry.State is not (EntityState.Unchanged or EntityState.Modified))
                return;

            try
            {
                entry.Reload();
            }
            catch (InvalidOperationException)
            {
                // Row was purged or is no longer in this DbContext.
            }

            return;
        }

        objectSpace.ReloadObject(template);
    }

    private static IReadOnlyList<ApplicationProfileTemplate> ResolveMergeTemplates(
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace,
        bool? recycled)
    {
        var profileId = application?.ApplicationProfile?.ID ?? Guid.Empty;
        if (objectSpace != null && profileId != Guid.Empty)
            return LoadMergeTemplates(objectSpace, profileId, recycled);

        if (application?.ApplicationProfile?.NestedTemplates == null)
            return Array.Empty<ApplicationProfileTemplate>();

        IEnumerable<ApplicationProfileTemplate> source = application.ApplicationProfile.NestedTemplates
            .Where(t => t != null && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm);

        if (recycled == true)
            source = source.Where(t => t.RecycledAtUtc != null);
        else if (recycled == false)
            source = source.Where(t => t.RecycledAtUtc == null);

        return source.ToList();
    }

    public static bool IsVisibleForInstance(
        ApplicationProfileTemplate template,
        ApplicationProfileInstance? application)
    {
        if (template == null)
            return false;
        if (template.CatalogScope != ApplicationProfileTemplateCatalogScope.ProfileSpecific)
            return true;

        var route = ApplicationProfileConfigurationResolver.GetProgressRoute(application);
        if (route == ApplicationProfileInstanceProgressRouteKind.ViaMinistries)
        {
            var requiredId = template.ApplicableProjectContractId
                ?? template.ApplicableProjectContract?.ID;
            if (!requiredId.HasValue || requiredId.Value == Guid.Empty)
                return true;

            var instanceId = application?.ProjectContract?.ID;
            return instanceId.HasValue && instanceId.Value == requiredId.Value;
        }

        if (route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
        {
            var requiredId = template.ApplicableMigrationServiceId
                ?? template.ApplicableMigrationService?.ID;
            if (!requiredId.HasValue || requiredId.Value == Guid.Empty)
                return true;

            var instanceId = application?.MigrationService?.ID;
            return instanceId.HasValue && instanceId.Value == requiredId.Value;
        }

        return true;
    }

    public static string BuildEntryKey(ApplicationProfileTemplate template) =>
        $"{EntryKeyPrefix}{template.ID:D}";

    public static bool TryParseEntryKey(string entryKey, out Guid profileTemplateId)
    {
        profileTemplateId = Guid.Empty;
        if (!entryKey.StartsWith(EntryKeyPrefix, StringComparison.Ordinal))
            return false;

        return Guid.TryParse(entryKey.AsSpan(EntryKeyPrefix.Length), out profileTemplateId)
            && profileTemplateId != Guid.Empty;
    }

    public static ApplicationProfileTemplate? LoadProfileTemplate(
        IObjectSpace objectSpace,
        Guid profileTemplateId)
    {
        if (objectSpace == null || profileTemplateId == Guid.Empty)
            return null;

        return objectSpace.GetObjectsQuery<ApplicationProfileTemplate>()
            .Include(t => t.TemplateFile)
            .FirstOrDefault(t => t.ID == profileTemplateId);
    }

    public static UserReportTemplate? TryResolveMergeTemplate(
        IObjectSpace objectSpace,
        ApplicationProfileTemplate profileTemplate)
    {
        if (objectSpace == null || profileTemplate == null)
            return null;

        var name = profileTemplate.TemplateName?.Trim();
        if (string.IsNullOrEmpty(name))
            return null;

        return objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.Placeholders)
            .Include(t => t.TemplateFile)
            .Where(t => t.IsActive)
            .AsEnumerable()
            .FirstOrDefault(t => string.Equals(t.TemplateName, name, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasMergeableFile(ApplicationProfileTemplate profileTemplate, UserReportTemplate? userTemplate) =>
        profileTemplate.TemplateFile is { Size: > 0 }
        || userTemplate?.TemplateFile is { Size: > 0 };

    public static ApplicationWordReportPackageEntryKind ResolveEntryKind(
        ApplicationProfileTemplate profileTemplate,
        UserReportTemplate? userTemplate)
    {
        if (profileTemplate.TemplateKind == ApplicationProfileTemplateKind.Excel
            || userTemplate?.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel)
        {
            return ApplicationWordReportPackageEntryKind.UserExcel;
        }

        return ApplicationWordReportPackageEntryKind.UserWord;
    }

    public static string ResolveOutputFileName(
        ApplicationProfileTemplate profileTemplate,
        UserReportTemplate? userTemplate)
    {
        var extension = ResolveEntryKind(profileTemplate, userTemplate) == ApplicationWordReportPackageEntryKind.UserExcel
            ? ".xlsx"
            : ".docx";
        return ZipEntryFileNameSanitizer.BuildReportEntryName(profileTemplate.TemplateName, extension);
    }
}
