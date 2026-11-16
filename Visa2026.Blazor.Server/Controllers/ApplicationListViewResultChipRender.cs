using System;
using Microsoft.AspNetCore.Components.Rendering;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>Shared stacked Application-result chips beside the ListView progress track.</summary>
internal static class ApplicationListViewResultChipRender
{
    public static void AppendStacked(
        RenderTreeBuilder builder,
        ref int seq,
        IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip> chips,
        VisaIssueWindowCalculator.Badge? visaWindow = null)
    {
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "lv-result-stems");
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "lv-result-chips lv-result-chips--stack");
        builder.AddAttribute(seq++, "aria-label", "Application result");
        foreach (var chip in chips)
        {
            var isVisa = string.Equals(
                chip.Key,
                ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa,
                StringComparison.OrdinalIgnoreCase);
            var tone = isVisa && visaWindow?.RecolorVisaChip == true
                ? "warn"
                : ApplicationWorkspaceIssuedResultListCoverage.Tone(chip);
            if (isVisa && visaWindow is { } window)
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "lv-visa-line");
                AppendChip(builder, ref seq, chip, tone);
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "lv-visa-window lv-visa-window--" + window.Tone);
                builder.AddAttribute(seq++, "title", window.Title);
                builder.AddContent(seq++, window.Days.ToString());
                builder.AddContent(seq++, " ");
                builder.OpenElement(seq++, "small");
                builder.AddContent(seq++, "gün");
                builder.CloseElement();
                builder.CloseElement();
                builder.CloseElement();
            }
            else
            {
                AppendChip(builder, ref seq, chip, tone);
            }
        }

        builder.CloseElement();
        builder.CloseElement();
    }

    private static void AppendChip(
        RenderTreeBuilder builder,
        ref int seq,
        ApplicationWorkspaceIssuedResultListCoverage.Chip chip,
        string tone)
    {
        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "lv-result-chip lv-result-chip--" + tone);
        builder.AddContent(seq++, ApplicationWorkspaceIssuedResultListCoverage.Caption(chip));
        builder.AddContent(seq++, " ");
        builder.OpenElement(seq++, "strong");
        builder.AddContent(seq++, ApplicationWorkspaceIssuedResultListCoverage.RatioText(chip));
        builder.CloseElement();
        builder.CloseElement();
    }
}
