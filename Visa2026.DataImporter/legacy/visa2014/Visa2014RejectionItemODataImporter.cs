using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014RejectionItemImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int SkippedMissingRequiredIdMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014RejectionItemODataImporter
{
    public static async Task<Visa2014RejectionItemImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string passportIdMapPath,
        string rejectionIdMapPath,
        string? rejectionItemIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var passportIdMap = Visa2014IdMapHelper.Load(passportIdMapPath);
        var rejectionIdMap = Visa2014IdMapHelper.Load(rejectionIdMapPath);

        if (verbose)
        {
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");
            Console.WriteLine($"INF Passport id-map entries: {passportIdMap.Count}");
            Console.WriteLine($"INF Rejection id-map entries: {rejectionIdMap.Count}");
        }

        var batch = Visa2014RejectionItemTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missing = CountMissingRequiredIdMap(batch.ImportRows, personIdMap, passportIdMap, rejectionIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {missing} missing required id-map).");
            return new Visa2014RejectionItemImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                SkippedMissingRequiredIdMap = missing,
            };
        }

        var rejectionItemIdMap = LoadOptionalIdMap(rejectionItemIdMapOutputPath);
        if (verbose && rejectionItemIdMap.Count > 0)
            Console.WriteLine($"INF Existing RejectionItem id-map entries: {rejectionItemIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedMissingRequired = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (rejectionItemIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in RejectionItem id-map");
                continue;
            }

            if (!TryResolveRequiredIds(
                    row,
                    personIdMap,
                    passportIdMap,
                    rejectionIdMap,
                    out var personId,
                    out var passportId,
                    out var rejectionId,
                    out var missingReason))
            {
                skippedMissingRequired++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: {missingReason}");
                continue;
            }

            try
            {
                var payload = BuildPayload(personId, passportId, rejectionId);
                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.RejectionItem), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                rejectionItemIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 100 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedMissingRequired} missing id-map...");
                if (verbose)
                    Console.WriteLine($"  SAVE RejectionItem {createdId.Value} <- legacy PersonInInvitation {legacyOid}");
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
        if (rejectionItemIdMap.Count > 0 && !string.IsNullOrWhiteSpace(rejectionItemIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(rejectionItemIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = rejectionItemIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014RejectionItemImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedMissingRequiredIdMap = skippedMissingRequired,
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
        IReadOnlyDictionary<Guid, Guid> rejectionIdMap)
    {
        int missing = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveRequiredIds(
                    row,
                    personIdMap,
                    passportIdMap,
                    rejectionIdMap,
                    out _,
                    out _,
                    out _,
                    out _))
                missing++;
        }

        return missing;
    }

    private static bool TryResolveRequiredIds(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> passportIdMap,
        IReadOnlyDictionary<Guid, Guid> rejectionIdMap,
        out Guid personId,
        out Guid passportId,
        out Guid rejectionId,
        out string missingReason)
    {
        personId = Guid.Empty;
        passportId = Guid.Empty;
        rejectionId = Guid.Empty;
        missingReason = "";

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

        if (!TryResolveLegacyGuid(row, "Rejection", out var legacyRejectionOid) ||
            !rejectionIdMap.TryGetValue(legacyRejectionOid, out rejectionId))
        {
            missingReason = "Rejection not in id-map";
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

    private static Dictionary<string, object?> BuildPayload(
        Guid personId,
        Guid passportId,
        Guid rejectionId) =>
        new(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["Passport"] = new { ID = passportId },
            ["Rejection"] = new { ID = rejectionId },
            ["Reason"] = null,
        };

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
