using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// During Office preparation, empty Case summary Use fields (red tiles) block
/// Progress / documents / Resminamalar / SLA. People and links stay open.
/// Process number is an Advance rule, not this gate.
/// </summary>
public static class ApplicationWorkspaceCaseSummaryCompletenessGate
{
    public const string OfficeStepKey = "office";

    public static bool IsProcessTab(string? tabKey)
    {
        if (string.IsNullOrWhiteSpace(tabKey))
            return false;

        return tabKey.Equals("progress", StringComparison.OrdinalIgnoreCase)
            || tabKey.Equals("documents", StringComparison.OrdinalIgnoreCase)
            || tabKey.Equals("resminamalar", StringComparison.OrdinalIgnoreCase)
            || tabKey.Equals("sla", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOfficePreparation(ApplicationWorkspaceCaseView? view)
    {
        if (view?.ProgressSteps is { Count: > 0 } steps)
        {
            return steps.Any(step =>
                string.Equals(step.State, "current", StringComparison.OrdinalIgnoreCase)
                && string.Equals(step.Key, OfficeStepKey, StringComparison.OrdinalIgnoreCase));
        }

        return string.Equals(
            view?.Chrome.CurrentStep,
            "Office preparation",
            StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<ApplicationWorkspaceCaseHeaderField> MissingRequiredFields(
        IEnumerable<ApplicationWorkspaceCaseHeaderField>? fields)
    {
        if (fields == null)
            return Array.Empty<ApplicationWorkspaceCaseHeaderField>();

        return fields
            .Where(field =>
                field.FillState == ApplicationWorkspaceCaseSummaryFillState.Empty
                && !string.Equals(
                    field.Key,
                    ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber,
                    StringComparison.Ordinal))
            .ToList();
    }

    public static bool BlocksProcessNavigation(ApplicationWorkspaceCaseView? view) =>
        IsOfficePreparation(view) && MissingRequiredFields(view?.HeaderFields).Count > 0;

    public static bool BlocksTab(ApplicationWorkspaceCaseView? view, string? tabKey) =>
        IsProcessTab(tabKey) && BlocksProcessNavigation(view);

    public static string FormatBannerMessage(IReadOnlyList<ApplicationWorkspaceCaseHeaderField> missing)
    {
        if (missing == null || missing.Count == 0)
            return string.Empty;

        var names = string.Join(", ", missing.Select(field => field.Label).Where(label => !string.IsNullOrWhiteSpace(label)));
        if (string.IsNullOrWhiteSpace(names))
            names = "required fields";

        if (missing.Count == 1)
            return "Complete the required Case summary field before continuing. Missing: " + names + ".";

        return "Complete required Case summary fields before continuing. Missing: " + names + ".";
    }

    public static string FormatReadinessMissingSuffix(IReadOnlyList<ApplicationWorkspaceCaseHeaderField> missing)
    {
        if (missing == null || missing.Count == 0)
            return string.Empty;

        return missing.Count == 1
            ? " (1 missing)"
            : " (" + missing.Count.ToString() + " missing)";
    }
}