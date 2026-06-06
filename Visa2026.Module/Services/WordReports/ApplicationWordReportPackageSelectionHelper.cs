using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Visa2026.Module.Services.WordReports;

/// <summary>Serializes report entry keys selected in the Resminamalar package dialog.</summary>
public static class ApplicationWordReportPackageSelectionHelper
{
    public static string? Serialize(IReadOnlyList<string>? entryKeys)
    {
        if (entryKeys == null || entryKeys.Count == 0)
            return null;

        var normalized = entryKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    public static IReadOnlySet<string>? Deserialize(string? selectedReportKeysJson)
    {
        if (string.IsNullOrWhiteSpace(selectedReportKeysJson))
            return null;

        try
        {
            var keys = JsonSerializer.Deserialize<List<string>>(selectedReportKeysJson)?
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return keys is { Count: > 0 }
                ? new HashSet<string>(keys, StringComparer.Ordinal)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static IReadOnlySet<string> NormalizeSelection(
        IReadOnlyList<ApplicationWordReportPackageCatalogEntry> catalogEntries,
        IReadOnlyList<string>? requestedEntryKeys)
    {
        var allowed = catalogEntries
            .Select(entry => entry.EntryKey)
            .ToHashSet(StringComparer.Ordinal);

        if (requestedEntryKeys == null || requestedEntryKeys.Count == 0)
            return allowed;

        return requestedEntryKeys
            .Where(key => !string.IsNullOrWhiteSpace(key) && allowed.Contains(key))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
    }
}
