using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014VisaImportResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int SkippedNoPassportMap { get; init; }
    public int SkippedAlreadyImported { get; init; }
    public int PostedCount { get; init; }
    public int FailedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

internal static class Visa2014VisaODataImporter
{
    public static async Task<Visa2014VisaImportResult> RunAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string passportIdMapPath,
        string? visaIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var passportIdMap = Visa2014IdMapHelper.Load(passportIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Passport id-map entries: {passportIdMap.Count}");

        var batch = Visa2014VisaTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose);

        LogPreparedVisaTypeHistogram(batch.ImportRows);

        if (dryRun)
        {
            int missingPassport = CountMissingPassportMap(batch.ImportRows, passportIdMap);
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged, {missingPassport} missing Passport id-map).");
            return new Visa2014VisaImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedNoPassportMap = missingPassport,
            };
        }

        resolver.EnsureVisaTypeLookupKeysLoaded();

        var visaIdMap = LoadOptionalVisaIdMap(visaIdMapOutputPath);
        if (verbose && visaIdMap.Count > 0)
            Console.WriteLine($"INF Existing Visa id-map entries: {visaIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedNoPassport = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (visaIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in Visa id-map");
                continue;
            }

            if (!TryResolveLegacyPassportOid(row, out var legacyPassportOid))
            {
                failed++;
                errors.Add($"{legacyOid}: missing legacy Passport Oid on row");
                continue;
            }

            if (!passportIdMap.TryGetValue(legacyPassportOid, out var passportId))
            {
                skippedNoPassport++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: Passport {legacyPassportOid} not in id-map");
                continue;
            }

            try
            {
                var payload = BuildPayload(row, resolver, passportId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload (lookup or required field)");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.Visa), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                visaIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoPassport} no passport map...");
                if (verbose)
                    Console.WriteLine($"  SAVE Visa {createdId.Value} <- legacy {legacyOid} ({row["VisaNumber"]})");
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
        if (visaIdMap.Count > 0 && !string.IsNullOrWhiteSpace(visaIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(visaIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = visaIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014VisaImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedNoPassportMap = skippedNoPassport,
            SkippedAlreadyImported = skippedAlreadyImported,
            PostedCount = posted,
            FailedCount = failed,
            IdMapPath = idMapPath,
            Errors = errors,
        };
    }

    private static int CountMissingPassportMap(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> passportIdMap)
    {
        int missing = 0;
        foreach (var row in importRows)
        {
            if (!TryResolveLegacyPassportOid(row, out var legacyPassportOid))
            {
                missing++;
                continue;
            }

            if (!passportIdMap.ContainsKey(legacyPassportOid))
                missing++;
        }

        return missing;
    }

    private static bool TryResolveLegacyPassportOid(Dictionary<string, object?> row, out Guid legacyPassportOid)
    {
        legacyPassportOid = Guid.Empty;
        var text = row.GetValueOrDefault("Passport") as string
            ?? row.GetValueOrDefault("_legacy_PassportOid") as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyPassportOid);
    }

    private static Dictionary<string, object?>? BuildPayload(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        Guid passportId)
    {
        if (row["VisaNumber"] is not string visaNumber || string.IsNullOrWhiteSpace(visaNumber))
            return null;
        if (row["IssueDate"] is not DateTime issueDate ||
            row["StartDate"] is not DateTime startDate ||
            row["ExpirationDate"] is not DateTime expirationDate)
            return null;

        var visaTypeId = resolver.ResolveVisaType(row.GetValueOrDefault("VisaType") as string);
        var visaCategoryId = resolver.ResolveVisaCategory(row.GetValueOrDefault("VisaCategory") as string);
        var visaIssuedPlaceId = resolver.ResolveVisaIssuedPlace(row.GetValueOrDefault("VisaIssuedPlace") as string);
        if (!visaTypeId.HasValue || !visaCategoryId.HasValue || !visaIssuedPlaceId.HasValue)
            return null;

        var borderZone = row.GetValueOrDefault("BorderZoneLocation") as string;
        if (string.IsNullOrWhiteSpace(borderZone))
            borderZone = "Ýok";

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["VisaNumber"] = visaNumber,
            ["IssueDate"] = DateTime.SpecifyKind(issueDate, DateTimeKind.Utc),
            ["StartDate"] = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            ["ExpirationDate"] = DateTime.SpecifyKind(expirationDate, DateTimeKind.Utc),
            ["BorderZoneLocation"] = borderZone.Trim(),
            ["ExtensionRequired"] = row.GetValueOrDefault("ExtensionRequired") is bool ext && ext,
            ["IsCancelled"] = row.GetValueOrDefault("IsCancelled") is bool cancelled && cancelled,
            ["IsChanged"] = row.GetValueOrDefault("IsChanged") is bool changed && changed,
            ["IsExtended"] = row.GetValueOrDefault("IsExtended") is bool extended && extended,
            // ShowOptionalFields is [NotMapped] on Visa — POSTing it yields OData 400 "Incorrect body."
            ["Passport"] = new { ID = passportId },
            ["VisaType"] = new { ID = visaTypeId.Value },
            ["VisaCategory"] = new { ID = visaCategoryId.Value },
            ["VisaIssuedPlace"] = new { ID = visaIssuedPlaceId.Value },
        };

        if (row.GetValueOrDefault("ProcessNumber") is string processNumber
            && !string.IsNullOrWhiteSpace(processNumber))
            payload["ProcessNumber"] = processNumber.Trim();

        if (row.GetValueOrDefault("LegacyPersonInApplicationProfileInstanceOid") is Guid piaOid)
            payload["LegacyPersonInApplicationProfileInstanceOid"] = piaOid;
        else if (row.GetValueOrDefault("LegacyPersonInApplicationProfileInstanceOid") is string piaText
                 && Guid.TryParse(piaText.Trim(), out var parsedPia))
            payload["LegacyPersonInApplicationProfileInstanceOid"] = parsedPia;

        return payload;
    }

    private static Dictionary<Guid, Guid> LoadOptionalVisaIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }

    private static void LogPreparedVisaTypeHistogram(IReadOnlyList<Dictionary<string, object?>> importRows)
    {
        var groups = importRows
            .GroupBy(r => (r.GetValueOrDefault("VisaType") as string)?.Trim() ?? "(null)", StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var summary = string.Join(", ", groups.Select(g => $"{g.Key}={g.Count()}"));
        Console.WriteLine($"INF Prepared VisaType histogram: {summary}");

        var wpCount = groups
            .Where(g => string.Equals(g.Key, "WP", StringComparison.OrdinalIgnoreCase))
            .Sum(g => g.Count());
        var nonWp = importRows.Count - wpCount;
        var legacyNonWp = importRows.Count(r =>
        {
            var composite = r.GetValueOrDefault("_legacy_VisaTypeComposite") as string;
            return !string.IsNullOrWhiteSpace(composite)
                && !composite.StartsWith("WP:", StringComparison.OrdinalIgnoreCase);
        });

        if (importRows.Count >= 100 && legacyNonWp > 0 && nonWp == 0)
        {
            throw new InvalidOperationException(
                $"Prepared Visa rows collapsed to WP only ({wpCount}/{importRows.Count}) while " +
                $"{legacyNonWp} legacy rows are non-WP (BS/FM/GL/EX). Check lookup-translations VisaType mapping.");
        }
    }
}
