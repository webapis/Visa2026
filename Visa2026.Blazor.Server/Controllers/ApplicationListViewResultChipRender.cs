using Microsoft.AspNetCore.Components.Rendering;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>Shared stacked Application-result chips beside the ListView / workspace progress track.</summary>
internal static class ApplicationListViewResultChipRender
{
    public static void AppendStacked(
        RenderTreeBuilder builder,
        ref int seq,
        IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip> chips)
    {
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "lv-result-chips lv-result-chips--stack");
        builder.AddAttribute(seq++, "aria-label", "Application result");
        foreach (var chip in chips)
        {
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "lv-result-chip lv-result-chip--" + ApplicationWorkspaceIssuedResultListCoverage.Tone(chip));
            builder.AddContent(seq++, ApplicationWorkspaceIssuedResultListCoverage.Caption(chip));
            builder.AddContent(seq++, " ");
            builder.OpenElement(seq++, "strong");
            builder.AddContent(seq++, ApplicationWorkspaceIssuedResultListCoverage.RatioText(chip));
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}