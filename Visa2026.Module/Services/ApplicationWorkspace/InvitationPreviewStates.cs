using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// State of each person on an invitation, for the preview banner.
/// Every person on the invitation is one card, because one letter can include several people.
/// </summary>
public static class InvitationPreviewStates
{
    public readonly record struct PersonState(Guid PersonId, string Name, string Life);

    public static IReadOnlyList<PersonState> ForInvitation(IObjectSpace? objectSpace, Guid invitationId)
    {
        if (objectSpace == null || invitationId == Guid.Empty)
            return Array.Empty<PersonState>();

        var rows = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(item => item.Invitation != null
                && item.Invitation.ID == invitationId
                && item.Person != null)
            .Select(item => new
            {
                item.ID,
                PersonId = item.Person.ID,
                item.Person.FirstName,
                item.Person.MiddleName,
                item.Person.LastName,
                Expiration = item.Invitation.ExpirationDate,
            })
            .ToList();
        return Build(objectSpace, rows.Select(row => (
            row.ID,
            row.PersonId,
            ApplicationListLinkedPeople.FormatName(row.FirstName, row.MiddleName, row.LastName),
            row.Expiration)));
    }

    public static IReadOnlyList<PersonState> ForItem(IObjectSpace? objectSpace, Guid invitationItemId)
    {
        if (objectSpace == null || invitationItemId == Guid.Empty)
            return Array.Empty<PersonState>();

        var invitationId = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(item => item.ID == invitationItemId && item.Invitation != null && item.Person != null)
            .Select(item => new { InvitationId = item.Invitation.ID, PersonId = item.Person.ID })
            .FirstOrDefault();
        if (invitationId == null)
            return Array.Empty<PersonState>();

        return ForInvitation(objectSpace, invitationId.InvitationId);
    }

    public static IReadOnlyList<PersonState> Choose(IReadOnlyList<PersonState> people, Guid? personId)
    {
        if (people.Count == 0)
            return people;
        if (personId is not Guid id || id == Guid.Empty)
            return people;

        foreach (var person in people)
        {
            if (person.PersonId == id)
                return new[] { person };
        }

        return people;
    }

    public static bool UseChips(IReadOnlyList<PersonState> people)
    {
        if (people.Count < 2)
            return false;

        var life = people[0].Life;
        for (var i = 1; i < people.Count; i++)
        {
            if (!string.Equals(people[i].Life, life, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static IReadOnlyList<PersonState> Build(
        IObjectSpace objectSpace,
        IEnumerable<(Guid ItemId, Guid PersonId, string Name, DateTime? Expiration)> rows)
    {
        var list = rows.Where(row => row.ItemId != Guid.Empty && row.PersonId != Guid.Empty).ToList();
        if (list.Count == 0)
            return Array.Empty<PersonState>();

        var itemIds = list.Select(row => row.ItemId).Distinct().ToList();
        var cancelled = Family(objectSpace, itemIds, ApplicationProfileActionFamily.Cancellation);
        var changed = Family(objectSpace, itemIds, ApplicationProfileActionFamily.Change);
        var used = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(objectSpace, itemIds);
        var byPerson = new Dictionary<Guid, PersonState>();
        foreach (var row in list)
        {
            var life = InvitationLaterLife.Resolve(
                cancelled.Contains(row.ItemId),
                changed.Contains(row.ItemId),
                used.Contains(row.ItemId),
                InvitationLaterLife.IsExpired(row.Expiration)) ?? InvitationLaterLife.InForce;
            if (!byPerson.TryGetValue(row.PersonId, out var current)
                || InvitationLaterLife.Strongest(new[] { current.Life, life }) == life)
            {
                byPerson[row.PersonId] = new PersonState(row.PersonId, row.Name, life);
            }
        }

        return byPerson.Values
            .OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static HashSet<Guid> Family(
        IObjectSpace objectSpace,
        IReadOnlyCollection<Guid> itemIds,
        ApplicationProfileActionFamily family)
    {
        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        return objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(item => itemIds.Contains(item.ID)
                && item.ApplicationProfileInstances.Any(application =>
                    application.ApplicationProfile != null
                    && application.ApplicationProfile.ActionFamily == family
                    && application.LatestPrimaryStateCode == issued))
            .Select(item => item.ID)
            .ToHashSet();
    }
}