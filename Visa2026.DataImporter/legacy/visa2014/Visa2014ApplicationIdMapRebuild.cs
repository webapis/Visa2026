using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Rebuilds ApplicationProfileInstance legacy-to-target id-map using FullApplicationNumber + ApplicationDate + ApplicationType,
/// with greedy one-to-one assignment and ApplicationItem parent overlap for twin disambiguation.
/// </summary>
internal static class Visa2014ApplicationProfileInstanceIdMapRebuild
{
    internal sealed class Result
    {
        public Dictionary<Guid, Guid> Map { get; init; } = [];
        public int Matched { get; init; }
        public int Skipped { get; init; }
    }

    public static async Task<Result> RebuildAsync(
        SqlConnection conn,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string? applicationItemIdMapPath,
        bool verbose)
    {
        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose: false);

        var importRows = batch.ImportRows
            .Where(r => r.GetValueOrDefault("_importAction") as string != "skip")
            .OrderBy(r => (Guid)r["_legacyRowId"]!)
            .ToList();

        var disambiguator = await ApplicationItemDisambiguator.LoadAsync(
            conn,
            legacyConnectionString,
            applicationItemIdMapPath,
            verbose);

        var map = new Dictionary<Guid, Guid>();
        var assignedTargets = new HashSet<Guid>();
        int matched = 0;
        int skipped = batch.Skipped.Count;

        foreach (var row in importRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            var identity = Visa2014ApplicationTransform.ApplicationImportIdentity.FromExportRow(row);
            if (identity == null)
            {
                skipped++;
                continue;
            }

            var candidates = await ListGuidAsync(
                conn,
                Visa2014ApplicationTransform.ApplicationTargetCandidatesSql,
                ("@fullNumber", identity.Value.FullApplicationNumber),
                ("@applicationDate", identity.Value.ApplicationDate),
                ("@applicationTypeName", identity.Value.ApplicationTypeName));

            var available = candidates.Where(c => !assignedTargets.Contains(c)).ToList();
            if (available.Count == 0)
                available = candidates;

            Guid? targetId = null;
            if (available.Count == 1)
            {
                targetId = available[0];
            }
            else if (available.Count > 1)
            {
                targetId = disambiguator.PickBestTarget(legacyOid, available);
                targetId ??= available[0];
            }

            if (!targetId.HasValue)
            {
                skipped++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid:D}: no target for {identity.Value.GroupKey}");
                continue;
            }

            if (assignedTargets.Contains(targetId.Value))
            {
                skipped++;
                if (verbose)
                {
                    Console.WriteLine(
                        $"  SKIP {legacyOid:D}: target {targetId.Value:D} already assigned " +
                        $"(identity {identity.Value.GroupKey})");
                }

                continue;
            }

            assignedTargets.Add(targetId.Value);
            map[legacyOid] = targetId.Value;
            matched++;
        }

        return new Result { Map = map, Matched = matched, Skipped = skipped };
    }

    private static async Task<List<Guid>> ListGuidAsync(
        SqlConnection conn,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        var ids = new List<Guid>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var text = reader.GetString(0);
            if (Guid.TryParse(text, out var id))
                ids.Add(id);
        }

        return ids;
    }

    private sealed class ApplicationItemDisambiguator
    {
        private readonly Dictionary<Guid, HashSet<Guid>> _legacyPiasByApplication = [];
        private Dictionary<Guid, Guid> _piaToTargetItem = [];
        private readonly Dictionary<Guid, Guid> _targetItemToApplication = [];

        private ApplicationItemDisambiguator() { }

        public static async Task<ApplicationItemDisambiguator> LoadAsync(
            SqlConnection conn,
            string legacyConnectionString,
            string? applicationItemIdMapPath,
            bool verbose)
        {
            var disambiguator = new ApplicationItemDisambiguator();
            disambiguator.LoadLegacyPias(legacyConnectionString);
            if (!string.IsNullOrWhiteSpace(applicationItemIdMapPath) && File.Exists(applicationItemIdMapPath))
                disambiguator._piaToTargetItem = Visa2014IdMapHelper.Load(applicationItemIdMapPath);

            if (disambiguator._piaToTargetItem.Count > 0)
                await disambiguator.LoadTargetItemParentsAsync(conn);

            if (verbose)
            {
                Console.WriteLine(
                    $"INF ApplicationProfileInstance id-map disambiguator: " +
                    $"{disambiguator._legacyPiasByApplication.Count} legacy app(s), " +
                    $"{disambiguator._piaToTargetItem.Count} item id-map entr(y/ies), " +
                    $"{disambiguator._targetItemToApplication.Count} target item parent(s)");
            }

            return disambiguator;
        }

        private void LoadLegacyPias(string legacyConnectionString)
        {
            const string sql = """
                SELECT CAST(Oid AS varchar(36)) AS Oid, CAST(ApplicationProfileInstance AS varchar(36)) AS ApplicationProfileInstanceOid
                FROM dbo.PersonInApplication
                WHERE GCRecord IS NULL
                """;

            foreach (var row in Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false))
            {
                if (!Guid.TryParse(row.GetValueOrDefault("Oid"), out var piaOid)
                    || !Guid.TryParse(row.GetValueOrDefault("ApplicationProfileInstanceOid"), out var legacyApplicationProfileInstanceOid))
                    continue;

                if (!_legacyPiasByApplication.TryGetValue(legacyApplicationProfileInstanceOid, out var pias))
                {
                    pias = [];
                    _legacyPiasByApplication[legacyApplicationProfileInstanceOid] = pias;
                }

                pias.Add(piaOid);
            }
        }

        private async Task LoadTargetItemParentsAsync(SqlConnection conn)
        {
            var itemIds = _piaToTargetItem.Values.Distinct().ToList();
            if (itemIds.Count == 0)
                return;

            const int chunkSize = 500;
            for (int offset = 0; offset < itemIds.Count; offset += chunkSize)
            {
                var chunk = itemIds.Skip(offset).Take(chunkSize).ToList();
                var paramNames = chunk.Select((_, i) => $"@p{i}").ToList();
                var sql = $"""
                    SELECT CAST(ID AS varchar(36)) AS ItemId, CAST(ApplicationProfileInstanceID AS varchar(36)) AS ApplicationProfileInstanceId
                    FROM ApplicationItems
                    WHERE (GCRecord IS NULL OR GCRecord = 0)
                      AND ID IN ({string.Join(", ", paramNames)})
                    """;

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                for (int i = 0; i < chunk.Count; i++)
                    cmd.Parameters.AddWithValue(paramNames[i], chunk[i]);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (Guid.TryParse(reader.GetString(0), out var itemId)
                        && Guid.TryParse(reader.GetString(1), out var applicationId))
                        _targetItemToApplication[itemId] = applicationId;
                }
            }
        }

        public Guid? PickBestTarget(Guid legacyApplicationProfileInstanceOid, IReadOnlyList<Guid> candidates)
        {
            if (candidates.Count == 0)
                return null;

            if (!_legacyPiasByApplication.TryGetValue(legacyApplicationProfileInstanceOid, out var pias) || pias.Count == 0)
                return null;

            var scores = new Dictionary<Guid, int>();
            foreach (var candidate in candidates)
                scores[candidate] = 0;

            foreach (var piaOid in pias)
            {
                if (!_piaToTargetItem.TryGetValue(piaOid, out var targetItemId))
                    continue;
                if (!_targetItemToApplication.TryGetValue(targetItemId, out var parentApplicationProfileInstanceId))
                    continue;
                if (scores.ContainsKey(parentApplicationProfileInstanceId))
                    scores[parentApplicationProfileInstanceId]++;
            }

            var bestScore = scores.Values.Max();
            if (bestScore <= 0)
                return null;

            return scores
                .Where(kv => kv.Value == bestScore)
                .OrderBy(kv => kv.Key)
                .First()
                .Key;
        }
    }
}