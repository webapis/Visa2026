namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014HotelTransform
{
    private const string SeherEtrap = "\u015E\u00E4herEtrap";

    internal static readonly string[] HotelMainColumnOrder =
    [
        "_importAction", "Name", "UsageCount", "Region", "City",
        "_legacy_AddressLine", "_legacy_RegionMgCode", "_legacy_CityMgCode", "_legacyVariantCount",
    ];

    internal const string ExtractSql = $"""
        SELECT
            a.AddressLine,
            ISNULL(r.mgCode, '') AS RegionMgCode,
            r.NameOfRegion AS RegionName,
            ISNULL(se.mgCode, '') AS CityMgCode,
            se.[{SeherEtrap}L] AS CityName,
            COUNT(*) AS UsageCount
        FROM dbo.Address a
        INNER JOIN dbo.DocumentOfAddress d ON a.DocumentOfAddress = d.Oid AND d.TypeOfDocument = N'myhmanhana'
        LEFT JOIN dbo.Region r ON a.Region = r.Oid
        LEFT JOIN dbo.[{SeherEtrap}] se ON a.[{SeherEtrap}] = se.Oid
        WHERE a.GCRecord IS NULL
          AND NULLIF(LTRIM(RTRIM(a.AddressLine)), N'') IS NOT NULL
        GROUP BY a.AddressLine, r.mgCode, r.NameOfRegion, se.mgCode, se.[{SeherEtrap}L]
        """;

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var orderBy = " ORDER BY UsageCount DESC, AddressLine";
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({ExtractSql}) AS q{orderBy}"
            : ExtractSql + orderBy;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var sourceRows = new List<Visa2014LodgingSourceRow>();
        foreach (var dict in dictRows)
        {
            if (Visa2014LodgingTransform.TryParseSourceRow(dict, out var parsed))
                sourceRows.Add(parsed);
        }

        AppendLojmanHotelSourceRows(connectionString, maxRows, verbose, sourceRows);
        AppendPatentHotelSourceRows(connectionString, maxRows, verbose, sourceRows);

        var batch = TransformRows(sourceRows, catalogs, out var skipped, out var unmappedDistinct, out var dedupeSummary);
        var importHotels = CollectAddressImportHotelRows(
            connectionString, lookupTranslationPaths, maxRows, batch.ImportRows);
        if (importHotels.Count == 0)
            return batch;

        return new Visa2014PersonImportBatch
        {
            LegacyRowCount = batch.LegacyRowCount,
            ImportRows = batch.ImportRows.Concat(importHotels).ToList(),
            Skipped = batch.Skipped,
            UnmappedLookups = batch.UnmappedLookups,
            DedupeMergedCount = batch.DedupeMergedCount,
            DedupeSummary = batch.DedupeSummary,
        };
    }

    private static List<Dictionary<string, object?>> CollectAddressImportHotelRows(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        IReadOnlyList<Dictionary<string, object?>> existingCatalogRows)
    {
        var existingKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in existingCatalogRows)
        {
            var key = Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey(
                row.GetValueOrDefault("City") as string,
                row.GetValueOrDefault("Name") as string);
            if (!string.IsNullOrEmpty(key))
                existingKeys.Add(key);
        }

        var addressBatch = Visa2014AddressOfResidenceTransform.PrepareImportBatch(
            connectionString, lookupTranslationPaths, maxRows, verbose: false);

        var rows = new List<Dictionary<string, object?>>();
        foreach (var row in addressBatch.ImportRows)
        {
            if (!string.Equals(row.GetValueOrDefault("Type") as string, "Hotel", StringComparison.OrdinalIgnoreCase))
                continue;

            var hotelName = row.GetValueOrDefault("Hotel") as string;
            var regionNameTm = row.GetValueOrDefault("Region") as string;
            var cityNameTm = row.GetValueOrDefault("City") as string;
            if (string.IsNullOrWhiteSpace(hotelName) || string.IsNullOrWhiteSpace(cityNameTm))
                continue;

            var key = Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey(cityNameTm, hotelName);
            if (string.IsNullOrEmpty(key) || !existingKeys.Add(key))
                continue;

            rows.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_importAction"] = "import",
                ["Name"] = hotelName.Trim(),
                ["UsageCount"] = 1,
                ["Region"] = regionNameTm,
                ["City"] = cityNameTm,
                ["_legacy_AddressLine"] = row.GetValueOrDefault("_legacy_AddressLine"),
                ["_legacy_RegionMgCode"] = row.GetValueOrDefault("_legacy_RegionMgCode"),
                ["_legacy_CityMgCode"] = row.GetValueOrDefault("_legacy_CityMgCode"),
                ["_legacyVariantCount"] = 1,
                ["_source"] = "AddressOfResidence import Hotel FK",
            });
        }

        return rows;
    }

    private static void AppendPatentHotelSourceRows(
        string connectionString,
        int? maxRows,
        bool verbose,
        List<Visa2014LodgingSourceRow> sourceRows)
    {
        const string patentHotelSql = $"""
            SELECT
                a.AddressLine,
                ISNULL(r.mgCode, '') AS RegionMgCode,
                r.NameOfRegion AS RegionName,
                ISNULL(se.mgCode, '') AS CityMgCode,
                se.[{SeherEtrap}L] AS CityName,
                COUNT(*) AS UsageCount
            FROM dbo.AddressOfResidence aor
            INNER JOIN dbo.Person p ON aor.Person = p.Oid AND p.GCRecord IS NULL
            INNER JOIN dbo.Address a ON aor.Address = a.Oid AND a.GCRecord IS NULL
            INNER JOIN dbo.DocumentOfAddress d ON a.DocumentOfAddress = d.Oid AND d.TypeOfDocument = N'Patent'
            LEFT JOIN dbo.Region r ON a.Region = r.Oid
            LEFT JOIN dbo.[{SeherEtrap}] se ON a.[{SeherEtrap}] = se.Oid
            WHERE aor.GCRecord IS NULL
              AND NULLIF(LTRIM(RTRIM(a.AddressLine)), N'') IS NOT NULL
            GROUP BY a.AddressLine, r.mgCode, r.NameOfRegion, se.mgCode, se.[{SeherEtrap}L]
            """;

        var orderBy = " ORDER BY UsageCount DESC, AddressLine";
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({patentHotelSql}) AS q{orderBy}"
            : patentHotelSql + orderBy;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        foreach (var dict in dictRows)
        {
            if (!Visa2014LodgingTransform.TryParseSourceRow(dict, out var parsed))
                continue;
            if (!Visa2014ResidenceClassifier.IsHotelAddressLine(parsed.AddressLine))
                continue;
            sourceRows.Add(parsed);
        }
    }

    private static void AppendLojmanHotelSourceRows(
        string connectionString,
        int? maxRows,
        bool verbose,
        List<Visa2014LodgingSourceRow> sourceRows)
    {
        var orderBy = " ORDER BY UsageCount DESC, AddressLine";
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({Visa2014LodgingTransform.ExtractSql}) AS q{orderBy}"
            : Visa2014LodgingTransform.ExtractSql + orderBy;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        foreach (var dict in dictRows)
        {
            if (!Visa2014LodgingTransform.TryParseSourceRow(dict, out var parsed))
                continue;
            if (!Visa2014ResidenceClassifier.IsHotelAddressLine(parsed.AddressLine))
                continue;
            sourceRows.Add(parsed);
        }
    }

    internal static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014LodgingSourceRow> sourceRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);
        var byKey = new Dictionary<string, HotelAccumulator>(StringComparer.Ordinal);
        var dedupeMerged = 0;

        foreach (var row in sourceRows)
        {
            if (Visa2014ResidenceClassifier.IsHospitalAddressLine(row.AddressLine))
                continue;

            if (!Visa2014AddressOfResidenceTransform.TryBuildHotelSiteAddress(
                    row.AddressLine,
                    row.RegionMgCode,
                    row.RegionName,
                    row.CityMgCode,
                    row.CityName,
                    catalogs,
                    out var hotelName,
                    out var regionNameTm,
                    out var cityNameTm,
                    out var unmappedReason))
            {
                skipped.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["reason"] = unmappedReason ?? "unmapped",
                    ["_legacy_AddressLine"] = row.AddressLine,
                    ["UsageCount"] = row.UsageCount,
                    ["_legacy_RegionMgCode"] = row.RegionMgCode,
                    ["_legacy_CityMgCode"] = row.CityMgCode,
                });
                foreach (var part in (unmappedReason ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (part.Contains(':'))
                        unmappedSet.Add(part);
                }
                continue;
            }

            var key = Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey(cityNameTm, hotelName);
            if (string.IsNullOrEmpty(key))
            {
                skipped.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["reason"] = "empty normalized key",
                    ["_legacy_AddressLine"] = row.AddressLine,
                    ["UsageCount"] = row.UsageCount,
                });
                continue;
            }

            if (!byKey.TryGetValue(key, out var acc))
            {
                byKey[key] = new HotelAccumulator(hotelName!, regionNameTm!, cityNameTm!, row);
                continue;
            }

            acc.Merge(row);
            dedupeMerged++;
            dedupeSummary.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["Name"] = acc.Name,
                ["_legacy_AddressLine"] = row.AddressLine,
                ["UsageCount"] = row.UsageCount,
                ["_importAction"] = "duplicate_merged",
                ["Region"] = acc.RegionNameTm,
                ["City"] = acc.CityNameTm,
            });
        }

        var importRows = byKey.Values
            .OrderByDescending(a => a.UsageCount)
            .ThenBy(a => a.Name, StringComparer.Ordinal)
            .Select(a => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_importAction"] = "import",
                ["Name"] = a.Name,
                ["UsageCount"] = a.UsageCount,
                ["Region"] = a.RegionNameTm,
                ["City"] = a.CityNameTm,
                ["_legacy_AddressLine"] = a.CanonicalLegacyLine,
                ["_legacy_RegionMgCode"] = a.CanonicalRegionMgCode,
                ["_legacy_CityMgCode"] = a.CanonicalCityMgCode,
                ["_legacyVariantCount"] = a.VariantCount,
            })
            .ToList();

        unmappedDistinct = unmappedSet
            .Select(u => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["catalog"] = u.Split(':')[0],
                ["legacyValue"] = u.Contains(':') ? u[(u.IndexOf(':') + 1)..] : u,
                ["reason"] = "unmapped",
            })
            .OrderBy(r => r["catalog"]?.ToString(), StringComparer.Ordinal)
            .ThenBy(r => r["legacyValue"]?.ToString(), StringComparer.Ordinal)
            .ToList();

        return new Visa2014PersonImportBatch
        {
            LegacyRowCount = sourceRows.Count(r => !Visa2014ResidenceClassifier.IsHospitalAddressLine(r.AddressLine)),
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = unmappedDistinct,
            DedupeMergedCount = dedupeMerged,
            DedupeSummary = dedupeSummary,
        };
    }

    private sealed class HotelAccumulator
    {
        public HotelAccumulator(
            string name,
            string regionNameTm,
            string cityNameTm,
            Visa2014LodgingSourceRow first)
        {
            Name = name;
            RegionNameTm = regionNameTm;
            CityNameTm = cityNameTm;
            CanonicalLegacyLine = first.AddressLine;
            CanonicalRegionMgCode = first.RegionMgCode;
            CanonicalCityMgCode = first.CityMgCode;
            UsageCount = first.UsageCount;
            VariantCount = 1;
        }

        public string Name { get; }
        public string RegionNameTm { get; }
        public string CityNameTm { get; }
        public string? CanonicalLegacyLine { get; }
        public string? CanonicalRegionMgCode { get; }
        public string? CanonicalCityMgCode { get; }
        public int UsageCount { get; private set; }
        public int VariantCount { get; private set; }

        public void Merge(Visa2014LodgingSourceRow row)
        {
            UsageCount += row.UsageCount;
            VariantCount++;
        }
    }
}
