using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

internal static class ProjectContractApprovalLegProfileLinker
{
    internal static bool TryLinkContractFromCatalogRow(
        IObjectSpace objectSpace,
        ProjectContract contract,
        IReadOnlyDictionary<string, JsonElement> row)
    {
        if (contract.ApprovalLegProfile != null)
            return false;

        if (!row.TryGetValue("MinistryLegs", out var legs) || legs.ValueKind != JsonValueKind.Array || legs.GetArrayLength() == 0)
            return false;

        var orderedShortNames = legs.EnumerateArray()
            .Where(l => l.ValueKind == JsonValueKind.Object)
            .OrderBy(l => l.TryGetProperty("Sequence", out var seq) ? seq.GetInt32() : 0)
            .Select(l => ReadString(l, "ApprovingMinistryShortNameTm"))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToList();

        var profile = ResolveProfile(objectSpace, orderedShortNames);
        if (profile == null)
            return false;

        contract.ApprovalLegProfile = profile;
        return true;
    }

    internal static ApprovalLegProfile? ResolveProfile(
        IObjectSpace objectSpace,
        IReadOnlyList<string> orderedMinistryShortNamesTm)
    {
        var profileCode = ApprovalLegProfileCodeHelper.BuildProfileCode(orderedMinistryShortNamesTm);
        var profileNameTm = ApprovalLegProfileCodeHelper.BuildProfileNameTm(orderedMinistryShortNamesTm);

        if (!string.IsNullOrWhiteSpace(profileCode))
        {
            var byCode = objectSpace.GetObjectsQuery<ApprovalLegProfile>()
                .FirstOrDefault(p => p.Code == profileCode);
            if (byCode != null)
                return byCode;
        }

        if (string.IsNullOrWhiteSpace(profileNameTm))
            return null;

        return objectSpace.GetObjectsQuery<ApprovalLegProfile>()
            .FirstOrDefault(p => p.NameTm == profileNameTm);
    }

    internal static int LinkAll(IObjectSpace objectSpace)
    {
        int fromJoin = LinkFromJoinTable(objectSpace);
        int fromCatalog = LinkFromProjectContractCatalogJson(objectSpace);
        return fromJoin + fromCatalog;
    }

    private static int LinkFromJoinTable(IObjectSpace objectSpace)
    {
        int linked = 0;
        foreach (var join in objectSpace.GetObjectsQuery<ProjectContractApprovalLegProfile>().ToList())
        {
            if (join.ProjectContract == null || join.ApprovalLegProfile == null)
                continue;
            if (join.ProjectContract.ApprovalLegProfile != null)
                continue;
            join.ProjectContract.ApprovalLegProfile = join.ApprovalLegProfile;
            linked++;
        }

        return linked;
    }

    private static int LinkFromProjectContractCatalogJson(IObjectSpace objectSpace)
    {
        int linked = 0;
        foreach (var fileName in new[] { "project-contract.json", "project-contract.calik-energi.json" })
        {
            var json = LookupCatalogResourceLoader.TryReadTenantOverlayText(fileName)
                ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + fileName)
                ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText(fileName);
            if (string.IsNullOrWhiteSpace(json))
                continue;

            linked += LinkFromCatalogJson(objectSpace, json);
        }

        return linked;
    }

    private static int LinkFromCatalogJson(IObjectSpace objectSpace, string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("rows", out var rows) != true || rows.ValueKind != JsonValueKind.Array)
            return 0;

        int linked = 0;
        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
                continue;

            var nameTm = ReadString(row, "NameTm");
            var code = ReadString(row, "Code") ?? nameTm;
            if (string.IsNullOrWhiteSpace(nameTm) && string.IsNullOrWhiteSpace(code))
                continue;

            var contract = FindProjectContract(objectSpace, nameTm, code);
            if (contract == null || contract.ApprovalLegProfile != null)
                continue;

            if (!row.TryGetProperty("MinistryLegs", out var legs) || legs.ValueKind != JsonValueKind.Array || legs.GetArrayLength() == 0)
                continue;

            var orderedShortNames = legs.EnumerateArray()
                .Where(l => l.ValueKind == JsonValueKind.Object)
                .OrderBy(l => l.TryGetProperty("Sequence", out var seq) ? seq.GetInt32() : 0)
                .Select(l => ReadString(l, "ApprovingMinistryShortNameTm"))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .ToList();

            var profile = ResolveProfile(objectSpace, orderedShortNames);
            if (profile == null)
                continue;

            contract.ApprovalLegProfile = profile;
            linked++;
        }

        return linked;
    }

    private static ProjectContract? FindProjectContract(IObjectSpace objectSpace, string? nameTm, string? code)
    {
        if (!string.IsNullOrWhiteSpace(nameTm))
        {
            var byName = objectSpace.GetObjectsQuery<ProjectContract>()
                .FirstOrDefault(c => c.NameTm == nameTm);
            if (byName != null)
                return byName;
        }

        if (string.IsNullOrWhiteSpace(code))
            return null;

        // Client-side match: EF Core cannot translate StartsWith(..., StringComparison.OrdinalIgnoreCase).
        return objectSpace.GetObjectsQuery<ProjectContract>()
            .ToList()
            .FirstOrDefault(c => c.NameTm == code
                || (c.NameTm != null && c.NameTm.StartsWith(code, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;
}

public sealed class ProjectContractApprovalLegProfileLinkUpdater : ModuleUpdater
{
    public ProjectContractApprovalLegProfileLinkUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        int linked = ProjectContractApprovalLegProfileLinker.LinkAll(ObjectSpace);
        if (linked > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText("ProjectContractApprovalLegProfileLinkUpdater: linked " + linked + " contract(s).");
        }
    }
}