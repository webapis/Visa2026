using System.Text.Json;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Shared progress id-map prune/merge used by ministry-leg and type-route correction commands.
/// Keys are <c>{legacyApplicationOid:D}:{step…}</c>; pruning drops all steps for listed apps.
/// </summary>
internal static class Visa2014ProgressIdMapFileHelper
{
    internal static Dictionary<string, string> PruneByLegacyApplicationPrefixes(
        IReadOnlyDictionary<string, string> existing,
        IEnumerable<Guid> legacyApplicationOids)
    {
        var prefixes = legacyApplicationOids
            .Select(id => $"{id:D}:")
            .ToHashSet(StringComparer.Ordinal);

        return existing
            .Where(kv => !prefixes.Any(p => kv.Key.StartsWith(p, StringComparison.Ordinal)))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
    }

    internal static void ApplyUpdates(
        Dictionary<string, string> existing,
        IReadOnlyDictionary<string, Guid> updates)
    {
        foreach (var (key, value) in updates)
            existing[key] = value.ToString();
    }

    internal static void PruneFileForLegacyApplications(string path, IEnumerable<Guid> legacyApplicationOids)
    {
        if (!File.Exists(path))
            return;

        var existing = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var pruned = PruneByLegacyApplicationPrefixes(existing, legacyApplicationOids);
        File.WriteAllText(path, JsonSerializer.Serialize(pruned, new JsonSerializerOptions { WriteIndented = true }));
    }

    internal static void MergeFileUpdates(string path, IReadOnlyDictionary<string, Guid> updates)
    {
        var existing = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                ?? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        ApplyUpdates(existing, updates);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true }));
    }
}
