using System.Text.Json;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014PassportIdMapExpander
{
    public static async Task<int> ExpandAsync(
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string idMapPath,
        bool verbose)
    {
        if (!File.Exists(idMapPath))
        {
            Console.Error.WriteLine($"ERR Id-map not found: {idMapPath}");
            return 1;
        }

        var idMap = JsonSerializer.Deserialize<Dictionary<string, string>>(
            await File.ReadAllTextAsync(idMapPath)) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var before = idMap.Count;
        var dedupeAliases = Visa2014PassportTransform.BuildDedupeLegacyAliases(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose);

        int addedFromDedupe = Visa2014PassportIdMapAliasApplier.ApplyDedupeAliases(idMap, dedupeAliases);

        await File.WriteAllTextAsync(
            idMapPath,
            JsonSerializer.Serialize(idMap, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"INF Passport id-map expanded: {before} -> {idMap.Count} (+{idMap.Count - before}; dedupe {addedFromDedupe})");
        return 0;
    }
}
