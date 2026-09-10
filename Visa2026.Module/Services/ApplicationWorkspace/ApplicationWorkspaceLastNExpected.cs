using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Last 1–3 on Visa / Work permit / Invitation / TravelHistory is a ceiling over the person's
/// currently linkable (active) rows, not a fixed quota. Two active visas → expect 2;
/// one active visa → expect 1. Travel history with zero person rows on a locked case → 0.
/// </summary>
public static class ApplicationWorkspaceLastNExpected
{
    public static bool UsesActivePool(ApplicationProfileInstancePersonLinkKind kind) =>
        kind is ApplicationProfileInstancePersonLinkKind.Visa
            or ApplicationProfileInstancePersonLinkKind.WorkPermitItem
            or ApplicationProfileInstancePersonLinkKind.InvitationItem
            or ApplicationProfileInstancePersonLinkKind.TravelHistory;

    public static int Resolve(
        ApplicationProfileInstancePersonLinkKind kind,
        int lastN,
        int availableActive,
        bool linksLocked)
    {
        var ceiling = Math.Max(lastN, 0);
        if (!UsesActivePool(kind))
            return Math.Max(ceiling, 1);

        var capped = Math.Min(Math.Max(ceiling, 1), Math.Max(availableActive, 0));
        if (availableActive <= 0)
            return linksLocked ? 0 : 1;

        return capped;
    }
}

/// <summary>Active Visa / WorkPermitItem / InvitationItem counts per roster person.</summary>
public sealed class ApplicationWorkspaceLinkableActiveCounts
{
    public static ApplicationWorkspaceLinkableActiveCounts Empty { get; } = new();

    private readonly Dictionary<Guid, int> _visa = new();
    private readonly Dictionary<Guid, int> _workPermit = new();
    private readonly Dictionary<Guid, int> _invitation = new();
    private readonly Dictionary<Guid, int> _travel = new();

    public int Get(Guid personId, ApplicationProfileInstancePersonLinkKind kind) => kind switch
    {
        ApplicationProfileInstancePersonLinkKind.Visa => _visa.GetValueOrDefault(personId),
        ApplicationProfileInstancePersonLinkKind.WorkPermitItem => _workPermit.GetValueOrDefault(personId),
        ApplicationProfileInstancePersonLinkKind.InvitationItem => _invitation.GetValueOrDefault(personId),
        ApplicationProfileInstancePersonLinkKind.TravelHistory => _travel.GetValueOrDefault(personId),
        _ => 0,
    };

    public static ApplicationWorkspaceLinkableActiveCounts Load(
        IObjectSpace? objectSpace,
        IReadOnlyList<Person> people)
    {
        var result = new ApplicationWorkspaceLinkableActiveCounts();
        if (objectSpace == null || people == null || people.Count == 0)
            return result;

        var ids = people.Select(p => p.ID).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return result;

        foreach (var visa in objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.Passport != null && v.Passport.Person != null && ids.Contains(v.Passport.Person.ID)))
        {
            if (!ApplicationProfileInstancePersonValidItems.CanLinkVisa(visa))
                continue;
            var personId = visa.Passport.Person.ID;
            result._visa[personId] = result._visa.GetValueOrDefault(personId) + 1;
        }

        foreach (var item in objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(w => w.Person != null && ids.Contains(w.Person.ID)))
        {
            if (!ApplicationProfileInstancePersonValidItems.CanLinkWorkPermitItem(item))
                continue;
            var personId = item.Person.ID;
            result._workPermit[personId] = result._workPermit.GetValueOrDefault(personId) + 1;
        }

        foreach (var item in objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => i.Person != null && ids.Contains(i.Person.ID)))
        {
            if (!ApplicationProfileInstancePersonValidItems.CanLinkInvitationItem(item))
                continue;
            var personId = item.Person.ID;
            result._invitation[personId] = result._invitation.GetValueOrDefault(personId) + 1;
        }

        foreach (var travel in objectSpace.GetObjectsQuery<TravelHistory>()
            .Where(t => t.Person != null && ids.Contains(t.Person.ID)))
        {
            var personId = travel.Person.ID;
            result._travel[personId] = result._travel.GetValueOrDefault(personId) + 1;
        }

        return result;
    }
}