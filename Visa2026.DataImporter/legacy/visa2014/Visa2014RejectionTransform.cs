namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014RejectionRawRow(
    Guid LegacyOid,
    string? RejectedDocNumber,
    DateTime? IssuedDate,
    Guid? LegacyApplicationProfileInstanceOid);

internal static class Visa2014RejectionTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(ar.Oid AS varchar(36)) AS Oid,
            ar.Number AS RejectedDocNumber,
            CONVERT(varchar(10), ar.IssuedDate, 23) AS IssuedDate,
            CAST(ar.Application AS varchar(36)) AS ApplicationProfileInstanceOid
        FROM dbo.ApplicationResult ar
        WHERE ar.GCRecord IS NULL
          AND ar.Result = 1
        """;

    internal static readonly string[] RejectionMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_dedupeGroupId", "_importAction",
        "RejectedDocNumber", "Date", "Application", "Reason",
        "_legacy_ApplicationProfileInstanceOid",
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
        var rawRows = new List<Visa2014RejectionRawRow>();
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

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014RejectionRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        DateTime? issuedDate = DateTime.TryParse(row.GetValueOrDefault("IssuedDate"), out var issued)
            ? issued
            : null;

        parsed = new Visa2014RejectionRawRow(
            LegacyOid: legacyOid,
            RejectedDocNumber: row.GetValueOrDefault("RejectedDocNumber"),
            IssuedDate: issuedDate,
            LegacyApplicationProfileInstanceOid: TryParseGuid(row.GetValueOrDefault("ApplicationProfileInstanceOid")));
        return true;
    }

    private static Guid? TryParseGuid(string? text) =>
        Guid.TryParse(text?.Trim(), out var oid) ? oid : null;

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014RejectionRawRow> rawRows,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var working = rawRows.Select(r => new WorkingRow(r)).ToList();
        ApplyRejectedDocNumberSuffix(working, dedupeSummary);

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

    private sealed class WorkingRow(Visa2014RejectionRawRow Raw)
    {
        public Visa2014RejectionRawRow Raw { get; } = Raw;
        public string? DedupeGroupId { get; set; }
        public string? ResolvedRejectedDocNumber { get; set; }
    }

    private static void ApplyRejectedDocNumberSuffix(
        List<WorkingRow> rows,
        List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new { Row = r, Norm = NormalizeDocNumber(r.Raw.RejectedDocNumber) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Norm))
            .GroupBy(x => x.Norm.ToUpperInvariant(), StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var groupId = $"REJ:{group.Key}";
            foreach (var member in members)
            {
                member.Row.DedupeGroupId = groupId;
                member.Row.ResolvedRejectedDocNumber = AppendLegacyOidTail(
                    member.Norm,
                    member.Row.Raw.LegacyOid);
            }

            dedupeSummary.Add(new Dictionary<string, object?>
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "RejectedDocNumber",
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

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "ApplicationResult",
            ["_dedupeGroupId"] = working.DedupeGroupId ?? "",
            ["_importAction"] = "import",
            ["_legacy_ApplicationProfileInstanceOid"] = raw.LegacyApplicationProfileInstanceOid?.ToString("D"),
            ["Reason"] = null,
        };

        if (string.IsNullOrWhiteSpace(raw.RejectedDocNumber))
        {
            skipReason = "required_null:RejectedDocNumber";
            row["RejectedDocNumber"] = null;
            row["Date"] = null;
            row["Application"] = null;
            return row;
        }

        if (!raw.IssuedDate.HasValue)
        {
            skipReason = "required_null:IssuedDate";
            row["RejectedDocNumber"] = raw.RejectedDocNumber;
            row["Date"] = null;
            row["Application"] = null;
            return row;
        }

        if (!raw.LegacyApplicationProfileInstanceOid.HasValue)
        {
            skipReason = "required_null:Application";
            row["RejectedDocNumber"] = raw.RejectedDocNumber;
            row["Date"] = raw.IssuedDate.Value.ToString("yyyy-MM-dd");
            row["Application"] = null;
            return row;
        }

        row["RejectedDocNumber"] = working.ResolvedRejectedDocNumber
            ?? NormalizeDocNumber(raw.RejectedDocNumber);
        row["Date"] = raw.IssuedDate.Value.ToString("yyyy-MM-dd");
        row["Application"] = raw.LegacyApplicationProfileInstanceOid.Value.ToString("D");
        return row;
    }

    private static string NormalizeDocNumber(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();

    private static string AppendLegacyOidTail(string docNumber, Guid legacyOid) =>
        docNumber + legacyOid.ToString("N")[^8..];
}
