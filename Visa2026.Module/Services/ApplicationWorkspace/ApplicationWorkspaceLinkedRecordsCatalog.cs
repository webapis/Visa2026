using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Canonical linked-record types for case workspace tabs, overview tiles, and people grids (plan §10.3).
/// Counts come from sticky <see cref="ApplicationProfileInstancePersonResolvedLink"/> rows.
/// </summary>
public static class ApplicationWorkspaceLinkedRecordsCatalog
{
    public sealed record Definition(
        ApplicationProfileInstancePersonLinkKind Kind,
        string TabKey,
        string PersonRecordKey,
        string Label,
        string Glyph,
        Func<ApplicationProfileInstance?, bool> IsConfigured);

    private static readonly Definition[] All =
    [
        new(ApplicationProfileInstancePersonLinkKind.Passport, "passport", "passport", "Passport", "🛂",
            ApplicationProfileConfigurationResolver.ShowPreviousPassport),
        new(ApplicationProfileInstancePersonLinkKind.Education, "education", "education", "Education", "🎓",
            ApplicationProfileConfigurationResolver.ShowCurrentEducation),
        new(ApplicationProfileInstancePersonLinkKind.Position, "position", "position", "Position", "💼",
            ApplicationProfileConfigurationResolver.ShowCurrentWorkDuty),
        new(ApplicationProfileInstancePersonLinkKind.AddressOfResidence, "address", "address", "Address", "📍",
            ApplicationProfileConfigurationResolver.ShowCurrentAddressOfResidence),
        new(ApplicationProfileInstancePersonLinkKind.Visa, "visa", "visa", "Visa", "💳",
            ApplicationProfileConfigurationResolver.ShowCurrentVisa),
        new(ApplicationProfileInstancePersonLinkKind.InvitationItem, "inv", "inv", "Invitation", "✉",
            ApplicationProfileConfigurationResolver.ShowCurrentInvitationItem),
        new(ApplicationProfileInstancePersonLinkKind.WorkPermitItem, "wp", "wp", "Work permit", "📄",
            ApplicationProfileConfigurationResolver.ShowCurrentWorkPermitItem),
        new(ApplicationProfileInstancePersonLinkKind.BorderZoneItem, "bz", "bz", "Border zone", "🚧",
            ApplicationProfileConfigurationResolver.RequirePersonBorderZoneItem),
        new(ApplicationProfileInstancePersonLinkKind.Salary, "salary", "salary", "Salary", "💰",
            ApplicationProfileConfigurationResolver.ShowCurrentSalary),
        new(ApplicationProfileInstancePersonLinkKind.MedicalRecord, "medical", "medical", "Medical", "🩺",
            ApplicationProfileConfigurationResolver.ShowCurrentMedicalRecord),
        new(ApplicationProfileInstancePersonLinkKind.RejectionItem, "rejection", "rejection", "Rejection", "⛔",
            ApplicationProfileConfigurationResolver.RequirePersonRejectionItem),
        new(ApplicationProfileInstancePersonLinkKind.TravelHistory, "travel", "travel", "Travel history", "✈",
            ApplicationProfileConfigurationResolver.RequirePersonTravelHistory),
    ];

    public static IReadOnlyList<Definition> Definitions { get; } = All;

    public static bool IsConfigured(ApplicationProfileInstance? application, ApplicationProfileInstancePersonLinkKind kind) =>
        TryGet(kind, out var def) && def.IsConfigured(application);

    public static bool IsConfigured(ApplicationProfileInstance? application, string tabKey) =>
        TryGetByTabKey(tabKey, out var def) && def.IsConfigured(application);

    public static int CountResolved(IEnumerable<ApplicationProfileInstancePersonResolvedLink> links, ApplicationProfileInstancePersonLinkKind kind) =>
        DistinctLinkedObjectIds(links, personId: null, kind);

    public static int CountResolvedForPerson(
        IEnumerable<ApplicationProfileInstancePersonResolvedLink> links,
        Guid personId,
        ApplicationProfileInstancePersonLinkKind kind) =>
        DistinctLinkedObjectIds(links, personId, kind);

    private static int DistinctLinkedObjectIds(
        IEnumerable<ApplicationProfileInstancePersonResolvedLink> links,
        Guid? personId,
        ApplicationProfileInstancePersonLinkKind kind)
    {
        var ids = new HashSet<Guid>();
        foreach (var link in links ?? [])
        {
            if (personId is Guid pid && link.PersonId != pid)
                continue;
            if (!HasResolvedLink(link, kind) || link.LinkedObjectId is not Guid id)
                continue;
            ids.Add(id);
        }

        return ids.Count;
    }

    public static bool TryGet(ApplicationProfileInstancePersonLinkKind kind, out Definition definition)
    {
        definition = All.FirstOrDefault(d => d.Kind == kind)!;
        return definition != null;
    }

    public static bool TryGetByTabKey(string tabKey, out Definition definition)
    {
        definition = All.FirstOrDefault(d =>
            string.Equals(d.TabKey, tabKey, StringComparison.OrdinalIgnoreCase))!;
        return definition != null;
    }

    public static string GlyphForTabKey(string tabKey) =>
        TryGetByTabKey(tabKey, out var def) ? def.Glyph : "📎";

    private static bool HasResolvedLink(ApplicationProfileInstancePersonResolvedLink? link, ApplicationProfileInstancePersonLinkKind kind) =>
        link?.LinkKind == kind
        && link.LinkedObjectId is Guid id
        && id != Guid.Empty;
}
