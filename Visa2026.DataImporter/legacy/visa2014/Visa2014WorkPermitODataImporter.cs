using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.DataImporter;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014WorkPermitImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int SkippedMissingApplicationProfileInstanceIdMap { get; init; }
    public int PatchedApplicationProfileInstanceCount { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014WorkPermitODataImporter
{
    public static async Task<Visa2014WorkPermitImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        INonSecuredObjectSpaceFactory? objectSpaceFactory,
        string? workPermitIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var batch = Visa2014WorkPermitTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged).");
            return new Visa2014WorkPermitImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
            };
        }

        var workPermitIdMap = LoadOptionalIdMap(workPermitIdMapOutputPath);
        if (verbose && workPermitIdMap.Count > 0)
            Console.WriteLine($"INF Existing WorkPermit id-map entries: {workPermitIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedAlreadyImported = 0;
        int skippedMissingApplicationProfileInstanceIdMap = 0;
        int patchedApplication = 0;
        var pendingBackfill = 0;

        using var backfillSpace = objectSpaceFactory?.CreateNonSecuredObjectSpace(typeof(Bo.WorkPermit));

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (workPermitIdMap.TryGetValue(legacyOid, out var existingWorkPermitId))
            {
                skippedAlreadyImported++;
                if (backfillSpace != null
                    && TryBackfillApplicationProfileInstance(
                        backfillSpace,
                        row,
                        existingWorkPermitId,
                        applicationIdMap,
                        verbose))
                {
                    patchedApplication++;
                    pendingBackfill++;
                    if (pendingBackfill >= 50)
                    {
                        backfillSpace.CommitChanges();
                        pendingBackfill = 0;
                    }
                }

                continue;
            }

            try
            {
                var payload = BuildPayload(row, applicationIdMap, out var missingApplication);
                if (missingApplication)
                    skippedMissingApplicationProfileInstanceIdMap++;

                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Bo.WorkPermit), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                workPermitIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {patchedApplication} Application FK patched...");
                if (verbose)
                    Console.WriteLine($"  SAVE WorkPermit {createdId.Value} <- legacy {legacyOid} ({row["WorkPermitNumber"]})");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        if (backfillSpace != null && pendingBackfill > 0)
            backfillSpace.CommitChanges();

        await target.FlushAsync();

        string? idMapPath = null;
        if (workPermitIdMap.Count > 0 && !string.IsNullOrWhiteSpace(workPermitIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(workPermitIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = workPermitIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014WorkPermitImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedAlreadyImported = skippedAlreadyImported,
            SkippedMissingApplicationProfileInstanceIdMap = skippedMissingApplicationProfileInstanceIdMap,
            PatchedApplicationProfileInstanceCount = patchedApplication,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static bool TryBackfillApplicationProfileInstance(
        IObjectSpace objectSpace,
        Dictionary<string, object?> row,
        Guid workPermitId,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        bool verbose)
    {
        if (!TryResolveLegacyGuid(row, "Application", out var legacyApplicationOid))
            return false;
        if (!applicationIdMap.TryGetValue(legacyApplicationOid, out var applicationId))
            return false;

        var workPermit = objectSpace.GetObjectByKey<Bo.WorkPermit>(workPermitId);
        if (workPermit == null)
            return false;
        if (workPermit.ApplicationProfileInstance != null
            && workPermit.ApplicationProfileInstance.ID == applicationId)
            return false;

        var application = objectSpace.GetObjectByKey<Bo.ApplicationProfileInstance>(applicationId);
        if (application == null)
            return false;

        workPermit.ApplicationProfileInstance = application;
        if (verbose)
            Console.WriteLine($"  PATCH WorkPermit {workPermitId} ApplicationProfileInstance={applicationId}");
        return true;
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        out bool missingApplication)
    {
        missingApplication = false;
        if (row["WorkPermitNumber"] is not string workPermitNumber || string.IsNullOrWhiteSpace(workPermitNumber))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("IssuedDate") as string, out var issuedDate))
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["WorkPermitNumber"] = workPermitNumber.Trim(),
            ["IssuedDate"] = DateTime.SpecifyKind(issuedDate, DateTimeKind.Utc),
        };

        if (TryResolveLegacyGuid(row, "Application", out var legacyApplicationProfileInstanceOid))
        {
            if (applicationIdMap.TryGetValue(legacyApplicationProfileInstanceOid, out var applicationId))
                payload["ApplicationProfileInstance"] = new Dictionary<string, object?> { ["ID"] = applicationId };
            else
                missingApplication = true;
        }

        return payload;
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
        if (string.IsNullOrWhiteSpace(row.GetValueOrDefault("WorkPermitNumber") as string))
            gaps.Add("WorkPermitNumber");
        if (!TryParseDate(row.GetValueOrDefault("IssuedDate") as string, out _))
            gaps.Add($"IssuedDate={row.GetValueOrDefault("IssuedDate")}");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}