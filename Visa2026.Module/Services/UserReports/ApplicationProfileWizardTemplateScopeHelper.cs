using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Edit-dialog catalog scope: clone onto this profile, or change shared User Report Template visibility.
/// </summary>
public static class ApplicationProfileWizardTemplateScopeHelper
{
    public const int TemplateNameMaxLength = 255;

    public sealed class ApplyResult
    {
        public required string TemplateName { get; init; }
        public required string StatusMessage { get; init; }
        public Guid LinkedUserReportTemplateId { get; init; }
    }

    public static bool IsShared(ApplicationProfileTemplateCatalogScope scope) =>
        scope != ApplicationProfileTemplateCatalogScope.ProfileSpecific;

    /// <summary>
    /// Officer UI has Profile-specific vs Shared. Shared keeps an existing Category/Global
    /// master; a profile-only file promoted to Shared becomes Global (no type links).
    /// </summary>
    public static ApplicationProfileTemplateCatalogScope SharedTarget(
        ApplicationProfileTemplateCatalogScope saved) =>
        saved == ApplicationProfileTemplateCatalogScope.ProfileSpecific
            ? ApplicationProfileTemplateCatalogScope.Global
            : saved;

    public static bool RequiresSharedVisibilityConfirm(
        ApplicationProfileTemplateCatalogScope from,
        ApplicationProfileTemplateCatalogScope to) =>
        from != to && to != ApplicationProfileTemplateCatalogScope.ProfileSpecific;

    public static bool IsCloneToThisProfile(
        ApplicationProfileTemplateCatalogScope from,
        ApplicationProfileTemplateCatalogScope to) =>
        from != ApplicationProfileTemplateCatalogScope.ProfileSpecific
        && to == ApplicationProfileTemplateCatalogScope.ProfileSpecific;

    public static string ConfirmMessage(
        ApplicationProfileTemplateCatalogScope from,
        ApplicationProfileTemplateCatalogScope to)
    {
        if (IsCloneToThisProfile(from, to))
        {
            return "Creates a copy for this profile. The shared catalog file is unchanged.";
        }

        return "This file will be Shared. Other profiles can Include it.";
    }

    public static string BuildProfileCopyName(
        string originalName,
        string? profileName,
        Func<string, bool> isTaken)
    {
        var original = string.IsNullOrWhiteSpace(originalName) ? "Template" : originalName.Trim();
        var profile = string.IsNullOrWhiteSpace(profileName) ? "profile" : profileName.Trim();
        var suffix = " (" + profile + ")";
        var maxBase = TemplateNameMaxLength - suffix.Length - 4;
        if (maxBase < 8)
            maxBase = 8;
        if (original.Length > maxBase)
            original = original[..maxBase].TrimEnd();

        var baseName = original + suffix;
        if (baseName.Length > TemplateNameMaxLength)
            baseName = baseName[..TemplateNameMaxLength].TrimEnd();

        var candidate = baseName;
        var n = 2;
        while (isTaken(candidate))
        {
            var numbered = baseName + " " + n;
            if (numbered.Length > TemplateNameMaxLength)
                numbered = numbered[..TemplateNameMaxLength].TrimEnd();
            candidate = numbered;
            n++;
            if (n > 99)
                break;
        }

        return candidate;
    }

    public static bool TypeMatchesCategory(ApplicationType type, string categoryKey)
    {
        if (type == null || string.IsNullOrWhiteSpace(categoryKey))
            return false;

        if (categoryKey.Equals(ApplicationProfileWizardTemplateCatalog.CategoryInvitation, StringComparison.OrdinalIgnoreCase))
            return type.CanIssueInvitation || type.ShowInvitations;
        if (categoryKey.Equals(ApplicationProfileWizardTemplateCatalog.CategoryVisa, StringComparison.OrdinalIgnoreCase))
            return type.CanIssueVisa || type.ShowVisas;
        if (categoryKey.Equals(ApplicationProfileWizardTemplateCatalog.CategoryWorkPermit, StringComparison.OrdinalIgnoreCase))
            return type.CanIssueWorkPermit || type.ShowWorkPermits;
        if (categoryKey.Equals(ApplicationProfileWizardTemplateCatalog.CategoryRegistration, StringComparison.OrdinalIgnoreCase))
            return type.ShowRegistrations;
        if (categoryKey.Equals(ApplicationProfileWizardTemplateCatalog.CategoryBorderZone, StringComparison.OrdinalIgnoreCase))
            return type.ShowBorderZoneLocation;

        return false;
    }

    public static ApplyResult Apply(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileTemplate nested,
        ApplicationProfileTemplateCatalogScope from,
        ApplicationProfileTemplateCatalogScope to,
        IReadOnlyCollection<string> categoryKeys)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(nested);

        if (IsCloneToThisProfile(from, to))
            return CloneToProfileSpecific(objectSpace, profile, nested);

        nested.CatalogScope = to;
        var keys = NormalizeCategoryKeys(categoryKeys);
        if (to == ApplicationProfileTemplateCatalogScope.Category)
        {
            if (keys.Count == 0)
                throw new InvalidOperationException("Select at least one category.");
            nested.CategoryKey = keys[0];
            ClearApplicability(nested);
        }
        else if (to == ApplicationProfileTemplateCatalogScope.Global && from != to)
        {
            nested.CategoryKey = null;
            ClearApplicability(nested);
        }
        else if (to == ApplicationProfileTemplateCatalogScope.ProfileSpecific)
        {
            nested.CategoryKey = null;
        }

        var userTemplate = ApplicationProfileTemplateUserReportBridge.EnsureLinkedUserReportTemplate(
            objectSpace,
            nested,
            ApplicationProfileWizardTemplateCatalog.RootBoFromDataScope(nested.DataScope));

        if (from != to && to == ApplicationProfileTemplateCatalogScope.Global)
            ClearSharedApplicability(objectSpace, userTemplate);
        else if (to == ApplicationProfileTemplateCatalogScope.Category)
            SetSharedCategoryTypes(objectSpace, userTemplate, keys);

        var status = from == to
            ? "Metadata saved."
            : to == ApplicationProfileTemplateCatalogScope.ProfileSpecific
                ? "Copied for this profile. Shared catalog file is unchanged."
                : "Scope set to Shared.";

        return new ApplyResult
        {
            TemplateName = nested.TemplateName ?? string.Empty,
            StatusMessage = status,
            LinkedUserReportTemplateId = userTemplate.ID,
        };
    }

    private static ApplyResult CloneToProfileSpecific(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileTemplate nested)
    {
        var sourceName = nested.TemplateName?.Trim() ?? "Template";
        var source = ApplicationProfileTemplateUserReportBridge.TryFindByName(objectSpace, sourceName);
        CopyMasterFileOntoNested(objectSpace, nested, source);

        var taken = BuildTakenNames(objectSpace, profile, nested);
        var copyName = BuildProfileCopyName(sourceName, profile.Name, taken.Contains);
        nested.TemplateName = copyName;
        nested.CatalogScope = ApplicationProfileTemplateCatalogScope.ProfileSpecific;
        nested.CategoryKey = null;
        ClearApplicability(nested);

        var userTemplate = ApplicationProfileTemplateUserReportBridge.EnsureLinkedUserReportTemplate(
            objectSpace,
            nested,
            ApplicationProfileWizardTemplateCatalog.RootBoFromDataScope(nested.DataScope));

        return new ApplyResult
        {
            TemplateName = copyName,
            StatusMessage = "Copied for this profile. Shared catalog file is unchanged.",
            LinkedUserReportTemplateId = userTemplate.ID,
        };
    }

    private static HashSet<string> BuildTakenNames(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileTemplate nested)
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var other in profile.NestedTemplates ?? Array.Empty<ApplicationProfileTemplate>())
        {
            if (other == null || ReferenceEquals(other, nested) || string.IsNullOrWhiteSpace(other.TemplateName))
                continue;
            taken.Add(other.TemplateName.Trim());
        }

        foreach (var template in objectSpace.GetObjectsQuery<UserReportTemplate>())
        {
            if (!string.IsNullOrWhiteSpace(template.TemplateName))
                taken.Add(template.TemplateName.Trim());
        }

        return taken;
    }

    private static void CopyMasterFileOntoNested(
        IObjectSpace objectSpace,
        ApplicationProfileTemplate nested,
        UserReportTemplate? source)
    {
        var bytes = source?.TemplateFile?.Content;
        var fileName = source?.TemplateFile?.FileName;
        if (bytes == null || bytes.Length == 0)
        {
            bytes = nested.TemplateFile?.Content;
            fileName ??= nested.TemplateFile?.FileName;
        }

        if (bytes == null || bytes.Length == 0)
            return;

        nested.TemplateFile ??= objectSpace.CreateObject<FileData>();
        nested.TemplateFile.FileName = string.IsNullOrWhiteSpace(fileName)
            ? (nested.TemplateName + ".docx")
            : fileName;
        nested.TemplateFile.Content = bytes;
    }

    private static void ClearApplicability(ApplicationProfileTemplate nested)
    {
        nested.ApplicableProjectContract = null;
        nested.ApplicableProjectContractId = null;
        nested.ApplicableMigrationService = null;
        nested.ApplicableMigrationServiceId = null;
    }

    private static void ClearSharedApplicability(IObjectSpace objectSpace, UserReportTemplate template)
    {
        foreach (var link in template.ApplicableTypeLinks.ToList())
        {
            template.ApplicableTypeLinks.Remove(link);
            objectSpace.Delete(link);
        }

        foreach (var link in template.ApplicableGroupLinks.ToList())
        {
            template.ApplicableGroupLinks.Remove(link);
            objectSpace.Delete(link);
        }
    }

    private static void SetSharedCategoryTypes(
        IObjectSpace objectSpace,
        UserReportTemplate template,
        IReadOnlyCollection<string> categoryKeys)
    {
        ClearSharedApplicability(objectSpace, template);
        var keys = NormalizeCategoryKeys(categoryKeys);
        var linked = new HashSet<Guid>();
        foreach (var type in objectSpace.GetObjectsQuery<ApplicationType>())
        {
            if (type == null || !keys.Any(k => TypeMatchesCategory(type, k)))
                continue;
            if (!linked.Add(type.ID))
                continue;

            var link = objectSpace.CreateObject<UserReportTemplateApplicationType>();
            link.UserReportTemplate = template;
            link.ApplicationType = type;
            template.ApplicableTypeLinks.Add(link);
        }
    }

    private static List<string> NormalizeCategoryKeys(IReadOnlyCollection<string>? categoryKeys) =>
        (categoryKeys ?? Array.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
