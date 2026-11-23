using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Officer-added Address 1–2 suggestions only (tenant-wide per field). No AddressOfResidence catalog.
/// </summary>
/// <remarks>
/// Suggestions are reusable across all applications — not scoped by Application Profile,
/// ProjectContract, ApplicationType, or other case parameters. <see cref="ApplicationProfileId"/>
/// is retained only as a required FK on insert (last writer); List ignores it.
/// </remarks>
public static class CaseSavedAddress
{
    public const int Cap = 50;
    public const string DuplicateError =
        "This address already exists in the list. Close this dialog and search for it.";

    /// <summary>Create a saved address with use count 0. Fails with <see cref="DuplicateError"/> if folded text exists.</summary>
    public static bool TryAdd(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedAddressField field,
        string? text,
        out string? error)
    {
        error = null;
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
        {
            error = "Enter an address.";
            return false;
        }

        var display = CaseAddressAutocomplete.NormalizeDisplay(text);
        if (string.IsNullOrEmpty(display))
        {
            error = "Enter an address.";
            return false;
        }

        if (display.Length > CaseAddressAutocomplete.MaxLength)
        {
            error = "Use at most 500 characters.";
            return false;
        }

        var key = CaseAddressAutocomplete.FoldKey(display);
        if (string.IsNullOrEmpty(key))
        {
            error = "Enter an address.";
            return false;
        }

        if (objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Any(row => row.Field == field && row.TextKey == key))
        {
            error = FormatDuplicateError(display);
            return false;
        }

        var profileId = profile.ID;
        var created = objectSpace.CreateObject<ApplicationProfileSavedAddress>();
        created.ApplicationProfile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId) ?? profile;
        created.ApplicationProfileId = profileId;
        created.ProjectContractId = Guid.Empty;
        created.Field = field;
        created.TextKey = key;
        created.Text = display;
        created.UseCount = 0;
        created.UsedAtUtc = DateTime.UtcNow;
        Prune(objectSpace, field);
        return true;
    }

    /// <summary>Rename without bumping use count. Same folded text (self) is allowed.</summary>
    public static bool TryRename(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedAddressField field,
        string? previous,
        string? next,
        out string? error)
    {
        error = null;
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
        {
            error = "Enter an address.";
            return false;
        }

        var from = CaseAddressAutocomplete.NormalizeDisplay(previous);
        var to = CaseAddressAutocomplete.NormalizeDisplay(next);
        if (string.IsNullOrEmpty(to))
        {
            error = "Enter an address.";
            return false;
        }

        if (to.Length > CaseAddressAutocomplete.MaxLength)
        {
            error = "Use at most 500 characters.";
            return false;
        }

        var fromKey = CaseAddressAutocomplete.FoldKey(from);
        var toKey = CaseAddressAutocomplete.FoldKey(to);
        if (string.IsNullOrEmpty(toKey))
        {
            error = "Enter an address.";
            return false;
        }

        if (!string.Equals(fromKey, toKey, StringComparison.Ordinal)
            && objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
                .Any(row => row.Field == field && row.TextKey == toKey))
        {
            error = FormatDuplicateError(to);
            return false;
        }

        if (string.IsNullOrEmpty(fromKey))
            return TryAdd(objectSpace, profile, field, to, out error);

        var source = objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Where(row => row.Field == field && row.TextKey == fromKey)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .FirstOrDefault();

        if (source == null)
            return TryAdd(objectSpace, profile, field, to, out error);

        source.Text = to;
        source.TextKey = toKey;
        source.UsedAtUtc = DateTime.UtcNow;
        return true;
    }

    /// <summary>Ensure stored case text exists in the saved list (use count 0). No-op if already present.</summary>
    public static bool EnsureExists(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedAddressField field,
        string? text)
    {
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
            return false;

        var display = CaseAddressAutocomplete.NormalizeDisplay(text);
        if (string.IsNullOrEmpty(display))
            return false;

        var key = CaseAddressAutocomplete.FoldKey(display);
        if (string.IsNullOrEmpty(key))
            return false;

        if (objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Any(row => row.Field == field && row.TextKey == key))
            return true;

        return TryAdd(objectSpace, profile, field, display, out _);
    }

    public static bool Remember(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfileSavedAddressField field,
        string? text)
        => Remember(objectSpace, application?.ApplicationProfile, field, text);

    public static bool Remember(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedAddressField field,
        string? text)
    {
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
            return false;

        var display = CaseAddressAutocomplete.NormalizeDisplay(text);
        if (string.IsNullOrEmpty(display))
            return false;

        var key = CaseAddressAutocomplete.FoldKey(display);
        if (string.IsNullOrEmpty(key))
            return false;

        var existing = objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Where(row => row.Field == field && row.TextKey == key)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .FirstOrDefault();

        if (existing != null)
        {
            existing.Text = display;
            existing.UseCount = Math.Max(1, existing.UseCount) + 1;
            existing.UsedAtUtc = DateTime.UtcNow;
            Prune(objectSpace, field);
            return true;
        }

        var profileId = profile.ID;

        var created = objectSpace.CreateObject<ApplicationProfileSavedAddress>();
        created.ApplicationProfile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId) ?? profile;
        created.ApplicationProfileId = profileId;
        // Not used for listing — kept Empty so rows are not project-keyed.
        created.ProjectContractId = Guid.Empty;
        created.Field = field;
        created.TextKey = key;
        created.Text = display;
        created.UseCount = 1;
        created.UsedAtUtc = DateTime.UtcNow;
        Prune(objectSpace, field);
        return true;
    }

    /// <summary>
    /// An in-place edit replaces that saved address, so the next search returns the new spelling.
    /// A newly typed address is remembered and the previous suggestion stays in the list.
    /// </summary>
    public static void ReplaceEdited(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedAddressField field,
        string? previous,
        string? next)
    {
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
            return;

        var prev = CaseAddressAutocomplete.NormalizeDisplay(previous);
        var nxt = CaseAddressAutocomplete.NormalizeDisplay(next);
        if (string.IsNullOrEmpty(nxt))
            return;

        if (!string.IsNullOrEmpty(prev)
            && !string.Equals(CaseAddressAutocomplete.FoldKey(prev), CaseAddressAutocomplete.FoldKey(nxt), StringComparison.Ordinal))
        {
            Rename(objectSpace, profile, field, prev, nxt);
            return;
        }

        Remember(objectSpace, profile, field, nxt);
    }

    private static void Rename(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileSavedAddressField field,
        string from,
        string to)
    {
        var toDisplay = CaseAddressAutocomplete.NormalizeDisplay(to);
        var toKey = CaseAddressAutocomplete.FoldKey(toDisplay);
        var fromKey = CaseAddressAutocomplete.FoldKey(from);
        if (string.IsNullOrEmpty(toKey))
            return;
        if (string.Equals(fromKey, toKey, StringComparison.Ordinal))
        {
            Remember(objectSpace, profile, field, toDisplay);
            return;
        }

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Where(row => row.Field == field && (row.TextKey == toKey || row.TextKey == fromKey))
            .ToList();
        var target = rows.FirstOrDefault(row => row.TextKey == toKey);
        var source = rows.FirstOrDefault(row => row.TextKey == fromKey);
        if (target != null)
        {
            target.Text = toDisplay;
            target.UseCount = Math.Max(1, target.UseCount) + 1;
            target.UsedAtUtc = DateTime.UtcNow;
            if (source != null && !ReferenceEquals(source, target))
                objectSpace.Delete(source);
            return;
        }

        if (source != null)
        {
            source.Text = toDisplay;
            source.TextKey = toKey;
            source.UseCount = Math.Max(1, source.UseCount) + 1;
            source.UsedAtUtc = DateTime.UtcNow;
            return;
        }

        Remember(objectSpace, profile, field, toDisplay);
    }

    public static IReadOnlyList<CaseAddressAutocomplete.SavedEntry> List(
        IObjectSpace objectSpace,
        ApplicationProfileSavedAddressField field)
    {
        if (objectSpace == null)
            return Array.Empty<CaseAddressAutocomplete.SavedEntry>();

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Where(row => row.Field == field)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .ToList();

        var deduped = new List<CaseAddressAutocomplete.SavedEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var text = CaseAddressAutocomplete.NormalizeDisplay(row.Text);
            if (string.IsNullOrEmpty(text))
                continue;
            var fold = CaseAddressAutocomplete.FoldKey(text);
            if (!seen.Add(fold))
                continue;
            deduped.Add(new CaseAddressAutocomplete.SavedEntry(text, row.UseCount, row.UsedAtUtc));
            if (deduped.Count >= Cap)
                break;
        }

        // Dual-read legacy Address2 recents until fully migrated (also unscoped).
        if (field == ApplicationProfileSavedAddressField.Address2 && deduped.Count == 0)
        {
            var legacy = objectSpace.GetObjectsQuery<ApplicationProfileAddress2Recent>()
                .OrderByDescending(row => row.UsedAtUtc)
                .Take(Cap * 4)
                .Select(row => new { row.Address2Text, row.UsedAtUtc })
                .ToList();
            var legacySeen = new HashSet<string>(StringComparer.Ordinal);
            var legacyOut = new List<CaseAddressAutocomplete.SavedEntry>();
            foreach (var row in legacy)
            {
                var text = CaseAddressAutocomplete.NormalizeDisplay(row.Address2Text);
                if (string.IsNullOrEmpty(text))
                    continue;
                if (!legacySeen.Add(CaseAddressAutocomplete.FoldKey(text)))
                    continue;
                legacyOut.Add(new CaseAddressAutocomplete.SavedEntry(text, 1, row.UsedAtUtc));
                if (legacyOut.Count >= Cap)
                    break;
            }
            return legacyOut;
        }

        return deduped;
    }

    private static string FormatDuplicateError(string display) =>
        "\u201c" + display + "\u201d already exists in this list. Close this dialog and search for it.";
    private static void Prune(IObjectSpace objectSpace, ApplicationProfileSavedAddressField field)
    {
        var extras = objectSpace.GetObjectsQuery<ApplicationProfileSavedAddress>()
            .Where(row => row.Field == field)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .Skip(Cap)
            .ToList();
        foreach (var row in extras)
            objectSpace.Delete(row);
    }
}