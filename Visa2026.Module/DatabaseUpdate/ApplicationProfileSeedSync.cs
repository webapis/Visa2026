using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent sync: one <see cref="ApplicationProfile"/> per <see cref="ApplicationType"/>,
/// then backfill <see cref="Application.ApplicationProfile"/> from <see cref="Application.ApplicationType"/>.
/// </summary>
public static class ApplicationProfileSeedSync
{
    public sealed class Result
    {
        public int ProfilesCreated { get; init; }
        public int ProfilesUpdated { get; init; }
        public int ApplicationsBackfilled { get; init; }
        public IReadOnlyList<string> TypesWithoutProfile { get; init; } = Array.Empty<string>();
    }

    public static Result Sync(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));

        var types = objectSpace.GetObjectsQuery<ApplicationType>()
            .OrderBy(t => t.Name)
            .ToList();

        var profilesByCode = objectSpace.GetObjectsQuery<ApplicationProfile>()
            .ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

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
        foreach (var application in objectSpace.GetObjectsQuery<Application>()
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
