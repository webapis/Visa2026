using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.DatabaseUpdate;

public sealed class ApplicationProfileNestedTemplateProposalRow
{
    public string ProfileCatalogKey { get; init; } = string.Empty;

    public string ApplicationTypeName { get; init; } = string.Empty;

    public string? DefaultProjectContractCode { get; init; }

    public string ProfileCode { get; init; } = string.Empty;

    public string TemplateName { get; init; } = string.Empty;

    public ApplicationProfileTemplateKind TemplateKind { get; init; }

    public int SortOrder { get; init; }

    public string RootBoType { get; init; } = string.Empty;
}

/// <summary>
/// Wave 3 — proposes <see cref="ApplicationProfileTemplate"/> rows per profile catalog key
/// from seeded <see cref="UserReportTemplate"/> visibility (type / group / contract).
/// </summary>
public static class ApplicationProfileNestedTemplateProposalBuilder
{
    public static IReadOnlyList<ApplicationProfileNestedTemplateProposalRow> BuildForCatalogRow(
        IObjectSpace objectSpace,
        ApplicationProfileTenantCatalogRow catalogRow,
        IUserReportVisibilityService visibilityService)
    {
        if (objectSpace == null || catalogRow == null || visibilityService == null)
            return Array.Empty<ApplicationProfileNestedTemplateProposalRow>();

        var catalogKey = !string.IsNullOrWhiteSpace(catalogRow.ProfileCatalogKey)
            ? catalogRow.ProfileCatalogKey.Trim()
            : ApplicationProfileCatalogGroupKey.BuildCatalogKey(
                catalogRow.ApplicationTypeName,
                catalogRow.DefaultProjectContractCode);

        var applicationType = objectSpace.GetObjectsQuery<ApplicationType>()
            .AsEnumerable()
            .FirstOrDefault(t => string.Equals(t.Name, catalogRow.ApplicationTypeName, StringComparison.OrdinalIgnoreCase));
        if (applicationType == null)
            return Array.Empty<ApplicationProfileNestedTemplateProposalRow>();

        ProjectContract? contract = null;
        if (!string.IsNullOrWhiteSpace(catalogRow.DefaultProjectContractCode))
        {
            var code = catalogRow.DefaultProjectContractCode.Trim();
            contract = objectSpace.GetObjectsQuery<ProjectContract>()
                .AsEnumerable()
                .FirstOrDefault(c =>
                    (c.NameTm?.StartsWith(code, StringComparison.OrdinalIgnoreCase) ?? false)
                    || string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        var probe = objectSpace.CreateObject<ApplicationProfileInstance>();
        probe.ApplicationType = applicationType;
        probe.ProjectContract = contract;

        var templates = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .Include(t => t.ApplicableTypeLinks)
                .ThenInclude(l => l.ApplicationType)
            .Include(t => t.ApplicableGroupLinks)
                .ThenInclude(l => l.ApplicationTypeGroup)
                    .ThenInclude(g => g!.Members)
                        .ThenInclude(m => m.ApplicationType)
            .Include(t => t.ApplicableProjectContractLinks)
                .ThenInclude(l => l.ProjectContract)
            .Where(t => t.IsActive)
            .AsEnumerable()
            .Where(t => t.RootBoType is UserReportBoType.ApplicationProfileInstance or UserReportBoType.ApplicationItem)
            .Where(t => visibilityService.IsTemplateVisible(t, probe))
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.TemplateName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return templates
            .Select(t => new ApplicationProfileNestedTemplateProposalRow
            {
                ProfileCatalogKey = catalogKey,
                ApplicationTypeName = catalogRow.ApplicationTypeName,
                DefaultProjectContractCode = catalogRow.DefaultProjectContractCode,
                ProfileCode = catalogRow.Code,
                TemplateName = t.TemplateName ?? string.Empty,
                TemplateKind = ResolveTemplateKind(t),
                SortOrder = t.SortOrder,
                RootBoType = t.RootBoType.ToString(),
            })
            .ToList();
    }

    public static IReadOnlyList<ApplicationProfileNestedTemplateProposalRow> BuildForTenantCatalog(
        IObjectSpace objectSpace,
        IUserReportVisibilityService visibilityService)
    {
        if (!ApplicationProfileTenantCatalogLoader.TryLoadRows(out var catalogRows) || catalogRows.Count == 0)
            return Array.Empty<ApplicationProfileNestedTemplateProposalRow>();

        var rows = new List<ApplicationProfileNestedTemplateProposalRow>();
        foreach (var catalogRow in catalogRows)
        {
            rows.AddRange(BuildForCatalogRow(objectSpace, catalogRow, visibilityService));
        }

        return rows;
    }

    private static ApplicationProfileTemplateKind ResolveTemplateKind(UserReportTemplate template) =>
        template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel
            ? ApplicationProfileTemplateKind.Excel
            : ApplicationProfileTemplateKind.Word;
}
