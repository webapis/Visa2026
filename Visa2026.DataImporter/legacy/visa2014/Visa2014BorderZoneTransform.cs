namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014BorderZoneRawRow(
    Guid LegacyOid,
    string? BorderZoneNumber,
    DateTime? StartDate,
    int? LegacyPeriodDays);

internal static class Visa2014BorderZoneTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(a.Oid AS varchar(36)) AS Oid,
            a.ProcessNumber AS BorderZoneNumber,
            CONVERT(varchar(10), a.ProcessDate, 23) AS StartDate,
            vp.CountMonth AS LegacyPeriodDays
        FROM dbo.Application a
        INNER JOIN dbo.ApplicationTypeForEmployee ate
            ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.VisaPeriod vp ON vp.Oid = a.BorderZonePeriod
        WHERE a.GCRecord IS NULL
          AND ate.TypeOfApplicationForEmployee = 11
        """;

    internal static readonly string[] BorderZoneMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction", "_skipReason",
        "BorderZoneNumber", "StartDate", "ValidityDurationLocalizationKey",
        "ValidityDurationDays", "_legacy_PeriodDays", "ApplicationProfileInstance",
    ];

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        _ = lookupTranslationPaths;
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows}) * FROM ({ExtractSql}) AS q ORDER BY StartDate, Oid"
            : $"{ExtractSql} ORDER BY StartDate, Oid";

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014BorderZoneRawRow>();
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

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014BorderZoneRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        DateTime? startDate = DateTime.TryParse(row.GetValueOrDefault("StartDate"), out var start)
            ? start
            : null;
        int? days = int.TryParse(row.GetValueOrDefault("LegacyPeriodDays"), out var parsedDays)
            ? parsedDays
            : null;

        parsed = new Visa2014BorderZoneRawRow(
            LegacyOid: legacyOid,
            BorderZoneNumber: row.GetValueOrDefault("BorderZoneNumber"),
            StartDate: startDate,
            LegacyPeriodDays: days);
        return true;
    }

    internal static bool LooksLikePermitNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return false;
        var trimmed = number.Trim();
        return trimmed.StartsWith("AS", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("CO", StringComparison.OrdinalIgnoreCase);
    }

    private static Visa2014PersonImportBatch TransformRows(IReadOnlyList<Visa2014BorderZoneRawRow> rawRows)
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

    private static Dictionary<string, object?> BuildExportRow(Visa2014BorderZoneRawRow raw, out string? skipReason)
    {
        skipReason = null;
        var closestDays = raw.LegacyPeriodDays.HasValue
            ? Visa2014ValidityDurationHelper.ClosestCandidateDaySpan(raw.LegacyPeriodDays.Value)
            : 180;
        var localizationKey = Visa2014ValidityDurationHelper.LocalizationKeyForDaySpan(closestDays);

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "Application",
            ["_importAction"] = "import",
            ["_skipReason"] = "",
            ["BorderZoneNumber"] = raw.BorderZoneNumber?.Trim(),
            ["StartDate"] = raw.StartDate?.ToString("yyyy-MM-dd"),
            ["ValidityDurationLocalizationKey"] = localizationKey,
            ["ValidityDurationDays"] = closestDays,
            ["_legacy_PeriodDays"] = raw.LegacyPeriodDays,
            ["ApplicationProfileInstance"] = raw.LegacyOid.ToString("D"),
        };

        if (!LooksLikePermitNumber(raw.BorderZoneNumber))
        {
            skipReason = "invalid_process_number";
            return row;
        }

        if (!raw.StartDate.HasValue)
        {
            skipReason = "required_null:StartDate";
            return row;
        }

        return row;
    }
}