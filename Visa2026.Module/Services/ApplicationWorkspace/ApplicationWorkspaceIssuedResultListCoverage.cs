using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// ListView chips for the same people-coverage ratios as Result-tab Netije syny.
/// Seretmezlik-excluded people are omitted from expected and coverage (same as the Result tab).
/// </summary>
public static class ApplicationWorkspaceIssuedResultListCoverage
{
    public readonly record struct Chip(
        string Key,
        string CatalogLabel,
        int CoverageCount,
        int ExpectedCount,
        bool IsOptional);

    public static string Tone(Chip chip)
    {
        if (chip.IsOptional)
            return "opt";
        return ApplicationWorkspaceIssuedResultOverview.Missing(chip.CoverageCount, chip.ExpectedCount, chip.IsOptional) > 0
            ? "miss"
            : "ok";
    }

    public static string RatioText(Chip chip) =>
        chip.IsOptional ? chip.CoverageCount.ToString() : $"{chip.CoverageCount} / {chip.ExpectedCount}";

    public static string Caption(Chip chip) =>
        ApplicationProfileLocalization.IssuedResultChipLabel(chip.CatalogLabel);

    public static string FormatDisplay(IReadOnlyList<Chip>? chips)
    {
        if (chips == null || chips.Count == 0)
            return "—";

        return string.Join(" · ", chips.Select(c => $"{Caption(c)} {RatioText(c)}"));
    }

    public static IReadOnlyList<Chip> Build(
        ApplicationProfileInstance? application,
        int rosterCount,
        IReadOnlyDictionary<string, int>? coverageByKey)
    {
        if (application == null)
            return Array.Empty<Chip>();

        var chips = new List<Chip>();
        foreach (var def in ApplicationWorkspaceIssuedRecordsCatalog.Definitions)
        {
            if (!def.IsVisible(application))
                continue;

            var coverage = 0;
            coverageByKey?.TryGetValue(def.Key, out coverage);
            chips.Add(new Chip(
                def.Key,
                def.Label,
                coverage,
                ApplicationWorkspaceIssuedResultOverview.ExpectedFor(def.IsOptional, rosterCount),
                def.IsOptional));
        }

        return chips;
    }

    public static void ApplyTo(IObjectSpace? objectSpace, IReadOnlyList<ApplicationProfileInstance> applications)
    {
        if (applications == null || applications.Count == 0)
            return;

        var ids = applications.Select(a => a.ID).Where(id => id != Guid.Empty).Distinct().ToList();
        var roster = LoadPersonSets(LoadRosterPairs(objectSpace, ids));
        var excluded = LoadPersonSets(LoadExcludedPairs(objectSpace, ids));
        var invitation = LoadPersonSets(LoadInvitationPairs(objectSpace, ids));
        var workPermit = LoadPersonSets(LoadWorkPermitPairs(objectSpace, ids));
        var rejection = LoadPersonSets(LoadRejectionPairs(objectSpace, ids));
        var visa = LoadPersonSets(LoadVisaPairs(objectSpace, ids));
        var borderZone = LoadPersonSets(LoadBorderZonePairs(objectSpace, ids));

        foreach (var application in applications)
        {
            roster.TryGetValue(application.ID, out var rosterIds);
            excluded.TryGetValue(application.ID, out var excludedIds);
            var activeRoster = ActiveRosterIds(rosterIds, excludedIds);
            var rosterCount = rosterIds is { Count: > 0 }
                ? activeRoster?.Count ?? 0
                : Math.Max(0, application.TotalPersonCount - (excludedIds?.Count ?? 0));
            var coverage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [ApplicationWorkspaceIssuedRecordsCatalog.Invitation] = CountOnRoster(invitation, application.ID, activeRoster),
                [ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit] = CountOnRoster(workPermit, application.ID, activeRoster),
                [ApplicationWorkspaceIssuedRecordsCatalog.Rejection] = CountOnRoster(rejection, application.ID, activeRoster),
                [ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa] = CountOnRoster(visa, application.ID, activeRoster),
                [ApplicationWorkspaceIssuedRecordsCatalog.BorderZone] = CountOnRoster(borderZone, application.ID, activeRoster),
            };
            application.SetListViewResultCoverage(Build(application, rosterCount, coverage));
        }
    }

    private static HashSet<Guid>? ActiveRosterIds(HashSet<Guid>? rosterIds, HashSet<Guid>? excludedIds)
    {
        if (rosterIds == null || rosterIds.Count == 0)
            return rosterIds;
        if (excludedIds == null || excludedIds.Count == 0)
            return rosterIds;

        var active = new HashSet<Guid>(rosterIds);
        active.ExceptWith(excludedIds);
        return active;
    }

    private static int CountOnRoster(
        IReadOnlyDictionary<Guid, HashSet<Guid>> byInstance,
        Guid instanceId,
        HashSet<Guid>? rosterIds)
    {
        if (!byInstance.TryGetValue(instanceId, out var people) || people.Count == 0)
            return 0;

        if (rosterIds == null || rosterIds.Count == 0)
            return people.Count;

        return people.Count(rosterIds.Contains);
    }

    private static Dictionary<Guid, HashSet<Guid>> LoadPersonSets(IEnumerable<(Guid InstanceId, Guid PersonId)> pairs)
    {
        var map = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var (instanceId, personId) in pairs)
        {
            if (instanceId == Guid.Empty || personId == Guid.Empty)
                continue;
            if (!map.TryGetValue(instanceId, out var set))
            {
                set = [];
                map[instanceId] = set;
            }

            set.Add(personId);
        }

        return map;
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId)> LoadRosterPairs(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return [];

        var fromPeople = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => ids.Contains(a.ID))
            .SelectMany(a => a.People.Select(p => new { InstanceId = a.ID, PersonId = p.ID }))
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId));

        var fromLinks = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(l => ids.Contains(l.ApplicationProfileInstanceId) && l.PersonId != Guid.Empty)
            .Select(l => new { l.ApplicationProfileInstanceId, l.PersonId })
            .ToList()
            .Select(x => (x.ApplicationProfileInstanceId, x.PersonId));

        return fromPeople.Concat(fromLinks);
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId)> LoadExcludedPairs(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return [];

        var letters = objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusion>()
            .Where(e => ids.Contains(e.ApplicationProfileInstanceId))
            .ToList();
        if (letters.Count == 0)
            return [];

        var instanceByExclusion = letters.ToDictionary(e => e.ID, e => e.ApplicationProfileInstanceId);
        var exclusionIds = instanceByExclusion.Keys.ToList();
        return objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusionPerson>()
            .Where(p => exclusionIds.Contains(p.ExclusionId) && p.PersonId != Guid.Empty)
            .ToList()
            .Select(p => (instanceByExclusion[p.ExclusionId], p.PersonId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId)> LoadInvitationPairs(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return [];

        return objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => i.Invitation != null
                && i.Invitation.ApplicationProfileInstance != null
                && ids.Contains(i.Invitation.ApplicationProfileInstance.ID)
                && i.Person != null)
            .Select(i => new { InstanceId = i.Invitation.ApplicationProfileInstance.ID, PersonId = i.Person.ID })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId)> LoadWorkPermitPairs(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return [];

        return objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(i => i.WorkPermit != null
                && i.WorkPermit.ApplicationProfileInstance != null
                && ids.Contains(i.WorkPermit.ApplicationProfileInstance.ID)
                && i.Person != null)
            .Select(i => new { InstanceId = i.WorkPermit.ApplicationProfileInstance.ID, PersonId = i.Person.ID })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId)> LoadRejectionPairs(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return [];

        return objectSpace.GetObjectsQuery<RejectionItem>()
            .Where(i => i.Rejection != null
                && i.Rejection.ApplicationProfileInstance != null
                && ids.Contains(i.Rejection.ApplicationProfileInstance.ID)
                && i.Person != null)
            .Select(i => new { InstanceId = i.Rejection.ApplicationProfileInstance.ID, PersonId = i.Person.ID })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId)> LoadVisaPairs(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return [];

        return objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.IssuingApplicationProfileInstance != null
                && ids.Contains(v.IssuingApplicationProfileInstance.ID)
                && v.Passport != null
                && v.Passport.Person != null)
            .Select(v => new { InstanceId = v.IssuingApplicationProfileInstance.ID, PersonId = v.Passport.Person.ID })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId)> LoadBorderZonePairs(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return [];

        return objectSpace.GetObjectsQuery<BorderZoneItem>()
            .Where(i => i.BorderZone != null
                && i.BorderZone.ApplicationProfileInstance != null
                && ids.Contains(i.BorderZone.ApplicationProfileInstance.ID)
                && i.Person != null)
            .Select(i => new { InstanceId = i.BorderZone.ApplicationProfileInstance.ID, PersonId = i.Person.ID })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId));
    }
}
