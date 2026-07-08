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
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string? historyIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose,
        bool supplementPermitReferencedOnly = false)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");

        var batch = supplementPermitReferencedOnly
            ? Visa2014EmployeePositionHistoryTransform.PrepareSupplementPermitReferencedImportBatch(
                legacyConnectionString,
                lookupTranslationPaths,
                maxRows,
                verbose)
            : Visa2014EmployeePositionHistoryTransform.PrepareImportBatch(
                legacyConnectionString,
                lookupTranslationPaths,
                maxRows,
                verbose);

        if (supplementPermitReferencedOnly && verbose)
            Console.WriteLine("INF Mode: supplement permit-referenced soft-deleted WorkHistoryOfEmployee rows.");

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

        var historyIdMap = LoadOptionalIdMap(historyIdMapOutputPath);
        if (verbose && historyIdMap.Count > 0)
            Console.WriteLine($"INF Existing EmployeePositionHistory id-map entries: {historyIdMap.Count}");

        var actualPositionCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var positionCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var departmentCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedNoPerson = 0;
        int skippedAlreadyImported = 0;
        int actualPositionsCreated = 0;
        int positionsCreated = 0;
        int departmentsCreated = 0;

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
                    target, resolver, actualPositionCache, actualPositionName, verbose);
                if (!actualPositionId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: could not resolve or create ActualPosition '{actualPositionName}'");
                    continue;
                }

                if (createdActual)
                    actualPositionsCreated++;

                var positionName = row.GetValueOrDefault("Position") as string;
                var (positionId, createdPosition) = await ResolveOrCreatePositionAsync(
                    target, resolver, positionCache, positionName, verbose);
                if (!positionId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: could not resolve or create Position '{positionName}'");
                    continue;
                }

                if (createdPosition)
                    positionsCreated++;

                var departmentName = row.GetValueOrDefault("Department") as string;
                var (departmentId, createdDepartment) = await ResolveOrCreateDepartmentAsync(
                    target, resolver, departmentCache, departmentName, verbose);
                if (!departmentId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: could not resolve or create Department '{departmentName}'");
                    continue;
                }

                if (createdDepartment)
                    departmentsCreated++;

                var payload = BuildPayload(row, personId, actualPositionId.Value, positionId.Value, departmentId.Value);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row, positionId, departmentId)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.EmployeePositionHistory), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                historyIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoPerson} no person map...");
                if (verbose)
                    Console.WriteLine($"  SAVE EmployeePositionHistory {createdId.Value} <- legacy {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        await target.FlushAsync();

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

    private static async Task<(Guid? Id, bool Created)> ResolveOrCreatePositionAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Dictionary<string, Guid> cache,
        string? nameTm,
        bool verbose)
    {
        var key = string.IsNullOrWhiteSpace(nameTm) ? "-" : nameTm.Trim();
        if (cache.TryGetValue(key, out var cached))
            return (cached, false);

        var existing = resolver.ResolvePosition(key);
        if (existing.HasValue)
        {
            cache[key] = existing.Value;
            return (existing.Value, false);
        }

        var createdId = await target.CreateAsync(
            typeof(Visa2026.Module.BusinessObjects.Position),
            new Dictionary<string, object?> { ["NameTm"] = key });
        if (!createdId.HasValue)
            return (null, false);

        await target.FlushAsync();
        resolver.RegisterPosition(new Position { Id = createdId.Value, NameTm = key });
        cache[key] = createdId.Value;
        if (verbose)
            Console.WriteLine($"  SAVE Position '{key}' -> {createdId.Value}");
        return (createdId.Value, true);
    }

    private static async Task<(Guid? Id, bool Created)> ResolveOrCreateDepartmentAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        Dictionary<string, Guid> cache,
        string? nameTm,
        bool verbose)
    {
        var key = string.IsNullOrWhiteSpace(nameTm) ? "-" : nameTm.Trim();
        if (cache.TryGetValue(key, out var cached))
            return (cached, false);

        var existing = resolver.ResolveDepartment(key);
        if (existing.HasValue)
        {
            cache[key] = existing.Value;
            return (existing.Value, false);
        }

        var createdId = await target.CreateAsync(
            typeof(Visa2026.Module.BusinessObjects.Department),
            new Dictionary<string, object?> { ["NameTm"] = key });
        if (!createdId.HasValue)
            return (null, false);

        await target.FlushAsync();
        resolver.RegisterDepartment(new Department { Id = createdId.Value, NameTm = key });
        cache[key] = createdId.Value;
        if (verbose)
            Console.WriteLine($"  SAVE Department '{key}' -> {createdId.Value}");
        return (createdId.Value, true);
    }

    private static async Task<(Guid? Id, bool Created)> ResolveOrCreateActualPositionAsync(
        IVisa2014ImportTarget target,
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

        var createdId = await target.CreateAsync(
            typeof(Visa2026.Module.BusinessObjects.ActualPosition),
            new Dictionary<string, object?> { ["Name"] = key });
        if (!createdId.HasValue)
            return (null, false);

        // Persist the new lookup row now so the EmployeePositionHistory FK ({ ID }) resolves
        // against a committed ActualPosition (in-process target keys ObjectSpaces by type).
        await target.FlushAsync();

        resolver.RegisterActualPosition(new ActualPosition { Id = createdId.Value, Name = key });
        cache[key] = createdId.Value;
        if (verbose)
            Console.WriteLine($"  SAVE ActualPosition '{key}' -> {createdId.Value}");
        return (createdId.Value, true);
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
        Guid personId,
        Guid actualPositionId,
        Guid positionId,
        Guid departmentId)
    {
        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out var startDate))
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["Position"] = new { ID = positionId },
            ["Department"] = new { ID = departmentId },
            ["ActualPosition"] = new { ID = actualPositionId },
            ["StartDate"] = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
        };

        if (TryParseDate(row.GetValueOrDefault("EndDate") as string, out var endDate))
            payload["EndDate"] = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        return payload;
    }

    private static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParse(text, out date);

    private static string DescribePayloadGap(
        Dictionary<string, object?> row,
        Guid? positionId,
        Guid? departmentId)
    {
        var gaps = new List<string>();
        if (!positionId.HasValue)
            gaps.Add($"Position={row.GetValueOrDefault("Position")}");
        if (!departmentId.HasValue)
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

    public static async Task<Visa2014SyncEntityResult> RunSyncAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        Visa2014SyncContext sync,
        int? maxRows,
        bool verbose,
        bool supplementPermitReferencedOnly = false)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");

        var batch = supplementPermitReferencedOnly
            ? Visa2014EmployeePositionHistoryTransform.PrepareSupplementPermitReferencedImportBatch(
                legacyConnectionString,
                lookupTranslationPaths,
                maxRows,
                verbose)
            : Visa2014EmployeePositionHistoryTransform.PrepareImportBatch(
                legacyConnectionString,
                lookupTranslationPaths,
                maxRows,
                verbose);

        var actualPositionCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var row in batch.ImportRows)
        {
            var name = row.GetValueOrDefault("ActualPosition") as string ?? "-";
            var key = string.IsNullOrWhiteSpace(name) ? "-" : name.Trim();
            if (actualPositionCache.ContainsKey(key))
                continue;

            var existing = resolver.ResolveActualPosition(key);
            if (existing.HasValue)
                actualPositionCache[key] = existing.Value;
        }

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (sync.IdMap.ContainsKey(legacyOid))
                continue;

            var name = row.GetValueOrDefault("ActualPosition") as string ?? "-";
            var key = string.IsNullOrWhiteSpace(name) ? "-" : name.Trim();
            if (actualPositionCache.ContainsKey(key))
                continue;

            var (actualPositionId, _) = await ResolveOrCreateActualPositionAsync(
                target, resolver, actualPositionCache, name, verbose);
            if (actualPositionId.HasValue)
                actualPositionCache[key] = actualPositionId.Value;
        }

        return await Visa2014SyncUpsertHelper.RunAsync(
            target,
            typeof(Visa2026.Module.BusinessObjects.EmployeePositionHistory),
            "EmployeePositionHistory",
            batch.ImportRows,
            sync,
            row => BuildSyncPayload(row, resolver, personIdMap, sync.IdMap, actualPositionCache),
            batch.LegacyRowCount,
            batch.Skipped.Count,
            batch.DedupeMergedCount,
            verbose);
    }

    private static Dictionary<string, object?>? BuildSyncPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> historyIdMap,
        IReadOnlyDictionary<string, Guid> actualPositionCache)
    {
        var legacyOid = (Guid)row["_legacyRowId"]!;
        var isUpdate = historyIdMap.ContainsKey(legacyOid);

        if (!TryResolveLegacyPersonOid(row, out var legacyPersonOid))
            return isUpdate ? BuildPayloadWithoutPerson(row, resolver, actualPositionCache) : null;

        if (!personIdMap.TryGetValue(legacyPersonOid, out var personId))
            return isUpdate ? BuildPayloadWithoutPerson(row, resolver, actualPositionCache) : null;

        var actualPositionName = row.GetValueOrDefault("ActualPosition") as string ?? "-";
        var key = string.IsNullOrWhiteSpace(actualPositionName) ? "-" : actualPositionName.Trim();
        if (!actualPositionCache.TryGetValue(key, out var actualPositionId))
            return isUpdate ? BuildPayloadWithoutPerson(row, resolver, actualPositionCache) : null;

        var positionId = resolver.ResolvePosition(row.GetValueOrDefault("Position") as string);
        var departmentId = resolver.ResolveDepartment(row.GetValueOrDefault("Department") as string);
        if (!positionId.HasValue || !departmentId.HasValue)
            return isUpdate ? BuildPayloadWithoutPerson(row, resolver, actualPositionCache) : null;

        return BuildPayload(row, personId, actualPositionId, positionId.Value, departmentId.Value);
    }

    private static Dictionary<string, object?>? BuildPayloadWithoutPerson(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        IReadOnlyDictionary<string, Guid> actualPositionCache)
    {
        var positionId = resolver.ResolvePosition(row.GetValueOrDefault("Position") as string);
        var departmentId = resolver.ResolveDepartment(row.GetValueOrDefault("Department") as string);
        if (!positionId.HasValue || !departmentId.HasValue)
            return null;

        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out var startDate))
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Position"] = new { ID = positionId.Value },
            ["Department"] = new { ID = departmentId.Value },
            ["StartDate"] = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
        };

        var actualPositionName = row.GetValueOrDefault("ActualPosition") as string ?? "-";
        var key = string.IsNullOrWhiteSpace(actualPositionName) ? "-" : actualPositionName.Trim();
        if (actualPositionCache.TryGetValue(key, out var actualPositionId))
            payload["ActualPosition"] = new { ID = actualPositionId };

        if (TryParseDate(row.GetValueOrDefault("EndDate") as string, out var endDate))
            payload["EndDate"] = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

        return payload;
    }
}
