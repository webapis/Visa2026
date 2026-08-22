namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014InvitationRawRow(
    Guid LegacyOid,
    string? InvitationNumber,
    DateTime? IssuedDate,
    DateTime? DateOfExpire,
    Guid? LegacyApplicationOid);

internal static class Visa2014InvitationTransform
{
    internal const string ExtractSql = """
        SELECT DISTINCT
            CAST(ar.Oid AS varchar(36)) AS Oid,
            ar.Number AS InvitationNumber,
            CONVERT(varchar(10), ar.IssuedDate, 23) AS IssuedDate,
            CONVERT(varchar(10), ar.DateOfExpire, 23) AS DateOfExpire,
            CAST(ar.Application AS varchar(36)) AS ApplicationOid
        FROM dbo.ApplicationResult ar
        WHERE ar.GCRecord IS NULL
          AND ar.Result = 0
          AND EXISTS (
              SELECT 1
              FROM dbo.PersonInInvitation pii
              WHERE pii.Invitation = ar.Oid
                AND pii.GCRecord IS NULL)
        """;

    internal static readonly string[] InvitationMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_dedupeGroupId", "_importAction",
        "InvitationNumber", "IssuedDate", "ExpirationDate", "VisaPeriodKey",
        "Application", "_legacy_ApplicationOid",
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
        var rawRows = new List<Visa2014InvitationRawRow>();
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

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014InvitationRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        DateTime? issuedDate = DateTime.TryParse(row.GetValueOrDefault("IssuedDate"), out var issued)
            ? issued
            : null;
        DateTime? dateOfExpire = DateTime.TryParse(row.GetValueOrDefault("DateOfExpire"), out var expire)
            ? expire
            : null;

        parsed = new Visa2014InvitationRawRow(
            LegacyOid: legacyOid,
            InvitationNumber: row.GetValueOrDefault("InvitationNumber"),
            IssuedDate: issuedDate,
            DateOfExpire: dateOfExpire,
            LegacyApplicationOid: TryParseGuid(row.GetValueOrDefault("ApplicationOid")));
        return true;
    }

    private static Guid? TryParseGuid(string? text) =>
        Guid.TryParse(text?.Trim(), out var oid) ? oid : null;

    internal static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014InvitationRawRow> rawRows,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var working = rawRows.Select(r => new WorkingRow(r)).ToList();
        ApplyInvitationNumberSuffix(working, dedupeSummary);

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

    private sealed class WorkingRow(Visa2014InvitationRawRow Raw)
    {
        public Visa2014InvitationRawRow Raw { get; } = Raw;
        public string? DedupeGroupId { get; set; }
        public string? ResolvedInvitationNumber { get; set; }
    }

    private static void ApplyInvitationNumberSuffix(
        List<WorkingRow> rows,
        List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new { Row = r, Norm = NormalizeInvitationNumber(r.Raw.InvitationNumber) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Norm))
            .GroupBy(x => x.Norm.ToUpperInvariant(), StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var groupId = $"INV:{group.Key}";
            foreach (var member in members)
            {
                member.Row.DedupeGroupId = groupId;
                member.Row.ResolvedInvitationNumber = AppendLegacyOidTail(
                    member.Norm,
                    member.Row.Raw.LegacyOid);
            }

            dedupeSummary.Add(new Dictionary<string, object?>
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "InvitationNumber",
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
            ["_legacy_ApplicationOid"] = raw.LegacyApplicationOid?.ToString("D"),
        };

        if (string.IsNullOrWhiteSpace(raw.InvitationNumber))
        {
            skipReason = "required_null:InvitationNumber";
            row["InvitationNumber"] = null;
            row["IssuedDate"] = null;
            row["ExpirationDate"] = null;
            return row;
        }

        if (!raw.IssuedDate.HasValue)
        {
            skipReason = "required_null:IssuedDate";
            row["InvitationNumber"] = raw.InvitationNumber;
            row["IssuedDate"] = null;
            row["ExpirationDate"] = null;
            return row;
        }

        if (!raw.DateOfExpire.HasValue)
        {
            skipReason = "required_null:DateOfExpire";
            row["InvitationNumber"] = raw.InvitationNumber;
            row["IssuedDate"] = raw.IssuedDate.Value.ToString("yyyy-MM-dd");
            row["ExpirationDate"] = null;
            return row;
        }

        var invitationNumber = working.ResolvedInvitationNumber
            ?? NormalizeInvitationNumber(raw.InvitationNumber);
        var daySpan = Visa2014ValidityDurationHelper.ComputeDaySpan(raw.IssuedDate.Value, raw.DateOfExpire.Value);
        var closestDays = Visa2014ValidityDurationHelper.ClosestCandidateDaySpan(daySpan);

        row["InvitationNumber"] = invitationNumber;
        row["IssuedDate"] = raw.IssuedDate.Value.ToString("yyyy-MM-dd");
        row["ExpirationDate"] = raw.DateOfExpire.Value.ToString("yyyy-MM-dd");
        // Best-effort VisaPeriod from invitation letter span until legacy VisaPeriod column is mapped.
        row["VisaPeriodKey"] = Visa2014ValidityDurationHelper.LocalizationKeyForDaySpan(closestDays);
        row["Application"] = raw.LegacyApplicationOid?.ToString("D");
        return row;
    }

    private static string NormalizeInvitationNumber(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();

    private static string AppendLegacyOidTail(string invitationNumber, Guid legacyOid) =>
        invitationNumber + legacyOid.ToString("N")[^8..];
}