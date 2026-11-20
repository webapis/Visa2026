using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

/// <summary>
/// Server-shared Address 2 recently-used list (Application Profile + ProjectContract), cap 6.
/// </summary>
public static class CasePersonAddress2Recent
{
    public const int Cap = 6;

    public sealed record Entry(string Text, DateTime UsedAtUtc, bool FromSameCase);

    public static string? NormalizeDisplay(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) || trimmed == "—" ? null : trimmed;
    }

    public static string TextKey(string display)
    {
        var folded = PersonSearchTextNormalizer.Fold(display ?? string.Empty).Trim();
        if (folded.Length > 450)
            folded = folded[..450];
        return folded;
    }

    public static bool Remember(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        string? text)
    {
        if (objectSpace == null || application == null)
            return false;

        var display = NormalizeDisplay(text);
        if (display == null)
            return false;

        var profileId = application.ApplicationProfile?.ID ?? Guid.Empty;
        if (profileId == Guid.Empty)
            return false;

        var projectId = application.ProjectContract?.ID ?? Guid.Empty;
        var key = TextKey(display);
        if (string.IsNullOrEmpty(key))
            return false;

        var existing = objectSpace.GetObjectsQuery<ApplicationProfileAddress2Recent>()
            .FirstOrDefault(row =>
                row.ApplicationProfileId == profileId
                && row.ProjectContractId == projectId
                && row.TextKey == key);

        if (existing != null)
        {
            existing.Address2Text = display;
            existing.UsedAtUtc = DateTime.UtcNow;
            Prune(objectSpace, profileId, projectId);
            return true;
        }

        var created = objectSpace.CreateObject<ApplicationProfileAddress2Recent>();
        created.ApplicationProfile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId)
            ?? application.ApplicationProfile!;
        created.ApplicationProfileId = profileId;
        created.ProjectContractId = projectId;
        created.TextKey = key;
        created.Address2Text = display;
        created.UsedAtUtc = DateTime.UtcNow;
        Prune(objectSpace, profileId, projectId);
        return true;
    }

    public static IReadOnlyList<Entry> ListForReview(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Guid excludePersonId)
    {
        if (objectSpace == null || application == null)
            return Array.Empty<Entry>();

        var profileId = application.ApplicationProfile?.ID ?? Guid.Empty;
        var projectId = application.ProjectContract?.ID ?? Guid.Empty;
        var map = new Dictionary<string, Entry>(StringComparer.Ordinal);

        void Offer(string? text, DateTime usedAt, bool fromSameCase) =>
            OfferInto(map, text, usedAt, fromSameCase);

        var caseNotes = CasePersonAddress2Note.LoadForApplication(objectSpace, application);
        foreach (var pair in caseNotes)
        {
            if (pair.Key == excludePersonId)
                continue;
            Offer(pair.Value, DateTime.UtcNow, fromSameCase: true);
        }

        OfferShared(objectSpace, profileId, projectId, map);

        return Finish(map);
    }

    /// <summary>Shared recent only (create wizard / no roster yet).</summary>
    public static IReadOnlyList<Entry> ListShared(
        IObjectSpace objectSpace,
        Guid profileId,
        Guid projectContractId)
    {
        if (objectSpace == null || profileId == Guid.Empty)
            return Array.Empty<Entry>();

        var map = new Dictionary<string, Entry>(StringComparer.Ordinal);
        OfferShared(objectSpace, profileId, projectContractId, map);
        return Finish(map);
    }

    private static void OfferShared(
        IObjectSpace objectSpace,
        Guid profileId,
        Guid projectId,
        Dictionary<string, Entry> map)
    {
        if (profileId == Guid.Empty)
            return;

        var shared = objectSpace.GetObjectsQuery<ApplicationProfileAddress2Recent>()
            .Where(row => row.ApplicationProfileId == profileId && row.ProjectContractId == projectId)
            .OrderByDescending(row => row.UsedAtUtc)
            .Take(Cap * 2)
            .ToList();
        foreach (var row in shared)
            OfferInto(map, row.Address2Text, row.UsedAtUtc, fromSameCase: false);
    }

    private static void OfferInto(
        Dictionary<string, Entry> map,
        string? text,
        DateTime usedAt,
        bool fromSameCase)
    {
        var display = NormalizeDisplay(text);
        if (display == null)
            return;
        var key = TextKey(display);
        if (string.IsNullOrEmpty(key))
            return;
        if (map.TryGetValue(key, out var existing))
        {
            var newer = usedAt > existing.UsedAtUtc ? usedAt : existing.UsedAtUtc;
            map[key] = new Entry(display, newer, existing.FromSameCase || fromSameCase);
            return;
        }

        map[key] = new Entry(display, usedAt, fromSameCase);
    }

    private static IReadOnlyList<Entry> Finish(Dictionary<string, Entry> map) =>
        map.Values
            .OrderByDescending(e => e.FromSameCase)
            .ThenByDescending(e => e.UsedAtUtc)
            .Take(Cap)
            .ToList();

    private static void Prune(IObjectSpace objectSpace, Guid profileId, Guid projectId)
    {
        var rows = objectSpace.GetObjectsQuery<ApplicationProfileAddress2Recent>()
            .Where(row => row.ApplicationProfileId == profileId && row.ProjectContractId == projectId)
            .OrderByDescending(row => row.UsedAtUtc)
            .ThenByDescending(row => row.ID)
            .ToList();
        for (var i = Cap; i < rows.Count; i++)
            objectSpace.Delete(rows[i]);
    }
}