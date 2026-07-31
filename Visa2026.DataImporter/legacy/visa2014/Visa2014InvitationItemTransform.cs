namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014InvitationItemRawRow(
    Guid LegacyOid,
    Guid? LegacyPersonOid,
    Guid? LegacyPassportOid,
    Guid? LegacyInvitationOid,
    int? ApplicationResultResult);

internal static class Visa2014InvitationItemTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(pii.Oid AS varchar(36)) AS Oid,
            CAST(COALESCE(pii.Employee, pii.FamilyMember) AS varchar(36)) AS PersonOid,
            CAST(pii.Passport AS varchar(36)) AS PassportOid,
            CAST(pii.Invitation AS varchar(36)) AS InvitationOid,
            ar.Result AS ApplicationResultResult
        FROM dbo.PersonInInvitation pii
        INNER JOIN dbo.ApplicationResult ar
            ON ar.Oid = pii.Invitation
           AND ar.GCRecord IS NULL
           AND ar.Result = 0
        WHERE pii.GCRecord IS NULL
        """;

    internal static readonly string[] InvitationItemMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction",
        "Person", "Passport", "Invitation", "IsCancelled",
        "_legacy_PersonOid", "_legacy_PassportOid", "_legacy_InvitationOid", "_legacy_ApplicationResultResult",
    ];

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        _ = lookupTranslationPaths;
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows}) * FROM ({ExtractSql}) AS q"
            : ExtractSql;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014InvitationItemRawRow>();
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

        var cancellationIndex = Visa2014LegacyInvitationItemCancellationIndex.Load(
            connectionString,
            lookupTranslationPaths,
            verbose);

        return TransformRows(rawRows, cancellationIndex, out var skipped);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014InvitationItemRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        int? result = int.TryParse(row.GetValueOrDefault("ApplicationResultResult"), out var parsedResult)
            ? parsedResult
            : null;

        parsed = new Visa2014InvitationItemRawRow(
            LegacyOid: legacyOid,
            LegacyPersonOid: TryParseGuid(row.GetValueOrDefault("PersonOid")),
            LegacyPassportOid: TryParseGuid(row.GetValueOrDefault("PassportOid")),
            LegacyInvitationOid: TryParseGuid(row.GetValueOrDefault("InvitationOid")),
            ApplicationResultResult: result);
        return true;
    }

    private static Guid? TryParseGuid(string? text) =>
        Guid.TryParse(text?.Trim(), out var oid) ? oid : null;

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014InvitationItemRawRow> rawRows,
        Visa2014LegacyInvitationItemCancellationIndex cancellationIndex,
        out List<Dictionary<string, object?>> skipped)
    {
        skipped = [];
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var raw in rawRows)
        {
            var export = BuildExportRow(raw, cancellationIndex, out var skipReason);
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

    private static Dictionary<string, object?> BuildExportRow(
        Visa2014InvitationItemRawRow raw,
        Visa2014LegacyInvitationItemCancellationIndex cancellationIndex,
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
            ["_legacy_InvitationOid"] = raw.LegacyInvitationOid?.ToString("D"),
            ["_legacy_ApplicationResultResult"] = raw.ApplicationResultResult,
        };

        if (!raw.LegacyPersonOid.HasValue)
            skipReason = "missing_fk:Person";
        if (!raw.LegacyPassportOid.HasValue)
            skipReason ??= "missing_fk:Passport";
        if (!raw.LegacyInvitationOid.HasValue)
            skipReason ??= "missing_fk:Invitation";

        row["Person"] = raw.LegacyPersonOid?.ToString("D");
        row["Passport"] = raw.LegacyPassportOid?.ToString("D");
        row["Invitation"] = raw.LegacyInvitationOid?.ToString("D");
        row["IsCancelled"] = Visa2014LegacyInvitationItemCancellationIndex.ResolveIsCancelled(
            raw.ApplicationResultResult,
            raw.LegacyOid,
            cancellationIndex);
        return row;
    }
}
