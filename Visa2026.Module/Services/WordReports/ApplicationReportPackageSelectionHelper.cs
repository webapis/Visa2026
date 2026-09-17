#nullable enable

using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Keeps Resminamalar ZIP checkboxes in sync when Approve adds a catalog row,
/// without re-checking rows the officer already cleared.
/// </summary>
public static class ApplicationReportPackageSelectionHelper
{
    public static void ReconcileSelectedKeys(
        HashSet<string> selectedKeys,
        HashSet<string> knownKeys,
        IReadOnlyCollection<string> currentKeys,
        ref bool initialized)
    {
        ArgumentNullException.ThrowIfNull(selectedKeys);
        ArgumentNullException.ThrowIfNull(knownKeys);
        currentKeys ??= Array.Empty<string>();

        if (!initialized)
        {
            selectedKeys.Clear();
            knownKeys.Clear();
            foreach (var key in currentKeys)
            {
                selectedKeys.Add(key);
                knownKeys.Add(key);
            }

            initialized = currentKeys.Count > 0;
            return;
        }

        foreach (var key in currentKeys)
        {
            if (!knownKeys.Contains(key))
                selectedKeys.Add(key);
        }

        selectedKeys.IntersectWith(currentKeys);
        knownKeys.Clear();
        foreach (var key in currentKeys)
            knownKeys.Add(key);
    }

    public static string? FindEntryKeyByName(
        IEnumerable<ApplicationWordReportPackageCatalogEntry>? entries,
        string? templateName)
    {
        if (entries == null || string.IsNullOrWhiteSpace(templateName))
            return null;

        var name = templateName.Trim();
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.DisplayName))
                continue;
            if (string.Equals(entry.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                return entry.EntryKey;
        }

        return null;
    }
}