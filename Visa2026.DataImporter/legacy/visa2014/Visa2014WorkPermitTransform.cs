namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014WorkPermitRawRow(
    Guid LegacyOid,
    string? WorkPermitNumber,
    DateTime? IssuedDate,
    string SourceTable,
    Guid? LegacyApplicationProfileInstanceOid);

internal static class Visa2014WorkPermitTransform
{
    /// <summary>
    /// Newly issued headers: Application via WorkPermit.ProcessNumber → PersonInApplication → Application
    /// (majority Application per letter). Existing PIA.WorkPermit is NOT used for header FK.
    /// </summary>
    internal const string ExtractSql = """
        SELECT
            CAST(wpl.Oid AS varchar(36)) AS Oid,
            wpl.Number AS WorkPermitNumber,
            CONVERT(varchar(10), wpl.Date, 23) AS IssuedDate,
            'WorkPermitLetter' AS SourceTable,
            CAST(app.ApplicationOid AS varchar(36)) AS ApplicationProfileInstanceOid
        FROM dbo.WorkPermitLetter wpl
        OUTER APPLY (
            SELECT TOP 1 pia.Application AS ApplicationOid
            FROM dbo.WorkPermit wp
            INNER JOIN dbo.PersonInApplication pia
                ON pia.Oid = wp.ProcessNumber
               AND pia.GCRecord IS NULL
            WHERE wp.WorkPermitLetter = wpl.Oid
              AND wp.GCRecord IS NULL
              AND wp.ProcessNumber IS NOT NULL
              AND pia.Application IS NOT NULL
            GROUP BY pia.Application
            ORDER BY COUNT(*) DESC, pia.Application
        ) app
        WHERE wpl.GCRecord IS NULL

        UNION ALL

        SELECT
            CAST(wp.Oid AS varchar(36)) AS Oid,
            wp.AppruvalNumber AS WorkPermitNumber,
            CONVERT(varchar(10), wp.StartDateOfWorkPermit, 23) AS IssuedDate,
            'WorkPermit' AS SourceTable,
            CAST(pia.Application AS varchar(36)) AS ApplicationProfileInstanceOid
        FROM dbo.WorkPermit wp
        LEFT JOIN dbo.PersonInApplication pia
            ON pia.Oid = wp.ProcessNumber
           AND pia.GCRecord IS NULL
        WHERE wp.GCRecord IS NULL
          AND wp.WorkPermitLetter IS NULL
        """;

    internal static readonly string[] WorkPermitMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_dedupeGroupId", "_importAction",
        "WorkPermitNumber", "IssuedDate", "Application",
        "_legacy_SourceTable", "_legacy_ApplicationProfileInstanceOid",
    ];

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        _ = lookupTranslationPaths;
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows}) * FROM ({ExtractSql}) AS q ORDER BY IssuedDate, Oid"
            : $"{ExtractSql} ORDER BY IssuedDate, Oid";

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014WorkPermitRawRow>();
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

        return TransformRows(rawRows, out var skipped, out var dedupeSummary);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014WorkPermitRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        var sourceTable = row.GetValueOrDefault("SourceTable") ?? "WorkPermitLetter";
        DateTime? issuedDate = DateTime.TryParse(row.GetValueOrDefault("IssuedDate"), out var issued)
            ? issued
            : null;

        Guid? applicationOid = null;
        if (Guid.TryParse(row.GetValueOrDefault("ApplicationProfileInstanceOid")?.Trim(), out var appOid))
            applicationOid = appOid;

        parsed = new Visa2014WorkPermitRawRow(
            LegacyOid: legacyOid,
            WorkPermitNumber: row.GetValueOrDefault("WorkPermitNumber"),
            IssuedDate: issuedDate,
            SourceTable: sourceTable.Trim(),
            LegacyApplicationProfileInstanceOid: applicationOid);
        return true;
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014WorkPermitRawRow> rawRows,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var working = rawRows.Select(r => new WorkingRow(r)).ToList();
        ApplyWorkPermitNumberSuffix(working, dedupeSummary);

        var importRows = new List<Dictionary<string, object?>>();
        foreach (var row in working)
        {
            var export = BuildExportRow(row, out var skipReason);
            if (skipReason != null)
            {
                export["_skipReason"] = skipReason;
                skipped.Add(export);
                continue;
            }

            importRows.Add(export);
        }

        return new Visa2014PersonImportBatch
        {
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = [],
            DedupeSummary = dedupeSummary,
            LegacyRowCount = rawRows.Count,
            DedupeMergedCount = 0,
        };
    }

    private sealed class WorkingRow(Visa2014WorkPermitRawRow Raw)
    {
        public Visa2014WorkPermitRawRow Raw { get; } = Raw;
        public string? DedupeGroupId { get; set; }
        public string? ResolvedWorkPermitNumber { get; set; }
    }

    private static void ApplyWorkPermitNumberSuffix(
        List<WorkingRow> rows,
        List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new { Row = r, Norm = NormalizeWorkPermitNumber(r.Raw.WorkPermitNumber) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Norm))
            .GroupBy(x => x.Norm.ToUpperInvariant(), StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var groupId = $"WPN:{group.Key}";
            foreach (var member in members)
            {
                member.Row.DedupeGroupId = groupId;
                member.Row.ResolvedWorkPermitNumber = AppendLegacyOidTail(
                    member.Norm,
                    member.Row.Raw.LegacyOid);
            }

            dedupeSummary.Add(new Dictionary<string, object?>
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "WorkPermitNumber",
                ["normalizedValue"] = group.Key,
                ["memberCount"] = members.Count,
                ["canonicalRule"] = "suffix_all_with_legacy_oid_tail",
            });
        }
    }

    private static Dictionary<string, object?> BuildExportRow(WorkingRow working, out string? skipReason)
    {
        skipReason = null;
        var raw = working.Raw;
        var legacyTable = string.Equals(raw.SourceTable, "WorkPermit", StringComparison.OrdinalIgnoreCase)
            ? "WorkPermit"
            : "WorkPermitLetter";

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = legacyTable,
            ["_dedupeGroupId"] = working.DedupeGroupId ?? "",
            ["_importAction"] = "import",
            ["_legacy_SourceTable"] = raw.SourceTable,
            ["_legacy_ApplicationProfileInstanceOid"] = raw.LegacyApplicationProfileInstanceOid?.ToString("D"),
        };

        if (string.IsNullOrWhiteSpace(raw.WorkPermitNumber))
        {
            skipReason = "required_null:WorkPermitNumber";
            row["WorkPermitNumber"] = null;
            row["IssuedDate"] = null;
            row["Application"] = null;
            return row;
        }

        if (!raw.IssuedDate.HasValue)
        {
            skipReason = "required_null:IssuedDate";
            row["WorkPermitNumber"] = raw.WorkPermitNumber;
            row["IssuedDate"] = null;
            row["Application"] = null;
            return row;
        }

        var workPermitNumber = working.ResolvedWorkPermitNumber
            ?? NormalizeWorkPermitNumber(raw.WorkPermitNumber);
        row["WorkPermitNumber"] = workPermitNumber;
        row["IssuedDate"] = raw.IssuedDate.Value.ToString("yyyy-MM-dd");
        row["Application"] = raw.LegacyApplicationProfileInstanceOid?.ToString("D");
        return row;
    }

    private static string NormalizeWorkPermitNumber(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();

    private static string AppendLegacyOidTail(string workPermitNumber, Guid legacyOid) =>
        workPermitNumber + legacyOid.ToString("N")[^8..];
}