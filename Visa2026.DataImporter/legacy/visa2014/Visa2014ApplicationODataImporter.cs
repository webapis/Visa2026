using System.Collections.Concurrent;
using DevExpress.ExpressApp;
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
        bool verbose,
        INonSecuredObjectSpaceFactory? objectSpaceFactory = null,
        int parallelism = 0,
        int batchSize = 50)
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

        var applicationIdMap = new ConcurrentDictionary<Guid, Guid>(
            LoadOptionalApplicationProfileInstanceIdMap(applicationIdMapOutputPath));
        if (verbose && applicationIdMap.Count > 0)
            Console.WriteLine($"INF Existing ApplicationProfileInstance id-map entries: {applicationIdMap.Count}");

        var degree = parallelism > 0 ? parallelism : Visa2014ParallelImportPoster.DefaultDegree;
        var stats = await Visa2014ParallelImportPoster.PostAsync(
            batch.ImportRows,
            degree,
            target,
            objectSpaceFactory,
            batchSize,
            async (row, workerTarget) =>
            {
                var legacyOid = (Guid)row["_legacyRowId"]!;
                if (applicationIdMap.ContainsKey(legacyOid))
                {
                    if (verbose)
                        Console.WriteLine($"  SKIP {legacyOid}: already in ApplicationProfileInstance id-map");
                    return new ParallelRowOutcome(ParallelRowKind.SkippedAlready);
                }

                try
                {
                    var payload = BuildPayload(row, resolver);
                    if (payload == null)
                    {
                        var detail = DescribePayloadGap(row, resolver);
                        return new ParallelRowOutcome(
                            ParallelRowKind.Failed,
                            $"{legacyOid}: incomplete OData payload ({detail})");
                    }

                    var createdId = await workerTarget.CreateAsync(
                        typeof(Visa2026.Module.BusinessObjects.ApplicationProfileInstance), payload);
                    if (!createdId.HasValue)
                        return new ParallelRowOutcome(ParallelRowKind.Failed, $"{legacyOid}: create returned null");

                    applicationIdMap[legacyOid] = createdId.Value;
                    if (verbose)
                        Console.WriteLine($"  SAVE ApplicationProfileInstance {createdId.Value} <- legacy {legacyOid} ({row.GetValueOrDefault("FullApplicationNumber")})");
                    return new ParallelRowOutcome(ParallelRowKind.Posted);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
                    return new ParallelRowOutcome(ParallelRowKind.Failed, $"{legacyOid}: {ex.Message}");
                }
            },
            "Application",
            applicationIdMapOutputPath);

        string? idMapPath = null;
        if (applicationIdMap.Count > 0 && !string.IsNullOrWhiteSpace(applicationIdMapOutputPath))
        {
            await Visa2014IdMapHelper.SaveAsync(
                applicationIdMapOutputPath,
                new Dictionary<Guid, Guid>(applicationIdMap));
            idMapPath = Path.GetFullPath(applicationIdMapOutputPath);
        }

        return new Visa2014ApplicationImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedAlreadyImported = stats.SkippedAlready,
            PostedCount = stats.Posted,
            FailedCount = stats.Failed,
            IdMapPath = idMapPath,
            Errors = stats.Errors,
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

        var applicationProfileId = resolver.ResolveApplicationProfile(
            row.GetValueOrDefault("ApplicationType") as string,
            row.GetValueOrDefault("ProjectContract") as string);
        if (applicationProfileId.HasValue)
            payload["ApplicationProfile"] = new { ID = applicationProfileId.Value };

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
        // Prefer explicit transform inference; fall back if older preview rows omit VisaType.
        var visaTypeKey = row.GetValueOrDefault("VisaType") as string;
        if (string.IsNullOrWhiteSpace(visaTypeKey))
            visaTypeKey = Visa2014ApplicationVisaTypeInference.TryGetVisaTypeLocalizationKey(
                row.GetValueOrDefault("ApplicationType") as string);
        if (!string.IsNullOrWhiteSpace(visaTypeKey))
        {
            var visaTypeId = resolver.ResolveVisaType(visaTypeKey.Trim());
            if (visaTypeId.HasValue)
                payload["VisaType"] = new { ID = visaTypeId.Value };
        }

        TryAddOptionalFk(payload, row, "ProjectContract", resolver.ResolveProjectContract);
        TryAddOptionalFk(payload, row, "ApprovalLegProfile", resolver.ResolveApprovalLegProfile);
        TryAddOptionalFk(payload, row, "ToCity", value => resolver.ResolveCity(value));

        var movementPermitLabels = row.GetValueOrDefault("MovementPermitLocation") as string;
        if (!string.IsNullOrWhiteSpace(movementPermitLabels))
            payload["MovementPermitLocation"] = movementPermitLabels.Trim();

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

    private static Dictionary<Guid, Guid> LoadOptionalApplicationProfileInstanceIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
