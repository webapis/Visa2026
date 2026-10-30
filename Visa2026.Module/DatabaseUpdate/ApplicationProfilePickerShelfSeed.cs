using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures system picker shelves exist and assigns heuristic shelves only when
/// <see cref="ApplicationProfile.PickerShelfId"/> is still null (never overwrites officer moves).
/// </summary>
public static class ApplicationProfilePickerShelfSeed
{
    public sealed record Result(int ShelvesCreated, int ProfilesAssigned);

    public static Result EnsureSynced(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));

        var shelvesCreated = EnsureSystemShelves(objectSpace);
        var assigned = AssignNullProfiles(objectSpace);
        if (shelvesCreated > 0 || assigned > 0)
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

    public static int AssignNullProfiles(IObjectSpace objectSpace)
    {
        var byCode = objectSpace.GetObjectsQuery<ApplicationProfilePickerShelf>()
            .Where(s => s.Code != null && s.Code != string.Empty)
            .ToDictionary(s => s.Code!, StringComparer.OrdinalIgnoreCase);

        var assigned = 0;
        foreach (var profile in objectSpace.GetObjectsQuery<ApplicationProfile>()
                     .Where(p => p.PickerShelfId == null)
                     .ToList())
        {
            var code = ApplicationProfilePickerShelfHeuristic.ResolveShelfCode(profile);
            if (code == null || !byCode.TryGetValue(code, out var shelf))
                continue;

            profile.PickerShelf = shelf;
            profile.PickerShelfId = shelf.ID;
            assigned++;
        }

        return assigned;
    }
}