namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014WorkPermitItemRawRow(
    Guid LegacyOid,
    Guid? LegacyEmployeeOid,
    Guid? LegacyPassportOid,
    Guid? LegacyPositionOid,
    Guid? LegacyWorkPermitLetterOid,
    Guid? LegacyWorkPermitLocationOid,
    DateTime? StartDate,
    DateTime? ExpirationDate,
    string? WorkPermitNumber,
    string? ASNumber);

internal static class Visa2014WorkPermitItemTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(wp.Oid AS varchar(36)) AS Oid,
            CAST(wp.Employee AS varchar(36)) AS EmployeeOid,
            CAST(wp.Passport AS varchar(36)) AS PassportOid,
            CAST(wp.Position AS varchar(36)) AS PositionOid,
            CAST(wp.WorkPermitLetter AS varchar(36)) AS WorkPermitLetterOid,
            CAST(wp.WorkPermitLocation AS varchar(36)) AS WorkPermitLocationOid,
            CONVERT(varchar(10), wp.StartDateOfWorkPermit, 23) AS StartDateOfWorkPermit,
            CONVERT(varchar(10), wp.ExpiringDateOfWorkPermit, 23) AS ExpiringDateOfWorkPermit,
            wp.AppruvalNumber,
            wp.ASNumber
        FROM dbo.WorkPermit wp
        WHERE wp.GCRecord IS NULL
        """;

    internal static readonly string[] WorkPermitItemMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction",
        "Person", "Passport", "CurrentPositionHistory", "WorkPermit",
        "WorkPermitNumber", "ASNumber", "StartDate", "ExpirationDate", "WorkPermittedLocations", "IsCancelled",
        "_legacy_EmployeeOid", "_legacy_PassportOid", "_legacy_PositionOid",
        "_legacy_WorkPermitLetterOid", "_legacy_WorkPermitLocationOid",
    ];

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var bitColumnNames = Visa2014WorkPermitLocationBitMatrix.LoadBitColumnNames(connectionString);

        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows}) * FROM ({ExtractSql}) AS q"
            : ExtractSql;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014WorkPermitItemRawRow>();
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

        var locationOids = rawRows
            .Select(r => r.LegacyWorkPermitLocationOid)
            .Where(o => o.HasValue)
            .Select(o => o!.Value);
        var locationRows = Visa2014WorkPermitLocationBitMatrix.LoadLocationRows(
            connectionString, locationOids, verbose);

        var cancellationIndex = Visa2014LegacyDocumentCancellationIndex.Load(
            connectionString,
            lookupTranslationPaths,
            verbose);

        return TransformRows(rawRows, catalogs, bitColumnNames, locationRows, cancellationIndex, out var skipped, out var unmappedDistinct);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014WorkPermitItemRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        parsed = new Visa2014WorkPermitItemRawRow(
            LegacyOid: legacyOid,
            LegacyEmployeeOid: TryParseGuid(row.GetValueOrDefault("EmployeeOid")),
            LegacyPassportOid: TryParseGuid(row.GetValueOrDefault("PassportOid")),
            LegacyPositionOid: TryParseGuid(row.GetValueOrDefault("PositionOid")),
            LegacyWorkPermitLetterOid: TryParseGuid(row.GetValueOrDefault("WorkPermitLetterOid")),
            LegacyWorkPermitLocationOid: TryParseGuid(row.GetValueOrDefault("WorkPermitLocationOid")),
            StartDate: DateTime.TryParse(row.GetValueOrDefault("StartDateOfWorkPermit"), out var start) ? start : null,
            ExpirationDate: DateTime.TryParse(row.GetValueOrDefault("ExpiringDateOfWorkPermit"), out var end) ? end : null,
            WorkPermitNumber: row.GetValueOrDefault("AppruvalNumber"),
            ASNumber: row.GetValueOrDefault("ASNumber"));
        return true;
    }

    private static Guid? TryParseGuid(string? text) =>
        Guid.TryParse(text?.Trim(), out var oid) ? oid : null;

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014WorkPermitItemRawRow> rawRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        IReadOnlyList<string> bitColumnNames,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string?>> locationRows,
        Visa2014LegacyDocumentCancellationIndex cancellationIndex,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct)
    {
        skipped = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var raw in rawRows)
        {
            var export = BuildExportRow(raw, catalogs, bitColumnNames, locationRows, cancellationIndex, out var skipReason, out var rowUnmapped);
            foreach (var key in rowUnmapped)
                unmappedSet.Add(key);

            if (skipReason != null)
            {
                export["_skipReason"] = skipReason;
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

        return new Visa2014PersonImportBatch
        {
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = unmappedDistinct,
            DedupeSummary = [],
            LegacyRowCount = rawRows.Count,
            DedupeMergedCount = 0,
        };
    }

    private static Dictionary<string, object?> BuildExportRow(
        Visa2014WorkPermitItemRawRow raw,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        IReadOnlyList<string> bitColumnNames,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, string?>> locationRows,
        Visa2014LegacyDocumentCancellationIndex cancellationIndex,
        out string? skipReason,
        out List<string> unmapped)
    {
        skipReason = null;
        unmapped = [];

        var legacyWorkPermitHeaderOid = raw.LegacyWorkPermitLetterOid ?? raw.LegacyOid;

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "WorkPermit",
            ["_importAction"] = "import",
            ["_legacy_EmployeeOid"] = raw.LegacyEmployeeOid?.ToString("D"),
            ["_legacy_PassportOid"] = raw.LegacyPassportOid?.ToString("D"),
            ["_legacy_PositionOid"] = raw.LegacyPositionOid?.ToString("D"),
            ["_legacy_WorkPermitLetterOid"] = raw.LegacyWorkPermitLetterOid?.ToString("D"),
            ["_legacy_WorkPermitLocationOid"] = raw.LegacyWorkPermitLocationOid?.ToString("D"),
        };

        if (!raw.LegacyEmployeeOid.HasValue)
            skipReason = "missing_fk:Employee";
        if (!raw.LegacyPassportOid.HasValue)
            skipReason ??= "missing_fk:Passport";
        if (!raw.LegacyPositionOid.HasValue)
            skipReason ??= "missing_fk:Position";

        row["Person"] = raw.LegacyEmployeeOid?.ToString("D");
        row["Passport"] = raw.LegacyPassportOid?.ToString("D");
        row["CurrentPositionHistory"] = raw.LegacyPositionOid?.ToString("D");
        row["WorkPermit"] = legacyWorkPermitHeaderOid.ToString("D");

        if (string.IsNullOrWhiteSpace(raw.WorkPermitNumber))
        {
            skipReason ??= "required_null:WorkPermitNumber";
            row["WorkPermitNumber"] = null;
        }
        else
        {
            row["WorkPermitNumber"] = raw.WorkPermitNumber.Trim();
        }

        if (string.IsNullOrWhiteSpace(raw.ASNumber))
        {
            skipReason ??= "required_null:ASNumber";
            row["ASNumber"] = null;
        }
        else
        {
            row["ASNumber"] = raw.ASNumber.Trim();
        }

        if (!raw.StartDate.HasValue)
        {
            skipReason ??= "required_null:StartDate";
            row["StartDate"] = null;
        }
        else
        {
            row["StartDate"] = raw.StartDate.Value.ToString("yyyy-MM-dd");
        }

        if (!raw.ExpirationDate.HasValue)
        {
            skipReason ??= "required_null:ExpirationDate";
            row["ExpirationDate"] = null;
        }
        else
        {
            row["ExpirationDate"] = raw.ExpirationDate.Value.ToString("yyyy-MM-dd");
        }

        if (raw.StartDate.HasValue && raw.ExpirationDate.HasValue && raw.ExpirationDate <= raw.StartDate)
            skipReason ??= "invalid_date_range:ExpirationDate<=StartDate";

        IReadOnlyDictionary<string, string?>? locationRow = null;
        if (raw.LegacyWorkPermitLocationOid.HasValue)
            locationRows.TryGetValue(raw.LegacyWorkPermitLocationOid.Value, out locationRow);

        var workPermittedLocations = Visa2014WorkPermitLocationBitMatrix.BuildWorkPermittedLocations(
            locationRow,
            bitColumnNames,
            catalogs,
            unmapped);
        row["WorkPermittedLocations"] = workPermittedLocations;
        row["IsCancelled"] = cancellationIndex.IsWorkPermitCancelled(raw.LegacyOid);

        return row;
    }
}