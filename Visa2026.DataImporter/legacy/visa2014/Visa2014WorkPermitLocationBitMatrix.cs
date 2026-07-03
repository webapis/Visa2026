namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014WorkPermitLocationBitMatrix
{
    private const string CommaSeparatedNoneValue = "";

    internal static IReadOnlyList<string> LoadBitColumnNames(string legacyConnectionString)
    {
        const string sql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = 'WorkPermitLocation'
              AND DATA_TYPE = 'bit'
            ORDER BY ORDINAL_POSITION
            """;

        var rows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        return rows
            .Select(r => r.GetValueOrDefault("COLUMN_NAME"))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .ToList();
    }

    internal static Dictionary<Guid, IReadOnlyDictionary<string, string?>> LoadLocationRows(
        string legacyConnectionString,
        IEnumerable<Guid> locationOids,
        bool verbose)
    {
        var oidList = locationOids.Distinct().Where(o => o != Guid.Empty).ToList();
        if (oidList.Count == 0)
            return new Dictionary<Guid, IReadOnlyDictionary<string, string?>>();

        var inClause = string.Join(",", oidList.Select(o => $"'{o:D}'"));
        var sql = $"""
            SELECT *
            FROM dbo.WorkPermitLocation
            WHERE Oid IN ({inClause})
              AND GCRecord IS NULL
            """;

        if (verbose)
            Console.WriteLine($"INF Loading {oidList.Count} WorkPermitLocation row(s)...");

        var dictRows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        var map = new Dictionary<Guid, IReadOnlyDictionary<string, string?>>();
        foreach (var row in dictRows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("Oid"), out var oid))
                continue;
            map[oid] = row;
        }

        return map;
    }

    internal static string BuildWorkPermittedLocations(
        IReadOnlyDictionary<string, string?>? locationRow,
        IReadOnlyList<string> bitColumnNames,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        ICollection<string>? unmappedCollector)
    {
        if (locationRow == null)
            return CommaSeparatedNoneValue;

        catalogs.TryGetValue("WorkPermittedLocationName", out var catalog);
        var bitToTarget = catalog?.LegacyToTarget ?? new Dictionary<string, string>(StringComparer.Ordinal);

        var labels = new List<string>();
        foreach (var column in bitColumnNames)
        {
            if (!IsBitSet(locationRow, column))
                continue;

            if (TryResolveBitTarget(column, bitToTarget, out var target))
            {
                labels.Add(target);
                continue;
            }

            var heuristic = Visa2014WorkPermitLocationLabelHeuristic.FromColumnName(column);
            if (string.IsNullOrWhiteSpace(heuristic))
            {
                unmappedCollector?.Add($"WorkPermittedLocationName:{column}");
                continue;
            }

            labels.Add(heuristic);
        }

        return labels.Count == 0 ? CommaSeparatedNoneValue : string.Join(", ", labels);
    }

    private static bool IsBitSet(IReadOnlyDictionary<string, string?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value))
            return false;

        return value is "1" or "True" or "true";
    }

    private static bool TryResolveBitTarget(
        string bitKey,
        IReadOnlyDictionary<string, string> bitToTarget,
        out string target)
    {
        if (bitToTarget.TryGetValue(bitKey, out var exact) && !string.IsNullOrWhiteSpace(exact))
        {
            target = exact;
            return true;
        }

        foreach (var (legacy, mapped) in bitToTarget)
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(legacy, bitKey))
            {
                target = mapped;
                return true;
            }
        }

        target = bitKey;
        return false;
    }
}

internal static class Visa2014WorkPermitLocationLabelHeuristic
{
    private static readonly string[] CitySuffixes =
    [
        "Seheri",
        "S\u00E4heri",
        "\u015Eeheri",
        "\u015E\u00E4heri",
    ];

    internal static string? FromColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return null;

        var name = columnName.Trim();
        foreach (var suffix in CitySuffixes)
        {
            if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var prefix = name[..^suffix.Length];
            return $"{NormalizePrefix(prefix)} \u015F\u00E4heri";
        }

        if (name.EndsWith("Etraby", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = name[..^6];
            return $"{NormalizePrefix(prefix)} etraby";
        }

        return NormalizePrefix(name);
    }

    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return prefix;

        return prefix
            .Replace("Asgabat", "A\u015Fgabat", StringComparison.OrdinalIgnoreCase)
            .Replace("Turkmenabat", "T\u00FCrkmenabat", StringComparison.OrdinalIgnoreCase)
            .Replace("Turkmenbasy", "T\u00FCrkmenba\u015fy", StringComparison.OrdinalIgnoreCase)
            .Replace("Akbugday", "Akbugda\u00FD", StringComparison.OrdinalIgnoreCase)
            .Replace("Sakaradge", "Sakar\u00E7\u00E4ge", StringComparison.OrdinalIgnoreCase)
            .Replace("Garabogaz", "Garabogaz", StringComparison.OrdinalIgnoreCase)
            .Replace("Serhetabat", "Serhetabat", StringComparison.OrdinalIgnoreCase)
            .Replace("Dasoguz", "Da\u015Foguz", StringComparison.OrdinalIgnoreCase)
            .Replace("Gokdepe", "G\u00F6kdepe", StringComparison.OrdinalIgnoreCase)
            .Replace("Mary", "Mary", StringComparison.OrdinalIgnoreCase)
            .Replace("Ruhabat", "Ruhabat", StringComparison.OrdinalIgnoreCase)
            .Replace("Balkanabat", "Balkanabat", StringComparison.OrdinalIgnoreCase)
            .Replace("Serdar", "Serdar", StringComparison.OrdinalIgnoreCase)
            .Replace("Sarahs", "Sarahs", StringComparison.OrdinalIgnoreCase)
            .Replace("Gumdag", "Gumdag", StringComparison.OrdinalIgnoreCase)
            .Replace("Anew", "\u00C4new", StringComparison.OrdinalIgnoreCase)
            .Replace("Seydi", "Se\u00FDdi", StringComparison.OrdinalIgnoreCase)
            .Replace("Yoloten", "\u00DDol\u00F6ten", StringComparison.OrdinalIgnoreCase)
            .Replace("BeyikSaparmyratTurkmenbasyAd", "Be\u00FDikSaparmyratT\u00FCrkmenba\u015fyAd", StringComparison.OrdinalIgnoreCase)
            .Replace("Turkmengala", "T\u00FCrkmengala", StringComparison.OrdinalIgnoreCase)
            .Replace("Dowletli", "D\u00F6wletli", StringComparison.OrdinalIgnoreCase)
            .Replace("Tagtabazar", "Tagtabazar", StringComparison.OrdinalIgnoreCase)
            .Replace("Murgap", "Murgap", StringComparison.OrdinalIgnoreCase)
            .Replace("Farap", "Farap", StringComparison.OrdinalIgnoreCase)
            .Replace("Atamyrat", "Atamyrat", StringComparison.OrdinalIgnoreCase)
            .Replace("Serdarabat", "Serdarabat", StringComparison.OrdinalIgnoreCase);
    }
}