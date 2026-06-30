namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014EmployeePositionHistoryRawRow(
    Guid LegacyOid,
    Guid LegacyPersonOid,
    string? TitleOfPosition,
    string? PositionCode,
    string? TitleOfDepartment,
    DateTime? StartDateOnThisPosition,
    string? PersonMiddleName);

internal static class Visa2014EmployeePositionHistoryTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(w.Oid AS varchar(36)) AS Oid,
            CAST(w.Employee AS varchar(36)) AS LegacyPersonOid,
            pos.TitleOfPosition,
            ISNULL(CAST(pos.Code AS varchar(100)), '') AS PositionCode,
            dep.TitleOfDepartment,
            CONVERT(varchar(10), w.StartDateOnThisPosition, 23) AS StartDateOnThisPosition,
            p.MiddleName AS PersonMiddleName
        FROM dbo.WorkHistoryOfEmployee w
        INNER JOIN dbo.Person p ON w.Employee = p.Oid AND p.GCRecord IS NULL
        INNER JOIN dbo.Position pos ON w.Position = pos.Oid
        INNER JOIN dbo.Department dep ON w.Department = dep.Oid
        WHERE w.GCRecord IS NULL
        """;

    internal static readonly string[] EmployeePositionHistoryMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction",
        "Position", "ActualPosition", "Department", "StartDate", "EndDate", "Person",
        "_legacy_PositionTitle", "_legacy_DepartmentTitle", "_legacy_PositionCode",
        "_legacy_PersonMiddleName", "_legacy_PersonOid",
    ];

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows}) * FROM ({ExtractSql}) AS q"
            : ExtractSql;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014EmployeePositionHistoryRawRow>();
        int parseSkipped = 0;
        foreach (var dict in dictRows)
        {
            if (TryParseRawRow(dict, out var parsed))
                rawRows.Add(parsed);
            else
                parseSkipped++;
        }

        if (verbose && parseSkipped > 0)
            Console.WriteLine($"  Skipped {parseSkipped} sqlcmd row(s) with invalid shape.");

        return TransformRows(rawRows, catalogs, out var skipped, out var unmappedDistinct, out _);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014EmployeePositionHistoryRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        if (!row.TryGetValue("LegacyPersonOid", out var personText) ||
            !Guid.TryParse(personText?.Trim(), out var legacyPersonOid))
            return false;

        DateTime? startDate = DateTime.TryParse(row.GetValueOrDefault("StartDateOnThisPosition"), out var start)
            ? start
            : null;

        parsed = new Visa2014EmployeePositionHistoryRawRow(
            LegacyOid: legacyOid,
            LegacyPersonOid: legacyPersonOid,
            TitleOfPosition: row.GetValueOrDefault("TitleOfPosition"),
            PositionCode: row.GetValueOrDefault("PositionCode"),
            TitleOfDepartment: row.GetValueOrDefault("TitleOfDepartment"),
            StartDateOnThisPosition: startDate,
            PersonMiddleName: row.GetValueOrDefault("PersonMiddleName"));
        return true;
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014EmployeePositionHistoryRawRow> rawRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        unmappedDistinct = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);
        var endDates = DeriveEndDates(rawRows);
        var currentRowOids = DeriveCurrentRowOids(rawRows);
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var raw in rawRows)
        {
            var row = BuildExportRow(raw, catalogs, endDates, currentRowOids, out var skipReason, out var rowUnmapped);
            foreach (var key in rowUnmapped)
                unmappedSet.Add(key);

            if (skipReason != null)
            {
                row["_skipReason"] = skipReason;
                skipped.Add(row);
                continue;
            }

            importRows.Add(row);
        }

        foreach (var key in unmappedSet.OrderBy(k => k, StringComparer.Ordinal))
            unmappedDistinct.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["catalog"] = key.Split(':')[0],
                ["legacyValue"] = key.Contains(':') ? key[(key.IndexOf(':') + 1)..] : key,
                ["reason"] = key,
            });

        return new Visa2014PersonImportBatch
        {
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = unmappedDistinct,
            DedupeSummary = dedupeSummary,
            LegacyRowCount = rawRows.Count,
            DedupeMergedCount = 0,
        };
    }

    private static Dictionary<Guid, DateTime?> DeriveEndDates(IReadOnlyList<Visa2014EmployeePositionHistoryRawRow> rawRows)
    {
        var result = new Dictionary<Guid, DateTime?>();
        foreach (var group in rawRows
                     .Where(r => r.StartDateOnThisPosition.HasValue)
                     .GroupBy(r => r.LegacyPersonOid))
        {
            var ordered = group
                .OrderBy(r => r.StartDateOnThisPosition!.Value)
                .ThenBy(r => r.LegacyOid)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                DateTime? endDate = i + 1 < ordered.Count
                    ? ordered[i + 1].StartDateOnThisPosition
                    : null;
                result[ordered[i].LegacyOid] = endDate;
            }
        }

        return result;
    }

    /// <summary>
    /// The current/latest position-history row per person (last by StartDate asc, EndDate null).
    /// Legacy Person.MiddleName (free-text actual position) is applied to ActualPosition only here.
    /// </summary>
    private static HashSet<Guid> DeriveCurrentRowOids(IReadOnlyList<Visa2014EmployeePositionHistoryRawRow> rawRows)
    {
        var current = new HashSet<Guid>();
        foreach (var group in rawRows
                     .Where(r => r.StartDateOnThisPosition.HasValue)
                     .GroupBy(r => r.LegacyPersonOid))
        {
            var latest = group
                .OrderBy(r => r.StartDateOnThisPosition!.Value)
                .ThenBy(r => r.LegacyOid)
                .Last();
            current.Add(latest.LegacyOid);
        }

        return current;
    }

    private static Dictionary<string, object?> BuildExportRow(
        Visa2014EmployeePositionHistoryRawRow raw,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        IReadOnlyDictionary<Guid, DateTime?> endDates,
        IReadOnlySet<Guid> currentRowOids,
        out string? skipReason,
        out List<string> unmapped)
    {
        skipReason = null;
        unmapped = [];
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "WorkHistoryOfEmployee",
            ["_importAction"] = "import",
            ["_legacy_PositionTitle"] = raw.TitleOfPosition,
            ["_legacy_DepartmentTitle"] = raw.TitleOfDepartment,
            ["_legacy_PositionCode"] = raw.PositionCode,
            ["_legacy_PersonMiddleName"] = raw.PersonMiddleName,
            ["_legacy_PersonOid"] = raw.LegacyPersonOid.ToString("D"),
        };

        TrySetLookup(row, catalogs, "Position", raw.TitleOfPosition, "Position", unmapped, ref skipReason);
        TrySetLookup(row, catalogs, "Department", raw.TitleOfDepartment, "Department", unmapped, ref skipReason);

        // Actual (company) position: legacy Person.MiddleName held the free-text title (no dedicated field in
        // VISA2014). Apply it to the current/latest row only; older periods fall back to Position.Code or "-".
        var middleName = raw.PersonMiddleName?.Trim();
        row["ActualPosition"] = currentRowOids.Contains(raw.LegacyOid) && !string.IsNullOrEmpty(middleName)
            ? middleName
            : ResolveActualPosition(raw.PositionCode);

        if (!raw.StartDateOnThisPosition.HasValue)
        {
            skipReason ??= "required_null:StartDate";
            row["StartDate"] = null;
        }
        else
        {
            row["StartDate"] = raw.StartDateOnThisPosition.Value.ToString("yyyy-MM-dd");
            row["EndDate"] = endDates.TryGetValue(raw.LegacyOid, out var end) && end.HasValue
                ? end.Value.ToString("yyyy-MM-dd")
                : null;
        }

        row["Person"] = raw.LegacyPersonOid.ToString("D");

        return row;
    }

    private static string ResolveActualPosition(string? positionCode)
    {
        var trimmed = positionCode?.Trim();
        return string.IsNullOrEmpty(trimmed) ? "-" : trimmed;
    }

    private static void TrySetLookup(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string catalogName,
        string? legacyValue,
        string targetProperty,
        List<string> unmapped,
        ref string? skipReason)
    {
        if (string.IsNullOrWhiteSpace(legacyValue))
        {
            skipReason ??= $"required_null:{targetProperty}";
            row[targetProperty] = null;
            return;
        }

        if (Visa2014LookupTranslator.TryTranslate(catalogs, catalogName, legacyValue, out var target, out var reason))
        {
            row[targetProperty] = target;
            if (reason != null)
                unmapped.Add(reason);
            return;
        }

        unmapped.Add(reason ?? $"unmapped_lookup:{catalogName}:{legacyValue}");
        if (catalogs.TryGetValue(catalogName, out var catalog) &&
            string.Equals(catalog.UnmappedPolicy, "skip_row", StringComparison.OrdinalIgnoreCase))
            skipReason ??= reason ?? $"unmapped_lookup:{catalogName}:{legacyValue}";
    }
}
