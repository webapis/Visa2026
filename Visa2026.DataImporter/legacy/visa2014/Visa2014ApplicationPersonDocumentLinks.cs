using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Replaces Passport/Visa ResolvedLinks + skip-nav M2M with the PersonInApplication
/// snapshot (Current/Previous passport, Current visa). Latest-N refresh is not the
/// source of truth for historical import rows.
/// </summary>
internal static class Visa2014ApplicationPersonDocumentLinks
{
    internal static void Diff(
        IReadOnlyCollection<Guid> existing,
        IReadOnlyCollection<Guid> desired,
        out List<Guid> remove,
        out List<Guid> add)
    {
        var existingSet = existing
            .Where(id => id != Guid.Empty)
            .ToHashSet();
        var desiredSet = desired
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        remove = existingSet.Except(desiredSet).ToList();
        add = desiredSet.Except(existingSet).ToList();
    }

    internal static List<Guid> MapLegacyOids(
        IReadOnlyDictionary<Guid, Guid> idMap,
        params Guid?[] legacyOids)
    {
        var ids = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var legacy in legacyOids)
        {
            if (legacy is not Guid oid || oid == Guid.Empty)
                continue;
            if (!idMap.TryGetValue(oid, out var target) || target == Guid.Empty)
                continue;
            if (!seen.Add(target))
                continue;
            ids.Add(target);
        }

        return ids;
    }

    internal static int ReplaceKind(
        IObjectSpace objectSpace,
        Bo.ApplicationProfileInstance application,
        Bo.Person person,
        ApplicationProfileInstancePersonLinkKind kind,
        IReadOnlyList<Guid> desiredIds)
    {
        if (objectSpace == null || application == null || person == null)
            return 0;
        if (desiredIds == null || desiredIds.Count == 0)
            return 0;

        var existingLinks = ApplicationProfileInstancePersonResolver
            .LoadLinks(objectSpace, application.ID, person.ID)
            .Where(l => l.LinkKind == kind)
            .ToList();
        // GetObjectsQuery does not include unsaved CreateObject rows from LinkPerson/RefreshResolvedLinks.
        if (application.PersonResolvedLinks != null)
        {
            foreach (var pending in application.PersonResolvedLinks)
            {
                if (pending == null || pending.LinkKind != kind)
                    continue;
                var pendingPersonId = pending.Person?.ID ?? pending.PersonId;
                if (pendingPersonId != person.ID)
                    continue;
                if (!existingLinks.Contains(pending))
                    existingLinks.Add(pending);
            }
        }
        var existingIds = existingLinks
            .Select(l => l.LinkedObjectId)
            .Where(id => id is Guid g && g != Guid.Empty)
            .Select(id => id!.Value)
            .ToList();

        Diff(existingIds, desiredIds, out var remove, out var add);
        var changed = 0;

        foreach (var id in remove)
        {
            var link = existingLinks.FirstOrDefault(l => l.LinkedObjectId == id);
            if (link != null)
                objectSpace.Delete(link);
            ApplicationProfileInstanceChildMembership.Remove(application, kind, id);
            changed++;
        }

        foreach (var id in add)
        {
            var link = objectSpace.CreateObject<ApplicationProfileInstancePersonResolvedLink>();
            link.ApplicationProfileInstance = application;
            link.Person = person;
            link.LinkKind = kind;
            link.LinkedObjectId = id;
            application.PersonResolvedLinks?.Add(link);
            ApplicationProfileInstanceChildMembership.Add(objectSpace, application, kind, id);
            changed++;
        }

        return changed;
    }
}
