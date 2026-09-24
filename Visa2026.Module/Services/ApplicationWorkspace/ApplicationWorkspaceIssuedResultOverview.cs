using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Result-tab overview: issued / expected / missing by <b>person</b>.
/// Invitation / work permit / rejection count distinct people on items (not headers).
/// Visa is one per linked person. Rejection is optional — zero is not missing.
/// Seretmezlik-excluded people are not required and do not count toward coverage.
/// </summary>
public static class ApplicationWorkspaceIssuedResultOverview
{
    public readonly record struct Totals(int Issued, int Missing, int Expected);

    public static int ExpectedFor(bool isOptional, int rosterCount) =>
        isOptional ? 0 : Math.Max(0, rosterCount);

    public static int Missing(int issued, int expected, bool isOptional) =>
        isOptional ? 0 : Math.Max(0, expected - issued);

    public static int Missing(ApplicationWorkspaceCaseIssuedTile tile) =>
        tile == null ? 0 : Missing(tile.CoverageCount, tile.ExpectedCount, tile.IsOptional);

    public static int Percent(int issued, int expected) =>
        expected <= 0 ? 0 : Math.Clamp((int)Math.Round(100.0 * issued / expected), 0, 100);

    public static int CompletenessPercent(Totals totals) =>
        totals.Expected <= 0 ? 100 : Percent(totals.Issued, totals.Expected);

    public static int CoveragePercent(ApplicationWorkspaceCaseIssuedTile tile)
    {
        if (tile == null)
            return 0;
        if (tile.IsOptional)
            return tile.CoverageCount > 0 ? 100 : 0;
        return Percent(tile.CoverageCount, tile.ExpectedCount);
    }

    /// <summary>
    /// Required types only: issued = complete types, missing = short types, expected = type count.
    /// Do not add people across work permit + visa — that exceeds the roster.
    /// </summary>
    public static Totals Sum(IEnumerable<ApplicationWorkspaceCaseIssuedTile>? tiles)
    {
        var issued = 0;
        var missing = 0;
        var expected = 0;
        if (tiles == null)
            return default;

        foreach (var tile in tiles)
        {
            if (tile.IsOptional)
                continue;

            expected++;
            if (Missing(tile) == 0)
                issued++;
            else
                missing++;
        }

        return new Totals(issued, missing, expected);
    }

    public static int CountCoveredPeople(
        string catalogKey,
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace,
        int headerFallback)
    {
        if (application == null)
            return headerFallback;

        var rosterIds = RosterPersonIds(application, objectSpace);
        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.Invitation, StringComparison.OrdinalIgnoreCase))
            return CountDistinct(InvitationPersonIds(application, objectSpace), rosterIds);
        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, StringComparison.OrdinalIgnoreCase))
            return CountDistinct(WorkPermitPersonIds(application, objectSpace), rosterIds);
        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.Rejection, StringComparison.OrdinalIgnoreCase))
            return CountDistinct(RejectionPersonIds(application, objectSpace), rosterIds);
        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase))
            return CountDistinct(VisaPersonIds(application, objectSpace), rosterIds);
        if (string.Equals(catalogKey, ApplicationWorkspaceIssuedRecordsCatalog.BorderZone, StringComparison.OrdinalIgnoreCase))
            return CountDistinct(BorderZonePersonIds(application, objectSpace), rosterIds);

        return headerFallback;
    }

    /// <summary>Active roster person ids — Seretmezlik exclusions removed.</summary>
    public static HashSet<Guid> RosterPersonIds(ApplicationProfileInstance application, IObjectSpace? objectSpace)
    {
        var ids = new HashSet<Guid>();
        if (application.People != null)
        {
            foreach (var person in application.People)
            {
                if (person != null && person.ID != Guid.Empty)
                    ids.Add(person.ID);
            }
        }

        if (application.PersonResolvedLinks != null)
        {
            foreach (var link in application.PersonResolvedLinks)
            {
                if (link.PersonId != Guid.Empty)
                    ids.Add(link.PersonId);
                else if (link.Person != null && link.Person.ID != Guid.Empty)
                    ids.Add(link.Person.ID);
            }
        }

        if (ids.Count == 0 && CanQuery(application, objectSpace))
        {
            foreach (var id in objectSpace!.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
                .Where(l => l.ApplicationProfileInstanceId == application.ID && l.PersonId != Guid.Empty)
                .Select(l => l.PersonId)
                .ToList())
            {
                ids.Add(id);
            }
        }

        foreach (var excludedId in ApplicationProfileInstanceExclusionQueries.GetExcludedPersonIds(application, objectSpace))
            ids.Remove(excludedId);

        return ids;
    }

    private static int CountDistinct(IEnumerable<Guid> ids, IReadOnlySet<Guid>? rosterIds)
    {
        var people = ids.Where(id => id != Guid.Empty);
        if (rosterIds != null && rosterIds.Count > 0)
            people = people.Where(rosterIds.Contains);
        return people.Distinct().Count();
    }

    private static bool CanQuery(ApplicationProfileInstance application, IObjectSpace? objectSpace) =>
        objectSpace != null && application.ID != Guid.Empty;

    private static IEnumerable<Guid> InvitationPersonIds(ApplicationProfileInstance application, IObjectSpace? objectSpace)
    {
        if (CanQuery(application, objectSpace))
        {
            return objectSpace.GetObjectsQuery<InvitationItem>()
                .Where(i => i.Invitation != null
                    && i.Invitation.ApplicationProfileInstance != null
                    && i.Invitation.ApplicationProfileInstance.ID == application.ID
                    && i.Person != null)
                .Select(i => i.Person.ID)
                .ToList();
        }

        return (application.Invitations ?? [])
            .SelectMany(h => h.InvitationItems ?? Array.Empty<InvitationItem>())
            .Select(i => i.Person?.ID ?? Guid.Empty);
    }

    private static IEnumerable<Guid> WorkPermitPersonIds(ApplicationProfileInstance application, IObjectSpace? objectSpace)
    {
        if (CanQuery(application, objectSpace))
        {
            return objectSpace.GetObjectsQuery<WorkPermitItem>()
                .Where(i => i.WorkPermit != null
                    && i.WorkPermit.ApplicationProfileInstance != null
                    && i.WorkPermit.ApplicationProfileInstance.ID == application.ID
                    && i.Person != null)
                .Select(i => i.Person.ID)
                .ToList();
        }

        return (application.WorkPermits ?? [])
            .SelectMany(h => h.WorkPermitItems ?? Array.Empty<WorkPermitItem>())
            .Select(i => i.Person?.ID ?? Guid.Empty);
    }

    private static IEnumerable<Guid> RejectionPersonIds(ApplicationProfileInstance application, IObjectSpace? objectSpace)
    {
        if (CanQuery(application, objectSpace))
        {
            return objectSpace.GetObjectsQuery<RejectionItem>()
                .Where(i => i.Rejection != null
                    && i.Rejection.ApplicationProfileInstance != null
                    && i.Rejection.ApplicationProfileInstance.ID == application.ID
                    && i.Person != null)
                .Select(i => i.Person.ID)
                .ToList();
        }

        return (application.Rejections ?? [])
            .SelectMany(h => h.RejectionItems ?? Array.Empty<RejectionItem>())
            .Select(i => i.Person?.ID ?? Guid.Empty);
    }

    private static IEnumerable<Guid> VisaPersonIds(ApplicationProfileInstance application, IObjectSpace? objectSpace)
    {
        if (CanQuery(application, objectSpace))
        {
            return objectSpace.GetObjectsQuery<Visa>()
                .Where(v => v.IssuingApplicationProfileInstance != null
                    && v.IssuingApplicationProfileInstance.ID == application.ID
                    && v.Passport != null
                    && v.Passport.Person != null)
                .Select(v => v.Passport.Person.ID)
                .ToList();
        }

        return (application.IssuedVisas ?? [])
            .Select(v => v.Passport?.Person?.ID ?? Guid.Empty);
    }

    private static IEnumerable<Guid> BorderZonePersonIds(ApplicationProfileInstance application, IObjectSpace? objectSpace)
    {
        if (CanQuery(application, objectSpace))
        {
            return objectSpace.GetObjectsQuery<BorderZoneItem>()
                .Where(i => i.BorderZone != null
                    && i.BorderZone.ApplicationProfileInstance != null
                    && i.BorderZone.ApplicationProfileInstance.ID == application.ID
                    && i.Person != null)
                .Select(i => i.Person.ID)
                .ToList();
        }

        return (application.BorderZones ?? [])
            .SelectMany(h => h.BorderZoneItems ?? Array.Empty<BorderZoneItem>())
            .Select(i => i.Person?.ID ?? Guid.Empty);
    }
}