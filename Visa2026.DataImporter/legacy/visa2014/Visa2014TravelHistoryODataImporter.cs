using System.Text.Json;
using Visa2026.DataImporter;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014TravelHistoryImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedNoPersonMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014TravelHistoryODataImporter
{
    public static async Task<Visa2014TravelHistoryImportResult> RunAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string? travelHistoryIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");

        var batch = Visa2014TravelHistoryTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        if (dryRun)
        {
            int missingPerson = CountMissingPersonMap(batch.ImportRows, personIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged, {missingPerson} missing Person id-map).");
            return new Visa2014TravelHistoryImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedNoPersonMap = missingPerson,
            };
        }

        var travelHistoryIdMap = LoadOptionalIdMap(travelHistoryIdMapOutputPath);
        if (verbose && travelHistoryIdMap.Count > 0)
            Console.WriteLine($"INF Existing TravelHistory id-map entries: {travelHistoryIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedNoPerson = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (travelHistoryIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in TravelHistory id-map");
                continue;
            }

            if (!TryResolveLegacyPersonOid(row, out var legacyPersonOid))
            {
                failed++;
                errors.Add($"{legacyOid}: missing legacy Person Oid on row");
                continue;
            }

            if (!personIdMap.TryGetValue(legacyPersonOid, out var personId))
            {
                skippedNoPerson++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: Person {legacyPersonOid} not in id-map");
                continue;
            }

            try
            {
                var concreteType = Visa2014TravelHistoryTransform.ResolveConcreteType(
                    row.GetValueOrDefault("TravelType") as string,
                    row.GetValueOrDefault("MovementType") as string);
                if (concreteType == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: unknown TravelHistory subtype");
                    continue;
                }

                var payload = BuildPayload(row, resolver, personId, concreteType);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete payload ({DescribePayloadGap(row, resolver)})");
                    continue;
                }

                var createdId = await target.CreateAsync(concreteType, payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                travelHistoryIdMap[legacyOid] = createdId.Value;
                if (Guid.TryParse(row.GetValueOrDefault("_legacyTravelInformationOid") as string, out var tiOid)
                    && tiOid != Guid.Empty
                    && !travelHistoryIdMap.ContainsKey(tiOid))
                    travelHistoryIdMap[tiOid] = createdId.Value;

                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoPerson} no person map...");
                if (verbose)
                    Console.WriteLine($"  SAVE {concreteType.Name} {createdId.Value} <- PIA {legacyOid}");
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
        if (travelHistoryIdMap.Count > 0 && !string.IsNullOrWhiteSpace(travelHistoryIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(travelHistoryIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = travelHistoryIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014TravelHistoryImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedNoPersonMap = skippedNoPerson,
            SkippedAlreadyImported = skippedAlreadyImported,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static int CountMissingPersonMap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> personIdMap)
    {
        int missing = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveLegacyPersonOid(row, out var legacyPersonOid) || !personIdMap.ContainsKey(legacyPersonOid))
                missing++;
        }

        return missing;
    }

    private static bool TryResolveLegacyPersonOid(Dictionary<string, object?> row, out Guid legacyPersonOid)
    {
        legacyPersonOid = Guid.Empty;
        var text = row.GetValueOrDefault("Person") as string
            ?? row.GetValueOrDefault("_legacy_PersonOid") as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyPersonOid);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        Guid personId,
        Type concreteType)
    {
        if (!DateTime.TryParse(row.GetValueOrDefault("TravelDate") as string, out var travelDate))
            return null;

        var isExternal = concreteType == typeof(ExternalArrival) || concreteType == typeof(ExternalDeparture);
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["@odata.type"] = $"#Visa2026.Module.BusinessObjects.{concreteType.Name}",
            ["Person"] = new { ID = personId },
            ["TravelDate"] = DateTime.SpecifyKind(travelDate.Date, DateTimeKind.Unspecified),
            ["TravelType"] = isExternal ? "External" : "Internal",
            ["MovementType"] = concreteType == typeof(ExternalArrival) || concreteType == typeof(InternalArrival)
                ? "Entry"
                : "Exit",
        };

        if (row.GetValueOrDefault("Notes") is string notes && !string.IsNullOrWhiteSpace(notes))
            payload["Notes"] = notes.Trim();

        if (isExternal)
        {
            var checkPointId = resolver.ResolveCheckPoint(row.GetValueOrDefault("CheckPoint") as string)
                ?? resolver.ResolveDefaultCheckPoint();
            var countryId = resolver.ResolveDefaultCountry();
            if (!checkPointId.HasValue || !countryId.HasValue)
                return null;
            payload["CheckPoint"] = new { ID = checkPointId.Value };
            payload["Country"] = new { ID = countryId.Value };
        }
        else
        {
            var regionId = resolver.ResolveDefaultRegion();
            var cityId = resolver.ResolveDefaultCity(regionId);
            if (!regionId.HasValue || !cityId.HasValue)
                return null;
            payload["Region"] = new { ID = regionId.Value };
            payload["City"] = new { ID = cityId.Value };
        }

        return payload;
    }

    private static string DescribePayloadGap(Dictionary<string, object?> row, Visa2014ODataLookupResolver resolver)
    {
        var gaps = new List<string>();
        if (!DateTime.TryParse(row.GetValueOrDefault("TravelDate") as string, out _))
            gaps.Add($"TravelDate={row.GetValueOrDefault("TravelDate")}");
        var checkPoint = row.GetValueOrDefault("CheckPoint") as string;
        if (!string.IsNullOrWhiteSpace(checkPoint) && !resolver.ResolveCheckPoint(checkPoint).HasValue)
            gaps.Add($"CheckPoint={checkPoint}");
        if (!resolver.ResolveDefaultCountry().HasValue)
            gaps.Add("Country(default)");
        return gaps.Count > 0 ? string.Join("; ", gaps) : "lookup or required field";
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
