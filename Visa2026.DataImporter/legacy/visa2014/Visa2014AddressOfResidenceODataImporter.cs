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
        ApiClient api,
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

        var resolver = new Visa2014ODataLookupResolver();
        var tenantCatalogDir = Path.Combine(
            Visa2014ContentRoot.FindSolutionRoot() ?? Directory.GetCurrentDirectory(),
            "Visa2026.Module",
            "DatabaseUpdate",
            "LookupCatalogs",
            "tenant");
        await resolver.LoadAsync(api, tenantCatalogDir);

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
                var payload = BuildPayload(row, resolver, personId);
                if (payload == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: incomplete OData payload ({DescribePayloadGap(row, resolver)})");
                    continue;
                }

                var created = await api.CreateAsync<AddressOfResidence>("AddressOfResidence", payload);
                if (created == null)
                {
                    failed++;
                    errors.Add($"{legacyOid}: POST returned null");
                    continue;
                }

                addressIdMap[legacyOid] = created.Id;
                posted++;
                if (posted % 250 == 0)
                    Console.WriteLine($"INF Progress: {posted} posted, {failed} failed, {skippedNoPerson} no person map...");
                if (verbose)
                    Console.WriteLine($"  POST AddressOfResidence {created.Id} <- legacy {legacyOid}");
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{legacyOid}: {ex.Message}");
                Console.Error.WriteLine($"ERR {legacyOid}: {ex.Message}");
            }
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
        var typeText = row.GetValueOrDefault("Type") as string;
        if (!Enum.TryParse<ResidenceType>(typeText, ignoreCase: true, out var residenceType))
            return null;

        var regionName = row.GetValueOrDefault("Region") as string;
        var cityName = row.GetValueOrDefault("City") as string;
        var regionId = resolver.ResolveRegion(regionName);
        var cityId = resolver.ResolveCity(cityName, regionName);
        if (!regionId.HasValue || !cityId.HasValue)
            return null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["Type"] = residenceType.ToString(),
            ["Region"] = new { ID = regionId.Value },
            ["City"] = new { ID = cityId.Value },
        };

        switch (residenceType)
        {
            case ResidenceType.PrivateHouse:
                var fullAddress = row.GetValueOrDefault("FullAddress") as string;
                if (string.IsNullOrWhiteSpace(fullAddress))
                    return null;
                payload["FullAddress"] = fullAddress.Trim();
                if (TryParseDate(row.GetValueOrDefault("ExpirationDate") as string, out var expirationDate))
                    payload["ExpirationDate"] = DateTime.SpecifyKind(expirationDate, DateTimeKind.Utc);
                break;

            case ResidenceType.Lodging:
                var lodgingId = resolver.ResolveLodging(cityName, regionName, row.GetValueOrDefault("Lodging") as string);
                if (!lodgingId.HasValue)
                    return null;
                payload["Lodging"] = new { ID = lodgingId.Value };
                break;

            case ResidenceType.Hotel:
                var hotelId = resolver.ResolveHotel(cityName, regionName, row.GetValueOrDefault("Hotel") as string);
                if (!hotelId.HasValue)
                    return null;
                payload["Hotel"] = new { ID = hotelId.Value };
                break;

            case ResidenceType.Hospital:
                var hospitalId = resolver.ResolveHospital(cityName, regionName, row.GetValueOrDefault("Hospital") as string);
                if (!hospitalId.HasValue)
                    return null;
                payload["Hospital"] = new { ID = hospitalId.Value };
                break;

            case ResidenceType.Other:
                var otherSiteId = resolver.ResolveOtherSite(cityName, regionName, row.GetValueOrDefault("OtherSite") as string);
                if (!otherSiteId.HasValue)
                    return null;
                payload["OtherSite"] = new { ID = otherSiteId.Value };
                break;

            default:
                return null;
        }

        return payload;
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

    private static Dictionary<Guid, Guid> LoadOptionalIdMap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }
}
