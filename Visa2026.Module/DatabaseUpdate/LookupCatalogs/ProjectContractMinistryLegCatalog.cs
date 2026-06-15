using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

/// <summary>Nested ministry legs on tenant <c>project-contract.json</c> rows.</summary>
internal sealed class ProjectContractCatalogFile
{
    public List<ProjectContractCatalogRow> Rows { get; set; } = new();
}

internal sealed class ProjectContractCatalogRow
{
    public string NameTm { get; set; } = string.Empty;

    public List<ProjectContractMinistryLegCatalogRow> MinistryLegs { get; set; } = new();
}

internal sealed class ProjectContractMinistryLegCatalogRow
{
    public int Sequence { get; set; }

    public string ApprovingMinistryShortNameTm { get; set; } = string.Empty;

    public int? MaxDaysInReview { get; set; }

    public int? WarningDaysBeforeMax { get; set; }
}

internal static class ProjectContractMinistryLegCatalogLoader
{
    private const string FileName = "project-contract.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ProjectContractCatalogFile? Load()
    {
        var json = LookupCatalogResourceLoader.TryReadTenantOverlayText(FileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + FileName);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<ProjectContractCatalogFile>(json, JsonOptions);
    }
}

internal static class ProjectContractMinistryLegCatalogSync
{
    public static void Sync(IObjectSpace objectSpace)
    {
        var catalog = ProjectContractMinistryLegCatalogLoader.Load();
        if (catalog?.Rows is not { Count: > 0 })
        {
            Tracing.Tracer.LogText(
                "ProjectContractMinistryLegCatalogSync: no rows in tenant/project-contract.json (embedded or disk overlay).");
            return;
        }

        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in catalog.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.NameTm))
            {
                skipped++;
                continue;
            }

            if (row.MinistryLegs is not { Count: > 0 })
                continue;

            var contract = FindContract(objectSpace, row.NameTm);
            if (contract == null)
            {
                skipped++;
                Tracing.Tracer.LogText(
                    $"ProjectContractMinistryLegCatalogSync: contract '{row.NameTm}' not found — ministry legs skipped.");
                continue;
            }

            var referenced = ProjectContractMinistryHelper.IsContractReferencedByApplications(contract, objectSpace);
            var jsonLegs = row.MinistryLegs
                .Where(l => l.Sequence > 0 && !string.IsNullOrWhiteSpace(l.ApprovingMinistryShortNameTm))
                .OrderBy(l => l.Sequence)
                .ToList();

            if (jsonLegs.Count == 0)
                continue;

            if (referenced)
            {
                updated += SyncReferencedContract(contract, jsonLegs);
                continue;
            }

            var (c, u) = SyncUnreferencedContract(objectSpace, contract, jsonLegs);
            created += c;
            updated += u;
        }

        Tracing.Tracer.LogText(
            $"ProjectContractMinistryLegCatalogSync: created={created}, updated={updated}, skippedContracts={skipped}.");
    }

    private static int SyncReferencedContract(
        ProjectContract contract,
        IReadOnlyList<ProjectContractMinistryLegCatalogRow> jsonLegs)
    {
        var updated = 0;
        foreach (var jsonLeg in jsonLegs)
        {
            var existing = contract.MinistryLegs?
                .FirstOrDefault(l => l.Sequence == jsonLeg.Sequence);
            if (existing == null)
                continue;

            if (ApplySlaFromJson(existing, jsonLeg, onlyIfMissing: true))
                updated++;
        }

        return updated;
    }

    private static (int created, int updated) SyncUnreferencedContract(
        IObjectSpace objectSpace,
        ProjectContract contract,
        IReadOnlyList<ProjectContractMinistryLegCatalogRow> jsonLegs)
    {
        var created = 0;
        var updated = 0;
        var jsonSequences = jsonLegs.Select(l => l.Sequence).ToHashSet();

        foreach (var orphan in contract.MinistryLegs?
                     .Where(l => l.Sequence is int seq && !jsonSequences.Contains(seq))
                     .ToList() ?? [])
        {
            objectSpace.Delete(orphan);
        }

        foreach (var jsonLeg in jsonLegs)
        {
            var ministry = FindMinistry(objectSpace, jsonLeg.ApprovingMinistryShortNameTm);
            if (ministry == null)
            {
                Tracing.Tracer.LogText(
                    $"ProjectContractMinistryLegCatalogSync: ministry '{jsonLeg.ApprovingMinistryShortNameTm}' not found for '{contract.NameTm}' leg {jsonLeg.Sequence}.");
                continue;
            }

            var existing = contract.MinistryLegs?
                .FirstOrDefault(l => l.Sequence == jsonLeg.Sequence);
            if (existing == null)
            {
                existing = objectSpace.CreateObject<ProjectContractMinistryLeg>();
                existing.ProjectContract = contract;
                existing.Sequence = jsonLeg.Sequence;
                contract.MinistryLegs.Add(existing);
                created++;
            }
            else
            {
                updated++;
            }

            existing.ApprovingMinistry = ministry;
            ApplySlaFromJson(existing, jsonLeg, onlyIfMissing: false);
        }

        return (created, updated);
    }

    private static bool ApplySlaFromJson(
        ProjectContractMinistryLeg leg,
        ProjectContractMinistryLegCatalogRow jsonLeg,
        bool onlyIfMissing)
    {
        var changed = false;

        if (jsonLeg.MaxDaysInReview is > 0
            && (!onlyIfMissing || leg.MaxDaysInReview is not > 0))
        {
            leg.MaxDaysInReview = jsonLeg.MaxDaysInReview;
            changed = true;
        }

        if (jsonLeg.WarningDaysBeforeMax is > 0
            && (!onlyIfMissing || leg.WarningDaysBeforeMax is not > 0))
        {
            leg.WarningDaysBeforeMax = jsonLeg.WarningDaysBeforeMax;
            changed = true;
        }

        return changed;
    }

    private static ProjectContract? FindContract(IObjectSpace objectSpace, string nameTm)
    {
        var trimmed = nameTm.Trim();
        return objectSpace.GetObjectsQuery<ProjectContract>()
            .AsEnumerable()
            .FirstOrDefault(c => LookupCatalogMatchHelper.KeysEqual(c.NameTm, trimmed));
    }

    private static ApprovingMinistry? FindMinistry(IObjectSpace objectSpace, string shortNameTm)
    {
        var trimmed = shortNameTm.Trim();
        return objectSpace.GetObjectsQuery<ApprovingMinistry>()
            .AsEnumerable()
            .FirstOrDefault(m => LookupCatalogMatchHelper.KeysEqual(m.ShortNameTm, trimmed));
    }
}
