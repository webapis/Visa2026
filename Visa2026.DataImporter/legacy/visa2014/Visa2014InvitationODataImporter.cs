using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014InvitationImportResult
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

internal static class Visa2014InvitationODataImporter
{
    public static async Task<Visa2014InvitationImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        INonSecuredObjectSpaceFactory? objectSpaceFactory,
        string? invitationIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var batch = Visa2014InvitationTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged).");
            return new Visa2014InvitationImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
            };
        }

        if (objectSpaceFactory == null)
        {
            throw new InvalidOperationException(
                "Invitation import requires a live headless session (INonSecuredObjectSpaceFactory) for ValidityDuration resolution — use --inprocess.");
        }

        var invitationIdMap = LoadOptionalIdMap(invitationIdMapOutputPath);
        if (verbose && invitationIdMap.Count > 0)
            Console.WriteLine($"INF Existing Invitation id-map entries: {invitationIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedAlreadyImported = 0;
        int skippedMissingApplicationIdMap = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (invitationIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in Invitation id-map");
                continue;
            }

            try
            {
                var payload = BuildPayload(row, applicationIdMap, objectSpaceFactory, out var missingApplication);
                if (missingApplication)
                    skippedMissingApplicationIdMap++;

                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.Invitation), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                invitationIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed...");
                if (verbose)
                    Console.WriteLine($"  SAVE Invitation {createdId.Value} <- legacy {legacyOid} ({row["InvitationNumber"]})");
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
        if (invitationIdMap.Count > 0 && !string.IsNullOrWhiteSpace(invitationIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(invitationIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = invitationIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014InvitationImportResult
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

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<Guid, Guid> applicationIdMap,
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        out bool missingApplication)
    {
        missingApplication = false;
        if (row["InvitationNumber"] is not string invitationNumber || string.IsNullOrWhiteSpace(invitationNumber))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out var startDate))
            return null;
        if (!TryParseDate(row.GetValueOrDefault("DateOfExpire") as string, out var dateOfExpire))
            return null;

        var validityDurationId = Visa2014ValidityDurationHelper.ResolveClosestValidityDurationId(
            objectSpaceFactory,
            startDate,
            dateOfExpire);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["InvitationNumber"] = invitationNumber.Trim(),
            ["StartDate"] = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            ["ValidityDuration"] = new Dictionary<string, object?> { ["ID"] = validityDurationId },
        };

        if (TryResolveLegacyGuid(row, "Application", out var legacyApplicationOid))
        {
            if (applicationIdMap.TryGetValue(legacyApplicationOid, out var applicationId))
                payload["Application"] = new Dictionary<string, object?> { ["ID"] = applicationId };
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
        if (string.IsNullOrWhiteSpace(row.GetValueOrDefault("InvitationNumber") as string))
            gaps.Add("InvitationNumber");
        if (!TryParseDate(row.GetValueOrDefault("StartDate") as string, out _))
            gaps.Add($"StartDate={row.GetValueOrDefault("StartDate")}");
        if (!TryParseDate(row.GetValueOrDefault("DateOfExpire") as string, out _))
            gaps.Add($"DateOfExpire={row.GetValueOrDefault("DateOfExpire")}");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
