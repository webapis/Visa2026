namespace Visa2026.DataImporter.Legacy.Visa2014;

using System.Text.Json;

/// <summary>
/// Review-only: DISTINCT AddressOnBusinessTrip destinations from VISA2015,
/// cleaned + classified into Lodging/Hotel/Hospital/OtherSite for tenant catalog merge.
/// </summary>
internal static class Visa2014BusinessTripDestinationCatalogTransform
{
    private const string SeherEtrap = "\u015E\u00E4herEtrap";

    internal static readonly string[] MainColumnOrder =
    [
        "_importAction",
        "ProposedCatalog",
        "ExistingMatch",
        "Region",
        "City",
        "CleanedScalar",
        "UsageCount",
        "_dedupeKey",
        "_legacy_AddressLine",
        "_legacy_RegionMgCode",
        "_legacy_CityMgCode",
        "_classifyReason",
        "_legacyVariantCount",
    ];

    internal const string ExtractSql = $"""
        SELECT
            LTRIM(RTRIM(ISNULL(addr.AddressLine, N''))) AS AddressLine,
            ISNULL(CAST(r.mgCode AS varchar(10)), N'') AS RegionMgCode,
            r.NameOfRegion AS RegionName,
            ISNULL(CAST(se.mgCode AS varchar(10)), N'') AS CityMgCode,
            se.[{SeherEtrap}L] AS CityName,
            COUNT(*) AS UsageCount
        FROM dbo.AddressOnBusinessTrip aobt
        INNER JOIN dbo.Address addr ON addr.Oid = aobt.AddressOnTrip AND addr.GCRecord IS NULL
        LEFT JOIN dbo.Region r ON addr.Region = r.Oid AND r.GCRecord IS NULL
        LEFT JOIN dbo.[{SeherEtrap}] se ON addr.[{SeherEtrap}] = se.Oid
        WHERE aobt.GCRecord IS NULL
          AND NULLIF(LTRIM(RTRIM(addr.AddressLine)), N'') IS NOT NULL
        GROUP BY
            LTRIM(RTRIM(ISNULL(addr.AddressLine, N''))),
            r.mgCode,
            r.NameOfRegion,
            se.mgCode,
            se.[{SeherEtrap}L]
        """;

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        string? solutionRoot,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var existing = LoadExistingTenantCatalogs(solutionRoot);

        var orderBy = " ORDER BY UsageCount DESC, AddressLine";
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({ExtractSql}) AS q{orderBy}"
            : ExtractSql + orderBy;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var legacyCount = dictRows.Count;
        var importRows = new List<Dictionary<string, object?>>();
        var skipped = new List<Dictionary<string, object?>>();
        var unmapped = new List<Dictionary<string, object?>>();
        var dedupeSummary = new List<Dictionary<string, object?>>();
        var byKey = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var variantCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var dict in dictRows)
        {
            var addressLine = dict.GetValueOrDefault("AddressLine") as string;
            var regionMg = dict.GetValueOrDefault("RegionMgCode") as string;
            var regionName = dict.GetValueOrDefault("RegionName") as string;
            var cityMg = dict.GetValueOrDefault("CityMgCode") as string;
            var cityName = dict.GetValueOrDefault("CityName") as string;
            var usage = ReadUsage(dict.GetValueOrDefault("UsageCount"));

            if (string.IsNullOrWhiteSpace(addressLine))
            {
                skipped.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["reason"] = "empty AddressLine",
                    ["_legacy_AddressLine"] = addressLine,
                    ["UsageCount"] = usage,
                });
                continue;
            }

            var proposed = ProposeCatalog(addressLine);
            string? cleaned;
            string? regionTm;
            string? cityTm;
            string? unmappedReason;

            if (proposed == "Hotel")
            {
                if (!Visa2014AddressOfResidenceTransform.TryBuildHotelSiteAddress(
                        addressLine, regionMg, regionName, cityMg, cityName, catalogs,
                        out cleaned, out regionTm, out cityTm, out unmappedReason))
                {
                    skipped.Add(Skip(addressLine, usage, unmappedReason ?? "hotel resolve failed"));
                    if (unmappedReason != null)
                        unmapped.Add(Unmapped("Hotel", addressLine, unmappedReason));
                    continue;
                }
            }
            else if (proposed == "Hospital")
            {
                if (!Visa2014AddressOfResidenceTransform.TryBuildHospitalSiteAddress(
                        addressLine, regionMg, regionName, cityMg, cityName, catalogs,
                        out cleaned, out regionTm, out cityTm, out unmappedReason))
                {
                    skipped.Add(Skip(addressLine, usage, unmappedReason ?? "hospital resolve failed"));
                    if (unmappedReason != null)
                        unmapped.Add(Unmapped("Hospital", addressLine, unmappedReason));
                    continue;
                }
            }
            else
            {
                // Lodging or OtherSite — same address cleaner.
                if (!Visa2014AddressOfResidenceTransform.TryBuildLodgingSiteAddress(
                        addressLine, regionMg, regionName, cityMg, cityName, catalogs,
                        out cleaned, out regionTm, out cityTm, out unmappedReason))
                {
                    // Still propose OtherSite with stripped text when lookups fail.
                    regionTm = string.IsNullOrWhiteSpace(regionName) ? null : regionName.Trim();
                    cityTm = string.IsNullOrWhiteSpace(cityName) ? null : cityName.Trim();
                    cleaned = Visa2014AddressLineNormalizer.NormalizeLodgingCatalogAddress(
                        addressLine, regionTm, cityTm);
                    if (string.IsNullOrWhiteSpace(cleaned))
                        cleaned = addressLine.Trim();
                    proposed = "OtherSite";
                    if (unmappedReason != null)
                        unmapped.Add(Unmapped(proposed, addressLine, unmappedReason));
                }
                else if (proposed != "Lodging")
                {
                    proposed = "OtherSite";
                }
            }

            if (string.IsNullOrWhiteSpace(cleaned))
            {
                skipped.Add(Skip(addressLine, usage, "empty after clean"));
                continue;
            }

            var dedupeKey = $"{proposed}|{Visa2014AddressLineNormalizer.BuildLodgingDedupeKey(cityTm, cleaned)}";
            variantCounts[dedupeKey] = variantCounts.GetValueOrDefault(dedupeKey) + 1;

            var existingMatch = existing.Contains((proposed, cityTm ?? "", cleaned));
            var row = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_importAction"] = existingMatch ? "already_in_catalog" : "add_to_catalog",
                ["ProposedCatalog"] = proposed,
                ["ExistingMatch"] = existingMatch ? "yes" : "no",
                ["Region"] = regionTm,
                ["City"] = cityTm,
                ["CleanedScalar"] = cleaned,
                ["UsageCount"] = usage,
                ["_dedupeKey"] = dedupeKey,
                ["_legacy_AddressLine"] = addressLine.Trim(),
                ["_legacy_RegionMgCode"] = regionMg,
                ["_legacy_CityMgCode"] = cityMg,
                ["_classifyReason"] = ClassifyReason(addressLine, proposed),
                ["_legacyVariantCount"] = 1,
            };

            if (byKey.TryGetValue(dedupeKey, out var existingRow))
            {
                var prevUsage = Convert.ToInt32(existingRow["UsageCount"] ?? 0);
                existingRow["UsageCount"] = prevUsage + usage;
                existingRow["_legacyVariantCount"] = variantCounts[dedupeKey];
                dedupeSummary.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["_dedupeKey"] = dedupeKey,
                    ["ProposedCatalog"] = proposed,
                    ["CleanedScalar"] = cleaned,
                    ["mergedUsage"] = usage,
                    ["_legacy_AddressLine"] = addressLine.Trim(),
                });
                continue;
            }

            byKey[dedupeKey] = row;
            importRows.Add(row);
        }

        foreach (var row in importRows)
        {
            var key = row["_dedupeKey"] as string ?? "";
            if (variantCounts.TryGetValue(key, out var variants))
                row["_legacyVariantCount"] = variants;
        }

        return new Visa2014PersonImportBatch
        {
            LegacyRowCount = legacyCount,
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = unmapped,
            DedupeSummary = dedupeSummary,
            DedupeMergedCount = dedupeSummary.Count,
        };
    }

    private static string ProposeCatalog(string addressLine)
    {
        if (Visa2014ResidenceClassifier.IsHotelAddressLine(addressLine))
            return "Hotel";
        if (Visa2014ResidenceClassifier.IsHospitalAddressLine(addressLine))
            return "Hospital";
        if (Visa2014ResidenceClassifier.IsLodgingSiteLine(addressLine))
            return "Lodging";
        return "OtherSite";
    }

    private static string ClassifyReason(string addressLine, string proposed) =>
        proposed switch
        {
            "Hotel" => "hotel pattern (myhmanhan/otel)",
            "Hospital" => "hospital pattern (hassahan/…)",
            "Lodging" => "lodging pattern (UÝJ/lojman/…)",
            _ => "no lodging/hotel/hospital pattern → OtherSite",
        };

    private static HashSet<(string Catalog, string City, string Scalar)> LoadExistingTenantCatalogs(string? solutionRoot)
    {
        var set = new HashSet<(string, string, string)>(CatalogTupleComparer.Instance);
        if (string.IsNullOrWhiteSpace(solutionRoot))
            return set;

        var tenantDir = Path.Combine(
            solutionRoot,
            "Visa2026.Module",
            "DatabaseUpdate",
            "LookupCatalogs",
            "tenant");

        LoadJson(set, Path.Combine(tenantDir, "lodging.calik-energi.json"), "Lodging", "FullAddress");
        LoadJson(set, Path.Combine(tenantDir, "lodging.json"), "Lodging", "FullAddress");
        LoadJson(set, Path.Combine(tenantDir, "hotel.calik-energi.json"), "Hotel", "Name");
        LoadJson(set, Path.Combine(tenantDir, "hotel.json"), "Hotel", "Name");
        LoadJson(set, Path.Combine(tenantDir, "hospital.calik-energi.json"), "Hospital", "Name");
        LoadJson(set, Path.Combine(tenantDir, "hospital.json"), "Hospital", "Name");
        LoadJson(set, Path.Combine(tenantDir, "other-site.calik-energi.json"), "OtherSite", "FullAddress");
        LoadJson(set, Path.Combine(tenantDir, "other-site.json"), "OtherSite", "FullAddress");
        return set;
    }

    private static void LoadJson(
        HashSet<(string Catalog, string City, string Scalar)> set,
        string path,
        string catalog,
        string scalarProperty)
    {
        if (!File.Exists(path))
            return;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            return;

        foreach (var row in rows.EnumerateArray())
        {
            var city = row.TryGetProperty("City", out var c) ? c.GetString()?.Trim() ?? "" : "";
            var scalar = row.TryGetProperty(scalarProperty, out var s) ? s.GetString()?.Trim() ?? "" : "";
            if (string.IsNullOrWhiteSpace(scalar))
                continue;
            set.Add((catalog, city, scalar));
        }
    }

    private sealed class CatalogTupleComparer : IEqualityComparer<(string Catalog, string City, string Scalar)>
    {
        public static CatalogTupleComparer Instance { get; } = new();

        public bool Equals((string Catalog, string City, string Scalar) x, (string Catalog, string City, string Scalar) y) =>
            string.Equals(x.Catalog, y.Catalog, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.City, y.City, StringComparison.OrdinalIgnoreCase)
            && Visa2014CatalogMatchHelper.KeysEqual(x.Scalar, y.Scalar);

        public int GetHashCode((string Catalog, string City, string Scalar) obj) =>
            HashCode.Combine(
                obj.Catalog.ToLowerInvariant(),
                obj.City.ToLowerInvariant(),
                Visa2014CatalogMatchHelper.NormalizeKey(obj.Scalar));
    }

    private static int ReadUsage(object? raw) => raw switch
    {
        int i => i,
        long l => (int)l,
        string s when int.TryParse(s, out var n) => n,
        _ => 1,
    };

    private static Dictionary<string, object?> Skip(string addressLine, int usage, string reason) =>
        new(StringComparer.Ordinal)
        {
            ["reason"] = reason,
            ["_legacy_AddressLine"] = addressLine,
            ["UsageCount"] = usage,
        };

    private static Dictionary<string, object?> Unmapped(string catalog, string legacy, string reason) =>
        new(StringComparer.Ordinal)
        {
            ["catalog"] = catalog,
            ["legacyValue"] = legacy,
            ["reason"] = reason,
        };
}