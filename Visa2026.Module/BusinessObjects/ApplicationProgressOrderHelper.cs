using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

public static class ApplicationProgressOrderHelper
{
    /// <summary>
    /// Canonical timeline rank for a progress step. Legacy <c>_REVIEW_STARTED</c> rows sort
    /// immediately before the matching <c>_REVIEW_APPROVED</c> step.
    /// </summary>
    public static int GetWorkflowSortKey(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return 500;

        var state = stateCode.Trim();

        if (string.Equals(state, ApplicationProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(state, out var leg))
        {
            if (state.EndsWith("_REVIEW_STARTED", StringComparison.OrdinalIgnoreCase))
                return 9 + leg * 2;

            if (state.EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase))
                return 10 + leg * 2;

            if (state.EndsWith("_REVIEW_REJECTED", StringComparison.OrdinalIgnoreCase))
                return 11 + leg * 2;
        }

        if (string.Equals(state, ApplicationProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
            return 999;
        if (string.Equals(state, ApplicationProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
            return 1000;
        if (string.Equals(state, ApplicationProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase))
            return 1001;
        if (string.Equals(state, ApplicationProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase))
            return 1002;

        return 500;
    }

    public static int CompareTimelineOrder(
        string? stateCodeA,
        DateTime dateA,
        Guid idA,
        string? stateCodeB,
        DateTime dateB,
        Guid idB)
    {
        var keyCompare = GetWorkflowSortKey(stateCodeA).CompareTo(GetWorkflowSortKey(stateCodeB));
        if (keyCompare != 0)
            return keyCompare;

        var dateCompare = dateA.CompareTo(dateB);
        if (dateCompare != 0)
            return dateCompare;

        return idA.CompareTo(idB);
    }

    public static int CompareTimelineOrder(ApplicationProgress a, ApplicationProgress b) =>
        CompareTimelineOrder(
            a?.State?.Code,
            a?.Date ?? DateTime.MinValue,
            a?.ID ?? Guid.Empty,
            b?.State?.Code,
            b?.Date ?? DateTime.MinValue,
            b?.ID ?? Guid.Empty);

    /// <summary>Assigns 1-based <see cref="ApplicationProgress.Order"/> values within each application group.</summary>
    public static void AssignTimelineOrders(IReadOnlyList<ApplicationProgress> siblings)
    {
        if (siblings == null || siblings.Count == 0)
            return;

        var ordered = siblings
            .OrderBy(p => p, Comparer<ApplicationProgress>.Create(CompareTimelineOrder))
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Order = i + 1;
    }

    public static int ResolveNextOrder(ApplicationProgress progress, IObjectSpace objectSpace)
    {
        if (progress == null || objectSpace == null)
            return 1;

        var application = progress.Application;
        if (application == null)
            return 1;

        var maxOrder = 0;

        if (application.ProgressHistory != null)
        {
            foreach (var sibling in application.ProgressHistory)
            {
                if (ReferenceEquals(sibling, progress) || objectSpace.IsObjectToDelete(sibling))
                    continue;

                if (sibling.Order > maxOrder)
                    maxOrder = sibling.Order;
            }
        }

        if (!objectSpace.IsNewObject(application) && application.ID != Guid.Empty)
        {
            var persistedMax = objectSpace.GetObjectsQuery<ApplicationProgress>()
                .Where(p => p.Application != null && p.Application.ID == application.ID && p.ID != progress.ID)
                .Select(p => (int?)p.Order)
                .Max() ?? 0;

            maxOrder = Math.Max(maxOrder, persistedMax);
        }

        return maxOrder + 1;
    }
    /// <summary>Compares sibling rows: <see cref="ApplicationProgress.Order"/> first, then workflow/date/ID.</summary>
    public static int CompareSiblingOrder(ApplicationProgress a, ApplicationProgress b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        var orderA = a?.Order ?? 0;
        var orderB = b?.Order ?? 0;

        if (orderA > 0 && orderB > 0)
        {
            var orderCompare = orderA.CompareTo(orderB);
            if (orderCompare != 0)
                return orderCompare;
        }
        else if (orderA > 0)
            return 1;
        else if (orderB > 0)
            return -1;

        return CompareTimelineOrder(a!, b!);
    }

    public static ApplicationProgress? GetLastTimelineStep(Application application, IObjectSpace objectSpace)
    {
        if (application == null || objectSpace == null)
            return null;

        ApplicationProgress? last = null;
        foreach (var sibling in GetTimelineSiblings(application, objectSpace))
        {
            if (last == null || CompareSiblingOrder(sibling, last) > 0)
                last = sibling;
        }

        return last;
    }

    public static bool IsLastTimelineStep(ApplicationProgress progress, IObjectSpace objectSpace)
    {
        if (progress?.Application == null || objectSpace == null)
            return false;

        var last = GetLastTimelineStep(progress.Application, objectSpace);
        return last != null && ReferenceEquals(last, progress);
    }

    private static IEnumerable<ApplicationProgress> GetTimelineSiblings(Application application, IObjectSpace objectSpace)
    {
        var seen = new HashSet<Guid>();

        if (application.ProgressHistory != null)
        {
            foreach (var progress in application.ProgressHistory)
            {
                if (objectSpace.IsObjectToDelete(progress))
                    continue;

                if (progress.ID != Guid.Empty && !seen.Add(progress.ID))
                    continue;

                yield return progress;
            }
        }

        if (objectSpace.IsNewObject(application) || application.ID == Guid.Empty)
            yield break;

        foreach (var progress in objectSpace.GetObjectsQuery<ApplicationProgress>()
                     .Where(p => p.Application != null && p.Application.ID == application.ID))
        {
            if (objectSpace.IsObjectToDelete(progress))
                continue;

            if (progress.ID != Guid.Empty && !seen.Add(progress.ID))
                continue;

            yield return progress;
        }
    }
}
