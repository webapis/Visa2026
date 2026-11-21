using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Persist / list Requested border zone and work permit location suggestions (tenant-wide per field).
/// </summary>
public static class CaseSavedLocation
{
    public const int Cap = 200;

    public static bool Remember(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfileSavedLocationField field,
        string? text,
        ApplicationProfileSavedLocationSource source = ApplicationProfileSavedLocationSource.User)
    {
        if (objectSpace == null || application == null)
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
            if (source == ApplicationProfileSavedLocationSource.User
                && existing.Source == ApplicationProfileSavedLocationSource.Lookup)
            {
                // Keep Lookup source; still bump use count.
            }
            Prune(objectSpace, field);
            return true;
        }

        var profileId = application.ApplicationProfile?.ID ?? Guid.Empty;
        if (profileId == Guid.Empty)
            return false;

        var created = objectSpace.CreateObject<ApplicationProfileSavedLocation>();
        created.ApplicationProfile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId)
            ?? application.ApplicationProfile!;
        created.ApplicationProfileId = profileId;
        created.Field = field;
        created.TextKey = key;
        created.Text = display;
        created.UseCount = 1;
        created.UsedAtUtc = DateTime.UtcNow;
        created.Source = source;
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

    public static IReadOnlyList<CaseAddressAutocomplete.SavedEntry> List(
        IObjectSpace objectSpace,
        ApplicationProfileSavedLocationField field,
        ApplicationProfileInstance? applicationForSeed = null)
    {
        if (objectSpace == null)
            return Array.Empty<CaseAddressAutocomplete.SavedEntry>();

        EnsureCatalogSeed(objectSpace, field, applicationForSeed);

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Where(row => row.Field == field)
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

    private static void EnsureCatalogSeed(
        IObjectSpace objectSpace,
        ApplicationProfileSavedLocationField field,
        ApplicationProfileInstance? application)
    {
        var any = objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
            .Any(row => row.Field == field);
        if (any)
            return;

        var profile = application?.ApplicationProfile
            ?? objectSpace.GetObjectsQuery<ApplicationProfile>().OrderBy(p => p.ID).FirstOrDefault();
        if (profile == null)
            return;

        IEnumerable<string> labels = field == ApplicationProfileSavedLocationField.BorderZone
            ? objectSpace.GetObjectsQuery<BorderZoneName>()
                .Select(row => row.NameTm ?? row.Name)
                .ToList()
            : objectSpace.GetObjectsQuery<WorkPermittedLocationName>()
                .Select(row => row.NameTm ?? row.Name)
                .ToList();

#pragma warning disable CS0618
        foreach (var raw in labels)
#pragma warning restore CS0618
        {
            var display = CaseLocationAutocomplete.NormalizeDisplay(raw);
            if (string.IsNullOrEmpty(display) || CommaSeparatedSelectionHelper.IsNoneValue(display))
                continue;
            var key = CaseLocationAutocomplete.FoldKey(display);
            if (string.IsNullOrEmpty(key))
                continue;
            if (objectSpace.GetObjectsQuery<ApplicationProfileSavedLocation>()
                .Any(row => row.Field == field && row.TextKey == key))
                continue;

            var created = objectSpace.CreateObject<ApplicationProfileSavedLocation>();
            created.ApplicationProfile = profile;
            created.ApplicationProfileId = profile.ID;
            created.Field = field;
            created.TextKey = key;
            created.Text = display;
            created.UseCount = 1;
            created.UsedAtUtc = DateTime.UtcNow;
            created.Source = ApplicationProfileSavedLocationSource.Lookup;
        }

        try
        {
            if (objectSpace.IsModified)
                objectSpace.CommitChanges();
        }
        catch
        {
            // Seed is best-effort; field still works as free entry.
        }
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