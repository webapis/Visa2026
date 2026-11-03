using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
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

    /// <summary>
    /// Fill for the progress-result pill. Rejection is black when empty and red when it has a count.
    /// Other types are issued (everyone covered) or not issued. A partial count is not issued.
    /// </summary>
    public static string Tone(Chip chip)
    {
        if (string.Equals(chip.Key, ApplicationWorkspaceIssuedRecordsCatalog.Rejection, StringComparison.OrdinalIgnoreCase))
            return chip.CoverageCount > 0 ? "rej" : "rej-empty";

        if (chip.ExpectedCount > 0 && chip.CoverageCount >= chip.ExpectedCount)
            return "done";

        if (chip.ExpectedCount > 0)
            return "none";

        return "zero";
    }

    public static string RatioText(Chip chip) =>
        chip.IsOptional ? chip.CoverageCount.ToString() : $"{chip.CoverageCount} / {chip.ExpectedCount}";

    public static string Caption(Chip chip) =>
        ApplicationProfileLocalization.IssuedResultChipLabel(chip.CatalogLabel);

    public static Chip FromIssuedTile(ApplicationWorkspaceCaseIssuedTile tile) =>
        new(tile.Key, tile.Label, tile.CoverageCount, tile.ExpectedCount, tile.IsOptional);

    public static IReadOnlyList<Chip> FromIssuedTiles(IReadOnlyList<ApplicationWorkspaceCaseIssuedTile>? tiles)
    {
        if (tiles == null || tiles.Count == 0)
            return Array.Empty<Chip>();

        var chips = new List<Chip>(tiles.Count);
        foreach (var tile in tiles)
            chips.Add(FromIssuedTile(tile));
        return chips;
    }

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

    /// <summary>
    /// Progress-track chips. An empty rejection is hidden; other zero ratios stay
    /// (for example Wiza 0/1) so a missing issued document is still visible.
    /// </summary>
    public static IReadOnlyList<Chip> ForDisplay(IReadOnlyList<Chip>? chips)
    {
        if (chips == null || chips.Count == 0)
            return Array.Empty<Chip>();

        var visible = new List<Chip>(chips.Count);
        foreach (var chip in chips)
        {
            if (string.Equals(chip.Key, ApplicationWorkspaceIssuedRecordsCatalog.Rejection, StringComparison.OrdinalIgnoreCase)
                && chip.CoverageCount <= 0)
                continue;

            visible.Add(chip);
        }

        return visible;
    }

    public static void ApplyTo(IObjectSpace? objectSpace, IReadOnlyList<ApplicationProfileInstance> applications)
    {
        if (applications == null || applications.Count == 0)
            return;

        var ids = applications.Select(a => a.ID).Where(id => id != Guid.Empty).Distinct().ToList();
        var roster = LoadPersonSets(LoadRosterPairs(objectSpace, ids));
        var excluded = LoadPersonSets(LoadExcludedPairs(objectSpace, ids));
        var invitationIndex = IndexIssuedDocuments(LoadInvitationPairs(objectSpace, ids));
        var workPermitIndex = IndexIssuedDocuments(LoadWorkPermitPairs(objectSpace, ids));
        var rejectionIndex = IndexIssuedDocuments(LoadRejectionPairs(objectSpace, ids));
        var visaIndex = IndexIssuedDocuments(LoadVisaPairs(objectSpace, ids));
        var borderZoneIndex = IndexIssuedDocuments(LoadBorderZonePairs(objectSpace, ids));
        var invitation = invitationIndex.People;
        var workPermit = workPermitIndex.People;
        var rejection = rejectionIndex.People;
        var visa = visaIndex.People;
        var borderZone = borderZoneIndex.People;
        var names = ApplicationListLinkedPeople.LoadNames(objectSpace, roster);
        var invitationLaterLife = LoadInvitationLaterLife(objectSpace, ids);
        var invitationReplacements = LoadInvitationReplacements(objectSpace, ids);
        var expiredWorkPermit = LoadExpiredWorkPermits(objectSpace, ids);
        var expiredVisa = LoadExpiredVisas(objectSpace, ids);

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
            application.SetListViewLinkedPeople(ApplicationListLinkedPeople.Build(
                application,
                activeRoster,
                names,
                Holders(invitation, application.ID),
                Holders(workPermit, application.ID),
                Holders(visa, application.ID),
                Holders(borderZone, application.ID),
                Holders(rejection, application.ID),
                LaterLifeFor(invitationLaterLife, application.ID),
                ExpiredFor(expiredWorkPermit, application.ID),
                ExpiredFor(expiredVisa, application.ID),
                DocumentsFor(invitationIndex.Documents, application.ID),
                DocumentsFor(workPermitIndex.Documents, application.ID),
                DocumentsFor(visaIndex.Documents, application.ID),
                DocumentsFor(borderZoneIndex.Documents, application.ID),
                DocumentsFor(rejectionIndex.Documents, application.ID),
                ReplacementsFor(invitationReplacements, application.ID)));
        }
    }

    private static IReadOnlyDictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement> ReplacementsFor(
        IReadOnlyDictionary<Guid, Dictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement>> byInstance,
        Guid applicationId) =>
        byInstance.TryGetValue(applicationId, out var people) ? people : EmptyReplacements;

    private static readonly IReadOnlyDictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement> EmptyReplacements =
        new Dictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement>();

    private static Dictionary<Guid, Dictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement>> LoadInvitationReplacements(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        var result = new Dictionary<Guid, Dictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement>>();
        if (objectSpace == null || ids.Count == 0)
            return result;

        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        var changeFamily = ApplicationProfileActionFamily.Change;
        var links = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ApplicationProfile != null
                && a.ApplicationProfile.ActionFamily == changeFamily
                && a.LatestPrimaryStateCode == issued
                && a.InvitationItems.Any(i =>
                    i.Invitation != null
                    && i.Invitation.ApplicationProfileInstance != null
                    && ids.Contains(i.Invitation.ApplicationProfileInstance.ID)
                    && i.Person != null))
            .SelectMany(a => a.InvitationItems
                .Where(i => i.Invitation != null
                    && i.Invitation.ApplicationProfileInstance != null
                    && ids.Contains(i.Invitation.ApplicationProfileInstance.ID)
                    && i.Person != null)
                .Select(i => new
                {
                    ChangeId = a.ID,
                    Number = a.FullApplicationNumber,
                    Date = a.ApplicationDate,
                    PersonId = i.Person.ID,
                    OldInstanceId = i.Invitation.ApplicationProfileInstance.ID,
                }))
            .ToList();
        if (links.Count == 0)
            return result;

        var changeIds = links.Select(link => link.ChangeId).Distinct().ToList();
        var copies = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => i.Invitation != null
                && i.Invitation.ApplicationProfileInstance != null
                && changeIds.Contains(i.Invitation.ApplicationProfileInstance.ID)
                && i.Person != null)
            .Select(i => new
            {
                ChangeId = i.Invitation.ApplicationProfileInstance.ID,
                PersonId = i.Person.ID,
                DocumentId = i.Invitation.ID,
                ItemId = i.ID,
                Expiration = i.Invitation.ExpirationDate,
            })
            .ToList();
        if (copies.Count == 0)
            return result;

        var itemIds = copies.Select(copy => copy.ItemId).Distinct().ToList();
        var cancelled = LoadInvitationFamily(objectSpace, itemIds, ApplicationProfileActionFamily.Cancellation);
        var changed = LoadInvitationFamily(objectSpace, itemIds, ApplicationProfileActionFamily.Change);
        var used = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(objectSpace, itemIds);
        var lifeByItem = new Dictionary<Guid, string>();
        foreach (var copy in copies)
        {
            var life = InvitationLaterLife.Resolve(
                cancelled.Contains(copy.ItemId),
                changed.Contains(copy.ItemId),
                used.Contains(copy.ItemId),
                InvitationLaterLife.IsExpired(copy.Expiration));
            if (life != null)
                lifeByItem[copy.ItemId] = life;
        }

        var copyByChangePerson = new Dictionary<(Guid ChangeId, Guid PersonId), (Guid DocumentId, Guid ItemId)>();
        foreach (var copy in copies)
            copyByChangePerson.TryAdd((copy.ChangeId, copy.PersonId), (copy.DocumentId, copy.ItemId));

        var grouped = new Dictionary<(Guid OldInstanceId, Guid PersonId), List<(DateTime Date, Guid DocumentId, Guid ApplicationId, string? Number, string? Life)>>();
        foreach (var link in links)
        {
            if (!copyByChangePerson.TryGetValue((link.ChangeId, link.PersonId), out var copy))
                continue;

            var key = (link.OldInstanceId, link.PersonId);
            if (!grouped.TryGetValue(key, out var options))
            {
                options = [];
                grouped[key] = options;
            }

            lifeByItem.TryGetValue(copy.ItemId, out var life);
            options.Add((link.Date, copy.DocumentId, link.ChangeId, link.Number, life));
        }

        foreach (var (key, options) in grouped)
        {
            var chosen = ApplicationListLinkedPeople.ChooseSuccessor(options);
            if (chosen == null)
                continue;
            if (!result.TryGetValue(key.OldInstanceId, out var people))
            {
                people = [];
                result[key.OldInstanceId] = people;
            }

            people[key.PersonId] = chosen;
        }

        return result;
    }

    private static IReadOnlyDictionary<Guid, string> LaterLifeFor(
        IReadOnlyDictionary<Guid, Dictionary<Guid, string>> byInstance,
        Guid applicationId) =>
        byInstance.TryGetValue(applicationId, out var people)
            ? people
            : EmptyLaterLife;

    private static readonly IReadOnlyDictionary<Guid, string> EmptyLaterLife =
        new Dictionary<Guid, string>();

    private static IReadOnlySet<Guid> ExpiredFor(
        IReadOnlyDictionary<Guid, HashSet<Guid>> byInstance,
        Guid applicationId) =>
        byInstance.TryGetValue(applicationId, out var people) ? people : EmptyHolders;

    private static Dictionary<Guid, HashSet<Guid>> LoadExpiredWorkPermits(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return new Dictionary<Guid, HashSet<Guid>>();

        var rows = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(i => i.WorkPermit != null
                && i.WorkPermit.ApplicationProfileInstance != null
                && ids.Contains(i.WorkPermit.ApplicationProfileInstance.ID)
                && i.Person != null)
            .Select(i => new
            {
                InstanceId = i.WorkPermit.ApplicationProfileInstance.ID,
                PersonId = i.Person.ID,
                Expiration = i.ExpirationDate,
            })
            .ToList();

        return IssuedResultExpiry.People(rows.Select(row => (
            row.InstanceId,
            row.PersonId,
            row.Expiration == default ? (DateTime?)null : row.Expiration)));
    }

    private static Dictionary<Guid, HashSet<Guid>> LoadExpiredVisas(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        if (objectSpace == null || ids.Count == 0)
            return new Dictionary<Guid, HashSet<Guid>>();

        var rows = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.IssuingApplicationProfileInstance != null
                && ids.Contains(v.IssuingApplicationProfileInstance.ID)
                && v.Passport != null
                && v.Passport.Person != null)
            .Select(v => new
            {
                InstanceId = v.IssuingApplicationProfileInstance.ID,
                PersonId = v.Passport.Person.ID,
                Expiration = v.ExpirationDate,
            })
            .ToList();

        return IssuedResultExpiry.People(rows.Select(row => (row.InstanceId, row.PersonId, row.Expiration)));
    }

    private static Dictionary<Guid, Dictionary<Guid, string>> LoadInvitationLaterLife(
        IObjectSpace? objectSpace,
        IReadOnlyCollection<Guid> ids)
    {
        var result = new Dictionary<Guid, Dictionary<Guid, string>>();
        if (objectSpace == null || ids.Count == 0)
            return result;

        var items = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => i.Invitation != null
                && i.Invitation.ApplicationProfileInstance != null
                && ids.Contains(i.Invitation.ApplicationProfileInstance.ID)
                && i.Person != null)
            .Select(i => new
            {
                ItemId = i.ID,
                InstanceId = i.Invitation.ApplicationProfileInstance.ID,
                PersonId = i.Person.ID,
                Expiration = i.Invitation.ExpirationDate,
            })
            .ToList();
        if (items.Count == 0)
            return result;

        var itemIds = items.Select(i => i.ItemId).Distinct().ToList();
        var cancelled = LoadInvitationFamily(objectSpace, itemIds, ApplicationProfileActionFamily.Cancellation);
        var changed = LoadInvitationFamily(objectSpace, itemIds, ApplicationProfileActionFamily.Change);
        var used = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(objectSpace, itemIds);

        foreach (var item in items)
        {
            var state = InvitationLaterLife.Resolve(
                cancelled.Contains(item.ItemId),
                changed.Contains(item.ItemId),
                used.Contains(item.ItemId),
                InvitationLaterLife.IsExpired(item.Expiration));
            if (state == null)
                continue;

            if (!result.TryGetValue(item.InstanceId, out var people))
            {
                people = new Dictionary<Guid, string>();
                result[item.InstanceId] = people;
            }

            people.TryGetValue(item.PersonId, out var current);
            var strongest = InvitationLaterLife.Strongest(new[] { current, state });
            if (strongest != null)
                people[item.PersonId] = strongest;
        }

        return result;
    }

    private static HashSet<Guid> LoadInvitationFamily(
        IObjectSpace objectSpace,
        IReadOnlyCollection<Guid> itemIds,
        ApplicationProfileActionFamily family)
    {
        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        return objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => itemIds.Contains(i.ID)
                && i.ApplicationProfileInstances.Any(a =>
                    a.ApplicationProfile != null
                    && a.ApplicationProfile.ActionFamily == family
                    && a.LatestPrimaryStateCode == issued))
            .Select(i => i.ID)
            .ToHashSet();
    }

    private static IReadOnlySet<Guid> Holders(
        IReadOnlyDictionary<Guid, HashSet<Guid>> sets,
        Guid applicationId) =>
        sets.TryGetValue(applicationId, out var people) ? people : EmptyHolders;

    private static readonly IReadOnlySet<Guid> EmptyHolders = new HashSet<Guid>();

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

    private readonly record struct IssuedDocumentIndex(
        Dictionary<Guid, HashSet<Guid>> People,
        Dictionary<Guid, Dictionary<Guid, Guid>> Documents);

    private static IssuedDocumentIndex IndexIssuedDocuments(
        IEnumerable<(Guid InstanceId, Guid PersonId, Guid DocumentId)> triples)
    {
        var people = new Dictionary<Guid, HashSet<Guid>>();
        var documents = new Dictionary<Guid, Dictionary<Guid, Guid>>();
        foreach (var (instanceId, personId, documentId) in triples)
        {
            if (instanceId == Guid.Empty || personId == Guid.Empty)
                continue;
            if (!people.TryGetValue(instanceId, out var set))
            {
                set = [];
                people[instanceId] = set;
            }

            set.Add(personId);
            if (documentId == Guid.Empty)
                continue;
            if (!documents.TryGetValue(instanceId, out var byPerson))
            {
                byPerson = [];
                documents[instanceId] = byPerson;
            }

            byPerson.TryAdd(personId, documentId);
        }

        return new IssuedDocumentIndex(people, documents);
    }

    private static IReadOnlyDictionary<Guid, Guid> DocumentsFor(
        IReadOnlyDictionary<Guid, Dictionary<Guid, Guid>> byInstance,
        Guid applicationId) =>
        byInstance.TryGetValue(applicationId, out var documents) ? documents : EmptyDocuments;

    private static readonly IReadOnlyDictionary<Guid, Guid> EmptyDocuments =
        new Dictionary<Guid, Guid>();

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

    private static IEnumerable<(Guid InstanceId, Guid PersonId, Guid DocumentId)> LoadInvitationPairs(
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
            .Select(i => new
            {
                InstanceId = i.Invitation.ApplicationProfileInstance.ID,
                PersonId = i.Person.ID,
                DocumentId = i.Invitation.ID,
            })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId, x.DocumentId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId, Guid DocumentId)> LoadWorkPermitPairs(
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
            .Select(i => new
            {
                InstanceId = i.WorkPermit.ApplicationProfileInstance.ID,
                PersonId = i.Person.ID,
                DocumentId = i.WorkPermit.ID,
            })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId, x.DocumentId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId, Guid DocumentId)> LoadRejectionPairs(
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
            .Select(i => new
            {
                InstanceId = i.Rejection.ApplicationProfileInstance.ID,
                PersonId = i.Person.ID,
                DocumentId = i.Rejection.ID,
            })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId, x.DocumentId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId, Guid DocumentId)> LoadVisaPairs(
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
            .Select(v => new
            {
                InstanceId = v.IssuingApplicationProfileInstance.ID,
                PersonId = v.Passport.Person.ID,
                DocumentId = v.ID,
            })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId, x.DocumentId));
    }

    private static IEnumerable<(Guid InstanceId, Guid PersonId, Guid DocumentId)> LoadBorderZonePairs(
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
            .Select(i => new
            {
                InstanceId = i.BorderZone.ApplicationProfileInstance.ID,
                PersonId = i.Person.ID,
                DocumentId = i.BorderZone.ID,
            })
            .ToList()
            .Select(x => (x.InstanceId, x.PersonId, x.DocumentId));
    }
}
