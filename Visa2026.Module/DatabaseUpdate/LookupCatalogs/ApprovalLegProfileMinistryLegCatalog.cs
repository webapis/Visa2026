using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

internal sealed class ApprovalLegProfileCatalogFile
{
    public List<ApprovalLegProfileCatalogRow> Rows { get; set; } = new();
}

internal sealed class ApprovalLegProfileCatalogRow
{
    public string Code { get; set; } = string.Empty;

    public string NameTm { get; set; } = string.Empty;

    public string? LocalizationKey { get; set; }

    public bool IsActive { get; set; } = true;

    public List<ApprovalLegProfileMinistryLegCatalogRow> MinistryLegs { get; set; } = new();
}

internal sealed class ApprovalLegProfileMinistryLegCatalogRow
{
    public int Sequence { get; set; }

    public string ApprovingMinistryShortNameTm { get; set; } = string.Empty;

    public int? MaxDaysInReview { get; set; }

    public int? WarningDaysBeforeMax { get; set; }
}

internal static class ApprovalLegProfileMinistryLegCatalogLoader
{
    private const string FileName = "approval-leg-profile.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ApprovalLegProfileCatalogFile? Load()
    {
        var json = LookupCatalogResourceLoader.TryReadTenantOverlayText(FileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + FileName);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<ApprovalLegProfileCatalogFile>(json, JsonOptions);
    }
}

internal static class ApprovalLegProfileMinistryLegCatalogSync
{
    private static bool ForceReseedRequested() =>
        string.Equals(
            Environment.GetEnvironmentVariable("FORCE_APPROVAL_LEG_PROFILE_RESEED"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Creates or updates profile header rows from tenant JSON (no ministry legs).</summary>
    public static void EnsureProfiles(IObjectSpace objectSpace)
    {
        if (!TryLoadCatalogRows(objectSpace, out var rows))
            return;

        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code) || row.MinistryLegs is not { Count: > 0 })
            {
                skipped++;
                continue;
            }

            if (FindOrCreateProfile(objectSpace, row, ref created, ref updated) == null)
                skipped++;
        }

        Tracing.Tracer.LogText(
            $"ApprovalLegProfileMinistryLegCatalogSync.EnsureProfiles: created={created}, updated={updated}, skippedProfiles={skipped}.");
    }

    /// <summary>Syncs nested ministry legs; call after <see cref="EnsureProfiles"/> is committed.</summary>
    public static void SyncMinistryLegs(IObjectSpace objectSpace)
    {
        if (!TryLoadCatalogRows(objectSpace, out var rows))
            return;

        var forceReseed = ForceReseedRequested();
        if (forceReseed)
        {
            Tracing.Tracer.LogText(
                "ApprovalLegProfileMinistryLegCatalogSync: FORCE_APPROVAL_LEG_PROFILE_RESEED=true — "
                + "rebuilding ministry legs for all profiles from tenant JSON.");
        }

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var profileCreated = 0;
        var profileUpdated = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Code))
            {
                skipped++;
                continue;
            }

            if (row.MinistryLegs is not { Count: > 0 })
            {
                skipped++;
                continue;
            }

            var profile = FindOrCreateProfile(objectSpace, row, ref profileCreated, ref profileUpdated);
            if (profile == null)
            {
                skipped++;
                continue;
            }

            var jsonLegs = row.MinistryLegs
                .Where(l => l.Sequence > 0 && !string.IsNullOrWhiteSpace(l.ApprovingMinistryShortNameTm))
                .OrderBy(l => l.Sequence)
                .ToList();

            if (jsonLegs.Count == 0)
                continue;

            if (forceReseed)
            {
                created += ForceRebuildProfileLegs(objectSpace, profile, jsonLegs);
                continue;
            }

            var (c, u) = SyncUnreferencedProfile(objectSpace, profile, jsonLegs);
            created += c;
            updated += u;
        }

        Tracing.Tracer.LogText(
            $"ApprovalLegProfileMinistryLegCatalogSync.SyncMinistryLegs: legCreated={created}, legUpdated={updated}, skippedProfiles={skipped}.");
    }

    private static bool TryLoadCatalogRows(IObjectSpace objectSpace, out List<ApprovalLegProfileCatalogRow> rows)
    {
        _ = objectSpace;
        rows = [];
        var catalog = ApprovalLegProfileMinistryLegCatalogLoader.Load();
        if (catalog?.Rows is not { Count: > 0 })
        {
            Tracing.Tracer.LogText(
                "ApprovalLegProfileMinistryLegCatalogSync: no rows in tenant/approval-leg-profile.json (embedded or disk overlay).");
            return false;
        }

        rows = catalog.Rows;
        return true;
    }

    private static ApprovalLegProfile? FindOrCreateProfile(
        IObjectSpace objectSpace,
        ApprovalLegProfileCatalogRow row,
        ref int created,
        ref int updated)
    {
        var code = row.Code.Trim();
        var profile = objectSpace.GetObjectsQuery<ApprovalLegProfile>()
            .AsEnumerable()
            .FirstOrDefault(p => LookupCatalogMatchHelper.KeysEqual(p.Code, code));

        if (profile == null)
        {
            profile = objectSpace.CreateObject<ApprovalLegProfile>();
            profile.Code = code;
            created++;
        }
        else
        {
            updated++;
        }

        if (!string.IsNullOrWhiteSpace(row.NameTm))
            profile.NameTm = row.NameTm.Trim();

        if (!string.IsNullOrWhiteSpace(row.LocalizationKey))
            profile.LocalizationKey = row.LocalizationKey.Trim();

        profile.IsActive = row.IsActive;
        return profile;
    }

    private static int ForceRebuildProfileLegs(
        IObjectSpace objectSpace,
        ApprovalLegProfile profile,
        IReadOnlyList<ApprovalLegProfileMinistryLegCatalogRow> jsonLegs)
    {
        foreach (var existing in profile.MinistryLegs?.ToList() ?? [])
            objectSpace.Delete(existing);

        var created = 0;
        foreach (var jsonLeg in jsonLegs)
        {
            var ministry = FindMinistry(objectSpace, jsonLeg.ApprovingMinistryShortNameTm);
            if (ministry == null)
            {
                Tracing.Tracer.LogText(
                    $"ApprovalLegProfileMinistryLegCatalogSync: ministry '{jsonLeg.ApprovingMinistryShortNameTm}' not found for profile '{profile.Code}' leg {jsonLeg.Sequence} (force reseed).");
                continue;
            }

            var leg = objectSpace.CreateObject<ApprovalLegProfileMinistryLeg>();
            leg.ApprovalLegProfile = profile;
            leg.Sequence = jsonLeg.Sequence;
            leg.ApprovingMinistry = ministry;
            ApplySlaFromJson(leg, jsonLeg, onlyIfMissing: false);
            profile.MinistryLegs.Add(leg);
            created++;
        }

        return created;
    }

    private static (int created, int updated) SyncUnreferencedProfile(
        IObjectSpace objectSpace,
        ApprovalLegProfile profile,
        IReadOnlyList<ApprovalLegProfileMinistryLegCatalogRow> jsonLegs)
    {
        var created = 0;
        var updated = 0;
        var jsonSequences = jsonLegs.Select(l => l.Sequence).ToHashSet();

        foreach (var orphan in profile.MinistryLegs?
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
                    $"ApprovalLegProfileMinistryLegCatalogSync: ministry '{jsonLeg.ApprovingMinistryShortNameTm}' not found for profile '{profile.Code}' leg {jsonLeg.Sequence}.");
                continue;
            }

            var existing = profile.MinistryLegs?
                .FirstOrDefault(l => l.Sequence == jsonLeg.Sequence);
            if (existing == null)
            {
                existing = objectSpace.CreateObject<ApprovalLegProfileMinistryLeg>();
                existing.ApprovalLegProfile = profile;
                existing.Sequence = jsonLeg.Sequence;
                profile.MinistryLegs.Add(existing);
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
        ApprovalLegProfileMinistryLeg leg,
        ApprovalLegProfileMinistryLegCatalogRow jsonLeg,
        bool onlyIfMissing)
    {
        // Ministry review SLA is tenant-wide (MinistryReviewSlaSettings). Per-leg JSON values are ignored.
        return false;
    }

    private static ApprovingMinistry? FindMinistry(IObjectSpace objectSpace, string shortNameTm)
    {
        var trimmed = shortNameTm.Trim();
        return objectSpace.GetObjectsQuery<ApprovingMinistry>()
            .AsEnumerable()
            .FirstOrDefault(m => LookupCatalogMatchHelper.KeysEqual(m.ShortNameTm, trimmed));
    }
}
