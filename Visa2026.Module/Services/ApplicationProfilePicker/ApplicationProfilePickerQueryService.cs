using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public sealed class ApplicationProfilePickerQueryService : IApplicationProfilePickerQueryService
{
    public IReadOnlyList<ApplicationProfilePickerRow> GetProfiles(
        IObjectSpace objectSpace,
        ApplicationProgressRouteKind? progressRouteFilter,
        Application? applicabilityProbe = null,
        Guid? seedPersonId = null)
    {
        if (objectSpace == null)
            return Array.Empty<ApplicationProfilePickerRow>();

        var lastUsedByProfile = objectSpace.GetObjectsQuery<Application>()
            .Where(a => a.ApplicationProfile != null)
            .GroupBy(a => a.ApplicationProfile!.ID)
            .Select(g => new { ProfileId = g.Key, LastUsedAt = g.Max(a => (DateTime?)a.ApplicationDate) })
            .ToDictionary(x => x.ProfileId, x => x.LastUsedAt);

        var seedUsage = seedPersonId is Guid seedId && seedId != Guid.Empty
            ? BuildSeedUsage(objectSpace, seedId)
            : null;

        return objectSpace.GetObjectsQuery<ApplicationProfile>()
            .Where(p => p.IsActive)
            .AsEnumerable()
            .Where(p => ApplicationProfileApplicabilityHelper.IsProfileSelectable(p, applicabilityProbe, progressRouteFilter))
            .Select(p =>
            {
                SeedProfileUsage? usage = null;
                if (seedUsage != null)
                    seedUsage.TryGetValue(p.ID, out usage);

                return new ApplicationProfilePickerRow
                {
                    ProfileId = p.ID,
                    Name = p.Name,
                    Code = p.Code,
                    SelectionCode = p.SelectionCode,
                    ActionFamily = p.ActionFamily,
                    ProgressRoute = p.ProgressRoute,
                    IsConfigLocked = ApplicationProfileLockHelper.IsProfileConfigLocked(p, objectSpace),
                    LastUsedAt = lastUsedByProfile.TryGetValue(p.ID, out var lastUsed) ? lastUsed : null,
                    UsedBySeedPersonCount = usage?.Count ?? 0,
                    LastUsedBySeedPersonAt = usage?.LastUsedAt,
                    HasOpenApplicationForSeedPerson = usage?.HasOpen ?? false,
                };
            })
            .OrderByDescending(r => seedPersonId.HasValue ? r.LastUsedBySeedPersonAt ?? DateTime.MinValue : r.LastUsedAt ?? DateTime.MinValue)
            .ThenByDescending(r => r.LastUsedAt ?? DateTime.MinValue)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<Guid, SeedProfileUsage> BuildSeedUsage(IObjectSpace objectSpace, Guid seedPersonId)
    {
        var result = new Dictionary<Guid, SeedProfileUsage>();

        foreach (var row in objectSpace.GetObjectsQuery<ApplicationPerson>()
                     .Where(ap => ap.PersonId == seedPersonId && ap.Application != null && ap.Application.ApplicationProfile != null)
                     .AsEnumerable())
        {
            var profileId = row.Application!.ApplicationProfile!.ID;
            if (!result.TryGetValue(profileId, out var usage))
                usage = new SeedProfileUsage();

            usage.Count++;
            var appDate = row.Application.ApplicationDate;
            if (!usage.LastUsedAt.HasValue || appDate > usage.LastUsedAt)
                usage.LastUsedAt = appDate;

            if (!usage.HasOpen && !IsApplicationTerminal(row.Application))
                usage.HasOpen = true;

            result[profileId] = usage;
        }

        return result;
    }

    private static bool IsApplicationTerminal(Application application)
    {
        var latest = application.LatestProgress ?? application.ProgressHistory?
            .OrderByDescending(p => p.Order)
            .FirstOrDefault();
        return ApplicationProgressTransitionHelper.IsTerminalStateCode(latest?.State?.Code);
    }

    private sealed class SeedProfileUsage
    {
        public int Count;
        public DateTime? LastUsedAt;
        public bool HasOpen;
    }
}
