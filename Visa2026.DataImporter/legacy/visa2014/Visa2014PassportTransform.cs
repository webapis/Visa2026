namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014PassportRawRow(
    Guid LegacyOid,
    string? PassportNumber,
    string? TypeOfPassportL,
    string? MgCode,
    DateTime? IssueDate,
    DateTime? ExpirationDate,
    string? Authority,
    string? LegacyIssuedCountry,
    Guid LegacyPersonOid,
    bool HasPassportCopy,
    int PassportCopyByteLength);

internal static class Visa2014PassportTransform
{
    private static readonly string[] SentinelPassportNumbers =
    [
        "AF000000000",
        "JL000000000",
    ];

    internal const string ExtractSql = """
        SELECT
            CAST(pp.Oid AS varchar(36)) AS Oid,
            pp.PassportNumber,
            pt.TypeOfPassportL,
            ISNULL(pt.mgCode, '') AS mgCode,
            CONVERT(varchar(10), pp.PassportIssuedDate, 23) AS PassportIssuedDate,
            CONVERT(varchar(10), pp.PassportExpiringDate, 23) AS PassportExpiringDate,
            pp.PassportIssuedPlace,
            ic.NameOfCountryL AS LegacyIssuedCountry,
            CAST(pp.Person AS varchar(36)) AS LegacyPersonOid,
            CASE WHEN EXISTS (
                SELECT 1 FROM dbo.PassportCopy pc
                WHERE pc.Passport = pp.Oid AND pc.GCRecord IS NULL
            ) THEN '1' ELSE '0' END AS HasPassportCopy,
            0 AS PassportCopyByteLength
        FROM dbo.Passport pp
        INNER JOIN dbo.Person p ON pp.Person = p.Oid AND p.GCRecord IS NULL
        LEFT JOIN dbo.PassportType pt ON pp.PassportType = pt.Oid
        LEFT JOIN dbo.Country ic ON pp.PassportIssuedCountry = ic.Oid
        WHERE pp.GCRecord IS NULL
        """;

    internal static readonly string[] PassportMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_dedupeGroupId", "_importAction",
        "_hasPassportCopy", "_passportCopyByteLength",
        "PassportNumber", "PassportType", "IssueDate", "ExpirationDate", "Authority",
        "IssuedCountry", "Person", "IsCancelled", "ShowOptionalFields",
        "_legacy_PassportTypeComposite", "_legacy_IssuedCountry", "_legacy_PersonOid",
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
        var rawRows = new List<Visa2014PassportRawRow>();
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

        var transformed = TransformRows(rawRows, catalogs, out var skipped, out var unmappedDistinct, out var dedupeSummary);
        return new Visa2014PersonImportBatch
        {
            ImportRows = transformed.ImportRows,
            Skipped = skipped,
            UnmappedLookups = unmappedDistinct,
            DedupeSummary = dedupeSummary,
            LegacyRowCount = rawRows.Count,
            DedupeMergedCount = transformed.DedupeMergedCount,
        };
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014PassportRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        if (!row.TryGetValue("LegacyPersonOid", out var personText) ||
            !Guid.TryParse(personText?.Trim(), out var legacyPersonOid))
            return false;

        parsed = new Visa2014PassportRawRow(
            LegacyOid: legacyOid,
            PassportNumber: row.GetValueOrDefault("PassportNumber"),
            TypeOfPassportL: row.GetValueOrDefault("TypeOfPassportL"),
            MgCode: row.GetValueOrDefault("MgCode"),
            IssueDate: DateTime.TryParse(row.GetValueOrDefault("PassportIssuedDate"), out var issued) ? issued : null,
            ExpirationDate: DateTime.TryParse(row.GetValueOrDefault("PassportExpiringDate"), out var expires) ? expires : null,
            Authority: row.GetValueOrDefault("PassportIssuedPlace"),
            LegacyIssuedCountry: row.GetValueOrDefault("LegacyIssuedCountry"),
            LegacyPersonOid: legacyPersonOid,
            HasPassportCopy: row.GetValueOrDefault("HasPassportCopy") == "1",
            PassportCopyByteLength: int.TryParse(row.GetValueOrDefault("PassportCopyByteLength"), out var len) ? len : 0);
        return true;
    }

    internal static Visa2014PersonTransform.TransformBatchResult TransformRows(
        IReadOnlyList<Visa2014PassportRawRow> rawRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        unmappedDistinct = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);

        var working = rawRows.Select(r => new WorkingRow(r)).ToList();
        ApplyPassportNumberDedupe(working, dedupeSummary);

        var importRows = new List<Dictionary<string, object?>>();
        var dedupeMergedCount = 0;

        foreach (var row in working)
        {
            if (row.ImportAction == "duplicate_merged")
            {
                dedupeMergedCount++;
                continue;
            }

            var export = BuildExportRow(row, catalogs, out var skipReason, out var rowUnmapped);
            foreach (var key in rowUnmapped)
                unmappedSet.Add(key);

            if (skipReason != null)
            {
                export["_reason"] = skipReason;
                skipped.Add(export);
                continue;
            }

            importRows.Add(export);
        }

        unmappedDistinct = unmappedSet
            .OrderBy(s => s, StringComparer.Ordinal)
            .Select(s =>
            {
                var parts = s.Split(':', 3);
                return new Dictionary<string, object?>
                {
                    ["catalog"] = parts.Length > 1 ? parts[1] : "",
                    ["legacyValue"] = parts.Length > 2 ? parts[2] : s,
                    ["reason"] = s,
                };
            })
            .ToList();

        return new Visa2014PersonTransform.TransformBatchResult(importRows, dedupeMergedCount);
    }

    private sealed class WorkingRow(Visa2014PassportRawRow Raw)
    {
        public Visa2014PassportRawRow Raw { get; } = Raw;
        public string ImportAction { get; set; } = "import";
        public string? DedupeGroupId { get; set; }
    }

    private static void ApplyPassportNumberDedupe(List<WorkingRow> rows, List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new { Row = r, Norm = NormalizePassportNumber(r.Raw.PassportNumber) })
            .Where(x => !IsSentinelPassportNumber(x.Norm))
            .GroupBy(x => x.Norm.ToUpperInvariant(), StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var canonical = members
                .OrderByDescending(x => x.Row.Raw.IssueDate ?? DateTime.MinValue)
                .ThenBy(x => x.Row.Raw.LegacyOid)
                .First();

            var groupId = $"PPN:{group.Key}";
            foreach (var member in members)
            {
                member.Row.DedupeGroupId = groupId;
                if (!ReferenceEquals(member.Row, canonical.Row))
                    member.Row.ImportAction = "duplicate_merged";
            }

            dedupeSummary.Add(new Dictionary<string, object?>
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "PassportNumber",
                ["normalizedValue"] = group.Key,
                ["memberCount"] = members.Count,
                ["canonical_legacyRowId"] = canonical.Row.Raw.LegacyOid,
                ["canonicalRule"] = "most_recent_issue_date; tieBreak Oid",
            });
        }
    }

    private static Dictionary<string, object?> BuildExportRow(
        WorkingRow working,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? skipReason,
        out List<string> unmapped)
    {
        skipReason = null;
        unmapped = [];
        var raw = working.Raw;

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "Passport",
            ["_dedupeGroupId"] = working.DedupeGroupId ?? "",
            ["_importAction"] = working.ImportAction,
            ["_hasPassportCopy"] = raw.HasPassportCopy,
            ["_passportCopyByteLength"] = raw.PassportCopyByteLength,
        };

        if (string.IsNullOrWhiteSpace(raw.PassportNumber) ||
            !raw.IssueDate.HasValue ||
            !raw.ExpirationDate.HasValue ||
            string.IsNullOrWhiteSpace(raw.Authority))
        {
            skipReason = "required_null:PassportNumber|IssueDate|ExpirationDate|Authority";
            row["PassportNumber"] = raw.PassportNumber;
            row["IssueDate"] = raw.IssueDate;
            row["ExpirationDate"] = raw.ExpirationDate;
            row["Authority"] = raw.Authority;
            return row;
        }

        if (raw.ExpirationDate <= raw.IssueDate)
        {
            skipReason = "invalid_date_range:ExpirationDate<=IssueDate";
            row["PassportNumber"] = raw.PassportNumber;
            row["IssueDate"] = raw.IssueDate;
            row["ExpirationDate"] = raw.ExpirationDate;
            return row;
        }

        var passportNumber = NormalizePassportNumber(raw.PassportNumber);
        if (IsSentinelPassportNumber(passportNumber))
            passportNumber = AppendLegacyOidTail(passportNumber, raw.LegacyOid);

        row["PassportNumber"] = passportNumber;
        row["IssueDate"] = raw.IssueDate;
        row["ExpirationDate"] = raw.ExpirationDate;
        row["Authority"] = raw.Authority.Trim();
        row["Person"] = raw.LegacyPersonOid.ToString("D");
        row["IsCancelled"] = false;
        row["ShowOptionalFields"] = false;

        var composite = BuildPassportTypeComposite(raw.TypeOfPassportL, raw.MgCode);
        row["_legacy_PassportTypeComposite"] = composite;
        TrySetPassportType(row, catalogs, composite, unmapped);
        TrySetLookup(row, catalogs, "Country", raw.LegacyIssuedCountry, "IssuedCountry", unmapped, ref skipReason);

        row["_legacy_IssuedCountry"] = raw.LegacyIssuedCountry;
        row["_legacy_PersonOid"] = raw.LegacyPersonOid.ToString("D");

        return row;
    }

    private static void TrySetPassportType(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string composite,
        List<string> unmapped)
    {
        if (Visa2014LookupTranslator.TryTranslate(catalogs, "PassportType", composite, out var target, out var reason))
        {
            row["PassportType"] = string.IsNullOrWhiteSpace(target) ? "P" : target;
            if (reason != null)
                unmapped.Add(reason);
            return;
        }

        if (reason != null)
            unmapped.Add(reason);

        row["PassportType"] = "P";
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
            string.Equals(catalog.UnmappedPolicy, "block_row", StringComparison.OrdinalIgnoreCase))
            skipReason ??= reason ?? $"unmapped_lookup:{catalogName}:{legacyValue}";

        row[targetProperty] = null;
    }

    private static string BuildPassportTypeComposite(string? typeL, string? mgCode)
    {
        var left = string.IsNullOrWhiteSpace(typeL) ? "" : typeL.Trim();
        var right = string.IsNullOrWhiteSpace(mgCode) ? "" : mgCode.Trim();
        return $"{left}:{right}";
    }

    private static string NormalizePassportNumber(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();

    private static bool IsSentinelPassportNumber(string normalized) =>
        SentinelPassportNumbers.Any(s =>
            string.Equals(s, normalized, StringComparison.OrdinalIgnoreCase));

    private static string AppendLegacyOidTail(string passportNumber, Guid legacyOid) =>
        passportNumber + legacyOid.ToString("N")[^8..];
}
