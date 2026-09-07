using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.DataImporter;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014BorderZoneImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int SkippedMissingApplicationProfileInstanceIdMap { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014BorderZoneODataImporter
{
    public static async Task<Visa2014BorderZoneImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        INonSecuredObjectSpaceFactory? objectSpaceFactory,
        string? borderZoneIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        if (objectSpaceFactory == null && !dryRun)
        {
            throw new InvalidOperationException(
                "BorderZone import requires a live headless session (INonSecuredObjectSpaceFactory) for ValidityDuration resolution — use --inprocess.");
        }

        var batch = Visa2014BorderZoneTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missingApp = CountMissingApplicationProfileInstanceIdMap(batch.ImportRows, applicationIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {missingApp} missing ApplicationProfileInstance id-map).");
            return new Visa2014BorderZoneImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                SkippedMissingApplicationProfileInstanceIdMap = missingApp,
            };
        }

        var borderZoneIdMap = LoadOptionalIdMap(borderZoneIdMapOutputPath);
        if (verbose && borderZoneIdMap.Count > 0)
            Console.WriteLine($"INF Existing BorderZone id-map entries: {borderZoneIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedAlreadyImported = 0;
        int skippedMissingApplicationProfileInstanceIdMap = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (borderZoneIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                continue;
            }

            if (!TryResolveApplication(row, applicationIdMap, out var applicationId))
            {
                skippedMissingApplicationProfileInstanceIdMap++;
                continue;
            }

            try
            {
                var payload = BuildPayload(row, applicationId, objectSpaceFactory!);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Bo.BorderZone), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                borderZoneIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 50 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed...");
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
        if (borderZoneIdMap.Count > 0 && !string.IsNullOrWhiteSpace(borderZoneIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(borderZoneIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = borderZoneIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014BorderZoneImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedMissingApplicationProfileInstanceIdMap = skippedMissingApplicationProfileInstanceIdMap,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static int CountMissingApplicationProfileInstanceIdMap(
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
        var text = row.GetValueOrDefault("ApplicationProfileInstance") as string;
        if (string.IsNullOrWhiteSpace(text) || !Guid.TryParse(text, out var legacyOid))
            return false;
        return applicationIdMap.TryGetValue(legacyOid, out applicationId);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Guid applicationId,
        INonSecuredObjectSpaceFactory objectSpaceFactory)
    {
        if (row["BorderZoneNumber"] is not string number || string.IsNullOrWhiteSpace(number))
            return null;
        if (!DateTime.TryParse(row.GetValueOrDefault("StartDate") as string, out var startDate))
            return null;
        if (!TryReadInt(row.GetValueOrDefault("ValidityDurationDays"), out var days))
            return null;

        var durationId = Visa2014ValidityDurationHelper.ResolveValidityDurationIdByDays(objectSpaceFactory, days);

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["BorderZoneNumber"] = number.Trim(),
            ["StartDate"] = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            ["ValidityDuration"] = new Dictionary<string, object?> { ["ID"] = durationId },
            ["ApplicationProfileInstance"] = new Dictionary<string, object?> { ["ID"] = applicationId },
        };
    }

    private static bool TryReadInt(object? value, out int number)
    {
        switch (value)
        {
            case int i:
                number = i;
                return true;
            case string s when int.TryParse(s, out var parsed):
                number = parsed;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}