#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.WordReports;

public enum ApplicationReportPackageCatalogSort
{
    NewestFirst = 0,
    OldestFirst = 1,
    NameAsc = 2,
    NameDesc = 3,
}

/// <summary>Officer Resminamalar catalog card order (This profile / Shared).</summary>
public static class ApplicationReportPackageCatalogSortHelper
{
    public static IReadOnlyList<ApplicationWordReportPackageCatalogEntry> Sort(
        IEnumerable<ApplicationWordReportPackageCatalogEntry>? entries,
        ApplicationReportPackageCatalogSort mode)
    {
        var list = entries?.Where(static e => e != null).ToList()
            ?? new List<ApplicationWordReportPackageCatalogEntry>();
        if (list.Count <= 1)
            return list;

        return mode switch
        {
            ApplicationReportPackageCatalogSort.OldestFirst => list
                .OrderBy(static e => e.CreatedOnUtc ?? DateTime.MaxValue)
                .ThenBy(static e => e.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
            ApplicationReportPackageCatalogSort.NameAsc => list
                .OrderBy(static e => e.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenByDescending(static e => e.CreatedOnUtc ?? DateTime.MinValue)
                .ToList(),
            ApplicationReportPackageCatalogSort.NameDesc => list
                .OrderByDescending(static e => e.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenByDescending(static e => e.CreatedOnUtc ?? DateTime.MinValue)
                .ToList(),
            _ => list
                .OrderByDescending(static e => e.CreatedOnUtc ?? DateTime.MinValue)
                .ThenBy(static e => e.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList(),
        };
    }
}