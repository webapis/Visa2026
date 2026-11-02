using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures system picker shelves exist and recomputes system-shelf membership from profile Name keywords.
/// Custom (non-system) shelves are left alone. System links are added and removed to match heuristics.
/// </summary>
public static class ApplicationProfilePickerShelfSeed
{
    public sealed record Result(int ShelvesCreated, int ProfilesAssigned);

    public static Result EnsureSynced(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));

        var shelvesCreated = EnsureSystemShelves(objectSpace);
        // Persist new shelves (e.g. business_trip) before membership sync so M2M FKs resolve.
        if (shelvesCreated > 0)
            objectSpace.CommitChanges();

        var assigned = SyncHeuristicShelves(objectSpace);
        objectSpace.CommitChanges();

        return new Result(shelvesCreated, assigned);
    }

    public static int EnsureSystemShelves(IObjectSpace objectSpace)
    {
        var created = 0;
        foreach (var (code, defaultName, sortOrder) in ApplicationProfilePickerShelfCodes.SystemShelves)
        {
            var existing = objectSpace.GetObjectsQuery<ApplicationProfilePickerShelf>()
                .FirstOrDefault(s => s.Code == code);
            if (existing != null)
            {
                if (!existing.IsSystem)
                    existing.IsSystem = true;
                if (existing.SortOrder != sortOrder)
                    existing.SortOrder = sortOrder;
                if (!string.Equals(existing.Name, defaultName, StringComparison.Ordinal))
                    existing.Name = defaultName;
                continue;
            }

            var shelf = objectSpace.CreateObject<ApplicationProfilePickerShelf>();
            shelf.Code = code;
            shelf.Name = defaultName;
            shelf.SortOrder = sortOrder;
            shelf.IsSystem = true;
            created++;
        }

        return created;
    }

    /// <summary>
    /// Recompute system-shelf membership from Name keywords (add missing, remove stale).
    /// Officer custom shelves are not removed.
    /// </summary>
    public static int SyncHeuristicShelves(IObjectSpace objectSpace)
    {
        // Recompute every call — do not cache in a static field (hot reload / incremental F5
        // can leave a stale set that omits newly added system codes like business_trip).
        var systemCodes = new HashSet<string>(
            ApplicationProfilePickerShelfCodes.SystemShelves.Select(s => s.Code),
            StringComparer.OrdinalIgnoreCase);

        var byCode = LoadShelvesByCode(objectSpace);
        if (byCode.Count == 0)
            return 0;

        var systemShelfIds = byCode
            .Where(kv => systemCodes.Contains(kv.Key))
            .Select(kv => kv.Value.ID)
            .ToHashSet();

        var touched = 0;
        foreach (var profile in objectSpace.GetObjectsQuery<ApplicationProfile>().ToList())
        {
            profile.PickerShelves ??= new ObservableCollection<ApplicationProfilePickerShelf>();
            _ = profile.PickerShelves.ToList();

            var wantedCodes = ApplicationProfilePickerShelfHeuristic.ResolveShelfCodes(profile)
                .Where(c => systemCodes.Contains(c))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var changed = false;

            foreach (var shelf in profile.PickerShelves.ToList())
            {
                if (!systemShelfIds.Contains(shelf.ID))
                    continue;

                var code = shelf.Code ?? string.Empty;
                if (wantedCodes.Contains(code))
                    continue;

                profile.PickerShelves.Remove(shelf);
                changed = true;
            }

            foreach (var code in wantedCodes)
            {
                if (!byCode.TryGetValue(code, out var shelf))
                    continue;
                if (profile.PickerShelves.Any(s => s.ID == shelf.ID))
                    continue;

                profile.PickerShelves.Add(shelf);
                changed = true;
            }

            if (changed)
                touched++;
        }

        return touched;
    }

    public static int MergeHeuristicShelves(IObjectSpace objectSpace) =>
        SyncHeuristicShelves(objectSpace);

    private static Dictionary<string, ApplicationProfilePickerShelf> LoadShelvesByCode(IObjectSpace objectSpace) =>
        objectSpace.GetObjectsQuery<ApplicationProfilePickerShelf>()
            .Where(s => s.Code != null && s.Code != string.Empty)
            .ToDictionary(s => s.Code!, StringComparer.OrdinalIgnoreCase);
}