using Microsoft.AspNetCore.Components.Rendering;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>Shared stacked Application-result chips beside the ListView / workspace progress track.</summary>
internal static class ApplicationListViewResultChipRender
{
    public static void AppendStacked(
        RenderTreeBuilder builder,
        ref int seq,
        IReadOnlyList<ApplicationWorkspaceIssuedResultListCoverage.Chip> chips,
        IReadOnlyList<ApplicationListLinkedPeople.Row>? people = null)
    {
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "lv-result-stems");
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

            if (string.Equals(chip.Key, ApplicationWorkspaceIssuedRecordsCatalog.Invitation, StringComparison.Ordinal))
                AppendInvitationLaterLife(builder, ref seq, people);
            else if (string.Equals(chip.Key, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, StringComparison.Ordinal))
                AppendDocumentExpiry(builder, ref seq, people, IssuedResultExpiry.WorkPermit);
            else if (string.Equals(chip.Key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.Ordinal))
                AppendDocumentExpiry(builder, ref seq, people, IssuedResultExpiry.Visa);
        }

        builder.CloseElement();
        builder.CloseElement();
    }

    private static void AppendInvitationLaterLife(
        RenderTreeBuilder builder,
        ref int seq,
        IReadOnlyList<ApplicationListLinkedPeople.Row>? people)
    {
        var used = ApplicationListLinkedPeople.CountMarks(people, InvitationLaterLife.Used);
        var expired = ApplicationListLinkedPeople.CountMarks(people, InvitationLaterLife.ExpiredUnused);
        var cancelled = ApplicationListLinkedPeople.CountMarks(people, InvitationLaterLife.Cancelled);
        var changed = ApplicationListLinkedPeople.CountMarks(people, InvitationLaterLife.Changed);
        if (used + expired + cancelled + changed <= 0)
            return;

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "lv-result-life");
        AppendLifePill(builder, ref seq, InvitationLaterLife.Used, used);
        AppendLifePill(builder, ref seq, InvitationLaterLife.ExpiredUnused, expired);
        AppendLifePill(builder, ref seq, InvitationLaterLife.Cancelled, cancelled);
        AppendLifePill(builder, ref seq, InvitationLaterLife.Changed, changed);
        builder.CloseElement();
    }

    private static void AppendDocumentExpiry(
        RenderTreeBuilder builder,
        ref int seq,
        IReadOnlyList<ApplicationListLinkedPeople.Row>? people,
        string stateKey)
    {
        var count = ApplicationListLinkedPeople.CountMarks(people, stateKey);
        if (count <= 0)
            return;

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "lv-result-life");
        AppendLifePill(builder, ref seq, stateKey, count);
        builder.CloseElement();
    }

    private static void AppendLifePill(RenderTreeBuilder builder, ref int seq, string key, int count)
    {
        if (count <= 0)
            return;

        builder.OpenElement(seq++, "span");
        var tone = IssuedResultExpiry.IsExpiryKey(key)
            ? IssuedResultExpiry.Tone
            : InvitationLaterLife.Tone(key);
        builder.AddAttribute(seq++, "class", "lv-result-life__pill lv-result-life__pill--" + tone);
        builder.AddAttribute(seq++, "title", ApplicationListLinkedPeople.Title(key));
        builder.AddContent(seq++, ApplicationListLinkedPeople.Letters(key));
        builder.AddContent(seq++, " ");
        builder.AddContent(seq++, count.ToString());
        builder.CloseElement();
    }
}