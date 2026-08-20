using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent sync: one <see cref="ApplicationProfile"/> per <see cref="ApplicationType"/>
/// when no tenant application-profile JSON is present, then backfill instance FKs.
/// Calik (and any pack with tenant JSON) owns the catalog — do not invent type-derived profiles.
/// When tenant JSON is present, host start still upserts those catalog rows (ModuleUpdater may skip).
/// </summary>
public static class ApplicationProfileSeedSync
{
    public sealed class Result
    {
        public int ProfilesCreated { get; init; }
        public int ProfilesUpdated { get; init; }
        public int ApplicationsBackfilled { get; init; }
        public IReadOnlyList<string> TypesWithoutProfile { get; init; } = Array.Empty<string>();
        public bool SkippedBecauseTenantCatalogPresent { get; init; }
    }

    public static Result Sync(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));

        if (ApplicationProfileTenantCatalogLoader.Load() != null)
        {
            Tracing.Tracer.LogText(
                "ApplicationProfileSeedSync: tenant application-profile JSON present; syncing catalog rows (not type-derived).");
            var tenant = ApplicationProfileTenantCatalogSync.Sync(objectSpace);
            ApplicationProfileNestedTemplateTenantCatalogSync.Sync(objectSpace);
            ApplicationProfileApprovalLegVersionTenantCatalogSync.Sync(objectSpace);
            // Commit catalog only here. Phase B instance heal runs in a fresh ObjectSpace
            // from ApplicationProfileSeedGate (soft-delete recreate tripped OptimisticLockField).
            objectSpace.CommitChanges();
            return new Result
            {
                SkippedBecauseTenantCatalogPresent = true,
                ProfilesCreated = tenant.Created,
                ProfilesUpdated = tenant.Updated,
                ApplicationsBackfilled = 0,
            };
        }

        var types = objectSpace.GetObjectsQuery<ApplicationType>()
            .OrderBy(t => t.Name)
            .ToList();

        var allProfiles = objectSpace.GetObjectsQuery<ApplicationProfile>().ToList();
        var profilesByCode = allProfiles
            .GroupBy(p => p.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.FirstOrDefault(p => p.DefaultProjectContractId == null && !ApplicationProfileCatalogGroupKey.NameLooksLikeContractVariant(p.Name))
                     ?? g.FirstOrDefault(p => p.DefaultProjectContractId == null)
                     ?? g.First(),
                StringComparer.OrdinalIgnoreCase);

        int created = 0, updated = 0;
        var typeToProfile = new Dictionary<ApplicationType, ApplicationProfile>(ReferenceEqualityComparer.Instance);
        var missing = new List<string>();

        foreach (var type in types)
        {
            var code = ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(type);
            if (!profilesByCode.TryGetValue(code, out var profile))
            {
                profile = objectSpace.CreateObject<ApplicationProfile>();
                profilesByCode[code] = profile;
                created++;
            }
            else
            {
                updated++;
            }

            ApplicationProfileFromApplicationTypeMapper.Apply(profile, type);
            typeToProfile[type] = profile;
        }

        int backfilled = 0;
        foreach (var application in objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                     .Where(a => a.ApplicationType != null))
        {
            if (!typeToProfile.TryGetValue(application.ApplicationType, out var profile))
            {
                missing.Add(application.ApplicationType.Name ?? application.ApplicationType.ID.ToString());
                continue;
            }

            if (application.ApplicationProfile?.ID != profile.ID)
            {
                application.ApplicationProfile = profile;
                backfilled++;
            }
        }

        objectSpace.CommitChanges();

        var instanceLegs = ApplicationProfileInstanceApprovalLegBackfill.Sync(objectSpace);
        if (instanceLegs.ProfilesAssigned + instanceLegs.NamesStamped + instanceLegs.SnapshotsFilled > 0)
            objectSpace.CommitChanges();

        if (created > 0 || updated > 0 || backfilled > 0)
        {
            Tracing.Tracer.LogText(
                $"ApplicationProfileSeedSync: profiles created={created}, updated={updated}, "
                + $"applications backfilled={backfilled}.");
        }

        return new Result
        {
            ProfilesCreated = created,
            ProfilesUpdated = updated,
            ApplicationsBackfilled = backfilled,
            TypesWithoutProfile = missing.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList(),
        };
    }
}
