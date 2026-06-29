namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014OtherSiteTransform
{
    internal static readonly string[] OtherSiteMainColumnOrder =
    [
        "_importAction", "FullAddress", "UsageCount", "Region", "City",
        "_legacy_AddressLine", "_legacy_RegionMgCode", "_legacy_CityMgCode", "_legacyVariantCount",
    ];

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var orderBy = " ORDER BY UsageCount DESC, AddressLine";
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({Visa2014LodgingTransform.ExtractSql}) AS q{orderBy}"
            : Visa2014LodgingTransform.ExtractSql + orderBy;

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
        var byKey = new Dictionary<string, CatalogAccumulator>(StringComparer.Ordinal);
        var dedupeMerged = 0;

        foreach (var row in sourceRows)
        {
            if (Visa2014ResidenceClassifier.IsHotelAddressLine(row.AddressLine))
                continue;
            if (Visa2014ResidenceClassifier.IsLodgingSiteLine(row.AddressLine))
                continue;

            if (!Visa2014AddressOfResidenceTransform.TryBuildOtherSiteAddress(
                    row.AddressLine,
                    row.RegionMgCode,
                    row.RegionName,
                    row.CityMgCode,
                    row.CityName,
                    catalogs,
                    out var fullAddress,
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

            var key = Visa2014AddressLineNormalizer.BuildCityScopedCatalogKey(cityNameTm, fullAddress);
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
                byKey[key] = new CatalogAccumulator(fullAddress!, regionNameTm!, cityNameTm!, row);
                continue;
            }

            acc.Merge(row);
            dedupeMerged++;
            dedupeSummary.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["FullAddress"] = acc.FullAddress,
                ["_legacy_AddressLine"] = row.AddressLine,
                ["UsageCount"] = row.UsageCount,
                ["_importAction"] = "duplicate_merged",
                ["Region"] = acc.RegionNameTm,
                ["City"] = acc.CityNameTm,
            });
        }

        var importRows = byKey.Values
            .OrderByDescending(a => a.UsageCount)
            .ThenBy(a => a.FullAddress, StringComparer.Ordinal)
            .Select(a => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_importAction"] = "import",
                ["FullAddress"] = a.FullAddress,
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
            LegacyRowCount = sourceRows.Count(r =>
                !Visa2014ResidenceClassifier.IsHotelAddressLine(r.AddressLine)
                && !Visa2014ResidenceClassifier.IsLodgingSiteLine(r.AddressLine)),
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = unmappedDistinct,
            DedupeMergedCount = dedupeMerged,
            DedupeSummary = dedupeSummary,
        };
    }

    private sealed class CatalogAccumulator
    {
        public CatalogAccumulator(
            string fullAddress,
            string regionNameTm,
            string cityNameTm,
            Visa2014LodgingSourceRow first)
        {
            FullAddress = fullAddress;
            RegionNameTm = regionNameTm;
            CityNameTm = cityNameTm;
            CanonicalLegacyLine = first.AddressLine;
            CanonicalRegionMgCode = first.RegionMgCode;
            CanonicalCityMgCode = first.CityMgCode;
            UsageCount = first.UsageCount;
            VariantCount = 1;
        }

        public string FullAddress { get; }
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
