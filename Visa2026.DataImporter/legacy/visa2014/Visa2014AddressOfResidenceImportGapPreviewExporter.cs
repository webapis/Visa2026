using Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Excel preview of AddressOfResidence rows that fail lookup resolve (City/Lodging/Hotel/...).
/// For manual review - not an approved exclusion list.
/// </summary>
internal static class Visa2014AddressOfResidenceImportGapPreviewExporter
{
    private static readonly string[] GapColumns =
    [
        "LegacyOid",
        "PersonOid",
        "Type",
        "Region",
        "City",
        "FullAddress",
        "Lodging",
        "Hotel",
        "Hospital",
        "OtherSite",
        "ExpirationDate",
        "GapDetail",
        "RegionResolved",
        "CityResolved",
        "SiteResolved",
        "AlreadyInIdMap",
        "PersonInIdMap",
        "LegacyAddressLine",
        "Source",
        "ReviewNotes",
    ];

    public static Visa2014PreviewExportResult Export(
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        Visa2014ODataLookupResolver resolver,
        string personIdMapPath,
        string? addressIdMapPath,
        string outputPath,
        int? maxRows,
        bool verbose,
        string? legacySourceId = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        var batch = Visa2014AddressOfResidenceTransform.PrepareImportBatch(
            legacyConnectionString, lookupTranslationPaths, maxRows, verbose);

        var personIdMap = Visa2014IdMapHelper.Load(personIdMapPath);
        var addressIdMap = string.IsNullOrWhiteSpace(addressIdMapPath) || !File.Exists(addressIdMapPath)
            ? new Dictionary<Guid, Guid>()
            : Visa2014IdMapHelper.Load(addressIdMapPath);

        var gapRows = new List<Dictionary<string, object?>>();
        var alreadyImported = 0;
        var wouldPost = 0;
        var missingPerson = 0;

        foreach (var row in batch.ImportRows)
        {
            var legacyOidText = row.GetValueOrDefault("_legacyRowId") as string;
            if (!Guid.TryParse(legacyOidText, out var legacyOid))
            {
                gapRows.Add(BuildGapRow(row, legacyOidText, null, "invalid_legacy_oid", false, false, resolver, "legacy-aor"));
                continue;
            }

            if (addressIdMap.ContainsKey(legacyOid))
            {
                alreadyImported++;
                continue;
            }

            var personText = row.GetValueOrDefault("Person") as string
                ?? row.GetValueOrDefault("_legacy_PersonOid") as string;
            if (!Guid.TryParse(personText, out var legacyPersonOid) ||
                !personIdMap.TryGetValue(legacyPersonOid, out var personId))
            {
                missingPerson++;
                gapRows.Add(BuildGapRow(row, legacyOidText, personText, "missing_person_id_map", false, false, resolver, "legacy-aor"));
                continue;
            }

            var payload = Visa2014AddressOfResidenceImportApplier.BuildODataPayload(row, resolver, personId);
            if (payload != null)
            {
                wouldPost++;
                continue;
            }

            var gap = Visa2014AddressOfResidenceODataImporter.DescribePayloadGap(row, resolver);
            gapRows.Add(BuildGapRow(row, legacyOidText, personText, gap, false, true, resolver, "legacy-aor"));
        }

        var piaBatch = Visa2014PiaAddressInference.PrepareEmployeeInferredAddresses(
            legacyConnectionString, lookupTranslationPaths, verbose);
        var piaWouldPost = 0;
        var piaAlready = 0;
        var piaMissingPerson = 0;

        foreach (var plan in piaBatch.Plans)
        {
            if (addressIdMap.ContainsKey(plan.SyntheticLegacyOid))
            {
                piaAlready++;
                continue;
            }

            if (!personIdMap.TryGetValue(plan.LegacyPersonOid, out var personId))
            {
                piaMissingPerson++;
                gapRows.Add(BuildGapRow(
                    plan.ImportRow,
                    plan.SyntheticLegacyOid.ToString(),
                    plan.LegacyPersonOid.ToString(),
                    "missing_person_id_map",
                    false,
                    false,
                    resolver,
                    "pia-inferred"));
                continue;
            }

            var payload = Visa2014AddressOfResidenceImportApplier.BuildODataPayload(plan.ImportRow, resolver, personId);
            if (payload != null)
            {
                piaWouldPost++;
                continue;
            }

            var gap = Visa2014AddressOfResidenceODataImporter.DescribePayloadGap(plan.ImportRow, resolver);
            gapRows.Add(BuildGapRow(
                plan.ImportRow,
                plan.SyntheticLegacyOid.ToString(),
                plan.LegacyPersonOid.ToString(),
                gap,
                false,
                true,
                resolver,
                "pia-inferred"));
        }

        var metaRows = new List<IReadOnlyDictionary<string, object?>>
        {
            Meta("exportedAt", DateTime.UtcNow.ToString("O")),
            Meta("entity", "AddressOfResidence"),
            Meta("purpose", "Manual review of import lookup gaps (not approved exclusions)"),
            Meta("legacyRowCount", batch.LegacyRowCount),
            Meta("preparedImportRows", batch.ImportRows.Count),
            Meta("alreadyInIdMapSkipped", alreadyImported),
            Meta("wouldPostOk", wouldPost),
            Meta("missingPersonIdMap", missingPerson),
            Meta("piaPlans", piaBatch.Plans.Count),
            Meta("piaAlreadyInIdMap", piaAlready),
            Meta("piaWouldPostOk", piaWouldPost),
            Meta("piaMissingPersonIdMap", piaMissingPerson),
            Meta("gapRowCount", gapRows.Count),
            Meta("personIdMap", Path.GetFullPath(personIdMapPath)),
            Meta("addressIdMap", string.IsNullOrWhiteSpace(addressIdMapPath) ? "(none)" : Path.GetFullPath(addressIdMapPath)),
        };
        if (!string.IsNullOrWhiteSpace(legacySourceId))
            metaRows.Add(Meta("legacySource", legacySourceId));

        var writtenPath = Visa2014MinimalXlsxWriter.WriteWorkbook(outputPath,
        [
            new Visa2014Worksheet { Name = "ImportGaps", Columns = GapColumns, Rows = gapRows },
            new Visa2014Worksheet
            {
                Name = "_Meta",
                Columns = ["_key", "value"],
                Rows = metaRows,
            },
        ]);

        Console.WriteLine($"INF AddressOfResidence import-gap preview: {gapRows.Count} gap row(s) -> {writtenPath}");
        Console.WriteLine($"INF Legacy would-post OK: {wouldPost}; already imported: {alreadyImported}; missing person map: {missingPerson}");
        Console.WriteLine($"INF PIA would-post OK: {piaWouldPost}; already imported: {piaAlready}; missing person map: {piaMissingPerson}");

        return new Visa2014PreviewExportResult
        {
            OutputPath = Path.GetFullPath(writtenPath),
            LegacyRowCount = batch.LegacyRowCount,
            ImportRowCount = gapRows.Count,
            SkippedRowCount = alreadyImported + piaAlready,
            DedupeMergedCount = batch.DedupeMergedCount,
            UnmappedLookupCount = gapRows.Count,
        };
    }

    private static Dictionary<string, object?> BuildGapRow(
        Dictionary<string, object?> row,
        string? legacyOid,
        string? personOid,
        string gapDetail,
        bool alreadyInIdMap,
        bool personInIdMap,
        Visa2014ODataLookupResolver resolver,
        string source)
    {
        var region = row.GetValueOrDefault("Region") as string;
        var city = row.GetValueOrDefault("City") as string;
        var typeText = row.GetValueOrDefault("Type") as string;
        var regionOk = resolver.ResolveRegion(region).HasValue;
        var cityOk = resolver.ResolveCity(city, region).HasValue;
        var siteOk = ResolveSiteOk(row, resolver, typeText, city, region);

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["LegacyOid"] = legacyOid,
            ["PersonOid"] = personOid,
            ["Type"] = typeText,
            ["Region"] = region,
            ["City"] = city,
            ["FullAddress"] = row.GetValueOrDefault("FullAddress"),
            ["Lodging"] = row.GetValueOrDefault("Lodging"),
            ["Hotel"] = row.GetValueOrDefault("Hotel"),
            ["Hospital"] = row.GetValueOrDefault("Hospital"),
            ["OtherSite"] = row.GetValueOrDefault("OtherSite"),
            ["ExpirationDate"] = row.GetValueOrDefault("ExpirationDate"),
            ["GapDetail"] = gapDetail,
            ["RegionResolved"] = regionOk ? "yes" : "no",
            ["CityResolved"] = cityOk ? "yes" : "no",
            ["SiteResolved"] = siteOk,
            ["AlreadyInIdMap"] = alreadyInIdMap ? "yes" : "no",
            ["PersonInIdMap"] = personInIdMap ? "yes" : "no",
            ["LegacyAddressLine"] = row.GetValueOrDefault("_legacy_AddressLine"),
            ["Source"] = source,
            ["ReviewNotes"] = "",
        };
    }

    private static string ResolveSiteOk(
        Dictionary<string, object?> row,
        Visa2014ODataLookupResolver resolver,
        string? typeText,
        string? city,
        string? region)
    {
        if (!Enum.TryParse<ResidenceType>(typeText, ignoreCase: true, out var type))
            return "n/a";

        return type switch
        {
            ResidenceType.PrivateHouse =>
                string.IsNullOrWhiteSpace(row.GetValueOrDefault("FullAddress") as string) ? "no" : "yes",
            ResidenceType.Lodging =>
                resolver.ResolveLodging(city, region, row.GetValueOrDefault("Lodging") as string).HasValue ? "yes" : "no",
            ResidenceType.Hotel =>
                resolver.ResolveHotel(city, region, row.GetValueOrDefault("Hotel") as string).HasValue ? "yes" : "no",
            ResidenceType.Hospital =>
                resolver.ResolveHospital(city, region, row.GetValueOrDefault("Hospital") as string).HasValue ? "yes" : "no",
            ResidenceType.Other =>
                resolver.ResolveOtherSite(city, region, row.GetValueOrDefault("OtherSite") as string).HasValue ? "yes" : "no",
            _ => "n/a",
        };
    }

    private static Dictionary<string, object?> Meta(string key, object? value) =>
        new(StringComparer.Ordinal) { ["_key"] = key, ["value"] = value };
}