using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014EmployeePositionHistoryImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedNoPersonMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public int ActualPositionsCreated { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014EmployeePositionHistoryODataImporter
{
    public static async Task<Visa2014EmployeePositionHistoryImportResult> RunAsync(
        ApiClient api,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string? historyIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");

        var batch = Visa2014EmployeePositionHistoryTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missingPerson = CountMissingPersonMap(batch.ImportRows, personIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged, {missingPerson} missing Person id-map).");
            return new Visa2014EmployeePositionHistoryImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedNoPersonMap = missingPerson,
            };
        }

        var resolver = new Visa2014ODataLookupResolver();
        await resolver.LoadAsync(api);

        var historyIdMap = LoadOptionalIdMap(historyIdMapOutputPath);
        if (verbose && historyIdMap.Count > 0)
            Console.WriteLine($"INF Existing EmployeePositionHistory id-map entries: {historyIdMap.Count}");

        var actualPositionCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedNoPerson = 0;
        int skippedAlreadyImported = 0;
        int actualPositionsCreated = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (historyIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in EmployeePositionHistory id-map");
                continue;
            }

            if (!TryResolveLegacyPersonOid(row, out var legacyPersonOid))
            {
                failed++;
                errors.Add($"{legacyOid}: missing legacy Person Oid on row");
                continue;
            }

            if (!personIdMap.TryGetValue(legacyPersonOid, out var personId))
            {
                skippedNoPerson++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: Person {legacyPersonOid} not in id-map");
                continue;
            }

            try
            {
                var actualPositionName = row.GetValueOrDefault("ActualPosition") as string ?? "-";
                var (actualPositionId, createdActual) = await ResolveOrCreateActualPositionAsync(
                    api, resolver, actualPositionCache, actualPositionName, verbose);
                if (!actualPositionId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: could not resolve or create ActualPosition '{actualPositionName}'");
                    continue;
                }

                if (createdActual)
                    actualPositionsCreated++;

                var payload = BuildPayload(row, resolver, personId, actualPositionId.Value);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row, resolver)})");
                    continue;
                }

                var created = await api.CreateAsync<EmployeePositionHistory>("EmployeePositionHistory", payload);
                if (created == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: POST returned null");
                    continue;
                }

                historyIdMap[legacyOid] = created.Id;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoPerson} no person map...");
                if (verbose)
                    Console.WriteLine($"  POST EmployeePositionHistory {created.Id} <- legacy {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        string? idMapPath = null;
        if (historyIdMap.Count > 0 && !string.IsNullOrWhiteSpace(historyIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(historyIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = historyIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014EmployeePositionHistoryImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedNoPersonMap = skippedNoPerson,
            SkippedAlreadyImported = skippedAlreadyImported,
            PostedCount = posted,
            FailedCount = failed,
            ActualPositionsCreated = actualPositionsCreated,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static async Task<(Guid? Id, bool Created)> ResolveOrCreateActualPositionAsync(
        ApiClient api,
        Visa2014ODataLookupResolver resolver,
        Dictionary<string, Guid> cache,
        string? name,
        bool verbose)
    {
        var key = string.IsNullOrWhiteSpace(name) ? "-" : name.Trim();
        if (cache.TryGetValue(key, out var cached))
            return (cached, false);

        var existing = resolver.ResolveActualPosition(key);
        if (existing.HasValue)
        {
            cache[key] = existing.Value;
            return (existing.Value, false);
        }

        var created = await api.CreateAsync<ActualPosition>("ActualPosition", new Dictionary<string, object?>
        {
            ["Name"] = key,
        });
        if (created == null)
            return (null, false);

        resolver.RegisterActualPosition(created);
        cache[key] = created.Id;
        if (verbose)
            Console.WriteLine($"  POST ActualPosition '{key}' -> {created.Id}");
        return (created.Id, true);
    }

    private static int CountMissingPersonMap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> personIdMap)
    {
        int missing = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveLegacyPersonOid(row, out var legacyPersonOid))
            {
                missing++;
                continue;
            }

            if (!personIdMap.ContainsKey(legacyPersonOid))
                missing++;
        }

        return missing;
    }

    private static bool TryResolveLegacyPersonOid(Dictionary<string, object?> row, out Guid legacyPersonOid)
    {
        legacyPersonOid = Guid.Empty;
        var text = row.GetValueOrDefault("Person") as string
            ?? row.GetValueOrDefault("_legacy_PersonOid") as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyPersonOid);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        Guid personId,
        Guid actualPositionId)
    {
        var positionId = resolver.ResolvePosition(row.GetValueOrDefault("Position") as string);
        var departmentId = resolver.ResolveDepartment(row.GetValueOrDefault("Department") as string);
        if (!positionId.HasValue || !departmentId.HasValue)
            return null;

        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out var startDate))
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["Position"] = new { ID = positionId.Value },
            ["Department"] = new { ID = departmentId.Value },
            ["ActualPosition"] = new { ID = actualPositionId },
            ["StartDate"] = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
        };

        if (TryParseDate(row.GetValueOrDefault("EndDate") as string, out var endDate))
            payload["EndDate"] = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        return payload;
    }

    private static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParse(text, out date);

    private static string DescribePayloadGap(Dictionary<string, object?> row, Visa2014ODataLookupResolver resolver)
    {
        var gaps = new List<string>();
        if (!resolver.ResolvePosition(row.GetValueOrDefault("Position") as string).HasValue)
            gaps.Add($"Position={row.GetValueOrDefault("Position")}");
        if (!resolver.ResolveDepartment(row.GetValueOrDefault("Department") as string).HasValue)
            gaps.Add($"Department={row.GetValueOrDefault("Department")}");
        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out _))
            gaps.Add($"StartDate={row.GetValueOrDefault("StartDate")}");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "lookup or required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
