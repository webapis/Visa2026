using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Compact workspace progress stepper for instance ListViews. Batch-loads history.
/// </summary>
public static class ApplicationWorkspaceListProgressSteps
{
    public readonly record struct Step(
        string Key,
        string Label,
        string State,
        string OutcomeKind,
        string Date,
        string StatusLabel);

    public static IReadOnlyList<Step> Build(
        ApplicationProfileInstance? application,
        IReadOnlyList<ApplicationProfileInstanceProgress>? history)
    {
        if (application == null)
            return Array.Empty<Step>();

        var rows = history ?? Array.Empty<ApplicationProfileInstanceProgress>();
        var steps = ApplicationWorkspaceProgressTimeline.BuildDisplay(
            application,
            application.ApplicationProfile,
            rows);
        return FromTimeline(steps);
    }

    public static IReadOnlyList<Step> FromTimeline(IReadOnlyList<ApplicationWorkspaceCaseProgressStep> steps)
    {
        if (steps == null || steps.Count == 0)
            return Array.Empty<Step>();

        var result = new List<Step>(steps.Count);
        foreach (var step in steps)
        {
            var showStatus = ShouldShowStatus(step);
            result.Add(new Step(
                step.Key,
                ApplicationProfileLocalization.ProgressStepLabel(step.Label),
                step.State,
                step.OutcomeKind,
                step.Date ?? string.Empty,
                showStatus
                    ? ApplicationProfileLocalization.ProgressStepLabel(step.CurrentStateLabel)
                    : string.Empty));
        }

        return result;
    }

    public static string Glyph(Step step, int index1Based)
    {
        if (string.Equals(step.OutcomeKind, "rejected", StringComparison.OrdinalIgnoreCase))
            return "\u2715";
        if (string.Equals(step.OutcomeKind, "cancelled", StringComparison.OrdinalIgnoreCase))
            return "\u2013";
        if (string.Equals(step.OutcomeKind, "issued", StringComparison.OrdinalIgnoreCase)
            || string.Equals(step.OutcomeKind, "ok", StringComparison.OrdinalIgnoreCase)
            || string.Equals(step.State, "done", StringComparison.OrdinalIgnoreCase))
            return "\u2713";

        return index1Based > 0 ? index1Based.ToString() : string.Empty;
    }

    public static string Tone(Step step)
    {
        if (string.Equals(step.OutcomeKind, "rejected", StringComparison.OrdinalIgnoreCase))
            return "rej";
        if (string.Equals(step.OutcomeKind, "cancelled", StringComparison.OrdinalIgnoreCase))
            return "cancel";
        if (string.Equals(step.OutcomeKind, "issued", StringComparison.OrdinalIgnoreCase))
            return "issued";
        if (string.Equals(step.State, "done", StringComparison.OrdinalIgnoreCase))
            return "done";
        if (string.Equals(step.State, "current", StringComparison.OrdinalIgnoreCase))
            return "current";
        return "pending";
    }

    public static string FormatDisplay(IReadOnlyList<Step>? steps)
    {
        if (steps == null || steps.Count == 0)
            return "\u2014";

        return string.Join(" \u00b7 ", steps.Select(s => s.Label));
    }

    public static void ApplyTo(IObjectSpace? objectSpace, IReadOnlyList<ApplicationProfileInstance> applications)
    {
        if (applications == null || applications.Count == 0)
            return;

        var ids = applications.Select(a => a.ID).Where(id => id != Guid.Empty).Distinct().ToList();
        var historyByInstance = LoadHistory(objectSpace, ids);

        foreach (var application in applications)
        {
            historyByInstance.TryGetValue(application.ID, out var rows);
            application.SetListViewProgressSteps(Build(application, rows));
        }
    }

    private static bool ShouldShowStatus(ApplicationWorkspaceCaseProgressStep step)
    {
        if (string.IsNullOrWhiteSpace(step.CurrentStateLabel))
            return false;

        return string.Equals(step.State, "current", StringComparison.OrdinalIgnoreCase)
            || string.Equals(step.OutcomeKind, "rejected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(step.OutcomeKind, "cancelled", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<Guid, List<ApplicationProfileInstanceProgress>> LoadHistory(
        IObjectSpace? objectSpace,
        IReadOnlyList<Guid> ids)
    {
        var map = new Dictionary<Guid, List<ApplicationProfileInstanceProgress>>();
        if (objectSpace == null || ids.Count == 0)
            return map;

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileInstanceProgress>()
            .Where(p => p.ApplicationProfileInstance != null && ids.Contains(p.ApplicationProfileInstance.ID))
            .Include(p => p.State)
            .ToList();

        foreach (var row in rows)
        {
            var instanceId = row.ApplicationProfileInstance?.ID ?? Guid.Empty;
            if (instanceId == Guid.Empty)
                continue;

            if (!map.TryGetValue(instanceId, out var list))
            {
                list = [];
                map[instanceId] = list;
            }

            list.Add(row);
        }

        return map;
    }
}