using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

/// <summary>
/// Person-first Document copies catalog helpers (workspace tab).
/// Roster ids are <c>Person.ID</c> (same values stored on
/// <c>RowApplicationProfileInstancePersonIds</c>).
/// </summary>
public static class ApplicationItemDocumentCopiesPersonCatalog
{
    public static string PersonSectionId(Guid personId) =>
        personId == Guid.Empty ? "person:empty" : "person:" + personId.ToString("N");

    public static IReadOnlyList<Guid> FilterRosterIds(
        IReadOnlyList<Guid> rosterIds,
        IReadOnlyList<Guid>? selectedPersonIds)
    {
        if (rosterIds == null || rosterIds.Count == 0)
            return Array.Empty<Guid>();

        var roster = rosterIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (selectedPersonIds == null)
            return roster;

        if (selectedPersonIds.Count == 0)
            return Array.Empty<Guid>();

        var selected = selectedPersonIds.Where(id => id != Guid.Empty).ToHashSet();
        return roster.Where(selected.Contains).ToList();
    }

    public static (int ReadyCount, int TotalCount) CountReadySlots(
        ApplicationItemLinkedDocumentsLineSnapshot? line)
    {
        if (line?.Groups == null || line.Groups.Count == 0)
            return (0, 0);

        var total = line.Groups.Count;
        var ready = line.Groups.Count(IsSlotReady);
        return (ready, total);
    }

    public static bool IsSlotReady(ApplicationItemLinkedDocumentGroup? group)
    {
        if (group == null || group.LinkMissing)
            return false;

        return group.Files != null && group.Files.Any(file => file.HasContent);
    }

    /// <summary>
    /// Officer display name. Drops standalone "-" tokens (legacy empty LastName)
    /// without touching hyphenated names such as Jean-Luc.
    /// </summary>
    public static string DisplayPersonName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "—";

        var parts = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part != "-")
            .ToArray();

        return parts.Length == 0 ? "—" : string.Join(" ", parts);
    }
}