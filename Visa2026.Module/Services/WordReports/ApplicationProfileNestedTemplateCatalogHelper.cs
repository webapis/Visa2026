using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Resminamalar catalog + merge bridge for <see cref="ApplicationProfileTemplate"/> rows (slice 12).
/// </summary>
public static class ApplicationProfileNestedTemplateCatalogHelper
{
    public const string EntryKeyPrefix = "profile:";

    /// <summary>
    /// True when the case should use This profile / Shared tabs (including an empty
    /// This profile). False only for dual-read Type-only instances with no profile.
    /// </summary>
    public static bool UsesProfileNestedCatalog(
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace = null)
    {
        if (application?.ApplicationProfile != null)
            return true;

        return HasAnyMergeNestedTemplate(application, objectSpace);
    }

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
            .Include(t => t.SourceFile)
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

        var lowered = name.ToLower();
        var matches = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.Placeholders)
            .Include(t => t.TemplateFile)
            .Where(t => t.IsActive
                && t.TemplateName != null
                && t.TemplateName.ToLower() == lowered)
            .OrderBy(t => t.ID)
            .ToList();

        // Word and Excel nested rows may share a display name. FirstOrDefault is unordered in
        // PostgreSQL, so Docker and a local database can each return the other file.
        return PickMergeTemplate(matches, profileTemplate.TemplateKind);
    }

    /// <summary>
    /// When several active templates share a name, keep the one whose output format matches
    /// <paramref name="kind"/>. A single name match is kept even when its format differs so an
    /// older row still merges; the caller overlays the nested file when that row has its own bytes.
    /// </summary>
    public static UserReportTemplate? PickMergeTemplate(
        IReadOnlyList<UserReportTemplate>? matches,
        ApplicationProfileTemplateKind kind,
        bool allowUnmatchedFallback = true)
    {
        if (matches == null || matches.Count == 0)
            return null;

        var wantExcel = kind == ApplicationProfileTemplateKind.Excel;
        var typed = matches
            .Where(template => (template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel) == wantExcel)
            .OrderBy(template => template.ID)
            .FirstOrDefault();
        if (typed != null)
            return typed;

        if (!allowUnmatchedFallback || matches.Count != 1)
            return null;

        return matches[0];
    }

    /// <summary>
    /// Preview and ZIP follow the nested row's own Word/Excel bytes. A shared
    /// <see cref="UserReportTemplate"/> looked up by name alone is the other format when both
    /// rows exist, which made the Excel download a <c>.docx</c>.
    /// </summary>
    public static UserReportTemplate WithProfileFile(
        UserReportTemplate userTemplate,
        ApplicationProfileTemplate profileTemplate)
    {
        ArgumentNullException.ThrowIfNull(userTemplate);
        ArgumentNullException.ThrowIfNull(profileTemplate);

        var file = profileTemplate.TemplateFile;
        var bytes = file?.Content;
        if (bytes == null || bytes.Length == 0)
            return userTemplate;

        var format = profileTemplate.TemplateKind == ApplicationProfileTemplateKind.Excel
            ? TemplateOutputFormat.Excel
            : TemplateOutputFormat.Word;
        var extension = format == TemplateOutputFormat.Excel ? ".xlsx" : ".docx";
        var fileName = file!.FileName;
        if (string.IsNullOrWhiteSpace(fileName)
            || !fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            fileName = ZipEntryFileNameSanitizer.BuildReportEntryName(
                string.IsNullOrWhiteSpace(profileTemplate.TemplateName)
                    ? userTemplate.TemplateName
                    : profileTemplate.TemplateName,
                extension);
        }

        var excelMode = format == TemplateOutputFormat.Excel
            && userTemplate.GetEffectiveOutputFormat() != TemplateOutputFormat.Excel
            ? ExcelMergeMode.ItemList
            : userTemplate.ExcelMergeMode;

        return new UserReportTemplate
        {
            ID = userTemplate.ID,
            TemplateName = string.IsNullOrWhiteSpace(profileTemplate.TemplateName)
                ? userTemplate.TemplateName
                : profileTemplate.TemplateName,
            TemplateOutputFormat = format,
            ExcelMergeMode = excelMode,
            RootBoType = userTemplate.RootBoType,
            IsActive = true,
            Placeholders = userTemplate.Placeholders,
            TemplateFile = new FileData
            {
                FileName = fileName,
                Content = bytes,
            },
        };
    }

    /// <summary>
    /// Active user templates matching nested catalog names. Placeholders only — do not
    /// load <see cref="UserReportTemplate.TemplateFile"/> (that is the Word/Excel blob).
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<UserReportTemplate>> LoadActiveUserTemplatesByName(
        IObjectSpace objectSpace,
        IEnumerable<string?> names)
    {
        var wanted = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (objectSpace == null || wanted.Count == 0)
            return new Dictionary<string, IReadOnlyList<UserReportTemplate>>(StringComparer.OrdinalIgnoreCase);

        var lowered = wanted.Select(name => name.ToLower()).ToList();
        var matches = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.Placeholders)
            .Where(t => t.IsActive
                && t.TemplateName != null
                && lowered.Contains(t.TemplateName.ToLower()))
            .OrderBy(t => t.ID)
            .ToList();

        var map = new Dictionary<string, List<UserReportTemplate>>(StringComparer.OrdinalIgnoreCase);
        foreach (var template in matches)
        {
            if (string.IsNullOrWhiteSpace(template.TemplateName))
                continue;
            if (!map.TryGetValue(template.TemplateName, out var list))
            {
                list = new List<UserReportTemplate>();
                map[template.TemplateName] = list;
            }

            list.Add(template);
        }

        var published = new Dictionary<string, IReadOnlyList<UserReportTemplate>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in map)
            published[pair.Key] = pair.Value;

        return published;
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
