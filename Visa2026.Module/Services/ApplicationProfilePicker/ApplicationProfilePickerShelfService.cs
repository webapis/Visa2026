using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerShelfService
{
    public static IReadOnlyList<ApplicationProfilePickerShelf> ListShelves(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgressRouteKind? progressRoute = null)
    {
        var shelves = objectSpace.GetObjectsQuery<ApplicationProfilePickerShelf>()
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToList();

        // Route-specific system tabs:
        // - Via ministry: hide Registration + Business trip; keep Family member.
        // - Direct migration: hide Family member; keep Registration + Business trip.
        if (progressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries)
        {
            shelves = shelves
                .Where(s =>
                    !string.Equals(s.Code, ApplicationProfilePickerShelfCodes.Registration, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(s.Code, ApplicationProfilePickerShelfCodes.BusinessTrip, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else if (progressRoute == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
        {
            shelves = shelves
                .Where(s =>
                    !string.Equals(s.Code, ApplicationProfilePickerShelfCodes.FamilyMemberOnly, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return shelves;
    }

    public static string DisplayName(ApplicationProfilePickerShelf shelf)
    {
        if (shelf == null)
            return string.Empty;

        if (shelf.IsSystem && !string.IsNullOrWhiteSpace(shelf.Code))
        {
            var key = shelf.Code switch
            {
                ApplicationProfilePickerShelfCodes.Invitations => "ApplicationProfileInstance.Picker.Shelf.Invitations",
                ApplicationProfilePickerShelfCodes.Visa => "ApplicationProfileInstance.Picker.Shelf.Visa",
                ApplicationProfilePickerShelfCodes.Registration => "ApplicationProfileInstance.Picker.Shelf.Registration",
                ApplicationProfilePickerShelfCodes.FamilyMemberOnly => "ApplicationProfileInstance.Picker.Shelf.FamilyMemberOnly",
                ApplicationProfilePickerShelfCodes.BusinessTrip => "ApplicationProfileInstance.Picker.Shelf.BusinessTrip",
                ApplicationProfilePickerShelfCodes.WorkPermitOnly => "ApplicationProfileInstance.Picker.Shelf.WorkPermitOnly",
                ApplicationProfilePickerShelfCodes.BorderZone => "ApplicationProfileInstance.Picker.Shelf.BorderZone",
                _ => null,
            };
            if (key != null)
                return VisaUiMessages.Get(key);
        }

        return shelf.Name;
    }

    /// <summary>
    /// Toggle membership: <paramref name="shelfId"/> null clears all shelves;
    /// otherwise adds the shelf if missing or removes it if present.
    /// </summary>
    public static bool TryToggleShelf(
        IObjectSpace objectSpace,
        Guid profileId,
        Guid? shelfId,
        out string? error)
    {
        error = null;
        if (objectSpace == null || profileId == Guid.Empty)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.MoveFailed");
            return false;
        }

        var profile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId);
        if (profile == null)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.MoveFailed");
            return false;
        }

        profile.PickerShelves ??= new System.Collections.ObjectModel.ObservableCollection<ApplicationProfilePickerShelf>();

        if (shelfId is not Guid id || id == Guid.Empty)
        {
            profile.PickerShelves.Clear();
            objectSpace.CommitChanges();
            return true;
        }

        var shelf = objectSpace.GetObjectByKey<ApplicationProfilePickerShelf>(id);
        if (shelf == null)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.MoveFailed");
            return false;
        }

        var existing = profile.PickerShelves.FirstOrDefault(s => s.ID == shelf.ID);
        if (existing != null)
            profile.PickerShelves.Remove(existing);
        else
            profile.PickerShelves.Add(shelf);

        objectSpace.CommitChanges();
        return true;
    }

    public static ApplicationProfilePickerShelf? TryCreateShelf(
        IObjectSpace objectSpace,
        string name,
        out string? error)
    {
        error = null;
        var trimmed = (name ?? string.Empty).Trim();
        if (objectSpace == null || trimmed.Length == 0)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.NameRequired");
            return null;
        }

        if (trimmed.Length > 100)
            trimmed = trimmed[..100];

        var exists = objectSpace.GetObjectsQuery<ApplicationProfilePickerShelf>()
            .Any(s => s.Name == trimmed);
        if (exists)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.NameExists");
            return null;
        }

        var maxSort = objectSpace.GetObjectsQuery<ApplicationProfilePickerShelf>()
            .Select(s => (int?)s.SortOrder)
            .Max() ?? 0;

        var shelf = objectSpace.CreateObject<ApplicationProfilePickerShelf>();
        shelf.Name = trimmed;
        shelf.IsSystem = false;
        shelf.SortOrder = maxSort + 10;
        objectSpace.CommitChanges();
        return shelf;
    }

    public static bool TryRenameShelf(
        IObjectSpace objectSpace,
        Guid shelfId,
        string name,
        out string? error)
    {
        error = null;
        var trimmed = (name ?? string.Empty).Trim();
        if (objectSpace == null || shelfId == Guid.Empty || trimmed.Length == 0)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.NameRequired");
            return false;
        }

        var shelf = objectSpace.GetObjectByKey<ApplicationProfilePickerShelf>(shelfId);
        if (shelf == null || shelf.IsSystem)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.CannotRenameSystem");
            return false;
        }

        if (trimmed.Length > 100)
            trimmed = trimmed[..100];

        var clash = objectSpace.GetObjectsQuery<ApplicationProfilePickerShelf>()
            .Any(s => s.ID != shelfId && s.Name == trimmed);
        if (clash)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.NameExists");
            return false;
        }

        shelf.Name = trimmed;
        objectSpace.CommitChanges();
        return true;
    }

    public static bool TryDeleteShelf(
        IObjectSpace objectSpace,
        Guid shelfId,
        out string? error)
    {
        error = null;
        if (objectSpace == null || shelfId == Guid.Empty)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.DeleteFailed");
            return false;
        }

        var shelf = objectSpace.GetObjectByKey<ApplicationProfilePickerShelf>(shelfId);
        if (shelf == null)
            return true;

        if (shelf.IsSystem)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Picker.Shelf.CannotDeleteSystem");
            return false;
        }

        foreach (var profile in objectSpace.GetObjectsQuery<ApplicationProfile>()
                     .Where(p => p.PickerShelves.Any(s => s.ID == shelfId))
                     .ToList())
        {
            var link = profile.PickerShelves.FirstOrDefault(s => s.ID == shelfId);
            if (link != null)
                profile.PickerShelves.Remove(link);
        }

        objectSpace.Delete(shelf);
        objectSpace.CommitChanges();
        return true;
    }
}