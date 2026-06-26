using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014PersonIdMapExpander
{
    public static async Task<int> ExpandAsync(
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string idMapPath,
        string targetConnectionString,
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
        var dedupeAliases = Visa2014PersonTransform.BuildDedupeLegacyAliases(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose);

        int addedFromDedupe = 0;
        foreach (var (mergedLegacyOid, canonicalLegacyOid) in dedupeAliases)
        {
            var canonicalKey = canonicalLegacyOid.ToString();
            if (!idMap.TryGetValue(canonicalKey, out var targetId))
                continue;

            var mergedKey = mergedLegacyOid.ToString();
            if (idMap.ContainsKey(mergedKey))
                continue;

            idMap[mergedKey] = targetId;
            addedFromDedupe++;
        }

        var batch = Visa2014PersonTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose: false);

        int addedFromPn = 0;
        await using (var conn = new SqlConnection(targetConnectionString))
        {
            await conn.OpenAsync();
            foreach (var row in batch.ImportRows)
            {
                var legacyKey = ((Guid)row["_legacyRowId"]!).ToString();
                if (idMap.ContainsKey(legacyKey))
                    continue;

                var pn = row.GetValueOrDefault("PersonalNumber") as string;
                if (string.IsNullOrWhiteSpace(pn))
                    continue;

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    SELECT TOP 1 CAST(ID AS varchar(36))
                    FROM People
                    WHERE GCRecord = 0 AND PersonalNumber = @pn
                    ORDER BY ID
                    """;
                cmd.Parameters.AddWithValue("@pn", pn);
                var existing = await cmd.ExecuteScalarAsync() as string;
                if (string.IsNullOrWhiteSpace(existing))
                    continue;

                idMap[legacyKey] = existing;
                addedFromPn++;
            }
        }

        await File.WriteAllTextAsync(
            idMapPath,
            JsonSerializer.Serialize(idMap, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"INF Id-map expanded: {before} → {idMap.Count} (+{idMap.Count - before}; dedupe {addedFromDedupe}, PN collision {addedFromPn})");
        return 0;
    }
}
