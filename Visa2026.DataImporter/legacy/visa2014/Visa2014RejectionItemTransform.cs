namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014RejectionItemRawRow(
    Guid LegacyOid,
    Guid? LegacyPersonOid,
    Guid? LegacyPassportOid,
    Guid? LegacyRejectionOid);

internal static class Visa2014RejectionItemTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(pii.Oid AS varchar(36)) AS Oid,
            CAST(COALESCE(pii.Employee, pii.FamilyMember) AS varchar(36)) AS PersonOid,
            CAST(pii.Passport AS varchar(36)) AS PassportOid,
            CAST(pii.Invitation AS varchar(36)) AS RejectionOid
        FROM dbo.PersonInInvitation pii
        INNER JOIN dbo.ApplicationResult ar
            ON ar.Oid = pii.Invitation
           AND ar.GCRecord IS NULL
           AND ar.Result = 1
        WHERE pii.GCRecord IS NULL
        """;

    internal static readonly string[] RejectionItemMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction",
        "Person", "Passport", "Rejection", "Reason",
        "_legacy_PersonOid", "_legacy_PassportOid", "_legacy_RejectionOid",
    ];

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        _ = lookupTranslationPaths;
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows}) * FROM ({ExtractSql}) AS q ORDER BY Oid"
            : $"{ExtractSql} ORDER BY Oid";

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014RejectionItemRawRow>();
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

        return TransformRows(rawRows, out var skipped);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014RejectionItemRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        parsed = new Visa2014RejectionItemRawRow(
            LegacyOid: legacyOid,
            LegacyPersonOid: TryParseGuid(row.GetValueOrDefault("PersonOid")),
            LegacyPassportOid: TryParseGuid(row.GetValueOrDefault("PassportOid")),
            LegacyRejectionOid: TryParseGuid(row.GetValueOrDefault("RejectionOid")));
        return true;
    }

    private static Guid? TryParseGuid(string? text) =>
        Guid.TryParse(text?.Trim(), out var oid) ? oid : null;

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014RejectionItemRawRow> rawRows,
        out List<Dictionary<string, object?>> skipped)
    {
        skipped = [];
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var raw in rawRows)
        {
            var export = BuildExportRow(raw, out var skipReason);
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
            DedupeSummary = [],
            LegacyRowCount = rawRows.Count,
            DedupeMergedCount = 0,
        };
    }

    internal static Dictionary<string, object?> BuildExportRow(
        Visa2014RejectionItemRawRow raw,
        out string? skipReason)
    {
        skipReason = null;

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "PersonInInvitation",
            ["_importAction"] = "import",
            ["_legacy_PersonOid"] = raw.LegacyPersonOid?.ToString("D"),
            ["_legacy_PassportOid"] = raw.LegacyPassportOid?.ToString("D"),
            ["_legacy_RejectionOid"] = raw.LegacyRejectionOid?.ToString("D"),
            ["Reason"] = null,
        };

        if (!raw.LegacyPersonOid.HasValue)
            skipReason = "missing_fk:Person";
        if (!raw.LegacyPassportOid.HasValue)
            skipReason ??= "missing_fk:Passport";
        if (!raw.LegacyRejectionOid.HasValue)
            skipReason ??= "missing_fk:Rejection";

        row["Person"] = raw.LegacyPersonOid?.ToString("D");
        row["Passport"] = raw.LegacyPassportOid?.ToString("D");
        row["Rejection"] = raw.LegacyRejectionOid?.ToString("D");
        return row;
    }
}
