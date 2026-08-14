using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

public sealed class ApplicationProfileNestedTemplateTenantCatalogFile
{
    public List<ApplicationProfileNestedTemplateTenantCatalogRow> Rows { get; set; } = new();
}

public sealed class ApplicationProfileNestedTemplateTenantCatalogRow
{
    public string ProfileCatalogKey { get; set; } = string.Empty;

    public string ApplicationTypeName { get; set; } = string.Empty;

    public string ProfileCode { get; set; } = string.Empty;

    public string? DefaultProjectContractCode { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public string TemplateKind { get; set; } = nameof(ApplicationProfileTemplateKind.Word);

    public int SortOrder { get; set; }

    public string RootBoType { get; set; } = string.Empty;

    public string? SignOff { get; set; }

    public static ApplicationProfileNestedTemplateTenantCatalogRow FromProposal(
        ApplicationProfileNestedTemplateProposalRow proposal) =>
        new()
        {
            ProfileCatalogKey = proposal.ProfileCatalogKey,
            ApplicationTypeName = proposal.ApplicationTypeName,
            ProfileCode = proposal.ProfileCode,
            DefaultProjectContractCode = proposal.DefaultProjectContractCode,
            TemplateName = proposal.TemplateName,
            TemplateKind = proposal.TemplateKind.ToString(),
            SortOrder = proposal.SortOrder,
            RootBoType = proposal.RootBoType,
            SignOff = string.Empty,
        };
}

internal static class ApplicationProfileNestedTemplateTenantCatalogLoader
{
    private const string DefaultFileName = "application-profile-nested-templates.json";
    private const string CalikFileName = "application-profile-nested-templates.calik-energi.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    public static ApplicationProfileNestedTemplateTenantCatalogFile? Load()
    {
        var json = LookupCatalogResourceLoader.TryReadTenantOverlayText(CalikFileName)
            ?? LookupCatalogResourceLoader.TryReadTenantOverlayText(DefaultFileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + CalikFileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + DefaultFileName);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<ApplicationProfileNestedTemplateTenantCatalogFile>(json, JsonOptions);
    }

    public static bool TryLoadRows(out List<ApplicationProfileNestedTemplateTenantCatalogRow> rows)
    {
        rows = new List<ApplicationProfileNestedTemplateTenantCatalogRow>();
        var catalog = Load();
        if (catalog?.Rows == null || catalog.Rows.Count == 0)
            return false;

        rows = catalog.Rows
            .Where(r => r != null && IsApproved(r.SignOff))
            .ToList();
        return rows.Count > 0;
    }

    internal static bool IsApproved(string? signOff) =>
        string.Equals(signOff?.Trim(), "approved", StringComparison.OrdinalIgnoreCase);
}

internal static class ApplicationProfileNestedTemplateTenantCatalogSync
{
    public static void Sync(IObjectSpace objectSpace)
    {
        if (!ApplicationProfileNestedTemplateTenantCatalogLoader.TryLoadRows(out var rows))
        {
            Tracing.Tracer.LogText(
                "ApplicationProfileNestedTemplateTenantCatalogSync: no approved rows in tenant nested-template JSON.");
            return;
        }

        var profiles = objectSpace.GetObjectsQuery<ApplicationProfile>().ToList();
        var contracts = objectSpace.GetObjectsQuery<ProjectContract>().ToList();
        int created = 0;
        int updated = 0;
        int skipped = 0;

        foreach (var group in rows.GroupBy(r => r.ProfileCatalogKey, StringComparer.OrdinalIgnoreCase))
        {
            var header = group.First();
            var profile = FindProfile(profiles, contracts, header);
            if (profile == null)
            {
                skipped += group.Count();
                continue;
            }

            foreach (var row in group.OrderBy(r => r.SortOrder).ThenBy(r => r.TemplateName, StringComparer.OrdinalIgnoreCase))
            {
                var template = profile.NestedTemplates
                    .FirstOrDefault(t =>
                        string.Equals(t.TemplateName, row.TemplateName, StringComparison.OrdinalIgnoreCase));
                if (template == null)
                {
                    template = objectSpace.CreateObject<ApplicationProfileTemplate>();
                    template.ApplicationProfile = profile;
                    profile.NestedTemplates.Add(template);
                    created++;
                }
                else
                {
                    updated++;
                }

                template.TemplateName = row.TemplateName.Trim();
                template.TemplateKind = ParseKind(row.TemplateKind);
                template.SortOrder = row.SortOrder;
            }
        }

        if (created > 0 || updated > 0 || skipped > 0)
        {
            Tracing.Tracer.LogText(
                $"ApplicationProfileNestedTemplateTenantCatalogSync: created={created}, updated={updated}, skippedRows={skipped}.");
        }
    }

    private static ApplicationProfile? FindProfile(
        IReadOnlyList<ApplicationProfile> profiles,
        IReadOnlyList<ProjectContract> contracts,
        ApplicationProfileNestedTemplateTenantCatalogRow row)
    {
        if (string.IsNullOrWhiteSpace(row.ProfileCode))
            return null;

        ProjectContract? contract = null;
        if (!string.IsNullOrWhiteSpace(row.DefaultProjectContractCode))
        {
            var code = row.DefaultProjectContractCode.Trim();
            contract = contracts.FirstOrDefault(c =>
                (c.NameTm?.StartsWith(code, StringComparison.OrdinalIgnoreCase) ?? false)
                || string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        return ApplicationProfileCatalogGroupKey.FindProfile(
            profiles,
            row.ProfileCode.Trim(),
            contract?.ID,
            row.DefaultProjectContractCode);
    }

    private static ApplicationProfileTemplateKind ParseKind(string? value) =>
        Enum.TryParse<ApplicationProfileTemplateKind>(value, ignoreCase: true, out var kind)
            ? kind
            : ApplicationProfileTemplateKind.Word;
}
