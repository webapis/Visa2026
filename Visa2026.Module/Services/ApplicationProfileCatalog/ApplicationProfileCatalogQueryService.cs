using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

public sealed class ApplicationProfileCatalogQueryService : IApplicationProfileCatalogQueryService
{
    public IReadOnlyList<ApplicationProfileCatalogRow> GetProfiles(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return Array.Empty<ApplicationProfileCatalogRow>();

        var linkedCounts = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationProfile != null)
            .GroupBy(a => a.ApplicationProfile!.ID)
            .Select(g => new { ProfileId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ProfileId, x => x.Count);

        return objectSpace.GetObjectsQuery<ApplicationProfile>()
            .AsEnumerable()
            .Select(p => new ApplicationProfileCatalogRow
            {
                ProfileId = p.ID,
                Name = p.Name ?? string.Empty,
                Code = p.Code ?? string.Empty,
                SelectionCode = p.SelectionCode,
                ActionFamily = p.ActionFamily,
                ProgressRoute = p.ProgressRoute,
                IsActive = p.IsActive,
                IsConfigLocked = ApplicationProfileLockHelper.IsProfileConfigLocked(p, objectSpace),
                LinkedApplicationCount = linkedCounts.TryGetValue(p.ID, out var count) ? count : 0,
            })
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}