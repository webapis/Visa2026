using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Output headers this case may produce (1:N). Visibility follows profile **May produce**
/// (<see cref="ApplicationProfileConfigurationResolver.ShowInvitations"/> and siblings).
/// Issued visa tile uses <see cref="ApplicationProfileConfigurationResolver.CanIssueVisa"/> (<c>ProduceVisa</c>) only —
/// not invitation; visa after invitation uses <see cref="InvitationItem"/> **Issue visa**.
/// Distinct from <see cref="ApplicationWorkspaceLinkedRecordsCatalog"/> (existing person records).
/// </summary>
public static class ApplicationWorkspaceIssuedRecordsCatalog
{
    public const string Invitation = "invitation";
    public const string WorkPermit = "workPermit";
    public const string BorderZone = "borderZone";
    public const string Rejection = "rejection";
    public const string IssuedVisa = "issuedVisa";

    public sealed record Definition(
        string Key,
        string Label,
        string Glyph,
        string Tone,
        string AddCaption,
        string NewCaption,
        string PanelTitle,
        string EmptyHint,
        Func<ApplicationProfileInstance?, bool> IsVisible);

    private static readonly Definition[] All =
    [
        new(Invitation, "Invitation", "✉", "blue",
            "+ Add invitation", "New invitation",
            "Invitations produced by this case",
            "No invitation yet. New invitation will be linked to this application.",
            ApplicationProfileConfigurationResolver.ShowInvitations),
        new(WorkPermit, "Work permit", "📄", "purple",
            "+ Add work permit", "New work permit",
            "Work permits produced by this case",
            "No work permit yet. New work permit will be linked to this application.",
            ApplicationProfileConfigurationResolver.ShowWorkPermits),
        new(BorderZone, "Border zone", "🚧", "green",
            "+ Add border zone", "New border zone",
            "Border-zone permits produced by this case",
            "No border-zone permit yet. New border zone will be linked to this application.",
            ApplicationProfileConfigurationResolver.ShowBorderZones),
        new(Rejection, "Rejection", "⛔", "orange",
            "+ Add rejection", "New rejection",
            "Rejections produced by this case",
            "No rejection yet. New rejection will be linked to this application.",
            ApplicationProfileConfigurationResolver.ShowRejections),
        new(IssuedVisa, "Issued visa", "💳", "teal",
            "+ Add issued visa", "New issued visa",
            "Visas issued by this case",
            "No issued visa yet. New visa will be linked as issued by this application.",
            ApplicationProfileConfigurationResolver.ShowIssuedVisas),
    ];

    public static IReadOnlyList<Definition> Definitions { get; } = All;

    public static bool TryGet(string key, out Definition definition)
    {
        definition = All.FirstOrDefault(d =>
            string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase))!;
        return definition != null;
    }

    public static bool IsVisible(ApplicationProfileInstance? application, string key) =>
        TryGet(key, out var def) && def.IsVisible(application);

    public static Type? ResolveHeaderType(string key) => key switch
    {
        Invitation => typeof(Invitation),
        WorkPermit => typeof(WorkPermit),
        BorderZone => typeof(BorderZone),
        Rejection => typeof(Rejection),
        IssuedVisa => typeof(Visa),
        _ => null,
    };

    public static bool TryGetDocumentCopiesFamily(string key, out HeaderDocumentCopiesFamily family)
    {
        family = default;
        if (string.Equals(key, Invitation, StringComparison.OrdinalIgnoreCase))
        {
            family = HeaderDocumentCopiesFamily.Invitation;
            return true;
        }

        if (string.Equals(key, WorkPermit, StringComparison.OrdinalIgnoreCase))
        {
            family = HeaderDocumentCopiesFamily.WorkPermit;
            return true;
        }

        if (string.Equals(key, Rejection, StringComparison.OrdinalIgnoreCase))
        {
            family = HeaderDocumentCopiesFamily.Rejection;
            return true;
        }

        if (string.Equals(key, BorderZone, StringComparison.OrdinalIgnoreCase))
        {
            family = HeaderDocumentCopiesFamily.BorderZone;
            return true;
        }

        return false;
    }
}
