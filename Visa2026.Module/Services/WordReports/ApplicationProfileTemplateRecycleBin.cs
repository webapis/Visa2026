using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Resminamalar Recycle Bin for officer-created nested templates
/// (<see cref="ApplicationProfileTemplateCatalogScope.ProfileSpecific"/>).
/// Catalog Delete sets <see cref="ApplicationProfileTemplate.RecycledAtUtc"/>;
/// Recycle Bin Restore clears it; Recycle Bin Delete permanently removes the nested row
/// (and the linked <see cref="UserReportTemplate"/> when nothing else shares the name).
/// </summary>
public static class ApplicationProfileTemplateRecycleBin
{
    public static bool CanMoveToRecycleBin(ApplicationProfileTemplate? template) =>
        template != null
        && template.CatalogScope == ApplicationProfileTemplateCatalogScope.ProfileSpecific
        && template.TemplateKind != ApplicationProfileTemplateKind.PdfForm
        && template.RecycledAtUtc == null;

    public static bool IsRecycled(ApplicationProfileTemplate? template) =>
        template?.RecycledAtUtc != null;

    public static void Recycle(ApplicationProfileTemplate template, string? userName)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (!CanMoveToRecycleBin(template))
        {
            throw new InvalidOperationException(
                "Only this-profile officer templates can be moved to Recycle Bin.");
        }

        template.RecycledAtUtc = DateTime.UtcNow;
        template.RecycledByUserName = string.IsNullOrWhiteSpace(userName)
            ? null
            : userName.Trim();
        if (template.RecycledByUserName?.Length > 255)
            template.RecycledByUserName = template.RecycledByUserName[..255];
    }

    public static void Restore(ApplicationProfileTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        template.RecycledAtUtc = null;
        template.RecycledByUserName = null;
    }

    /// <summary>
    /// Permanently deletes a recycled nested row. Deletes the linked master
    /// <see cref="UserReportTemplate"/> only when this was a profile-specific row and no
    /// other nested template (any profile, including recycled) uses the same name.
    /// </summary>
    public static void Purge(IObjectSpace objectSpace, ApplicationProfileTemplate template)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(template);
        if (template.RecycledAtUtc == null)
        {
            throw new InvalidOperationException(
                "Permanent delete is only allowed from Recycle Bin.");
        }

        var name = template.TemplateName?.Trim();
        var templateId = template.ID;
        var catalogScope = template.CatalogScope;

        UserReportTemplate? userTemplate = null;
        if (catalogScope == ApplicationProfileTemplateCatalogScope.ProfileSpecific)
        {
            userTemplate = ApplicationProfileNestedTemplateCatalogHelper.TryResolveMergeTemplate(
                objectSpace,
                template);
        }

        var otherNestedUsesName = !string.IsNullOrEmpty(name)
            && objectSpace.GetObjectsQuery<ApplicationProfileTemplate>()
                .AsEnumerable()
                .Any(t => t.ID != templateId
                    && string.Equals(t.TemplateName?.Trim(), name, StringComparison.OrdinalIgnoreCase));

        objectSpace.Delete(template);

        if (userTemplate != null
            && ShouldDeleteLinkedUserReportTemplate(catalogScope, otherNestedUsesName))
        {
            objectSpace.Delete(userTemplate);
        }
    }

    public static bool ShouldDeleteLinkedUserReportTemplate(
        ApplicationProfileTemplateCatalogScope catalogScope,
        bool otherNestedUsesSameName) =>
        catalogScope == ApplicationProfileTemplateCatalogScope.ProfileSpecific
        && !otherNestedUsesSameName;
}
