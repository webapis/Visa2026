using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014PassportImportResult
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

internal static class Visa2014PassportODataImporter
{
    public static async Task<Visa2014PassportImportResult> RunAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string? passportIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose,
        bool supplementPermitReferencedOnly = false)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");

        var batch = supplementPermitReferencedOnly
            ? Visa2014PassportTransform.PrepareSupplementPermitReferencedImportBatch(
                legacyConnectionString,
                lookupTranslationPaths,
                maxRows,
                verbose)
            : Visa2014PassportTransform.PrepareImportBatch(
                legacyConnectionString,
                lookupTranslationPaths,
                maxRows,
                verbose);

        if (supplementPermitReferencedOnly && verbose)
            Console.WriteLine("INF Mode: supplement permit-referenced Passport rows.");

        var existingPassportIdMap = !string.IsNullOrWhiteSpace(passportIdMapOutputPath)
            ? LoadOptionalPassportIdMap(passportIdMapOutputPath)
            : new Dictionary<Guid, Guid>();

        if (dryRun)
        {
            int missingPerson = CountMissingPersonMap(batch.ImportRows, personIdMap);
            int alreadyImported = supplementPermitReferencedOnly
                ? CountAlreadyImported(batch.ImportRows, existingPassportIdMap)
                : 0;
            Console.WriteLine(
                $"DRY RUN: {batch.ImportRows.Count} row(s) ready to POST " +
                $"({batch.Skipped.Count} skipped, {batch.DedupeMergedCount} dedupe merged, {missingPerson} missing Person id-map" +
                (supplementPermitReferencedOnly ? $", {alreadyImported} already in id-map" : "") +
                ").");
            return new Visa2014PassportImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedNoPersonMap = missingPerson,
                SkippedAlreadyImported = alreadyImported,
            };
        }

        var passportIdMap = new Dictionary<Guid, Guid>(existingPassportIdMap);
        if (verbose && passportIdMap.Count > 0)
            Console.WriteLine($"INF Existing Passport id-map entries: {passportIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedNoPerson = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOid = (Guid)row["_legacyRowId"]!;
            if (passportIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in Passport id-map");
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
                var payload = BuildPayload(row, resolver, personId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload (lookup or required field)");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.Passport), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                passportIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                {
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoPerson} no person map...");
                    if (supplementPermitReferencedOnly && !string.IsNullOrWhiteSpace(passportIdMapOutputPath))
                        await WritePassportIdMapAsync(passportIdMapOutputPath, passportIdMap);
                }
                else if (verbose)
                    Console.WriteLine($"  SAVE Passport {createdId.Value} <- legacy {legacyOid} ({row["PassportNumber"]})");
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
        if (passportIdMap.Count > 0 && !string.IsNullOrWhiteSpace(passportIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(passportIdMapOutputPath);
            await WritePassportIdMapAsync(idMapPath, passportIdMap);
        }

        return new Visa2014PassportImportResult
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
            if (!TryResolveLegacyPersonOid(row, out var legacyPersonOid))
            {
                missing++;
                continue;
            }

            if (!personIdMap.ContainsKey(legacyPersonOid))
                missing++;
        }

        return missing;
    }

    private static int CountAlreadyImported(
        IReadOnlyList<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, Guid> passportIdMap)
    {
        int count = 0;
        foreach (var row in importRows)
        {
            if (row["_legacyRowId"] is Guid legacyOid && passportIdMap.ContainsKey(legacyOid))
                count++;
        }

        return count;
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
        Guid personId)
    {
        if (row["PassportNumber"] is not string passportNumber || string.IsNullOrWhiteSpace(passportNumber))
            return null;
        if (row["IssueDate"] is not DateTime issueDate || row["ExpirationDate"] is not DateTime expirationDate)
            return null;
        if (row["Authority"] is not string authority || string.IsNullOrWhiteSpace(authority))
            return null;

        var passportTypeId = resolver.ResolvePassportType(row.GetValueOrDefault("PassportType") as string);
        var issuedCountryId = resolver.ResolveCountry(row.GetValueOrDefault("IssuedCountry") as string);
        if (!passportTypeId.HasValue || !issuedCountryId.HasValue)
            return null;

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["PassportNumber"] = passportNumber,
            ["Authority"] = authority,
            ["IssueDate"] = DateTime.SpecifyKind(issueDate, DateTimeKind.Utc),
            ["ExpirationDate"] = DateTime.SpecifyKind(expirationDate, DateTimeKind.Utc),
            ["IsCancelled"] = row.GetValueOrDefault("IsCancelled") is bool cancelled && cancelled,
            ["Person"] = new { ID = personId },
            ["PassportType"] = new { ID = passportTypeId.Value },
            ["IssuedCountry"] = new { ID = issuedCountryId.Value },
        };
    }

    private static Dictionary<Guid, Guid> LoadOptionalPassportIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }

    private static async Task WritePassportIdMapAsync(
        string passportIdMapOutputPath,
        IReadOnlyDictionary<Guid, Guid> passportIdMap)
    {
        var idMapPath = Path.GetFullPath(passportIdMapOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
        var serializable = passportIdMap.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value.ToString());
        await File.WriteAllTextAsync(
            idMapPath,
            JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
    }
}
