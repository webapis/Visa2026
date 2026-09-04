using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Builds Category / Global lists for Application Profile wizard step 4 from live
/// <see cref="UserReportTemplate"/> rows (no mock catalog).
/// </summary>
public static class ApplicationProfileWizardTemplateCatalog
{
    public const string CategoryInvitation = "Invitation";
    public const string CategoryVisa = "Visa";
    public const string CategoryWorkPermit = "WorkPermit";
    public const string CategoryRegistration = "Registration";
    public const string CategoryBorderZone = "BorderZone";

    /// <summary>
    /// User Report Templates whose name contains this marker are contract-bound
    /// letters officers upload under Profile-specific — not Shared Include.
    /// </summary>
    public const string ProfileSpecificUploadNameMarker = "GT-15";

    public static readonly (string Key, string Label)[] CategoryChips =
    [
        (CategoryInvitation, "Invitation"),
        (CategoryVisa, "Visa"),
        (CategoryWorkPermit, "Work permit"),
        (CategoryRegistration, "Registration"),
        (CategoryBorderZone, "Border zone"),
    ];

    public sealed class CatalogRow
    {
        public required Guid UserReportTemplateId { get; init; }
        public required string Name { get; init; }
        public required ApplicationProfileTemplateKind Kind { get; init; }
        public required int SortOrder { get; init; }
        public required ApplicationProfileTemplateCatalogScope Scope { get; init; }
        public required ApplicationProfileTemplateDataScope DataScope { get; init; }
        public required IReadOnlyList<string> CategoryKeys { get; init; }
        public string? FileLabel { get; init; }
        public long FileSizeBytes { get; init; }
    }

    public sealed class CatalogSnapshot
    {
        public required IReadOnlyList<CatalogRow> Global { get; init; }
        public required IReadOnlyList<CatalogRow> Category { get; init; }
        public IReadOnlyList<CatalogRow> Shared => MergeShared(Global, Category);
    }

    public static CatalogSnapshot Build(IObjectSpace objectSpace)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);

        var templates = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.TemplateFile)
            .Include(t => t.ApplicableTypeLinks)
                .ThenInclude(l => l.ApplicationType)
            .Include(t => t.ApplicableGroupLinks)
                .ThenInclude(l => l.ApplicationTypeGroup)
                    .ThenInclude(g => g.Members)
                        .ThenInclude(m => m.ApplicationType)
            .Where(t => t.IsActive)
            .AsEnumerable()
            .Where(t => t.GetEffectiveOutputFormat() is TemplateOutputFormat.Word or TemplateOutputFormat.Excel)
            .ToList();

        var global = new List<CatalogRow>();
        var category = new List<CatalogRow>();

        foreach (var template in templates)
        {
            var row = ToRow(template);
            if (IsProfileSpecificUploadOnly(row.Name))
                continue;
            if (row.Scope == ApplicationProfileTemplateCatalogScope.Global)
                global.Add(row);
            else
                category.Add(row);
        }

        return new CatalogSnapshot
        {
            Global = global
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Category = category
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };
    }

    public static IReadOnlyList<CatalogRow> MergeShared(
        IEnumerable<CatalogRow>? global,
        IEnumerable<CatalogRow>? category) =>
        (global ?? Array.Empty<CatalogRow>())
            .Concat(category ?? Array.Empty<CatalogRow>())
            .Where(r => !IsProfileSpecificUploadOnly(r.Name))
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static bool IsProfileSpecificUploadOnly(string? templateName) =>
        !string.IsNullOrWhiteSpace(templateName)
        && templateName.Contains(ProfileSpecificUploadNameMarker, StringComparison.OrdinalIgnoreCase);

    public static bool MatchesSharedSearch(CatalogRow row, string? query)
    {
        if (row == null)
            return false;
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var q = query.Trim();
        if (!string.IsNullOrEmpty(row.Name)
            && row.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var kind = row.Kind == ApplicationProfileTemplateKind.Excel ? "Excel" : "Word";
        if (kind.Contains(q, StringComparison.OrdinalIgnoreCase))
            return true;

        var data = row.DataScope switch
        {
            ApplicationProfileTemplateDataScope.ApplicationHeader => "header",
            ApplicationProfileTemplateDataScope.Both => "header roster",
            _ => "people m2m",
        };
        return data.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Families this profile is likely to Include: May produce flags, plus Registration related-to.
    /// Empty means there is no signal — callers should show the full shared catalog.
    /// </summary>
    public static IReadOnlyList<string> SuggestedCategoryKeys(ApplicationProfile? profile)
    {
        if (profile == null)
            return Array.Empty<string>();

        var keys = new List<string>();
        if (profile.ProduceInvitation)
            keys.Add(CategoryInvitation);
        if (profile.ProduceVisa)
            keys.Add(CategoryVisa);
        if (profile.ProduceWorkPermit)
            keys.Add(CategoryWorkPermit);
        if (profile.ActionFamily == ApplicationProfileActionFamily.Registration)
            keys.Add(CategoryRegistration);
        if (profile.ProduceBorderZone)
            keys.Add(CategoryBorderZone);
        return keys;
    }

    public static bool IsSuggestedForProfile(CatalogRow row, IReadOnlyCollection<string>? suggestedKeys)
    {
        if (row == null)
            return false;
        if (row.CategoryKeys == null || row.CategoryKeys.Count == 0)
            return true;
        if (suggestedKeys == null || suggestedKeys.Count == 0)
            return true;
        return row.CategoryKeys.Any(k => suggestedKeys.Contains(k));
    }

    public static ApplicationProfileTemplateDataScope DataScopeFromRootBo(UserReportBoType root) =>
        root switch
        {
            UserReportBoType.ApplicationProfileInstance => ApplicationProfileTemplateDataScope.ApplicationHeader,
            _ => ApplicationProfileTemplateDataScope.PeopleM2M,
        };

    public static UserReportBoType RootBoFromDataScope(ApplicationProfileTemplateDataScope dataScope) =>
        dataScope switch
        {
            ApplicationProfileTemplateDataScope.ApplicationHeader => UserReportBoType.ApplicationProfileInstance,
            _ => UserReportBoType.ApplicationItem,
        };

    private static CatalogRow ToRow(UserReportTemplate template)
    {
        var typeLinks = template.ApplicableTypeLinks?
            .Where(l => l.ApplicationTypeId != Guid.Empty || l.ApplicationType != null)
            .ToList() ?? [];
        var groupLinks = template.ApplicableGroupLinks?
            .Where(l => l.ApplicationTypeGroupId != Guid.Empty || l.ApplicationTypeGroup != null)
            .ToList() ?? [];

        var isGlobal = typeLinks.Count == 0 && groupLinks.Count == 0;
        var categoryKeys = isGlobal
            ? Array.Empty<string>()
            : DeriveCategoryKeys(typeLinks, groupLinks);

        var size = template.TemplateFile?.Size
            ?? template.TemplateFile?.Content?.Length
            ?? 0L;
        var fileName = template.TemplateFile?.FileName;
        var kind = template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel
            ? ApplicationProfileTemplateKind.Excel
            : ApplicationProfileTemplateKind.Word;

        return new CatalogRow
        {
            UserReportTemplateId = template.ID,
            Name = template.TemplateName?.Trim() ?? string.Empty,
            Kind = kind,
            SortOrder = template.SortOrder,
            Scope = isGlobal
                ? ApplicationProfileTemplateCatalogScope.Global
                : ApplicationProfileTemplateCatalogScope.Category,
            DataScope = DataScopeFromRootBo(template.RootBoType),
            CategoryKeys = categoryKeys,
            FileLabel = string.IsNullOrWhiteSpace(fileName) ? null : fileName,
            FileSizeBytes = size,
        };
    }

    private static IReadOnlyList<string> DeriveCategoryKeys(
        IReadOnlyList<UserReportTemplateApplicationType> typeLinks,
        IReadOnlyList<UserReportTemplateApplicationTypeGroup> groupLinks)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var link in groupLinks)
        {
            AddKeysFromText(keys, link.ApplicationTypeGroup?.Name);
            if (link.ApplicationTypeGroup?.Members == null)
                continue;
            foreach (var member in link.ApplicationTypeGroup.Members)
                AddKeysFromApplicationType(keys, member?.ApplicationType);
        }

        foreach (var link in typeLinks)
            AddKeysFromApplicationType(keys, link.ApplicationType);

        if (keys.Count == 0)
            keys.Add(CategoryVisa);

        return keys
            .OrderBy(k => Array.FindIndex(CategoryChips, c => c.Key.Equals(k, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddKeysFromApplicationType(HashSet<string> keys, ApplicationType? type)
    {
        if (type == null)
            return;

        AddKeysFromText(keys, type.Name);

        if (type.CanIssueInvitation || type.ShowInvitations)
            keys.Add(CategoryInvitation);
        if (type.CanIssueVisa || type.ShowVisas)
            keys.Add(CategoryVisa);
        if (type.CanIssueWorkPermit || type.ShowWorkPermits)
            keys.Add(CategoryWorkPermit);
        if (type.ShowRegistrations)
            keys.Add(CategoryRegistration);
        if (type.ShowBorderZoneLocation)
            keys.Add(CategoryBorderZone);
    }

    private static void AddKeysFromText(HashSet<string> keys, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (ContainsAny(text, "invit", "cagyr"))
            keys.Add(CategoryInvitation);
        if (ContainsAny(text, "visa", "wiza"))
            keys.Add(CategoryVisa);
        if (ContainsAny(text, "workpermit", "work permit", "is rugsady", "rugsat"))
            keys.Add(CategoryWorkPermit);
        if (ContainsAny(text, "registr", "hasaba", ApplicationTypeGroupNames.Registration))
            keys.Add(CategoryRegistration);
        if (ContainsAny(text, "border", "serhet", "zone"))
            keys.Add(CategoryBorderZone);
    }

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.OrdinalIgnoreCase));
}