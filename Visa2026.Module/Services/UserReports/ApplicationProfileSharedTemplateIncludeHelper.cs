using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Case Resminamalar Shared-tab include/exclude — same nested attach as wizard Include.
/// Persists immediately on the case (wizard waits for Save profile).
/// </summary>
public static class ApplicationProfileSharedTemplateIncludeHelper
{
    public static bool ShowsSharedChip(ApplicationProfileTemplateCatalogScope scope) =>
        ApplicationProfileWizardTemplateScopeHelper.IsShared(scope);

    public static ApplicationProfileTemplate Include(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileWizardTemplateCatalog.CatalogRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return Include(
            objectSpace,
            profile,
            row.Name,
            row.Kind,
            row.SortOrder,
            row.Scope,
            row.DataScope,
            row.CategoryKeys?.FirstOrDefault());
    }

    public static ApplicationProfileTemplate Include(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        string? name,
        ApplicationProfileTemplateKind kind,
        int sortOrder,
        ApplicationProfileTemplateCatalogScope catalogScope,
        ApplicationProfileTemplateDataScope dataScope,
        string? categoryKey)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(profile);

        name = name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A template name is required.", nameof(name));

        var existing = FindLiveInclude(objectSpace, profile, name);
        if (existing != null)
            return existing;

        var template = objectSpace.CreateObject<ApplicationProfileTemplate>();
        template.ApplicationProfile = profile;
        template.TemplateName = name;
        template.TemplateKind = kind;
        template.SortOrder = sortOrder > 0
            ? sortOrder
            : (profile.NestedTemplates?.Count ?? 0) + 1;
        template.CatalogScope = catalogScope;
        template.DataScope = dataScope;
        template.CategoryKey = string.IsNullOrWhiteSpace(categoryKey) ? null : categoryKey.Trim();
        if (profile.NestedTemplates != null && !profile.NestedTemplates.Contains(template))
            profile.NestedTemplates.Add(template);

        ApplicationProfileTemplateUserReportBridge.EnsureLinkedUserReportTemplate(
            objectSpace,
            template,
            ApplicationProfileWizardTemplateCatalog.RootBoFromDataScope(template.DataScope));

        return template;
    }

    public static void Exclude(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileTemplate nested)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(nested);

        if (!ShowsSharedChip(nested.CatalogScope))
            throw new InvalidOperationException("Only shared catalog includes can be turned off here.");

        // Mark deleted first so the config-lock hook sees a Shared-include mutation
        // (same pattern as Recycle Bin: operational catalog change, not a locked scalar edit).
        objectSpace.Delete(nested);
        ApplicationProfileLockHelper.EnsureNestedConfigurationEditable(profile, objectSpace, nested);
    }

    public static ApplicationProfileTemplate? FindLiveInclude(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        string? templateName)
    {
        if (objectSpace == null || profile == null || string.IsNullOrWhiteSpace(templateName))
            return null;

        var profileId = profile.ID;
        var name = templateName.Trim();
        var lowered = name.ToLower();
        return objectSpace.GetObjectsQuery<ApplicationProfileTemplate>()
            .FirstOrDefault(t =>
                t.ApplicationProfileId == profileId
                && t.RecycledAtUtc == null
                && t.TemplateKind != ApplicationProfileTemplateKind.PdfForm
                && t.TemplateName != null
                && t.TemplateName.ToLower() == lowered);
    }
}