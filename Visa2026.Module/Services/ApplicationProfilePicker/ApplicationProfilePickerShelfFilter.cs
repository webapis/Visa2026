using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public enum ApplicationProfilePickerTabKind
{
    Recent = 0,
    All = 1,
    Shelf = 2,
}

public readonly record struct ApplicationProfilePickerTabKey(
    ApplicationProfilePickerTabKind Kind,
    Guid? ShelfId = null);

public static class ApplicationProfilePickerShelfFilter
{
    public static IReadOnlyList<ApplicationProfilePickerRow> Filter(
        ApplicationProfilePickerTabKey tab,
        IReadOnlyList<ApplicationProfilePickerRow> rows)
    {
        if (rows == null || rows.Count == 0)
            return Array.Empty<ApplicationProfilePickerRow>();

        return tab.Kind switch
        {
            ApplicationProfilePickerTabKind.All => rows,
            ApplicationProfilePickerTabKind.Recent => rows
                .Where(r => r.LastUsedAt.HasValue)
                .OrderByDescending(r => r.LastUsedAt)
                .Take(ApplicationProfilePickerShelfCodes.RecentCap)
                .ToList(),
            ApplicationProfilePickerTabKind.Shelf when tab.ShelfId is Guid shelfId && shelfId != Guid.Empty =>
                rows.Where(r => r.ShelfId == shelfId).ToList(),
            _ => Array.Empty<ApplicationProfilePickerRow>(),
        };
    }

    public static ApplicationProfilePickerTabKey DefaultTab(IReadOnlyList<ApplicationProfilePickerRow> rows)
    {
        if (rows != null && rows.Any(r => r.LastUsedAt.HasValue))
            return new ApplicationProfilePickerTabKey(ApplicationProfilePickerTabKind.Recent);

        return new ApplicationProfilePickerTabKey(ApplicationProfilePickerTabKind.All);
    }
}