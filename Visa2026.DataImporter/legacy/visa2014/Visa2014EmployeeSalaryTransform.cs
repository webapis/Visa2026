namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014EmployeeSalaryRawRow(
    Guid LegacyPersonOid,
    Guid? LegacySalaryOid,
    string? SalaryDetail,
    DateTime? CurrentPositionStart);

internal static class Visa2014EmployeeSalaryTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(e.Oid AS varchar(36)) AS LegacyPersonOid,
            CAST(e.Salary AS varchar(36)) AS LegacySalaryOid,
            s.Detail AS SalaryDetail,
            CONVERT(varchar(10), pos.CurrentPositionStart, 23) AS CurrentPositionStart
        FROM dbo.Employee e
        INNER JOIN dbo.Person p ON p.Oid = e.Oid AND p.GCRecord IS NULL
        LEFT JOIN dbo.Salary s ON s.Oid = e.Salary
        OUTER APPLY (
            SELECT MAX(w.StartDateOnThisPosition) AS CurrentPositionStart
            FROM dbo.WorkHistoryOfEmployee w
            WHERE w.Employee = e.Oid AND w.GCRecord IS NULL
        ) pos
        """;

    internal static readonly string[] EmployeeSalaryMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction",
        "Person", "Amount", "Currency", "StartDate", "EndDate",
        "_legacy_SalaryOid", "_legacy_SalaryDetail", "_legacy_PersonOid",
    ];

    internal static readonly string[] AmountParseColumnOrder =
    [
        "_legacyPersonOid", "_rawDetail", "_normalizedAmount", "_currency", "_parseNote", "_startDate",
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
        var rawRows = new List<Visa2014EmployeeSalaryRawRow>();
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

        return TransformRows(rawRows, out var skipped, out var amountParseRows);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014EmployeeSalaryRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("LegacyPersonOid", out var personText) ||
            !Guid.TryParse(personText?.Trim(), out var legacyPersonOid))
            return false;

        Guid? legacySalaryOid = null;
        if (row.TryGetValue("LegacySalaryOid", out var salaryText) &&
            Guid.TryParse(salaryText?.Trim(), out var salaryOid))
            legacySalaryOid = salaryOid;

        DateTime? positionStart = DateTime.TryParse(row.GetValueOrDefault("CurrentPositionStart"), out var start)
            ? start
            : null;

        parsed = new Visa2014EmployeeSalaryRawRow(
            LegacyPersonOid: legacyPersonOid,
            LegacySalaryOid: legacySalaryOid,
            SalaryDetail: row.GetValueOrDefault("SalaryDetail"),
            CurrentPositionStart: positionStart);
        return true;
    }

    internal static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014EmployeeSalaryRawRow> rawRows,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> amountParseRows)
    {
        skipped = [];
        amountParseRows = [];
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var raw in rawRows)
        {
            var parseAudit = BuildAmountParseRow(raw);
            amountParseRows.Add(parseAudit);

            var row = BuildExportRow(raw, parseAudit, out var skipReason);
            if (skipReason != null)
            {
                row["_skipReason"] = skipReason;
                skipped.Add(row);
                continue;
            }

            importRows.Add(row);
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

    internal static Dictionary<string, object?> BuildAmountParseRow(Visa2014EmployeeSalaryRawRow raw)
    {
        var normalized = string.Empty;
        var parseNote = "empty";
        if (Visa2014SalaryAmountNormalizer.TryNormalize(raw.SalaryDetail, out normalized, out parseNote))
        { }

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyPersonOid"] = raw.LegacyPersonOid.ToString("D"),
            ["_rawDetail"] = raw.SalaryDetail,
            ["_normalizedAmount"] = string.IsNullOrWhiteSpace(normalized) ? null : normalized,
            ["_currency"] = Visa2014SalaryAmountNormalizer.ResolveCurrency(raw.SalaryDetail),
            ["_parseNote"] = parseNote,
            ["_startDate"] = raw.CurrentPositionStart?.ToString("yyyy-MM-dd"),
        };
    }

    internal static Dictionary<string, object?> BuildExportRow(
        Visa2014EmployeeSalaryRawRow raw,
        IReadOnlyDictionary<string, object?> parseAudit,
        out string? skipReason)
    {
        skipReason = null;
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyPersonOid,
            ["_legacyTable"] = "Employee",
            ["_importAction"] = "import",
            ["_legacy_SalaryOid"] = raw.LegacySalaryOid?.ToString("D"),
            ["_legacy_SalaryDetail"] = raw.SalaryDetail,
            ["_legacy_PersonOid"] = raw.LegacyPersonOid.ToString("D"),
            ["Person"] = raw.LegacyPersonOid.ToString("D"),
            ["EndDate"] = null,
        };

        var amount = parseAudit.GetValueOrDefault("_normalizedAmount") as string;
        if (string.IsNullOrWhiteSpace(amount))
        {
            skipReason = raw.LegacySalaryOid == null
                ? "missing_salary_fk"
                : string.IsNullOrWhiteSpace(raw.SalaryDetail)
                    ? "empty_salary_detail"
                    : "unparseable_amount";
            row["Amount"] = null;
        }
        else
        {
            row["Amount"] = amount;
        }

        row["Currency"] = parseAudit.GetValueOrDefault("_currency") ?? "USD";

        if (!raw.CurrentPositionStart.HasValue)
        {
            skipReason ??= "required_null:StartDate";
            row["StartDate"] = null;
        }
        else
        {
            row["StartDate"] = raw.CurrentPositionStart.Value.ToString("yyyy-MM-dd");
        }

        return row;
    }
}
