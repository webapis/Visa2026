namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014BorderZoneItemRawRow(
    Guid LegacyOid,
    Guid? LegacyPersonOid,
    Guid? LegacyPassportOid,
    Guid? LegacyBorderZoneOid,
    string? ParentProcessNumber);

internal static class Visa2014BorderZoneItemTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(pia.Oid AS varchar(36)) AS Oid,
            CAST(COALESCE(pia.Employee, pia.FamilyMember) AS varchar(36)) AS PersonOid,
            CAST(pia.Passport AS varchar(36)) AS PassportOid,
            CAST(pia.Application AS varchar(36)) AS BorderZoneOid,
            a.ProcessNumber AS ParentProcessNumber
        FROM dbo.PersonInApplication pia
        INNER JOIN dbo.Application a
            ON a.Oid = pia.Application AND a.GCRecord IS NULL
        INNER JOIN dbo.ApplicationTypeForEmployee ate
            ON ate.Oid = a.ApplicationTypeForEmployee
           AND ate.TypeOfApplicationForEmployee = 11
        WHERE pia.GCRecord IS NULL
        """;

    internal static readonly string[] BorderZoneItemMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction", "_skipReason",
        "Person", "Passport", "BorderZone",
        "_legacy_PersonOid", "_legacy_PassportOid", "_legacy_BorderZoneOid",
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
        var rawRows = new List<Visa2014BorderZoneItemRawRow>();
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

        return TransformRows(rawRows);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014BorderZoneItemRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        parsed = new Visa2014BorderZoneItemRawRow(
            LegacyOid: legacyOid,
            LegacyPersonOid: TryParseGuid(row.GetValueOrDefault("PersonOid")),
            LegacyPassportOid: TryParseGuid(row.GetValueOrDefault("PassportOid")),
            LegacyBorderZoneOid: TryParseGuid(row.GetValueOrDefault("BorderZoneOid")),
            ParentProcessNumber: row.GetValueOrDefault("ParentProcessNumber"));
        return true;
    }

    private static Guid? TryParseGuid(string? text) =>
        Guid.TryParse(text?.Trim(), out var oid) ? oid : null;

    private static Visa2014PersonImportBatch TransformRows(IReadOnlyList<Visa2014BorderZoneItemRawRow> rawRows)
    {
        var skipped = new List<Dictionary<string, object?>>();
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var raw in rawRows)
        {
            var export = BuildExportRow(raw, out var skipReason);
            if (skipReason != null)
            {
                export["_skipReason"] = skipReason;
                export["_importAction"] = "skip";
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

    private static Dictionary<string, object?> BuildExportRow(Visa2014BorderZoneItemRawRow raw, out string? skipReason)
    {
        skipReason = null;
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "PersonInApplication",
            ["_importAction"] = "import",
            ["_skipReason"] = "",
            ["Person"] = raw.LegacyPersonOid?.ToString("D"),
            ["Passport"] = raw.LegacyPassportOid?.ToString("D"),
            ["BorderZone"] = raw.LegacyBorderZoneOid?.ToString("D"),
            ["_legacy_PersonOid"] = raw.LegacyPersonOid?.ToString("D"),
            ["_legacy_PassportOid"] = raw.LegacyPassportOid?.ToString("D"),
            ["_legacy_BorderZoneOid"] = raw.LegacyBorderZoneOid?.ToString("D"),
        };

        if (!Visa2014BorderZoneTransform.LooksLikePermitNumber(raw.ParentProcessNumber))
        {
            skipReason = "parent_header_skipped";
            return row;
        }

        if (!raw.LegacyPersonOid.HasValue)
        {
            skipReason = "missing_fk:Person";
            return row;
        }

        if (!raw.LegacyPassportOid.HasValue)
        {
            skipReason = "missing_fk:Passport";
            return row;
        }

        if (!raw.LegacyBorderZoneOid.HasValue)
        {
            skipReason = "missing_fk:BorderZone";
            return row;
        }

        return row;
    }
}