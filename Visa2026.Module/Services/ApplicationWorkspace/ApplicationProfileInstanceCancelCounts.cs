using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Visas to cancel on a cancel-visa letter (CVCNT): distinct People &amp; links Visa pins.
/// Last-N can pin two already-started visas; CurrentVisa + NextVisa (future-start) is not that set.
/// </summary>
public static class ApplicationProfileInstanceCancelCounts
{
    public static int Visas(ApplicationProfileInstance? application)
    {
        if (application == null)
            return 0;

        var linked = CountLinked(application, ApplicationProfileInstancePersonLinkKind.Visa);
        if (linked > 0)
            return linked;

        return FromCurrentAndNextVisa(application);
    }

    public static int WorkPermits(ApplicationProfileInstance? application)
    {
        if (application == null)
            return 0;

        var linked = CountLinked(application, ApplicationProfileInstancePersonLinkKind.WorkPermitItem);
        if (linked > 0)
            return linked;

        return FromCurrentAndPreviousWorkPermit(application);
    }

    /// <summary>
    /// Distinct Invitation headers on linked InvitationItems (CICNT).
    /// Two people on the same invitation count as one.
    /// </summary>
    public static int Invitations(ApplicationProfileInstance? application)
    {
        if (application == null)
            return 0;

        var fromHeaders = DistinctInvitationHeadersFromLinkedItems(application);
        if (fromHeaders > 0)
            return fromHeaders;

        var fromLines = FromCurrentInvitationHeaders(ApplicationRosterHelper.GetMergeLineItems(application));
        if (fromLines > 0)
            return fromLines;

        return CountLinked(application, ApplicationProfileInstancePersonLinkKind.InvitationItem);
    }

    public static int Resolve(int linkedCount, int mergeLineCurrentAndNextCount) =>
        linkedCount > 0 ? linkedCount : Math.Max(0, mergeLineCurrentAndNextCount);

    public static int FromCurrentAndNextVisa(IEnumerable<ApplicationRosterMergeLine>? lines) =>
        lines?.Sum(line => (line.CurrentVisa != null ? 1 : 0) + (line.NextVisa != null ? 1 : 0)) ?? 0;

    public static int FromCurrentAndPreviousWorkPermit(IEnumerable<ApplicationRosterMergeLine>? lines) =>
        lines?.Sum(line =>
            (line.CurrentWorkPermitItem != null ? 1 : 0) + (line.PreviousWorkPermitItem != null ? 1 : 0)) ?? 0;

    public static int FromCurrentInvitationHeaders(IEnumerable<ApplicationRosterMergeLine>? lines)
    {
        if (lines == null)
            return 0;

        var ids = new HashSet<Guid>();
        foreach (var line in lines)
        {
            var id = line.CurrentInvitationItem?.Invitation?.ID ?? Guid.Empty;
            if (id != Guid.Empty)
                ids.Add(id);
        }

        return ids.Count;
    }

    private static int FromCurrentAndNextVisa(ApplicationProfileInstance application) =>
        FromCurrentAndNextVisa(ApplicationRosterHelper.GetMergeLineItems(application));

    private static int FromCurrentAndPreviousWorkPermit(ApplicationProfileInstance application) =>
        FromCurrentAndPreviousWorkPermit(ApplicationRosterHelper.GetMergeLineItems(application));

    private static int DistinctInvitationHeadersFromLinkedItems(ApplicationProfileInstance application)
    {
        var itemIds = LinkedObjectIds(application, ApplicationProfileInstancePersonLinkKind.InvitationItem);
        if (itemIds.Count == 0)
            return 0;

        var objectSpace = ObjectSpaceHelper.Get(application);
        if (objectSpace == null)
            return 0;

        return objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(i => itemIds.Contains(i.ID) && i.Invitation != null)
            .Select(i => i.Invitation.ID)
            .Distinct()
            .Count();
    }

    private static HashSet<Guid> LinkedObjectIds(
        ApplicationProfileInstance application,
        ApplicationProfileInstancePersonLinkKind kind)
    {
        var ids = new HashSet<Guid>();
        foreach (var link in application.PersonResolvedLinks ?? [])
        {
            if (link?.LinkKind != kind || link.LinkedObjectId is not Guid id || id == Guid.Empty)
                continue;
            ids.Add(id);
        }

        if (ids.Count > 0)
            return ids;

        var objectSpace = ObjectSpaceHelper.Get(application);
        if (objectSpace == null || application.ID == Guid.Empty)
            return ids;

        foreach (var link in objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(l => l.ApplicationProfileInstanceId == application.ID && l.LinkKind == kind))
        {
            if (link.LinkedObjectId is Guid id && id != Guid.Empty)
                ids.Add(id);
        }

        return ids;
    }

    public static int CountLinked(
        ApplicationProfileInstance application,
        ApplicationProfileInstancePersonLinkKind kind)
    {
        var inMemory = ApplicationWorkspaceLinkedRecordsCatalog.CountResolved(
            application.PersonResolvedLinks,
            kind);
        if (inMemory > 0)
            return inMemory;

        var objectSpace = ObjectSpaceHelper.Get(application);
        if (objectSpace == null || application.ID == Guid.Empty)
            return 0;

        var fromQuery = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(l => l.ApplicationProfileInstanceId == application.ID)
            .ToList();
        return ApplicationWorkspaceLinkedRecordsCatalog.CountResolved(fromQuery, kind);
    }
}