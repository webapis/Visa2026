using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationProgressImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int SkippedNoApplicationMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int SeedsRemovedBeforeImport { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyDictionary<string, Guid> ProgressIdMapUpdates { get; init; }
        = new Dictionary<string, Guid>(StringComparer.Ordinal);
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014ApplicationProgressODataImporter
{
    public static async Task<Visa2014ApplicationProgressImportResult> RunAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        ApiClient? seedCleanupApi,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string applicationIdMapPath,
        string? progressIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var applicationIdMap = Visa2014IdMapHelper.Load(applicationIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Application id-map entries: {applicationIdMap.Count}");

        var batch = Visa2014ApplicationProgressTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missingApp = CountMissingApplicationMap(batch.ImportRows, applicationIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} parent-skipped, {missingApp} missing Application id-map).");
            return new Visa2014ApplicationProgressImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                SkippedNoApplicationMap = missingApp,
            };
        }

        int seedsRemoved = 0;
        if (seedCleanupApi != null)
        {
            var seedCleanup = await Visa2014ApplicationProgressSeedCleanup.RunAsync(
                seedCleanupApi,
                applicationIdMap.Values.ToHashSet(),
                dryRun: false,
                verbose);
            seedsRemoved = seedCleanup.Deleted;
            if (seedsRemoved > 0)
                Console.WriteLine($"INF Removed {seedsRemoved} initializer seed row(s) before progress import.");
        }
        else if (verbose)
        {
            Console.WriteLine("INF Headless import: skipping initializer seed cleanup (Applications imported with SuppressInitialProgress).");
        }

        var progressIdMap = LoadOptionalProgressIdMap(progressIdMapOutputPath);
        if (verbose && progressIdMap.Count > 0)
            Console.WriteLine($"INF Existing ApplicationProgress id-map entries: {progressIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedNoApp = 0;
        int skippedAlready = 0;

        foreach (var row in batch.ImportRows)
        {
            var syntheticKey = row.GetValueOrDefault("_syntheticStepKey") as string
                ?? row.GetValueOrDefault("_legacyRowId") as string;
            if (string.IsNullOrWhiteSpace(syntheticKey))
            {
                failed++;
                errors.Add("row: missing _syntheticStepKey");
                continue;
            }

            if (progressIdMap.ContainsKey(syntheticKey))
            {
                skippedAlready++;
                continue;
            }

            if (!TryResolveLegacyApplicationOid(row, out var legacyApplicationOid))
            {
                failed++;
                errors.Add($"{syntheticKey}: missing legacy Application Oid");
                continue;
            }

            if (!applicationIdMap.TryGetValue(legacyApplicationOid, out var applicationId))
            {
                skippedNoApp++;
                if (verbose)
                    Console.WriteLine($"  SKIP {syntheticKey}: Application {legacyApplicationOid} not in id-map");
                continue;
            }

            try
            {
                var payload = BuildPayload(row, resolver, applicationId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{syntheticKey}: incomplete OData payload ({DescribePayloadGap(row, resolver)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.ApplicationProgress), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{syntheticKey}: create returned null");
                    continue;
                }

                progressIdMap[syntheticKey] = createdId.Value;
                posted++;
                if (posted % 500 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoApp} no app map...");
                if (verbose)
                    Console.WriteLine($"  SAVE ApplicationProgress {createdId.Value} <- {syntheticKey}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{syntheticKey}: {ex.Message}");
                Console.Error.WriteLine($"ERR {syntheticKey}: {ex.Message}");
            }
        }

        await target.FlushAsync();

        string? idMapPath = null;
        if (progressIdMap.Count > 0 && !string.IsNullOrWhiteSpace(progressIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(progressIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(progressIdMap, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014ApplicationProgressImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            SkippedNoApplicationMap = skippedNoApp,
            SkippedAlreadyImported = skippedAlready,
            SeedsRemovedBeforeImport = seedsRemoved,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            ProgressIdMapUpdates = progressIdMap,
            Errors = errors,
        };
    }

    /// <summary>Re-import synthesized progress for a subset of applications (after route correction).</summary>
    internal static async Task<Visa2014ApplicationProgressImportResult> RegenerateForLegacyApplicationsAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> legacyApplicationIdToTargetId,
        bool dryRun,
        bool verbose)
    {
        var targetIds = legacyApplicationIdToTargetId.Values.ToHashSet();
        var batch = Visa2014ApplicationProgressTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows: null,
            verbose);

        var errors = new List<string>();
        var progressIdMap = new Dictionary<string, Guid>(StringComparer.Ordinal);
        int posted = 0, failed = 0, skippedNoApp = 0;

        foreach (var row in batch.ImportRows)
        {
            if (!TryResolveLegacyApplicationOid(row, out var legacyApplicationOid)
                || !legacyApplicationIdToTargetId.TryGetValue(legacyApplicationOid, out var applicationId)
                || !targetIds.Contains(applicationId))
            {
                continue;
            }

            var syntheticKey = row.GetValueOrDefault("_syntheticStepKey") as string
                ?? row.GetValueOrDefault("_legacyRowId") as string;
            if (string.IsNullOrWhiteSpace(syntheticKey))
            {
                failed++;
                errors.Add("row: missing _syntheticStepKey");
                continue;
            }

            if (dryRun)
            {
                posted++;
                continue;
            }

            try
            {
                var payload = BuildPayload(row, resolver, applicationId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{syntheticKey}: incomplete payload ({DescribePayloadGap(row, resolver)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.ApplicationProgress), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{syntheticKey}: create returned null");
                    continue;
                }

                progressIdMap[syntheticKey] = createdId.Value;
                posted++;
                if (posted % 250 == 0 && verbose)
                    Console.WriteLine($"INF Progress regen: {posted} posted, {failed} failed...");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{syntheticKey}: {ex.Message}");
            }
        }

        if (!dryRun)
            await target.FlushAsync();

        return new Visa2014ApplicationProgressImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            PostedCount = posted,
            FailedCount = failed,
            SkippedNoApplicationMap = skippedNoApp,
            ProgressIdMapUpdates = progressIdMap,
            Errors = errors,
        };
    }

    private static int CountMissingApplicationMap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap)
    {
        int missing = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveLegacyApplicationOid(row, out var legacyApplicationOid))
            {
                missing++;
                continue;
            }

            if (!applicationIdMap.ContainsKey(legacyApplicationOid))
                missing++;
        }

        return missing;
    }

    private static bool TryResolveLegacyApplicationOid(Dictionary<string, object?> row, out Guid legacyApplicationOid)
    {
        legacyApplicationOid = Guid.Empty;
        var text = row.GetValueOrDefault("Application") as string
            ?? row.GetValueOrDefault("_legacyApplicationOid") as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyApplicationOid);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        Guid applicationId)
    {
        var stateCode = row.GetValueOrDefault("State") as string;
        var locationCode = row.GetValueOrDefault("Location") as string;
        if (!TryParseDate(row.GetValueOrDefault("Date") as string, out var date))
            return null;

        var stateId = resolver.ResolveApplicationState(stateCode);
        var locationId = resolver.ResolveApplicationLocation(locationCode);
        if (!stateId.HasValue || !locationId.HasValue)
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Application"] = new { ID = applicationId },
            ["State"] = new { ID = stateId.Value },
            ["Location"] = new { ID = locationId.Value },
            ["Date"] = DateTime.SpecifyKind(date, DateTimeKind.Utc),
        };

        var description = row.GetValueOrDefault("Description") as string;
        if (!string.IsNullOrWhiteSpace(description))
            payload["Description"] = description.Trim();

        return payload;
    }

    private static string DescribePayloadGap(Dictionary<string, object?> row, Visa2014ODataLookupResolver resolver)
    {
        var gaps = new List<string>();
        if (!resolver.ResolveApplicationState(row.GetValueOrDefault("State") as string).HasValue)
            gaps.Add($"State={row.GetValueOrDefault("State")}");
        if (!resolver.ResolveApplicationLocation(row.GetValueOrDefault("Location") as string).HasValue)
            gaps.Add($"Location={row.GetValueOrDefault("Location")}");
        if (!TryParseDate(row.GetValueOrDefault("Date") as string, out _))
            gaps.Add($"Date={row.GetValueOrDefault("Date")}");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "unknown";
    }

    private static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParse(text, out date);

    private static Dictionary<string, Guid> LoadOptionalProgressIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<string, Guid>(StringComparer.Ordinal);

        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (Guid.TryParse(prop.Value.GetString(), out var targetId))
                map[prop.Name] = targetId;
        }

        return map;
    }
}
