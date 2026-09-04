using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014WorkPermitItemImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedMissingRequiredIdMap { get; init; }
    public int PositionResolvedViaFallback { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014WorkPermitItemODataImporter
{
    public static async Task<Visa2014WorkPermitItemImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string passportIdMapPath,
        string employeePositionHistoryIdMapPath,
        string workPermitIdMapPath,
        string? workPermitItemIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var passportIdMap = Visa2014IdMapHelper.Load(passportIdMapPath);
        var positionHistoryIdMap = Visa2014IdMapHelper.Load(employeePositionHistoryIdMapPath);
        var workPermitIdMap = Visa2014IdMapHelper.Load(workPermitIdMapPath);
        var positionResolver = new Visa2014WorkPermitItemPositionResolver(
            legacyConnectionString,
            positionHistoryIdMap,
            verbose);

        if (verbose)
        {
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");
            Console.WriteLine($"INF Passport id-map entries: {passportIdMap.Count}");
            Console.WriteLine($"INF EmployeePositionHistory id-map entries: {positionHistoryIdMap.Count}");
            Console.WriteLine($"INF WorkPermit id-map entries: {workPermitIdMap.Count}");
        }

        var batch = Visa2014WorkPermitItemTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missing = CountMissingRequiredIdMap(
                batch.ImportRows,
                personIdMap,
                passportIdMap,
                positionResolver,
                workPermitIdMap,
                out var fallbackCount);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged, {missing} missing required id-map, {fallbackCount} position fallback).");
            return new Visa2014WorkPermitItemImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedMissingRequiredIdMap = missing,
                PositionResolvedViaFallback = fallbackCount,
            };
        }

        var workPermitItemIdMap = LoadOptionalIdMap(workPermitItemIdMapOutputPath);
        if (verbose && workPermitItemIdMap.Count > 0)
            Console.WriteLine($"INF Existing WorkPermitItem id-map entries: {workPermitItemIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedMissingRequired = 0;
        int skippedAlreadyImported = 0;
        int positionResolvedViaFallback = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (workPermitItemIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in WorkPermitItem id-map");
                continue;
            }

            if (!TryResolveRequiredIds(
                    row,
                    personIdMap,
                    passportIdMap,
                    positionResolver,
                    workPermitIdMap,
                    out var personId,
                    out var passportId,
                    out var positionHistoryId,
                    out var workPermitId,
                    out var missingReason,
                    out var usedPositionFallback))
            {
                skippedMissingRequired++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: {missingReason}");
                continue;
            }

            if (usedPositionFallback)
                positionResolvedViaFallback++;

            try
            {
                var payload = BuildPayload(row, personId, passportId, positionHistoryId, workPermitId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.WorkPermitItem), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                workPermitItemIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedMissingRequired} missing id-map...");
                if (verbose)
                    Console.WriteLine($"  SAVE WorkPermitItem {createdId.Value} <- legacy WorkPermit {legacyOid}");
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
        if (workPermitItemIdMap.Count > 0 && !string.IsNullOrWhiteSpace(workPermitItemIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(workPermitItemIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = workPermitItemIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014WorkPermitItemImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedMissingRequiredIdMap = skippedMissingRequired,
            SkippedAlreadyImported = skippedAlreadyImported,
            PositionResolvedViaFallback = positionResolvedViaFallback,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static int CountMissingRequiredIdMap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        Visa2014WorkPermitItemPositionResolver positionResolver,
        IReadOnlyDictionary<Guid, Guid> workPermitIdMap,
        out int positionFallbackCount)
    {
        int missing = 0;
        positionFallbackCount = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveRequiredIds(
                    row,
                    personIdMap,
                    passportIdMap,
                    positionResolver,
                    workPermitIdMap,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var usedFallback))
                missing++;
            else if (usedFallback)
                positionFallbackCount++;
        }

        return missing;
    }

    private static bool TryResolveRequiredIds(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        Visa2014WorkPermitItemPositionResolver positionResolver,
        IReadOnlyDictionary<Guid, Guid> workPermitIdMap,
        out Guid personId,
        out Guid passportId,
        out Guid positionHistoryId,
        out Guid workPermitId,
        out string missingReason,
        out bool usedPositionFallback)
    {
        personId = Guid.Empty;
        passportId = Guid.Empty;
        positionHistoryId = Guid.Empty;
        workPermitId = Guid.Empty;
        missingReason = "";
        usedPositionFallback = false;

        if (!TryResolveLegacyGuid(row, "Person", out var legacyPersonOid) ||
            !personIdMap.TryGetValue(legacyPersonOid, out personId))
        {
            missingReason = "Person not in id-map";
            return false;
        }

        if (!TryResolveLegacyGuid(row, "Passport", out var legacyPassportOid) ||
            !passportIdMap.TryGetValue(legacyPassportOid, out passportId))
        {
            missingReason = "Passport not in id-map";
            return false;
        }

        if (!TryResolveLegacyGuid(row, "CurrentPositionHistory", out var legacyPositionOid))
        {
            missingReason = "missing legacy CurrentPositionHistory OID";
            return false;
        }

        DateTime? permitStartDate = TryParseDate(row.GetValueOrDefault("StartDate") as string, out var start)
            ? start
            : null;

        if (!positionResolver.TryResolvePositionHistoryId(
                legacyPositionOid,
                legacyPersonOid,
                permitStartDate,
                out positionHistoryId,
                out var resolutionNote))
        {
            missingReason = string.IsNullOrEmpty(resolutionNote)
                ? "EmployeePositionHistory not in id-map"
                : resolutionNote;
            return false;
        }

        usedPositionFallback = !string.IsNullOrEmpty(resolutionNote);

        if (!TryResolveLegacyGuid(row, "WorkPermit", out var legacyWorkPermitOid) ||
            !workPermitIdMap.TryGetValue(legacyWorkPermitOid, out workPermitId))
        {
            missingReason = "WorkPermit not in id-map";
            return false;
        }

        return true;
    }

    private static bool TryResolveLegacyGuid(Dictionary<string, object?> row, string field, out Guid legacyOid)
    {
        legacyOid = Guid.Empty;
        var text = row.GetValueOrDefault(field) as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyOid);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Guid personId,
        Guid passportId,
        Guid positionHistoryId,
        Guid workPermitId)
    {
        if (row["WorkPermitNumber"] is not string workPermitNumber || string.IsNullOrWhiteSpace(workPermitNumber))
            return null;
        if (row["ASNumber"] is not string asNumber || string.IsNullOrWhiteSpace(asNumber))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out var startDate))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("ExpirationDate") as string, out var expirationDate))
            return null;

        var workPermittedLocations = row.GetValueOrDefault("WorkPermittedLocations") as string ?? "";

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["Passport"] = new { ID = passportId },
            ["CurrentPositionHistory"] = new { ID = positionHistoryId },
            ["WorkPermit"] = new { ID = workPermitId },
            ["WorkPermitNumber"] = workPermitNumber.Trim(),
            ["ASNumber"] = asNumber.Trim(),
            ["StartDate"] = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            ["ExpirationDate"] = DateTime.SpecifyKind(expirationDate, DateTimeKind.Utc),
            ["WorkPermittedLocations"] = workPermittedLocations.Trim(),
        };
    }

    private static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParse(text, out date);

    private static string DescribePayloadGap(Dictionary<string, object?> row)
    {
        var gaps = new List<string>();
        if (string.IsNullOrWhiteSpace(row.GetValueOrDefault("WorkPermitNumber") as string))
            gaps.Add("WorkPermitNumber");
        if (string.IsNullOrWhiteSpace(row.GetValueOrDefault("ASNumber") as string))
            gaps.Add("ASNumber");
        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out _))
            gaps.Add($"StartDate={row.GetValueOrDefault("StartDate")}");
        if (!TryParseDate(row.GetValueOrDefault("ExpirationDate") as string, out _))
            gaps.Add($"ExpirationDate={row.GetValueOrDefault("ExpirationDate")}");
        if (row.GetValueOrDefault("WorkPermittedLocations") is not string)
            gaps.Add("WorkPermittedLocations");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }

    private static Dictionary<string, object?>? BuildPayloadWithoutParents(Dictionary<string, object?> row)
    {
        if (row["WorkPermitNumber"] is not string workPermitNumber || string.IsNullOrWhiteSpace(workPermitNumber))
            return null;
        if (row["ASNumber"] is not string asNumber || string.IsNullOrWhiteSpace(asNumber))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out var startDate))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("ExpirationDate") as string, out var expirationDate))
            return null;

        var workPermittedLocations = row.GetValueOrDefault("WorkPermittedLocations") as string ?? "";

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["WorkPermitNumber"] = workPermitNumber.Trim(),
            ["ASNumber"] = asNumber.Trim(),
            ["StartDate"] = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            ["ExpirationDate"] = DateTime.SpecifyKind(expirationDate, DateTimeKind.Utc),
            ["WorkPermittedLocations"] = workPermittedLocations.Trim(),
        };
    }
}
