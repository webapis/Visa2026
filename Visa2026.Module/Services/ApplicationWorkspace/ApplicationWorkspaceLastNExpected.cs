using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Last 1–3 on Visa / Work permit / Invitation / TravelHistory / Medical is a ceiling over the person's
/// currently linkable (active) rows, not a fixed quota. Two active visas → expect 2;
/// one active visa → expect 1. Travel history / Medical with zero person rows on a locked case → 0.
/// Imported (<c>IsManualEntry</c>) cases also expect 0 Medical when the person has none in legacy.
/// </summary>
public static class ApplicationWorkspaceLastNExpected
{
    public static bool UsesActivePool(ApplicationProfileInstancePersonLinkKind kind) =>
        kind is ApplicationProfileInstancePersonLinkKind.Visa
            or ApplicationProfileInstancePersonLinkKind.WorkPermitItem
            or ApplicationProfileInstancePersonLinkKind.InvitationItem
            or ApplicationProfileInstancePersonLinkKind.TravelHistory
            or ApplicationProfileInstancePersonLinkKind.MedicalRecord;

    public static int Resolve(
        ApplicationProfileInstancePersonLinkKind kind,
        int lastN,
        int availableActive,
        bool linksLocked,
        bool importedManualEntry = false)
    {
        var ceiling = Math.Max(lastN, 0);
        if (!UsesActivePool(kind))
            return Math.Max(ceiling, 1);

        var capped = Math.Min(Math.Max(ceiling, 1), Math.Max(availableActive, 0));
        if (availableActive <= 0)
        {
            if (kind == ApplicationProfileInstancePersonLinkKind.MedicalRecord && importedManualEntry)
                return 0;
            return linksLocked ? 0 : 1;
        }

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
    private readonly Dictionary<Guid, int> _medical = new();

    public int Get(Guid personId, ApplicationProfileInstancePersonLinkKind kind) => kind switch
    {
        ApplicationProfileInstancePersonLinkKind.Visa => _visa.GetValueOrDefault(personId),
        ApplicationProfileInstancePersonLinkKind.WorkPermitItem => _workPermit.GetValueOrDefault(personId),
        ApplicationProfileInstancePersonLinkKind.InvitationItem => _invitation.GetValueOrDefault(personId),
        ApplicationProfileInstancePersonLinkKind.TravelHistory => _travel.GetValueOrDefault(personId),
        ApplicationProfileInstancePersonLinkKind.MedicalRecord => _medical.GetValueOrDefault(personId),
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

        var asOf = DateTime.Today;

        var visaPersonIds = IssuedDocumentLifecycle.WhereVisaNotChanged(
                IssuedDocumentLifecycle.WhereVisaNotCancelled(
                    objectSpace.GetObjectsQuery<Visa>()
                        .Where(v => v.Passport != null
                            && v.Passport.Person != null
                            && ids.Contains(v.Passport.Person.ID))))
            .Where(v => v.StartDate != default && v.StartDate <= asOf)
            .Select(v => new { PersonId = v.Passport.Person.ID, v.ExpirationDate })
            .ToList();
        foreach (var row in visaPersonIds)
        {
            if (IsExpired(row.ExpirationDate, asOf))
                continue;
            result._visa[row.PersonId] = result._visa.GetValueOrDefault(row.PersonId) + 1;
        }

        var workPermitPersonIds = IssuedDocumentLifecycle.WhereWorkPermitItemNotChanged(
                IssuedDocumentLifecycle.WhereWorkPermitItemNotCancelled(
                    objectSpace.GetObjectsQuery<WorkPermitItem>()
                        .Where(w => w.Person != null && ids.Contains(w.Person.ID))))
            .Select(w => new { PersonId = w.Person.ID, w.ExpirationDate })
            .ToList();
        foreach (var row in workPermitPersonIds)
        {
            if (IsExpired(row.ExpirationDate, asOf))
                continue;
            result._workPermit[row.PersonId] = result._workPermit.GetValueOrDefault(row.PersonId) + 1;
        }

        var invitationRows = IssuedDocumentLifecycle.WhereInvitationItemNotChanged(
                IssuedDocumentLifecycle.WhereInvitationItemNotCancelled(
                    objectSpace.GetObjectsQuery<InvitationItem>()
                        .Where(i => i.Person != null
                            && ids.Contains(i.Person.ID)
                            && i.Invitation != null)))
            .Select(i => new
            {
                PersonId = i.Person.ID,
                i.ID,
                Expiration = i.Invitation!.ExpirationDate,
            })
            .ToList();
        var usedInvitationIds = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(
            objectSpace,
            invitationRows.Select(row => row.ID).ToList());
        foreach (var row in invitationRows)
        {
            if (usedInvitationIds.Contains(row.ID) || IsExpired(row.Expiration, asOf))
                continue;
            result._invitation[row.PersonId] = result._invitation.GetValueOrDefault(row.PersonId) + 1;
        }

        foreach (var row in objectSpace.GetObjectsQuery<TravelHistory>()
            .Where(t => t.Person != null && ids.Contains(t.Person.ID))
            .Select(t => t.Person.ID)
            .ToList())
        {
            result._travel[row] = result._travel.GetValueOrDefault(row) + 1;
        }

        foreach (var row in objectSpace.GetObjectsQuery<MedicalRecord>()
            .Where(m => m.Person != null && ids.Contains(m.Person.ID))
            .Select(m => new { PersonId = m.Person.ID, m.ExpirationDate })
            .ToList())
        {
            if (IsExpired(row.ExpirationDate, asOf))
                continue;
            result._medical[row.PersonId] = result._medical.GetValueOrDefault(row.PersonId) + 1;
        }

        return result;
    }

    private static bool IsExpired(DateTime? expirationDate, DateTime asOf) =>
        expirationDate.HasValue && expirationDate.Value.Date < asOf;

    private static bool IsExpired(DateTime expirationDate, DateTime asOf) =>
        expirationDate != default && expirationDate.Date < asOf;
}