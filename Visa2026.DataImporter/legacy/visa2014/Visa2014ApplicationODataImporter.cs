using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationImportResult
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

internal static class Visa2014ApplicationODataImporter
{
    private const string BorderZoneNoneLabel = "Ýok";

    public static async Task<Visa2014ApplicationImportResult> RunAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string? applicationIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged).");
            return new Visa2014ApplicationImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
            };
        }

        var applicationIdMap = LoadOptionalApplicationIdMap(applicationIdMapOutputPath);
        if (verbose && applicationIdMap.Count > 0)
            Console.WriteLine($"INF Existing Application id-map entries: {applicationIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (applicationIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in Application id-map");
                continue;
            }

            try
            {
                var payload = BuildPayload(row, resolver);
                if (payload == null)
                {
                    failed++;
                    var detail = DescribePayloadGap(row, resolver);
                    errors.Add($"{legacyOid}: incomplete OData payload ({detail})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.Application), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                applicationIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                {
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedAlreadyImported} already imported...");
                    if (!string.IsNullOrWhiteSpace(applicationIdMapOutputPath))
                        await Visa2014IdMapHelper.SaveAsync(applicationIdMapOutputPath, applicationIdMap);
                }
                if (verbose)
                    Console.WriteLine($"  SAVE Application {createdId.Value} <- legacy {legacyOid} ({row.GetValueOrDefault("FullApplicationNumber")})");
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
        if (applicationIdMap.Count > 0 && !string.IsNullOrWhiteSpace(applicationIdMapOutputPath))
        {
            await Visa2014IdMapHelper.SaveAsync(applicationIdMapOutputPath, applicationIdMap);
            idMapPath = Path.GetFullPath(applicationIdMapOutputPath);
        }

        return new Visa2014ApplicationImportResult
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

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver)
    {
        var fullNumber = row.GetValueOrDefault("FullApplicationNumber") as string;
        if (string.IsNullOrWhiteSpace(fullNumber))
            return null;

        if (!TryParseDate(row.GetValueOrDefault("ApplicationDate") as string, out var applicationDate))
            return null;

        if (!TryReadInt(row, "Year", out var year) || !TryReadInt(row, "Month", out var month))
            return null;

        var applicationTypeId = resolver.ResolveApplicationType(row.GetValueOrDefault("ApplicationType") as string);
        if (!applicationTypeId.HasValue)
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["IsManualEntry"] = true,
            ["SuppressInitialProgress"] = true,
            ["FullApplicationNumber"] = fullNumber.Trim(),
            ["ApplicationDate"] = DateTime.SpecifyKind(applicationDate, DateTimeKind.Utc),
            ["Year"] = year,
            ["Month"] = month,
            ["ApplicationType"] = new { ID = applicationTypeId.Value },
        };

        var applicationNumber = row.GetValueOrDefault("ApplicationNumber") as string;
        if (!string.IsNullOrWhiteSpace(applicationNumber))
            payload["ApplicationNumber"] = applicationNumber.Trim();

        var appNumberPrefix = row.GetValueOrDefault("AppNumberPrefix") as string;
        if (!string.IsNullOrWhiteSpace(appNumberPrefix))
            payload["AppNumberPrefix"] = appNumberPrefix.Trim();

        TryAddOptionalFk(payload, row, "MigrationService", resolver.ResolveMigrationService);
        TryAddOptionalFk(payload, row, "Urgency", resolver.ResolveUrgency);
        TryAddOptionalFk(payload, row, "VisaPeriod", resolver.ResolveVisaPeriod);
        TryAddOptionalFk(payload, row, "VisaCategory", value => resolver.ResolveVisaCategory(value));
        TryAddOptionalFk(payload, row, "ProjectContract", resolver.ResolveProjectContract);
        TryAddOptionalFk(payload, row, "ApprovalLegProfile", resolver.ResolveApprovalLegProfile);
        TryAddOptionalFk(payload, row, "ToCity", value => resolver.ResolveCity(value));
        TryAddOptionalFk(payload, row, "MovementPermitLocation", resolver.ResolveMovementPermitLocation);

        var borderZoneLabels = row.GetValueOrDefault("BorderZoneLocation") as string;
        if (!string.IsNullOrWhiteSpace(borderZoneLabels))
            payload["BorderZoneLocation"] = borderZoneLabels.Trim();

        if (TryParseDate(row.GetValueOrDefault("BusinessTripStartDate") as string, out var tripStart))
            payload["BusinessTripStartDate"] = DateTime.SpecifyKind(tripStart, DateTimeKind.Utc);

        if (TryParseDate(row.GetValueOrDefault("BusinessTripEndDate") as string, out var tripEnd))
            payload["BusinessTripEndDate"] = DateTime.SpecifyKind(tripEnd, DateTimeKind.Utc);

        return payload;
    }

    private static void TryAddOptionalFk(
        Dictionary<string, object?> payload,
        Dictionary<string, object?> row,
        string fieldName,
        Func<string?, Guid?> resolve)
    {
        var value = row.GetValueOrDefault(fieldName) as string;
        if (string.IsNullOrWhiteSpace(value))
            return;

        var id = resolve(value.Trim());
        if (id.HasValue)
            payload[fieldName] = new { ID = id.Value };
    }

    private static bool IsBorderZoneNoneLabel(string labels)
    {
        foreach (var part in labels.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!IsBorderZoneNoneLabelPart(part))
                return false;
        }

        return true;
    }

    private static bool IsBorderZoneNoneLabelPart(string label) =>
        Visa2014CatalogMatchHelper.KeysEqual(label, BorderZoneNoneLabel)
        || string.Equals(label.Trim(), BorderZoneNoneLabel, StringComparison.Ordinal);

    private static bool TryReadInt(Dictionary<string, object?> row, string key, out int value)
    {
        value = 0;
        if (!row.TryGetValue(key, out var raw) || raw == null)
            return false;

        switch (raw)
        {
            case int i:
                value = i;
                return true;
            case long l:
                value = (int)l;
                return true;
            case string s when int.TryParse(s, out var parsed):
                value = parsed;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParse(text, out date);

    private static string DescribePayloadGap(Dictionary<string, object?> row, Visa2014ODataLookupResolver resolver)
    {
        var gaps = new List<string>();
        if (string.IsNullOrWhiteSpace(row.GetValueOrDefault("FullApplicationNumber") as string))
            gaps.Add("FullApplicationNumber");
        if (!TryParseDate(row.GetValueOrDefault("ApplicationDate") as string, out _))
            gaps.Add($"ApplicationDate={row.GetValueOrDefault("ApplicationDate")}");
        if (!TryReadInt(row, "Year", out _))
            gaps.Add($"Year={row.GetValueOrDefault("Year")}");
        if (!TryReadInt(row, "Month", out _))
            gaps.Add($"Month={row.GetValueOrDefault("Month")}");
        if (!resolver.ResolveApplicationType(row.GetValueOrDefault("ApplicationType") as string).HasValue)
            gaps.Add($"ApplicationType={row.GetValueOrDefault("ApplicationType")}");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "lookup or required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalApplicationIdMap(string? path)
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
        Visa2014SyncContext sync,
        int? maxRows,
        bool verbose)
    {
        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        return await Visa2014SyncUpsertHelper.RunAsync(
            target,
            typeof(Visa2026.Module.BusinessObjects.Application),
            "Application",
            batch.ImportRows,
            sync,
            row => BuildPayload(row, resolver),
            batch.LegacyRowCount,
            batch.Skipped.Count,
            batch.DedupeMergedCount,
            verbose);
    }
}
