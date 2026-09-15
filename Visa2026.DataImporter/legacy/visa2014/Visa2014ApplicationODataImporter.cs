using System.Collections.Concurrent;
using DevExpress.ExpressApp;
using Visa2026.DataImporter;
using Visa2026.Module.BusinessObjects;

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
        int batchSize = 50,
        string? applicationTypeName = null)
    {
        var batch = Visa2014ApplicationTransform.PrepareImportBatch(
            legacyConnectionString,
            lookupTranslationPaths,
            maxRows,
            verbose,
            applicationTypeName);

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
                    var payload = await BuildPayloadAsync(row, resolver, workerTarget);
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

    private static async Task<Dictionary<string, object?>?> BuildPayloadAsync(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        IVisa2014ImportTarget target)
    {
        var payload = BuildPayload(row, resolver);
        if (payload == null)
            return null;

        await TryAddCaseSummaryFieldsAsync(payload, row, resolver, target);
        return payload;
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
        TryAddOptionalFk(payload, row, "ToRegion", resolver.ResolveRegion);

        var cityName = row.GetValueOrDefault("ToCity") as string;
        var cityId = resolver.ResolveCity(cityName);
        if (cityId.HasValue && !payload.ContainsKey("ToRegion"))
        {
            var regionId = resolver.ResolveRegionForCity(cityId.Value);
            if (regionId.HasValue)
                payload["ToRegion"] = new { ID = regionId.Value };
        }

        var purpose = Visa2014ApplicationTransform.ResolvePurposeForImport(
            row.GetValueOrDefault("Purpose") as string,
            row.GetValueOrDefault("ApplicationType") as string);
        if (!string.IsNullOrWhiteSpace(purpose))
            payload["Purpose"] = purpose;

        TryAddBusinessTripDestinationFields(
            payload,
            row.GetValueOrDefault("BusinessTripAddress") as string,
            FirstNonBlank(
                row.GetValueOrDefault("BusinessTripAddressCity") as string,
                cityName),
            FirstNonBlank(
                row.GetValueOrDefault("ToRegion") as string,
                row.GetValueOrDefault("Region") as string),
            resolver);

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

    internal static async Task TryAddCaseSummaryFieldsAsync(
        Dictionary<string, object?> payload,
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        IVisa2014ImportTarget target)
    {
        _ = target;
        var cityName = (row.GetValueOrDefault("ToCity") as string)
            ?? (row.GetValueOrDefault("City") as string);
        var cityId = resolver.ResolveCity(cityName);
        if (cityId.HasValue)
        {
            payload["ToCity"] = new { ID = cityId.Value };

            var regionId = resolver.ResolveRegion(
                    FirstNonBlank(
                        row.GetValueOrDefault("ToRegion") as string,
                        row.GetValueOrDefault("Region") as string))
                ?? resolver.ResolveRegionForCity(cityId.Value);
            if (regionId.HasValue)
                payload["ToRegion"] = new { ID = regionId.Value };
        }

        var purpose = Visa2014ApplicationTransform.ResolvePurposeForImport(
            row.GetValueOrDefault("Purpose") as string,
            row.GetValueOrDefault("ApplicationType") as string);
        if (!string.IsNullOrWhiteSpace(purpose))
            payload["Purpose"] = purpose;

        TryAddBusinessTripDestinationFields(
            payload,
            row.GetValueOrDefault("BusinessTripAddress") as string,
            (row.GetValueOrDefault("BusinessTripAddressCity") as string) ?? cityName,
            FirstNonBlank(
                row.GetValueOrDefault("ToRegion") as string,
                row.GetValueOrDefault("Region") as string),
            resolver);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Resolves trip destination into Type + Lodging/Hotel/Hospital/OtherSite.
    /// Does not create BusinessTripAddress catalog rows; unmatched lines resolve via OtherSite tenant catalog.
    /// </summary>
    internal static void TryAddBusinessTripDestinationFields(
        Dictionary<string, object?> payload,
        string? rawAddressText,
        string? cityName,
        string? regionName,
        Visa2014ODataLookupResolver resolver)
    {
        var addressText = Visa2014ApplicationTransform.TrimBusinessTripAddress(rawAddressText);
        if (string.IsNullOrWhiteSpace(addressText))
            return;

        var region = regionName?.Trim();
        if (string.IsNullOrWhiteSpace(region) && resolver.ResolveCity(cityName) is Guid cityId)
            region = resolver.GetCityRegionNameTm(cityId);

        // Clean like AoR / BTA catalog preview so "City; Hotel name" matches tenant scalars.
        var lodgingScalar = Visa2014AddressLineNormalizer.NormalizeLodgingCatalogAddress(
            addressText, region, cityName);
        var hotelScalar = Visa2014AddressLineNormalizer.NormalizeHotelCatalogName(
            addressText, region, cityName);
        if (string.IsNullOrWhiteSpace(lodgingScalar))
            lodgingScalar = addressText;
        if (string.IsNullOrWhiteSpace(hotelScalar))
            hotelScalar = addressText;

        if (Visa2014ResidenceClassifier.IsHotelAddressLine(addressText))
        {
            var hotelId = resolver.ResolveHotel(cityName, region, hotelScalar)
                ?? resolver.ResolveHotel(cityName, region, addressText);
            if (hotelId.HasValue)
            {
                ClearBusinessTripDestinationFks(payload);
                payload["BusinessTripAddressType"] = (int)ResidenceType.Hotel;
                payload["BusinessTripHotel"] = new { ID = hotelId.Value };
                return;
            }
        }

        if (Visa2014ResidenceClassifier.IsHospitalAddressLine(addressText))
        {
            var hospitalId = resolver.ResolveHospital(cityName, region, hotelScalar)
                ?? resolver.ResolveHospital(cityName, region, addressText);
            if (hospitalId.HasValue)
            {
                ClearBusinessTripDestinationFks(payload);
                payload["BusinessTripAddressType"] = (int)ResidenceType.Hospital;
                payload["BusinessTripHospital"] = new { ID = hospitalId.Value };
                return;
            }
        }

        if (Visa2014ResidenceClassifier.IsLodgingSiteLine(addressText))
        {
            var lodgingId = resolver.ResolveLodging(cityName, region, lodgingScalar)
                ?? resolver.ResolveLodging(cityName, region, addressText);
            if (lodgingId.HasValue)
            {
                ClearBusinessTripDestinationFks(payload);
                payload["BusinessTripAddressType"] = (int)ResidenceType.Lodging;
                payload["BusinessTripLodging"] = new { ID = lodgingId.Value };
                return;
            }
        }

        // Unmatched / non-pattern lines -> OtherSite (tenant catalog; no PrivateHouse invent).
        var otherId = resolver.ResolveOtherSite(cityName, region, lodgingScalar)
            ?? resolver.ResolveOtherSite(cityName, region, addressText);

        if (otherId.HasValue)
        {
            ClearBusinessTripDestinationFks(payload);
            payload["BusinessTripAddressType"] = (int)ResidenceType.Other;
            payload["BusinessTripOtherSite"] = new { ID = otherId.Value };
            return;
        }

        // Absolute last resort when catalog still missing the line (should be rare after BTA merge).
        ClearBusinessTripDestinationFks(payload);
        payload["BusinessTripAddressType"] = (int)ResidenceType.PrivateHouse;
        payload["BusinessTripPrivateHouseAddress"] = addressText;
    }

    private static void ClearBusinessTripDestinationFks(Dictionary<string, object?> payload)
    {
        payload["BusinessTripLodging"] = null;
        payload["BusinessTripHotel"] = null;
        payload["BusinessTripHospital"] = null;
        payload["BusinessTripOtherSite"] = null;
        payload["BusinessTripPrivateHouseAddress"] = null;
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

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

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
