using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Document copies nav completeness: missing required scans (gap + partial slots).
/// Empty roster is a red dot. Application form is not a gap.
/// </summary>
public static class ApplicationWorkspaceDocumentCopiesCompleteness
{
    public enum NavStatus
    {
        EmptyRoster = 0,
        Incomplete = 1,
        Complete = 2,
    }

    public static bool IsSlotMissing(ApplicationItemLinkedDocumentMergedGroup? group)
    {
        if (group == null)
            return false;

        return group.LinkMissing || group.Files.Count == 0 || group.MissingLines.Count > 0;
    }

    public static int MissingSlotCount(ApplicationItemDocumentCopiesReadinessSummary? summary)
    {
        if (summary == null)
            return 0;

        return summary.GapSlotCount + summary.PartialSlotCount;
    }

    public static NavStatus Resolve(bool hasPeople, ApplicationItemDocumentCopiesReadinessSummary? summary)
    {
        if (!hasPeople)
            return NavStatus.EmptyRoster;

        return MissingSlotCount(summary) > 0 ? NavStatus.Incomplete : NavStatus.Complete;
    }

    public static ApplicationItemDocumentCopiesReadinessSummary? TryLoadSummary(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        IReadOnlyList<Guid> personIds)
    {
        if (objectSpace == null || applicationProfileInstanceId == Guid.Empty)
            return null;

        var ids = (personIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return ApplicationItemDocumentCopiesReadinessSummary.Compute(
                Array.Empty<ApplicationItemLinkedDocumentMergedGroup>());
        }

        var (application, people) = ApplicationRosterHelper.LoadApplicationPeople(
            objectSpace,
            applicationProfileInstanceId,
            ids);
        if (application == null)
            return null;

        if (people.Count == 0)
        {
            return ApplicationItemDocumentCopiesReadinessSummary.Compute(
                Array.Empty<ApplicationItemLinkedDocumentMergedGroup>());
        }

        var lines = ApplicationPersonLinkedDocumentsResolver.ResolveMany(objectSpace, application, people);
        var merged = ApplicationItemLinkedDocumentsMerger.MergeBySlot(lines);
        return ApplicationItemDocumentCopiesReadinessSummary.Compute(merged);
    }
}