using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014RejectionImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int SkippedMissingApplicationIdMap { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014RejectionODataImporter
{
    public static async Task<Visa2014RejectionImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        string? rejectionIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var batch = Visa2014RejectionTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missingApp = CountMissingApplicationIdMap(batch.ImportRows, applicationIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {missingApp} missing Application id-map, {batch.DedupeMergedCount} dedupe groups).");
            return new Visa2014RejectionImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedMissingApplicationIdMap = missingApp,
            };
        }

        var rejectionIdMap = LoadOptionalIdMap(rejectionIdMapOutputPath);
        if (verbose && rejectionIdMap.Count > 0)
            Console.WriteLine($"INF Existing Rejection id-map entries: {rejectionIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedAlreadyImported = 0;
        int skippedMissingApplicationIdMap = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (rejectionIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in Rejection id-map");
                continue;
            }

            if (!TryResolveApplication(row, applicationIdMap, out var applicationId))
            {
                skippedMissingApplicationIdMap++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: Application not in id-map");
                continue;
            }

            try
            {
                var payload = BuildPayload(row, applicationId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.Rejection), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                rejectionIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 100 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed...");
                if (verbose)
                    Console.WriteLine($"  SAVE Rejection {createdId.Value} <- legacy {legacyOid} ({row["RejectedDocNumber"]})");
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
        if (rejectionIdMap.Count > 0 && !string.IsNullOrWhiteSpace(rejectionIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(rejectionIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = rejectionIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014RejectionImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedMissingApplicationIdMap = skippedMissingApplicationIdMap,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static int CountMissingApplicationIdMap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap)
    {
        int missing = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveApplication(row, applicationIdMap, out _))
                missing++;
        }

        return missing;
    }

    private static bool TryResolveApplication(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        out Guid applicationId)
    {
        applicationId = Guid.Empty;
        if (!TryResolveLegacyGuid(row, "Application", out var legacyApplicationOid))
            return false;
        return applicationIdMap.TryGetValue(legacyApplicationOid, out applicationId);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Guid applicationId)
    {
        if (row["RejectedDocNumber"] is not string rejectedDocNumber || string.IsNullOrWhiteSpace(rejectedDocNumber))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("Date") as string, out var date))
            return null;

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["RejectedDocNumber"] = rejectedDocNumber.Trim(),
            ["Date"] = DateTime.SpecifyKind(date, DateTimeKind.Utc),
            ["Application"] = new Dictionary<string, object?> { ["ID"] = applicationId },
            ["Reason"] = null,
        };
    }

    private static bool TryResolveLegacyGuid(Dictionary<string, object?> row, string field, out Guid legacyOid)
    {
        legacyOid = Guid.Empty;
        var text = row.GetValueOrDefault(field) as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyOid);
    }

    private static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParse(text, out date);

    private static string DescribePayloadGap(Dictionary<string, object?> row)
    {
        var gaps = new List<string>();
        if (string.IsNullOrWhiteSpace(row.GetValueOrDefault("RejectedDocNumber") as string))
            gaps.Add("RejectedDocNumber");
        if (!TryParseDate(row.GetValueOrDefault("Date") as string, out _))
            gaps.Add($"Date={row.GetValueOrDefault("Date")}");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
