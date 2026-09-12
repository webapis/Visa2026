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

    public static int Resolve(int linkedCount, int mergeLineCurrentAndNextCount) =>
        linkedCount > 0 ? linkedCount : Math.Max(0, mergeLineCurrentAndNextCount);

    public static int FromCurrentAndNextVisa(IEnumerable<ApplicationRosterMergeLine>? lines) =>
        lines?.Sum(line => (line.CurrentVisa != null ? 1 : 0) + (line.NextVisa != null ? 1 : 0)) ?? 0;

    public static int FromCurrentAndPreviousWorkPermit(IEnumerable<ApplicationRosterMergeLine>? lines) =>
        lines?.Sum(line =>
            (line.CurrentWorkPermitItem != null ? 1 : 0) + (line.PreviousWorkPermitItem != null ? 1 : 0)) ?? 0;

    private static int FromCurrentAndNextVisa(ApplicationProfileInstance application) =>
        FromCurrentAndNextVisa(ApplicationRosterHelper.GetMergeLineItems(application));

    private static int FromCurrentAndPreviousWorkPermit(ApplicationProfileInstance application) =>
        FromCurrentAndPreviousWorkPermit(ApplicationRosterHelper.GetMergeLineItems(application));

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