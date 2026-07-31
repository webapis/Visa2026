namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014EducationRawRow(
    Guid LegacyOid,
    string? TitleOfEducationLevel,
    string? EducationLevelMgCode,
    string? TitleOfInstitution,
    string? CountryMgCode,
    string? CountryName,
    string? CountryNameL,
    string? TitleOfSpeciality,
    DateTime? EducationEndDate,
    Guid LegacyPersonOid);

internal static class Visa2014EducationTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(e.Oid AS varchar(36)) AS Oid,
            el.TitleOfEducationLevel,
            ISNULL(CAST(el.mgCode AS varchar(10)), '') AS EducationLevelMgCode,
            ei.TitleOfIEducationInstitution,
            c.mgCode AS EducationCountryCode,
            c.NameOfCountry AS EducationCountryName,
            c.NameOfCountryL AS EducationCountryNameL,
            s.TitleOfSpeciality,
            CONVERT(varchar(10), e.EducationEndDate, 23) AS EducationEndDate,
            CAST(e.Person AS varchar(36)) AS LegacyPersonOid
        FROM dbo.Education e
        INNER JOIN dbo.Person p ON e.Person = p.Oid AND p.GCRecord IS NULL
        INNER JOIN dbo.EducationLevel el ON e.EducationLevel = el.Oid
        INNER JOIN dbo.EducationInstitution ei ON e.EducationInstitution = ei.Oid
        INNER JOIN dbo.Country c ON e.EducationCountry = c.Oid
        INNER JOIN dbo.Speciality s ON e.Spcialty = s.Oid
        WHERE e.GCRecord IS NULL
        """;

    internal static readonly string[] EducationMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_importAction",
        "EducationLevel", "EducationInstitution", "EducationCountry", "Specialty", "GraduationYear", "Person",
        "_legacy_EducationLevelComposite", "_legacy_EducationCountryCode", "_legacy_PersonOid",
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
        var rawRows = new List<Visa2014EducationRawRow>();
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

        return TransformRows(rawRows, catalogs, out var skipped, out var unmappedDistinct, out _);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014EducationRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        if (!row.TryGetValue("LegacyPersonOid", out var personText) ||
            !Guid.TryParse(personText?.Trim(), out var legacyPersonOid))
            return false;

        DateTime? endDate = DateTime.TryParse(row.GetValueOrDefault("EducationEndDate"), out var end) ? end : null;

        parsed = new Visa2014EducationRawRow(
            LegacyOid: legacyOid,
            TitleOfEducationLevel: row.GetValueOrDefault("TitleOfEducationLevel"),
            EducationLevelMgCode: row.GetValueOrDefault("EducationLevelMgCode"),
            TitleOfInstitution: row.GetValueOrDefault("TitleOfIEducationInstitution"),
            CountryMgCode: row.GetValueOrDefault("EducationCountryCode"),
            CountryName: row.GetValueOrDefault("EducationCountryName"),
            CountryNameL: row.GetValueOrDefault("EducationCountryNameL"),
            TitleOfSpeciality: row.GetValueOrDefault("TitleOfSpeciality"),
            EducationEndDate: endDate,
            LegacyPersonOid: legacyPersonOid);
        return true;
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014EducationRawRow> rawRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        unmappedDistinct = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);
        var importRows = new List<Dictionary<string, object?>>();

        foreach (var raw in rawRows)
        {
            var row = BuildExportRow(raw, catalogs, out var skipReason, out var rowUnmapped);
            foreach (var key in rowUnmapped)
                unmappedSet.Add(key);

            if (skipReason != null)
            {
                row["_skipReason"] = skipReason;
                skipped.Add(row);
                continue;
            }

            importRows.Add(row);
        }

        foreach (var key in unmappedSet.OrderBy(k => k, StringComparer.Ordinal))
            unmappedDistinct.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["catalog"] = key.Split(':')[0],
                ["legacyValue"] = key.Contains(':') ? key[(key.IndexOf(':') + 1)..] : key,
                ["reason"] = key,
            });

        return new Visa2014PersonImportBatch
        {
            ImportRows = importRows,
            Skipped = skipped,
            UnmappedLookups = unmappedDistinct,
            DedupeSummary = dedupeSummary,
            LegacyRowCount = rawRows.Count,
            DedupeMergedCount = 0,
        };
    }

    private static Dictionary<string, object?> BuildExportRow(
        Visa2014EducationRawRow raw,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? skipReason,
        out List<string> unmapped)
    {
        skipReason = null;
        unmapped = [];
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "Education",
            ["_importAction"] = "import",
        };

        var levelComposite = BuildEducationLevelComposite(raw.TitleOfEducationLevel, raw.EducationLevelMgCode);
        row["_legacy_EducationLevelComposite"] = levelComposite;
        TrySetEducationLevel(row, catalogs, levelComposite, unmapped);

        TrySetLookup(row, catalogs, "EducationInstitution", raw.TitleOfInstitution, "EducationInstitution", unmapped, ref skipReason);
        var countryKey = ResolveEducationCountryLegacyKey(raw.CountryMgCode, raw.CountryName, raw.CountryNameL);
        TrySetLookup(row, catalogs, "Country", countryKey, "EducationCountry", unmapped, ref skipReason);
        TrySetLookup(row, catalogs, "Specialty", raw.TitleOfSpeciality, "Specialty", unmapped, ref skipReason);

        if (raw.EducationEndDate.HasValue)
            row["GraduationYear"] = raw.EducationEndDate.Value.Year.ToString();

        row["Person"] = raw.LegacyPersonOid.ToString("D");
        row["_legacy_EducationCountryCode"] = countryKey ?? raw.CountryMgCode;
        row["_legacy_PersonOid"] = raw.LegacyPersonOid.ToString("D");

        return row;
    }

    private static void TrySetEducationLevel(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string composite,
        List<string> unmapped)
    {
        if (Visa2014LookupTranslator.TryTranslate(catalogs, "EducationLevel", composite, out var target, out var reason))
        {
            row["EducationLevel"] = string.IsNullOrWhiteSpace(target) ? "SpecialSecondary" : target;
            if (reason != null)
                unmapped.Add(reason);
            return;
        }

        if (reason != null)
            unmapped.Add(reason);

        row["EducationLevel"] = "SpecialSecondary";
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
            string.Equals(catalog.UnmappedPolicy, "skip_row", StringComparison.OrdinalIgnoreCase))
            skipReason ??= reason ?? $"unmapped_lookup:{catalogName}:{legacyValue}";
    }

    private static string BuildEducationLevelComposite(string? title, string? mgCode) =>
        $"{title?.Trim()}:{mgCode?.Trim()}";

    /// <summary>Legacy Education Country.mgCode is often ISO3-SUFFIX (e.g. GBR-WELIKOBRITANIYA); Visa2026 Country.Code is ISO3 only.</summary>
    internal static string? NormalizeLegacyCountryMgCode(string? mgCode)
    {
        if (string.IsNullOrWhiteSpace(mgCode))
            return null;

        var trimmed = mgCode.Trim();
        var dash = trimmed.IndexOf('-');
        return dash > 0 ? trimmed[..dash] : trimmed;
    }

    /// <summary>
    /// Prefer Country.mgCode (prefix-normalized). When mgCode is null/blank on legacy dbo.Country
    /// (32 active rows on Çalik), fall back to NameOfCountry then NameOfCountryL — same string
    /// Person/Passport already resolve against Visa2026 Country.Code.
    /// </summary>
    internal static string? ResolveEducationCountryLegacyKey(
        string? mgCode,
        string? nameOfCountry,
        string? nameOfCountryL)
    {
        var fromMg = NormalizeLegacyCountryMgCode(mgCode);
        if (!string.IsNullOrWhiteSpace(fromMg))
            return fromMg;

        if (!string.IsNullOrWhiteSpace(nameOfCountry))
            return nameOfCountry.Trim();

        if (!string.IsNullOrWhiteSpace(nameOfCountryL))
            return nameOfCountryL.Trim();

        return null;
    }
}
