using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Microsoft.Data.SqlClient;

internal static class AorGapProgram
{
    private const string XlsxPath = @"c:\Users\webap\Documents\GitHub\Visa2026\Visa2026.DataImporter\legacy\visa2014\preview-export\AddressOfResidence-preview.calik-energi.xlsx";
    private const string Conn = "Server=localhost\\SQLEXPRESS;Database=VISA2015;User Id=ReadOnlyUser;Password=159357;TrustServerCertificate=True";

    public static async Task Main()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var outDir = Path.GetDirectoryName(typeof(AorGapProgram).Assembly.Location)!;
        outDir = Path.GetFullPath(Path.Combine(outDir, "..", "..", ".."));

        var meta = ReadSheet(XlsxPath, "_Meta");
        var skipped = ReadSheet(XlsxPath, "_Skipped");
        var unmapped = ReadSheet(XlsxPath, "_UnmappedLookups");
        var importRows = ReadSheet(XlsxPath, "AddressOfResidence");

        var skipAnalysis = AnalyzeSkipped(skipped);
        var unmappedAnalysis = AnalyzeUnmapped(unmapped, skipped);
        var importTypeCounts = importRows
            .GroupBy(r => Get(r, "Type") ?? "(null)")
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        var skippedOids = skipped
            .Select(r => Get(r, "_legacyRowId"))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => Guid.Parse(s!))
            .ToList();

        var sqlEnrichment = await EnrichFromSqlAsync(skippedOids);

        var report = new
        {
            meta = meta.ToDictionary(r => Get(r, "_key") ?? "", r => Get(r, "value")),
            skipReasonCategories = skipAnalysis.Categories,
            skipReasonTopValues = skipAnalysis.TopByCategory,
            unmappedByCatalog = unmappedAnalysis.ByCatalog,
            unmappedTopLegacyValues = unmappedAnalysis.TopWithCounts,
            importTypeCounts,
            skippedTypeBreakdown = sqlEnrichment.TypeCounts,
            skippedFkPatterns = sqlEnrichment.FkPatterns,
            skippedAddressPrefixPatterns = sqlEnrichment.PrefixPatterns,
            skippedRegionOnlyTop = sqlEnrichment.RegionOnlyExamples.Select(x => new { value = x.Value, count = x.Count }).Take(15),
            skippedCityOnlyTop = sqlEnrichment.CityOnlyExamples.Select(x => new { value = x.Value, count = x.Count }).Take(15),
            skippedBothTop = sqlEnrichment.BothExamples.Select(x => new { value = x.Value, count = x.Count }).Take(15),
            allRowFkCoverage = sqlEnrichment.AllRowFkCoverage,
        };

        var jsonPath = Path.Combine(outDir, "analysis.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        PrintReport(report, skipAnalysis, unmappedAnalysis, sqlEnrichment, importTypeCounts, meta);
        Console.WriteLine($"Wrote {jsonPath}");
    }

    private static void PrintReport(
        object report,
        SkipAnalysis skipAnalysis,
        UnmappedAnalysis unmappedAnalysis,
        SqlEnrichment sql,
        Dictionary<string, int> importTypes,
        List<Dictionary<string, string?>> meta)
    {
        Console.WriteLine("=== AddressOfResidence Preview Analysis (calik-energi) ===");
        foreach (var row in meta)
            Console.WriteLine($"  {Get(row, "_key")}: {Get(row, "value")}");

        Console.WriteLine("\n--- Skip reason categories ---");
        foreach (var kv in skipAnalysis.Categories.OrderByDescending(x => x.Value))
            Console.WriteLine($"  {kv.Key}: {kv.Value}");

        Console.WriteLine("\n--- Top skip values (Region-only) ---");
        PrintTop(skipAnalysis.TopByCategory.GetValueOrDefault("RegionOnly") ?? [], 15);

        Console.WriteLine("\n--- Top skip values (City-only) ---");
        PrintTop(skipAnalysis.TopByCategory.GetValueOrDefault("CityOnly") ?? [], 15);

        Console.WriteLine("\n--- Top skip values (Both) ---");
        PrintTop(skipAnalysis.TopByCategory.GetValueOrDefault("Both") ?? [], 10);

        Console.WriteLine("\n--- Unmapped lookups by catalog ---");
        foreach (var kv in unmappedAnalysis.ByCatalog.OrderByDescending(x => x.Value))
            Console.WriteLine($"  {kv.Key}: {kv.Value} distinct");

        Console.WriteLine("\n--- Unmapped Region (top by skipped row hits) ---");
        PrintTop(unmappedAnalysis.TopWithCounts.GetValueOrDefault("Region") ?? [], 15);

        Console.WriteLine("\n--- Unmapped City (top by skipped row hits) ---");
        PrintTop(unmappedAnalysis.TopWithCounts.GetValueOrDefault("City") ?? [], 15);

        Console.WriteLine("\n--- Import row Type breakdown ---");
        foreach (var kv in importTypes.OrderByDescending(x => x.Value))
            Console.WriteLine($"  {kv.Key}: {kv.Value}");

        Console.WriteLine("\n--- Skipped row Type breakdown (from SQL) ---");
        foreach (var kv in sql.TypeCounts.OrderByDescending(x => x.Value))
            Console.WriteLine($"  {kv.Key}: {kv.Value}");

        Console.WriteLine("\n--- Skipped FK patterns ---");
        foreach (var kv in sql.FkPatterns.OrderByDescending(x => x.Value))
            Console.WriteLine($"  {kv.Key}: {kv.Value}");

        Console.WriteLine("\n--- Skipped address prefix patterns ---");
        foreach (var kv in sql.PrefixPatterns.OrderByDescending(x => x.Value))
            Console.WriteLine($"  {kv.Key}: {kv.Value}");

        Console.WriteLine("\n--- Skipped City-only examples (region resolved, city missing) ---");
        PrintTop(sql.CityOnlyExamples, 15);

        Console.WriteLine("\n--- Skipped Both-fail examples ---");
        PrintTop(sql.BothExamples, 10);

        Console.WriteLine("\n--- Legacy row FK coverage (all AddressOfResidence) ---");
        foreach (var kv in sql.AllRowFkCoverage.OrderByDescending(x => x.Value))
            Console.WriteLine($"  {kv.Key}: {kv.Value}");
    }

    private static void PrintTop(List<(string Value, int Count)> items, int max)
    {
        foreach (var (value, count) in items.Take(max))
            Console.WriteLine($"  {count,5}  {Truncate(value, 80)}");
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private sealed record SkipAnalysis(
        Dictionary<string, int> Categories,
        Dictionary<string, List<(string Value, int Count)>> TopByCategory);

    private static SkipAnalysis AnalyzeSkipped(List<Dictionary<string, string?>> rows)
    {
        var categories = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["RegionOnly"] = 0,
            ["CityOnly"] = 0,
            ["Both"] = 0,
        };

        var regionOnlyCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var cityOnlyCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var bothCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var reason = Get(row, "reason") ?? "";
            var hasRegion = reason.Contains("Region:", StringComparison.Ordinal);
            var hasCity = reason.Contains("City:", StringComparison.Ordinal);

            string cat;
            Dictionary<string, int> bucket;
            if (hasRegion && hasCity)
            {
                cat = "Both";
                bucket = bothCounts;
                categories["Both"]++;
            }
            else if (hasRegion)
            {
                cat = "RegionOnly";
                bucket = regionOnlyCounts;
                categories["RegionOnly"]++;
            }
            else if (hasCity)
            {
                cat = "CityOnly";
                bucket = cityOnlyCounts;
                categories["CityOnly"]++;
            }
            else
            {
                cat = "Unknown";
                bucket = regionOnlyCounts;
                categories.TryAdd("Unknown", 0);
                categories["Unknown"]++;
            }

            var key = ExtractPrimaryFailureKey(reason, cat);
            bucket[key] = bucket.GetValueOrDefault(key) + 1;
            _ = cat;
        }

        return new SkipAnalysis(categories, new Dictionary<string, List<(string, int)>>
        {
            ["RegionOnly"] = TopList(regionOnlyCounts),
            ["CityOnly"] = TopList(cityOnlyCounts),
            ["Both"] = TopList(bothCounts),
        });
    }

    private static string ExtractPrimaryFailureKey(string reason, string cat)
    {
        if (cat == "RegionOnly")
            return ExtractAfterPrefix(reason, "Region:");
        if (cat == "CityOnly")
            return ExtractAfterPrefix(reason, "City:");
        if (cat == "Both")
        {
            var region = ExtractAfterPrefix(reason, "Region:");
            var city = ExtractAfterPrefix(SplitCityPart(reason), "City:");
            return $"Region:{region}; City:{city}";
        }
        return reason;
    }

    private static string SplitCityPart(string reason)
    {
        var idx = reason.IndexOf("City:", StringComparison.Ordinal);
        return idx >= 0 ? reason[idx..] : reason;
    }

    private static string ExtractAfterPrefix(string reason, string prefix)
    {
        var idx = reason.IndexOf(prefix, StringComparison.Ordinal);
        if (idx < 0) return reason;
        var rest = reason[(idx + prefix.Length)..];
        var semi = rest.IndexOf(';', StringComparison.Ordinal);
        return semi >= 0 ? rest[..semi].Trim() : rest.Trim();
    }

    private static List<(string, int)> TopList(Dictionary<string, int> counts) =>
        counts.OrderByDescending(x => x.Value).Select(x => (x.Key, x.Value)).ToList();

    private sealed record UnmappedAnalysis(
        Dictionary<string, int> ByCatalog,
        Dictionary<string, List<(string Value, int Count)>> TopWithCounts);

    private static UnmappedAnalysis AnalyzeUnmapped(
        List<Dictionary<string, string?>> unmappedRows,
        List<Dictionary<string, string?>> skippedRows)
    {
        var byCatalog = unmappedRows
            .GroupBy(r => Get(r, "catalog") ?? "(null)")
            .ToDictionary(g => g.Key, g => g.Count());

        var hitCounts = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal)
        {
            ["Region"] = new(StringComparer.Ordinal),
            ["City"] = new(StringComparer.Ordinal),
        };

        foreach (var row in skippedRows)
        {
            var reason = Get(row, "reason") ?? "";
            foreach (Match m in Regex.Matches(reason, @"(Region|City):([^;]+)"))
            {
                var catalog = m.Groups[1].Value;
                var value = m.Groups[2].Value.Trim();
                if (hitCounts.TryGetValue(catalog, out var dict))
                    dict[value] = dict.GetValueOrDefault(value) + 1;
            }
        }

        var topWithCounts = hitCounts.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.OrderByDescending(x => x.Value).Select(x => (x.Key, x.Value)).ToList());

        return new UnmappedAnalysis(byCatalog, topWithCounts);
    }

    private sealed record SqlEnrichment(
        Dictionary<string, int> TypeCounts,
        Dictionary<string, int> FkPatterns,
        Dictionary<string, int> PrefixPatterns,
        List<(string Value, int Count)> RegionOnlyExamples,
        List<(string Value, int Count)> CityOnlyExamples,
        List<(string Value, int Count)> BothExamples,
        Dictionary<string, int> AllRowFkCoverage);

    private static async Task<SqlEnrichment> EnrichFromSqlAsync(List<Guid> skippedOids)
    {
        var allFkCoverage = await LoadAllFkCoverageAsync();

        if (skippedOids.Count == 0)
            return new([], [], [], [], [], [], allFkCoverage);

        var oidToReason = ReadSheet(XlsxPath, "_Skipped")
            .Where(r => Guid.TryParse(Get(r, "_legacyRowId"), out _))
            .ToDictionary(
                r => Guid.Parse(Get(r, "_legacyRowId")!),
                r => Get(r, "reason") ?? "",
                EqualityComparer<Guid>.Default);

        const string SeherEtrap = "\u015E\u00E4herEtrap";
        var sql = $"""
            SELECT
                CAST(aor.Oid AS varchar(36)) AS Oid,
                doa.TypeOfDocument,
                CASE WHEN a.Region IS NULL THEN 0 ELSE 1 END AS HasRegionFk,
                ISNULL(r.mgCode, '') AS RegionMgCode,
                r.NameOfRegion AS RegionName,
                CASE WHEN a.[{SeherEtrap}] IS NULL THEN 0 ELSE 1 END AS HasCityFk,
                ISNULL(se.mgCode, '') AS CityMgCode,
                se.[{SeherEtrap}L] AS CityName,
                a.AddressLine
            FROM dbo.AddressOfResidence aor
            INNER JOIN dbo.Address a ON aor.Address = a.Oid
            LEFT JOIN dbo.Region r ON a.Region = r.Oid
            LEFT JOIN dbo.[{SeherEtrap}] se ON a.[{SeherEtrap}] = se.Oid
            LEFT JOIN dbo.DocumentOfAddress doa ON a.DocumentOfAddress = doa.Oid
            WHERE aor.Oid IN ({string.Join(",", skippedOids.Select((_, i) => $"@p{i}"))})
            """;

        var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fkPatterns = new Dictionary<string, int>(StringComparer.Ordinal);
        var prefixPatterns = new Dictionary<string, int>(StringComparer.Ordinal);
        var regionOnly = new Dictionary<string, int>(StringComparer.Ordinal);
        var cityOnly = new Dictionary<string, int>(StringComparer.Ordinal);
        var both = new Dictionary<string, int>(StringComparer.Ordinal);

        await using var conn = new SqlConnection(Conn);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        for (var i = 0; i < skippedOids.Count; i++)
            cmd.Parameters.AddWithValue($"@p{i}", skippedOids[i]);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var oid = Guid.Parse(reader.GetString(0));
            var docType = reader.IsDBNull(1) ? "(null)" : reader.GetString(1).Trim();
            var hasRegionFk = reader.GetInt32(2) == 1;
            var regionMg = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();
            var regionName = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim();
            var hasCityFk = reader.GetInt32(5) == 1;
            var cityMg = reader.IsDBNull(6) ? "" : reader.GetString(6).Trim();
            var cityName = reader.IsDBNull(7) ? "" : reader.GetString(7).Trim();
            var addressLine = reader.IsDBNull(8) ? "" : reader.GetString(8).Trim();

            typeCounts[docType] = typeCounts.GetValueOrDefault(docType) + 1;

            var fkKey = $"RegionFK={(hasRegionFk ? "Y" : "N")}, CityFK={(hasCityFk ? "Y" : "N")}, RegionMg={(string.IsNullOrEmpty(regionMg) ? "empty" : "set")}, CityMg={(string.IsNullOrEmpty(cityMg) ? "empty" : "set")}";
            fkPatterns[fkKey] = fkPatterns.GetValueOrDefault(fkKey) + 1;

            var prefix = InferAddressPrefix(addressLine);
            prefixPatterns[prefix] = prefixPatterns.GetValueOrDefault(prefix) + 1;

            var reason = oidToReason.GetValueOrDefault(oid, "");
            var hasRegionFail = reason.Contains("Region:", StringComparison.Ordinal);
            var hasCityFail = reason.Contains("City:", StringComparison.Ordinal);
            var example = BuildSqlExample(docType, regionMg, regionName, cityMg, cityName, addressLine);

            if (hasRegionFail && hasCityFail)
                both[example] = both.GetValueOrDefault(example) + 1;
            else if (hasRegionFail)
                regionOnly[example] = regionOnly.GetValueOrDefault(example) + 1;
            else if (hasCityFail)
                cityOnly[example] = cityOnly.GetValueOrDefault(example) + 1;
        }

        return new SqlEnrichment(
            typeCounts,
            fkPatterns,
            prefixPatterns,
            TopList(regionOnly),
            TopList(cityOnly),
            TopList(both),
            allFkCoverage);
    }

    private static async Task<Dictionary<string, int>> LoadAllFkCoverageAsync()
    {
        const string SeherEtrap = "\u015E\u00E4herEtrap";
        const string sql = $"""
            SELECT
                CASE
                    WHEN a.Region IS NOT NULL AND a.[{SeherEtrap}] IS NOT NULL THEN 'Both FK set'
                    WHEN a.Region IS NOT NULL THEN 'Region FK only'
                    WHEN a.[{SeherEtrap}] IS NOT NULL THEN 'City FK only'
                    ELSE 'Neither FK'
                END AS fkPattern,
                COUNT(*) AS cnt
            FROM dbo.AddressOfResidence aor
            INNER JOIN dbo.Person p ON aor.Person = p.Oid AND p.GCRecord IS NULL
            INNER JOIN dbo.Address a ON aor.Address = a.Oid AND a.GCRecord IS NULL
            WHERE aor.GCRecord IS NULL
            GROUP BY
                CASE
                    WHEN a.Region IS NOT NULL AND a.[{SeherEtrap}] IS NOT NULL THEN 'Both FK set'
                    WHEN a.Region IS NOT NULL THEN 'Region FK only'
                    WHEN a.[{SeherEtrap}] IS NOT NULL THEN 'City FK only'
                    ELSE 'Neither FK'
                END
            """;

        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        await using var conn = new SqlConnection(Conn);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result[reader.GetString(0)] = reader.GetInt32(1);
        return result;
    }

    private static string BuildSqlExample(string docType, string regionMg, string regionName, string cityMg, string cityName, string addressLine)
    {
        var parts = new List<string> { $"type={docType}" };
        if (!string.IsNullOrEmpty(regionMg) || !string.IsNullOrEmpty(regionName))
            parts.Add($"region={regionMg}/{regionName}");
        if (!string.IsNullOrEmpty(cityMg) || !string.IsNullOrEmpty(cityName))
            parts.Add($"city={cityMg}/{cityName}");
        parts.Add($"addr={Truncate(addressLine, 60)}");
        return string.Join(" | ", parts);
    }

    private static string InferAddressPrefix(string addressLine)
    {
        if (string.IsNullOrWhiteSpace(addressLine)) return "(empty)";
        var line = addressLine.Trim();
        var lower = line.ToLowerInvariant();
        if (lower.StartsWith("s. asgabat") || lower.StartsWith("s.asgabat")) return "s.Asgabat…";
        if (lower.StartsWith("balkan")) return "Balkan…";
        if (lower.StartsWith("mary")) return "Mary…";
        if (lower.StartsWith("ahal")) return "Ahal…";
        if (lower.StartsWith("lebap")) return "Lebap…";
        if (lower.StartsWith("dasoguz") || lower.StartsWith("dashoguz")) return "Dasoguz…";
        if (lower.StartsWith("asgabat")) return "Asgabat…";
        if (lower.Contains("myhmanhan")) return "contains myhmanhan…";
        if (lower.StartsWith("s.")) return "s.(other settlement)…";
        return "other/unrecognized prefix";
    }

    private static List<Dictionary<string, string?>> ReadSheet(string path, string sheetName)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        do
        {
            if (!string.Equals(reader.Name, sheetName, StringComparison.OrdinalIgnoreCase))
                continue;

            var rows = new List<Dictionary<string, string?>>();
            var headers = new List<string>();
            var isHeader = true;

            while (reader.Read())
            {
                if (isHeader)
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                        headers.Add(reader.GetValue(i)?.ToString()?.Trim() ?? $"col{i}");
                    isHeader = false;
                    continue;
                }

                var row = new Dictionary<string, string?>(StringComparer.Ordinal);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var key = i < headers.Count ? headers[i] : $"col{i}";
                    row[key] = reader.GetValue(i)?.ToString();
                }
                rows.Add(row);
            }
            return rows;
        } while (reader.NextResult());

        throw new InvalidOperationException($"Sheet '{sheetName}' not found in {path}");
    }

    private static string? Get(Dictionary<string, string?> row, string key) =>
        row.TryGetValue(key, out var v) ? v : null;
}
