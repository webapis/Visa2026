using System.Text.Json;
using Visa2026.DataImporter;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014AddressOfResidenceImportResult
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

internal static class Visa2014AddressOfResidenceODataImporter
{
    public static async Task<Visa2014AddressOfResidenceImportResult> RunAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string personIdMapPath,
        string? addressIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        if (verbose)
            Console.WriteLine($"INF Person id-map entries: {personIdMap.Count}");

        var batch = Visa2014AddressOfResidenceTransform.PrepareImportBatch(
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
            return new Visa2014AddressOfResidenceImportResult
            {
                LegacyRowCount = batch.LegacyRowCount,
                PreparedCount = batch.ImportRows.Count,
                SkippedCount = batch.Skipped.Count,
                DedupeMergedCount = batch.DedupeMergedCount,
                SkippedNoPersonMap = missingPerson,
            };
        }

        var addressIdMap = LoadOptionalIdMap(addressIdMapOutputPath);
        if (verbose && addressIdMap.Count > 0)
            Console.WriteLine($"INF Existing AddressOfResidence id-map entries: {addressIdMap.Count}");

        var errors = new List<string>();
        int posted = 0;
        int failed = 0;
        int skippedNoPerson = 0;
        int skippedAlreadyImported = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOidText = row.GetValueOrDefault("_legacyRowId") as string;
            if (!Guid.TryParse(legacyOidText, out var legacyOid))
            {
                failed++;
                errors.Add($"{legacyOidText ?? "(null)"}: invalid legacy row id");
                continue;
            }

            if (addressIdMap.ContainsKey(legacyOid))
            {
                skippedAlreadyImported++;
                if (verbose)
                    Console.WriteLine($"  SKIP {legacyOid}: already in AddressOfResidence id-map");
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
                var payload = Visa2014AddressOfResidenceImportApplier.BuildODataPayload(row, resolver, personId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row, resolver)})");
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.AddressOfResidence), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    errors.Add($"{legacyOid}: create returned null");
                    continue;
                }

                addressIdMap[legacyOid] = createdId.Value;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoPerson} no person map...");
                if (verbose)
                    Console.WriteLine($"  SAVE AddressOfResidence {createdId.Value} <- legacy {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
        }

        await target.FlushAsync();

        int inferredPosted = 0;
        int inferredFailed = 0;
        int inferredSkipped = 0;
        if (!dryRun)
        {
            (inferredPosted, inferredFailed, inferredSkipped) = await ImportInferredFromPiaAsync(
                target,
                resolver,
                legacyConnectionString,
                lookupTranslationPaths,
                personIdMap,
                addressIdMap,
                verbose);
            if (inferredPosted > 0 || inferredSkipped > 0)
                Console.WriteLine(
                    $"INF PIA-inferred AddressOfResidence: posted {inferredPosted}, skipped {inferredSkipped}, failed {inferredFailed}");
        }

        string? idMapPath = null;
        if (addressIdMap.Count > 0 && !string.IsNullOrWhiteSpace(addressIdMapOutputPath))
        {
            idMapPath = Path.GetFullPath(addressIdMapOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(idMapPath)!);
            var serializable = addressIdMap.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.ToString());
            await File.WriteAllTextAsync(
                idMapPath,
                JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true }));
        }

        return new Visa2014AddressOfResidenceImportResult
        {
            LegacyRowCount = batch.LegacyRowCount,
            PreparedCount = batch.ImportRows.Count,
            SkippedCount = batch.Skipped.Count,
            DedupeMergedCount = batch.DedupeMergedCount,
            SkippedNoPersonMap = skippedNoPerson,
            SkippedAlreadyImported = skippedAlreadyImported,
            PostedCount = posted + inferredPosted,
            FailedCount = failed + inferredFailed,
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

    private static bool TryResolveLegacyPersonOid(Dictionary<string, object?> row, out Guid legacyPersonOid)
    {
        legacyPersonOid = Guid.Empty;
        var text = row.GetValueOrDefault("Person") as string
            ?? row.GetValueOrDefault("_legacy_PersonOid") as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyPersonOid);
    }

    private static bool TryParseDate(string? text, out DateTime date) =>
        DateTime.TryParse(text, out date);

    private static string DescribePayloadGap(Dictionary<string, object?> row, Visa2014ODataLookupResolver resolver)
    {
        var gaps = new List<string>();
        var typeText = row.GetValueOrDefault("Type") as string;
        if (!Enum.TryParse<ResidenceType>(typeText, ignoreCase: true, out var residenceType))
        {
            gaps.Add($"Type={typeText}");
            return string.Join("; ", gaps);
        }

        var regionName = row.GetValueOrDefault("Region") as string;
        var cityName = row.GetValueOrDefault("City") as string;

        if (!resolver.ResolveRegion(regionName).HasValue)
            gaps.Add($"Region={regionName}");
        if (!resolver.ResolveCity(cityName, regionName).HasValue)
            gaps.Add($"City={cityName}");

        switch (residenceType)
        {
            case ResidenceType.PrivateHouse:
                if (string.IsNullOrWhiteSpace(row.GetValueOrDefault("FullAddress") as string))
                    gaps.Add("FullAddress=(empty)");
                break;
            case ResidenceType.Lodging:
                if (!resolver.ResolveLodging(cityName, regionName, row.GetValueOrDefault("Lodging") as string).HasValue)
                    gaps.Add($"Lodging={row.GetValueOrDefault("Lodging")}");
                break;
            case ResidenceType.Hotel:
                if (!resolver.ResolveHotel(cityName, regionName, row.GetValueOrDefault("Hotel") as string).HasValue)
                    gaps.Add($"Hotel={row.GetValueOrDefault("Hotel")}");
                break;
            case ResidenceType.Hospital:
                if (!resolver.ResolveHospital(cityName, regionName, row.GetValueOrDefault("Hospital") as string).HasValue)
                    gaps.Add($"Hospital={row.GetValueOrDefault("Hospital")}");
                break;
            case ResidenceType.Other:
                if (!resolver.ResolveOtherSite(cityName, regionName, row.GetValueOrDefault("OtherSite") as string).HasValue)
                    gaps.Add($"OtherSite={row.GetValueOrDefault("OtherSite")}");
                break;
        }

        return gaps.Count > 0 ? string.Join("; ", gaps) : "lookup or required field";
    }

    private static async Task<(int Posted, int Failed, int Skipped)> ImportInferredFromPiaAsync(
        IVisa2014ImportTarget target,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        Dictionary<Guid, Guid> addressIdMap,
        bool verbose)
    {
        RegisterSponsorCanonicalFromExistingLegacyAor(
            legacyConnectionString, personIdMap, addressIdMap, verbose);

        var batch = Visa2014PiaAddressInference.PrepareEmployeeInferredAddresses(
            legacyConnectionString, lookupTranslationPaths, verbose);

        int posted = 0;
        int failed = 0;
        int skipped = 0;

        foreach (var plan in batch.Plans)
        {
            if (addressIdMap.ContainsKey(plan.SyntheticLegacyOid))
            {
                skipped++;
                continue;
            }

            if (!personIdMap.TryGetValue(plan.LegacyPersonOid, out var personId))
            {
                skipped++;
                if (verbose)
                    Console.WriteLine($"  SKIP inferred {plan.SyntheticLegacyOid}: Person {plan.LegacyPersonOid} not in id-map");
                continue;
            }

            try
            {
                var payload = Visa2014AddressOfResidenceImportApplier.BuildODataPayload(plan.ImportRow, resolver, personId);
                if (payload == null)
                {
                    failed++;
                    continue;
                }

                var createdId = await target.CreateAsync(typeof(Visa2026.Module.BusinessObjects.AddressOfResidence), payload);
                if (!createdId.HasValue)
                {
                    failed++;
                    continue;
                }

                Visa2014PiaAddressInference.RegisterPlanAliases(plan, createdId.Value, addressIdMap);
                posted++;
                if (verbose)
                    Console.WriteLine($"  SAVE inferred AddressOfResidence {createdId.Value} <- person {plan.LegacyPersonOid}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"ERR inferred {plan.SyntheticLegacyOid}: {ex.Message}");
            }
        }

        await target.FlushAsync();
        return (posted, failed, skipped);
    }

    private static void RegisterSponsorCanonicalFromExistingLegacyAor(
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IDictionary<Guid, Guid> addressIdMap,
        bool verbose)
    {
        const string sql = """
            SELECT
                CAST(aor.Person AS varchar(36)) AS PersonOid,
                CAST(aor.Oid AS varchar(36)) AS AorOid,
                CONVERT(varchar(10), addr.ExpiringDateOfAddressDocument, 23) AS ExpirationDate
            FROM dbo.AddressOfResidence aor
            INNER JOIN dbo.Address addr ON addr.Oid = aor.Address AND addr.GCRecord IS NULL
            WHERE aor.GCRecord IS NULL
            """;

        var rows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        var bestPerPerson = new Dictionary<Guid, (Guid AorOid, DateTime? Expiration)>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("PersonOid"), out var personOid))
                continue;
            if (!Guid.TryParse(row.GetValueOrDefault("AorOid"), out var aorOid))
                continue;
            if (!personIdMap.ContainsKey(personOid))
                continue;

            DateTime? expiration = DateTime.TryParse(row.GetValueOrDefault("ExpirationDate"), out var exp) ? exp : null;
            if (!bestPerPerson.TryGetValue(personOid, out var current) ||
                CompareAddressRecency(expiration, aorOid, current.Expiration, current.AorOid) > 0)
            {
                bestPerPerson[personOid] = (aorOid, expiration);
            }
        }

        int registered = 0;
        foreach (var (personOid, best) in bestPerPerson)
        {
            if (!addressIdMap.TryGetValue(best.AorOid, out var targetId))
                continue;

            var synthetic = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(personOid);
            if (addressIdMap.ContainsKey(synthetic))
                continue;

            addressIdMap[synthetic] = targetId;
            registered++;
        }

        if (verbose && registered > 0)
            Console.WriteLine($"INF Registered {registered} sponsor canonical AddressOfResidence alias(es) from existing legacy rows.");
    }

    private static int CompareAddressRecency(DateTime? expA, Guid oidA, DateTime? expB, Guid oidB)
    {
        var rankA = expA?.Date ?? DateTime.MaxValue;
        var rankB = expB?.Date ?? DateTime.MaxValue;
        var cmp = rankA.CompareTo(rankB);
        return cmp != 0 ? cmp : oidA.CompareTo(oidB);
    }

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
