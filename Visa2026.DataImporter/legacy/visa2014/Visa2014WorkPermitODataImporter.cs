using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014WorkPermitImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedAlreadyImported { get; init; }
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

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (workPermitIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in WorkPermit id-map");
                continue;
            }

            try
            {
                var payload = BuildPayload(row);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.WorkPermit), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                workPermitIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed...");
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
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static Dictionary<string, object?>? BuildPayload(Dictionary<string, object?> row)
    {
        if (row["WorkPermitNumber"] is not string workPermitNumber || string.IsNullOrWhiteSpace(workPermitNumber))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("IssuedDate") as string, out var issuedDate))
            return null;

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["WorkPermitNumber"] = workPermitNumber.Trim(),
            ["IssuedDate"] = DateTime.SpecifyKind(issuedDate, DateTimeKind.Utc),
        };
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
