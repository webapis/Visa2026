using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Officer-added location suggestions only (tenant-wide per field). Same model as Address 1/2 — no BorderZoneName/WorkPermittedLocationName catalog.
/// </summary>
public static class CaseSavedLocation
{
    public const int Cap = 200;
    public const string DuplicateError =
        "This location already exists in the list. Close this dialog and search for it.";

    /// <summary>
    /// Unsaved Field+TextKey rows for this ObjectSpace. EF GetObjectsQuery often misses Added entities,
    /// so Remember on many work-permit people with the same chips would insert duplicates.
    /// </summary>
    private static readonly ConditionalWeakTable<IObjectSpace, Dictionary<(ApplicationProfileSavedLocationField Field, string TextKey), ApplicationProfileSavedLocation>> PendingBySpace =
        new();

    /// <summary>Create a saved location with use count 0. Fails on duplicate folded text.</summary>
    public static bool TryAdd(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedLocationField field,
        string? text,
        out string? error)
    {
        error = null;
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
        {
            error = "Enter a location name.";
            return false;
        }

        var display = CaseLocationAutocomplete.NormalizeDisplay(text);
        if (string.IsNullOrEmpty(display) || CommaSeparatedSelectionHelper.IsNoneValue(display))
        {
            error = "Enter a location name.";
            return false;
        }

        if (display.Length > CaseLocationAutocomplete.MaxLength)
        {
            error = "Use at most 100 characters.";
            return false;
        }

        var key = CaseLocationAutocomplete.FoldKey(display);
        if (string.IsNullOrEmpty(key))
        {
            error = "Enter a location name.";
            return false;
        }

        // No duplicates for officer-added: any existing Field+TextKey blocks Add
        // (including rows created earlier in this same save batch).
        if (FindExisting(objectSpace, field, key) != null)
        {
            error = FormatDuplicateError(display);
            return false;
        }

        var created = CreateRow(objectSpace, profile, field, key, display, useCount: 0);
        Prune(objectSpace, field);
        return created != null;
    }

    /// <summary>Rename without bumping use count. Same folded text (self) is allowed.</summary>
    public static bool TryRename(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedLocationField field,
        string? previous,
        string? next,
        out string? error)
    {
        error = null;
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
        {
            error = "Enter a location name.";
            return false;
        }

        var from = CaseLocationAutocomplete.NormalizeDisplay(previous);
        var to = CaseLocationAutocomplete.NormalizeDisplay(next);
        if (string.IsNullOrEmpty(to) || CommaSeparatedSelectionHelper.IsNoneValue(to))
        {
            error = "Enter a location name.";
            return false;
        }

        if (to.Length > CaseLocationAutocomplete.MaxLength)
        {
            error = "Use at most 100 characters.";
            return false;
        }

        var fromKey = CaseLocationAutocomplete.FoldKey(from);
        var toKey = CaseLocationAutocomplete.FoldKey(to);
        if (string.IsNullOrEmpty(toKey))
        {
            error = "Enter a location name.";
            return false;
        }

        if (string.IsNullOrEmpty(fromKey))
            return TryAdd(objectSpace, profile, field, to, out error);

        var source = FindExisting(objectSpace, field, fromKey);

        if (source == null)
            return TryAdd(objectSpace, profile, field, to, out error);

        if (!string.Equals(fromKey, toKey, StringComparison.Ordinal))
        {
            var target = FindExisting(objectSpace, field, toKey);
            if (target != null && !ReferenceEquals(target, source))
            {
                error = FormatDuplicateError(to);
                return false;
            }
        }

        source.Text = to;
        source.TextKey = toKey;
        source.UsedAtUtc = DateTime.UtcNow;
        source.Source = ApplicationProfileSavedLocationSource.User;
        return true;
    }

    /// <summary>Ensure stored chip label exists in the saved list (use count 0). No-op if present.</summary>
    public static bool EnsureExists(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedLocationField field,
        string? text)
    {
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
            return false;

        var display = CaseLocationAutocomplete.NormalizeDisplay(text);
        if (string.IsNullOrEmpty(display) || CommaSeparatedSelectionHelper.IsNoneValue(display))
            return false;

        var key = CaseLocationAutocomplete.FoldKey(display);
        if (string.IsNullOrEmpty(key))
            return false;

        var existing = FindExisting(objectSpace, field, key);
        if (existing != null)
        {
            // One row per TextKey — never insert a duplicate. Promote legacy Lookup to User so List shows it.
            existing.Text = display;
            existing.Source = ApplicationProfileSavedLocationSource.User;
            existing.UsedAtUtc = DateTime.UtcNow;
            return true;
        }

        return TryAdd(objectSpace, profile, field, display, out _);
    }

    private static string FormatDuplicateError(string display) =>
        $"\u201c{display}\u201d already exists in this list. Close this dialog and search for it.";

    public static bool Remember(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfileSavedLocationField field,
        string? text,
        ApplicationProfileSavedLocationSource source = ApplicationProfileSavedLocationSource.User)
        => Remember(objectSpace, application?.ApplicationProfile, field, text, source);

    public static bool Remember(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedLocationField field,
        string? text,
        ApplicationProfileSavedLocationSource source = ApplicationProfileSavedLocationSource.User)
    {
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
            return false;

        var display = CaseLocationAutocomplete.NormalizeDisplay(text);
        if (string.IsNullOrEmpty(display))
            return false;
        if (CommaSeparatedSelectionHelper.IsNoneValue(display))
            return false;

        var key = CaseLocationAutocomplete.FoldKey(display);
        if (string.IsNullOrEmpty(key))
            return false;

        var existing = FindExisting(objectSpace, field, key);

        if (existing != null)
        {
            existing.Text = display;
            existing.UseCount = Math.Max(1, existing.UseCount) + 1;
            existing.UsedAtUtc = DateTime.UtcNow;
            existing.Source = ApplicationProfileSavedLocationSource.User;
            TrackPending(objectSpace, existing);
            Prune(objectSpace, field);
            return true;
        }

        var created = CreateRow(objectSpace, profile, field, key, display, useCount: 1);
        Prune(objectSpace, field);
        return created != null;
    }

    private static ApplicationProfileSavedLocation? CreateRow(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileSavedLocationField field,
        string key,
        string display,
        int useCount)
    {
        // Re-check after any concurrent Remember in the same batch (GetObjectsQuery misses Added).
        var existing = FindExisting(objectSpace, field, key);
        if (existing != null)
        {
            existing.Text = display;
            if (useCount > 0)
                existing.UseCount = Math.Max(1, existing.UseCount) + 1;
            existing.UsedAtUtc = DateTime.UtcNow;
            existing.Source = ApplicationProfileSavedLocationSource.User;
            TrackPending(objectSpace, existing);
            return existing;
        }

        var profileId = profile.ID;
        var created = objectSpace.CreateObject<ApplicationProfileSavedLocation>();
        created.ApplicationProfile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId) ?? profile;
        created.ApplicationProfileId = profileId;
        created.Field = field;
        created.TextKey = key;
        created.Text = display;
        created.UseCount = useCount;
        created.UsedAtUtc = DateTime.UtcNow;
        created.Source = ApplicationProfileSavedLocationSource.User;
        TrackPending(objectSpace, created);
        return created;
    }

    /// <summary>
    /// DB row, ModifiedObjects / GetObjects, or a row we already CreateObject'd in this ObjectSpace.
    /// </summary>
    private static ApplicationProfileSavedLocation? FindExisting(
        IObjectSpace objectSpace,
        ApplicationProfileSavedLocationField field,
        string key)
    {
        if (PendingBySpace.TryGetValue(objectSpace, out var pending)
            && pending.TryGetValue((field, key), out var tracked)
            && tracked != null
            && !objectSpace.IsDeletedObject(tracked))
        {
            return tracked;
        }

        ApplicationProfileSavedLocation? best = null;
        foreach (var obj in objectSpace.ModifiedObjects)
        {
            if (obj is not ApplicationProfileSavedLocation row)
                continue;
            if (row.Field != field
                || !string.Equals(row.TextKey, key, StringComparison.Ordinal)
                || objectSpace.IsDeletedObject(row))
            {
                continue;
            }

            if (best == null
                || row.UseCount > best.UseCount
                || (row.UseCount == best.UseCount && row.UsedAtUtc > best.UsedAtUtc))
            {
                best = row;
            }
        }

        if (best != null)
        {
            TrackPending(objectSpace, best);
            return best;
        }

        try
        {
            foreach (var row in objectSpace.GetObjects<ApplicationProfileSavedLocation>())
            {
                if (row.Field != field
                    || !string.Equals(row.TextKey, key, StringComparison.Ordinal)
                    || objectSpace.IsDeletedObject(row))
                {
                    continue;
                }

                if (best == null
                    || row.UseCount > best.UseCount
                    || (row.UseCount == best.UseCount && row.UsedAtUtc > best.UsedAtUtc))
                {
                    best = row;
                }
            }
        }
        catch
        {
            // Some ObjectSpaces reject unbound GetObjects; fall through to query.
        }

        if (best != null)
        {
            TrackPending(objectSpace, best);
            return best;
        }

        var fromStore = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Where(row => row.Field == field && row.TextKey == key)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .FirstOrDefault();
        if (fromStore != null)
            TrackPending(objectSpace, fromStore);
        return fromStore;
    }

    private static void TrackPending(IObjectSpace objectSpace, ApplicationProfileSavedLocation row)
    {
        if (objectSpace == null || row == null || string.IsNullOrEmpty(row.TextKey))
            return;

        var map = PendingBySpace.GetValue(objectSpace, static _ => new Dictionary<(ApplicationProfileSavedLocationField, string), ApplicationProfileSavedLocation>());
        map[(row.Field, row.TextKey)] = row;
    }

    public static void RememberAll(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfileSavedLocationField field,
        IEnumerable<string>? labels)
    {
        if (labels == null)
            return;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var label in labels)
        {
            var key = CaseLocationAutocomplete.FoldKey(label);
            if (string.IsNullOrEmpty(key) || !seen.Add(key))
                continue;
            Remember(objectSpace, application, field, label, ApplicationProfileSavedLocationSource.User);
        }
    }

    /// <summary>Remember each chip in a stored comma-separated location. Skips Ýok and blank.</summary>
    public static void RememberStored(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? application,
        ApplicationProfileSavedLocationField field,
        string? stored,
        string? noneValue)
    {
        if (application == null)
            return;
        RememberAll(objectSpace, application, field, CaseLocationAutocomplete.ParseChips(stored, noneValue));
    }

    public static void RememberStored(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedLocationField field,
        string? stored,
        string? noneValue)
    {
        if (objectSpace == null || profile == null)
            return;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var label in CaseLocationAutocomplete.ParseChips(stored, noneValue))
        {
            var key = CaseLocationAutocomplete.FoldKey(label);
            if (string.IsNullOrEmpty(key) || !seen.Add(key))
                continue;
            Remember(objectSpace, profile, field, label, ApplicationProfileSavedLocationSource.User);
        }
    }

    /// <summary>
    /// Keep an in-place chip edit. One label replaced by another updates that saved row
    /// (so the next search returns the new spelling). Other changes remember the new labels.
    /// </summary>
    public static void ReplaceEdited(
        IObjectSpace objectSpace,
        ApplicationProfile? profile,
        ApplicationProfileSavedLocationField field,
        string? previousStored,
        string? nextStored,
        string? noneValue)
    {
        if (objectSpace == null || profile == null || profile.ID == Guid.Empty)
            return;

        var previous = CaseLocationAutocomplete.ParseChips(previousStored, noneValue);
        var next = CaseLocationAutocomplete.ParseChips(nextStored, noneValue);
        var removed = previous
            .Where(old => next.All(label => !SameFold(old, label)))
            .ToList();
        var added = next
            .Where(label => previous.All(old => !SameFold(old, label)))
            .ToList();

        if (removed.Count == 1 && added.Count == 1)
        {
            Rename(objectSpace, profile, field, removed[0], added[0]);
            return;
        }

        foreach (var label in next)
            Remember(objectSpace, profile, field, label, ApplicationProfileSavedLocationSource.User);
    }

    private static void Rename(
        IObjectSpace objectSpace,
        ApplicationProfile profile,
        ApplicationProfileSavedLocationField field,
        string from,
        string to)
    {
        var toDisplay = CaseLocationAutocomplete.NormalizeDisplay(to);
        var toKey = CaseLocationAutocomplete.FoldKey(toDisplay);
        var fromKey = CaseLocationAutocomplete.FoldKey(from);
        if (string.IsNullOrEmpty(toKey) || CommaSeparatedSelectionHelper.IsNoneValue(toDisplay))
            return;
        if (string.Equals(fromKey, toKey, StringComparison.Ordinal))
        {
            Remember(objectSpace, profile, field, toDisplay, ApplicationProfileSavedLocationSource.User);
            return;
        }

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Where(row => row.Field == field && (row.TextKey == toKey || row.TextKey == fromKey))
            .ToList();
        var target = rows.FirstOrDefault(row => row.TextKey == toKey);
        var source = rows.FirstOrDefault(row => row.TextKey == fromKey);
        if (target != null)
        {
            target.Text = toDisplay;
            target.UseCount = Math.Max(1, target.UseCount) + 1;
            target.UsedAtUtc = DateTime.UtcNow;
            target.Source = ApplicationProfileSavedLocationSource.User;
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
            source.Source = ApplicationProfileSavedLocationSource.User;
            return;
        }

        Remember(objectSpace, profile, field, toDisplay, ApplicationProfileSavedLocationSource.User);
    }

    private static bool SameFold(string left, string right) =>
        string.Equals(
            CaseLocationAutocomplete.FoldKey(left),
            CaseLocationAutocomplete.FoldKey(right),
            StringComparison.Ordinal);

    public static IReadOnlyList<CaseAddressAutocomplete.SavedEntry> List(
        IObjectSpace objectSpace,
        ApplicationProfileSavedLocationField field)
    {
        if (objectSpace == null)
            return Array.Empty<CaseAddressAutocomplete.SavedEntry>();

        // Officer-added only. Catalog BorderZoneName / WorkPermittedLocationName are not used.
        var rows = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Where(row => row.Field == field && row.Source == ApplicationProfileSavedLocationSource.User)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .ToList();

        var deduped = new List<CaseAddressAutocomplete.SavedEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var text = CaseLocationAutocomplete.NormalizeDisplay(row.Text);
            if (string.IsNullOrEmpty(text) || CommaSeparatedSelectionHelper.IsNoneValue(text))
                continue;
            var fold = CaseLocationAutocomplete.FoldKey(text);
            if (!seen.Add(fold))
                continue;
            deduped.Add(new CaseAddressAutocomplete.SavedEntry(text, row.UseCount, row.UsedAtUtc));
            if (deduped.Count >= Cap)
                break;
        }

        return deduped;
    }

    private static void Prune(IObjectSpace objectSpace, ApplicationProfileSavedLocationField field)
    {
        var extras = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Where(row => row.Field == field
                && row.Source == ApplicationProfileSavedLocationSource.User)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .Skip(Cap)
            .ToList();
        foreach (var row in extras)
            objectSpace.Delete(row);
    }
}