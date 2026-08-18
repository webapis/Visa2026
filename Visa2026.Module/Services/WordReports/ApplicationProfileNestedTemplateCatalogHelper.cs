using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Resminamalar catalog + merge bridge for <see cref="ApplicationProfileTemplate"/> rows (slice 12).
/// </summary>
public static class ApplicationProfileNestedTemplateCatalogHelper
{
    public const string EntryKeyPrefix = "profile:";

    public static bool UsesProfileNestedCatalog(ApplicationProfileInstance? application) =>
        GetOrderedTemplates(application).Count > 0;

    public static IReadOnlyList<ApplicationProfileTemplate> GetOrderedTemplates(ApplicationProfileInstance? application)
    {
        if (application?.ApplicationProfile?.NestedTemplates == null)
            return Array.Empty<ApplicationProfileTemplate>();

        return application.ApplicationProfile.NestedTemplates
            .Where(t => t != null && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm)
            .Where(t => IsVisibleForInstance(t, application))
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.TemplateName, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
