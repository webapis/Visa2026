using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfileCatalog;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public sealed class ApplicationProfilePickerQueryService : IApplicationProfilePickerQueryService
{
    public IReadOnlyList<ApplicationProfilePickerRow> GetProfiles(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgressRouteKind? progressRouteFilter,
        ApplicationProfileInstance? applicabilityProbe = null,
        Guid? seedPersonId = null)
    {
        if (objectSpace == null)
            return Array.Empty<ApplicationProfilePickerRow>();

        var lastUsedByProfile = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ApplicationProfile != null)
            .GroupBy(a => a.ApplicationProfile!.ID)
            .Select(g => new { ProfileId = g.Key, LastUsedAt = g.Max(a => (DateTime?)a.ApplicationDate) })
            .ToDictionary(x => x.ProfileId, x => x.LastUsedAt);

        var seedUsage = seedPersonId is Guid seedId && seedId != Guid.Empty
            ? BuildSeedUsage(objectSpace, seedId)
            : null;

        return ApplicationProfileOfficerCatalogSelector
            .SelectDistinctTemplates(
                objectSpace.GetObjectsQuery<ApplicationProfile>()
                    .Where(p => p.IsActive)
                    .AsEnumerable()
                    .Where(p => ApplicationProfileApplicabilityHelper.IsProfileSelectable(p, applicabilityProbe, progressRouteFilter)))
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
                    RegistrationKind = p.RegistrationKind,
                    ProgressRoute = p.ProgressRoute,
                    IsConfigLocked = ApplicationProfileLockHelper.IsProfileConfigLocked(p, objectSpace),
                    LastUsedAt = lastUsedByProfile.TryGetValue(p.ID, out var lastUsed) ? lastUsed : null,
                    UsedBySeedPersonCount = usage?.Count ?? 0,
                    LastUsedBySeedPersonAt = usage?.LastUsedAt,
                    HasOpenApplicationForSeedPerson = usage?.HasOpen ?? false,
                    ApprovalLegVersions = BuildVersionOptions(objectSpace, p),
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

        foreach (var application in objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                     .Where(a => a.People.Any(p => p.ID == seedPersonId) && a.ApplicationProfile != null)
                     .AsEnumerable())
        {
            var profileId = application.ApplicationProfile!.ID;
            if (!result.TryGetValue(profileId, out var usage))
                usage = new SeedProfileUsage();

            usage.Count++;
            var appDate = application.ApplicationDate;
            if (!usage.LastUsedAt.HasValue || appDate > usage.LastUsedAt)
                usage.LastUsedAt = appDate;

            if (!usage.HasOpen && !IsApplicationTerminal(application))
                usage.HasOpen = true;

            result[profileId] = usage;
        }

        return result;
    }

    private static bool IsApplicationTerminal(ApplicationProfileInstance application)
    {
        var latest = application.LatestProgress ?? application.ProgressHistory?
            .OrderByDescending(p => p.Order)
            .FirstOrDefault();
        return ApplicationProfileInstanceProgressTransitionHelper.IsTerminalStateCode(latest?.State?.Code);
    }

    private static IReadOnlyList<ApplicationProfilePickerVersionOption> BuildVersionOptions(
        IObjectSpace objectSpace,
        ApplicationProfile profile)
    {
        var shared = ApplicationProfileApprovalLegVersionHelper.GetSharedActiveProfiles(objectSpace);
        var defaultId = profile.DefaultApprovalLegProfileId;
        return shared.Select(p => new ApplicationProfilePickerVersionOption
        {
            VersionId = p.ID,
            Name = string.IsNullOrWhiteSpace(p.NameTm) ? (p.Code ?? p.ID.ToString()) : p.NameTm!,
            IsDefault = defaultId.HasValue && defaultId.Value == p.ID,
            MinistryNames = (p.MinistryLegs ?? Enumerable.Empty<ApprovalLegProfileMinistryLeg>())
                .Where(l => l.ApprovingMinistry != null)
                .OrderBy(l => l.Sequence ?? int.MaxValue)
                .Select(l => l.ApprovingMinistry!.NameTm
                    ?? l.ApprovingMinistry.ShortNameTm
                    ?? $"Ministry {l.Sequence}")
                .ToList(),
        }).ToList();
    }

    private sealed class SeedProfileUsage
    {
        public int Count;
        public DateTime? LastUsedAt;
        public bool HasOpen;
    }
}
