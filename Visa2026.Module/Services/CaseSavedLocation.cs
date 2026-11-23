using System;
using System.Collections.Generic;
using System.Linq;
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

        // No duplicates for officer-added: any existing Field+TextKey blocks Add.
        if (objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Any(row => row.Field == field && row.TextKey == key))
        {
            error = FormatDuplicateError(display);
            return false;
        }

        var profileId = profile.ID;
        var created = objectSpace.CreateObject<ApplicationProfileSavedLocation>();
        created.ApplicationProfile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId) ?? profile;
        created.ApplicationProfileId = profileId;
        created.Field = field;
        created.TextKey = key;
        created.Text = display;
        created.UseCount = 0;
        created.UsedAtUtc = DateTime.UtcNow;
        created.Source = ApplicationProfileSavedLocationSource.User;
        Prune(objectSpace, field);
        return true;
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

        var source = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Where(row => row.Field == field && row.TextKey == fromKey)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .FirstOrDefault();

        if (source == null)
            return TryAdd(objectSpace, profile, field, to, out error);

        if (!string.Equals(fromKey, toKey, StringComparison.Ordinal))
        {
            var target = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
                .Where(row => row.Field == field && row.TextKey == toKey)
                .OrderByDescending(row => row.UseCount)
                .ThenByDescending(row => row.UsedAtUtc)
                .FirstOrDefault();
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

        var existing = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Where(row => row.Field == field && row.TextKey == key)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .FirstOrDefault();
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

        var existing = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Where(row => row.Field == field && row.TextKey == key)
            .OrderByDescending(row => row.UseCount)
            .ThenByDescending(row => row.UsedAtUtc)
            .FirstOrDefault();

        if (existing != null)
        {
            existing.Text = display;
            existing.UseCount = Math.Max(1, existing.UseCount) + 1;
            existing.UsedAtUtc = DateTime.UtcNow;
            existing.Source = ApplicationProfileSavedLocationSource.User;
            Prune(objectSpace, field);
            return true;
        }

        var profileId = profile.ID;
        var created = objectSpace.CreateObject<ApplicationProfileSavedLocation>();
        created.ApplicationProfile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId) ?? profile;
        created.ApplicationProfileId = profileId;
        created.Field = field;
        created.TextKey = key;
        created.Text = display;
        created.UseCount = 1;
        created.UsedAtUtc = DateTime.UtcNow;
        created.Source = ApplicationProfileSavedLocationSource.User;
        Prune(objectSpace, field);
        return true;
    }

    public static void RememberAll(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfileSavedLocationField field,
        IEnumerable<string>? labels)
    {
        if (labels == null)
            return;
        foreach (var label in labels)
            Remember(objectSpace, application, field, label, ApplicationProfileSavedLocationSource.User);
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
        foreach (var label in CaseLocationAutocomplete.ParseChips(stored, noneValue))
            Remember(objectSpace, profile, field, label, ApplicationProfileSavedLocationSource.User);
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