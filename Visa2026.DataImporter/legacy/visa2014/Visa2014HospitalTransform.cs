namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014HospitalTransform
{
    private const string SeherEtrap = "\u015E\u00E4herEtrap";

    internal static readonly string[] HospitalMainColumnOrder =
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

        return TransformRows(sourceRows, catalogs, out var skipped, out var unmappedDistinct, out var dedupeSummary);
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014LodgingSourceRow> sourceRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);
        var byKey = new Dictionary<string, HospitalAccumulator>(StringComparer.Ordinal);
        var dedupeMerged = 0;

        foreach (var row in sourceRows)
        {
            if (!Visa2014ResidenceClassifier.IsHospitalAddressLine(row.AddressLine))
                continue;

            if (!Visa2014AddressOfResidenceTransform.TryBuildHospitalSiteAddress(
                    row.AddressLine,
                    row.RegionMgCode,
                    row.RegionName,
                    row.CityMgCode,
                    row.CityName,
                    catalogs,
                    out var hospitalName,
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

            var key = Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey(cityNameTm, hospitalName);
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
                byKey[key] = new HospitalAccumulator(hospitalName!, regionNameTm!, cityNameTm!, row);
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
            LegacyRowCount = sourceRows.Count(r => Visa2014ResidenceClassifier.IsHospitalAddressLine(r.AddressLine)),
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = unmappedDistinct,
            DedupeMergedCount = dedupeMerged,
            DedupeSummary = dedupeSummary,
        };
    }

    private sealed class HospitalAccumulator
    {
        public HospitalAccumulator(
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
