namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014PersonRawRow(
    Guid LegacyOid,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    DateTime? BirthDate,
    string? BirthPlace,
    string? LegacyBirthCountry,
    string? ForeignAddress,
    string? LegacyForeignAddressCountry,
    string? LegacyGender,
    bool IsEmployee,
    bool IsFamilyMember,
    Guid? LegacyEmployeeOid,
    string? LegacyRelationship,
    string? LegacyProjectContract,
    string? LegacyMaritalStatusStatus,
    string? LegacyMaritalStatusText,
    bool ActivePerson,
    bool HasPhoto,
    int PhotoByteLength,
    string? PhotoSha256,
    string? RawPersonalNumber,
    string? LegacyNationality);

internal sealed class Visa2014PersonImportBatch
{
    public required IReadOnlyList<Dictionary<string, object?>> ImportRows { get; init; }
    public required IReadOnlyList<Dictionary<string, object?>> Skipped { get; init; }
    public required IReadOnlyList<Dictionary<string, object?>> UnmappedLookups { get; init; }
    public required IReadOnlyList<Dictionary<string, object?>> DedupeSummary { get; init; }
    public int LegacyRowCount { get; init; }
    public int DedupeMergedCount { get; init; }
}

internal static class Visa2014PersonTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(p.Oid AS varchar(36)) AS Oid,
            p.FirstName,
            p.LastName,
            p.MiddleName,
            CONVERT(varchar(10), p.BirthDate, 23) AS BirthDate,
            p.BirthPlace,
            bc.NameOfCountryL AS LegacyBirthCountry,
            p.ForeignAddress,
            fac.NameOfCountryL AS LegacyForeignAddressCountry,
            g.TypeOfGenderL AS LegacyGender,
            CASE WHEN p.IsEmployee = 1 THEN '1' ELSE '0' END AS IsEmployee,
            CASE WHEN p.IsFamilyMember = 1 THEN '1' ELSE '0' END AS IsFamilyMember,
            CAST(p.Employee AS varchar(36)) AS LegacyEmployeeOid,
            rel.RelativeAsL AS LegacyRelationship,
            c.NumberOfContract AS LegacyProjectContract,
            CAST(ms.Status AS varchar(10)) AS LegacyMaritalStatusStatus,
            ms.StatusL AS LegacyMaritalStatusText,
            CASE WHEN p.ActivePerson = 1 THEN '1' ELSE '0' END AS ActivePerson,
            CASE WHEN p.Photo IS NOT NULL AND DATALENGTH(p.Photo) > 0 THEN '1' ELSE '0' END AS HasPhoto,
            ISNULL(DATALENGTH(p.Photo), 0) AS PhotoByteLength,
            CASE WHEN p.Photo IS NULL OR DATALENGTH(p.Photo) = 0 THEN NULL
                 ELSE LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', p.Photo), 2)) END AS PhotoSha256,
            Passport.PersonalNumber AS RawPersonalNumber,
            nc.NameOfCountryL AS LegacyNationality
        FROM dbo.Person p
        OUTER APPLY (
            SELECT TOP 1 pp.*
            FROM dbo.Passport pp
            WHERE pp.Person = p.Oid AND pp.GCRecord IS NULL
            ORDER BY
                CASE WHEN NULLIF(LTRIM(RTRIM(pp.PersonalNumber)), '') IN ('-', '.') THEN 1 ELSE 0 END,
                pp.PassportIssuedDate DESC,
                pp.Oid
        ) Passport
        LEFT JOIN dbo.Country bc ON p.BirthCountry = bc.Oid
        LEFT JOIN dbo.Country fac ON p.ForeignAddressCountry = fac.Oid
        LEFT JOIN dbo.Gender g ON p.Gender = g.Oid
        LEFT JOIN dbo.Country nc ON Passport.Citizenship = nc.Oid
        LEFT JOIN dbo.Relation rel ON p.FamilyMemberRelation = rel.Oid
        LEFT JOIN dbo.Contract c ON p.Contract = c.Oid
        LEFT JOIN dbo.MaritalStatus ms ON p.MaritalStatus = ms.Oid
        WHERE p.GCRecord IS NULL
        """;

    internal static readonly string[] PersonMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_dedupeGroupId", "_importAction",
        "_hasPhoto", "_photoByteLength", "_photoSha256",
        "FirstName", "LastName", "MiddleName", "DateOfBirth", "BirthPlace",
        "CountryOfBirth", "ForeignAddress", "ForeignAddressCountry", "Gender",
        "PersonalNumber", "Nationality", "Email", "IsEmployee", "PersonRole",
        "IsArchived", "SponsoringEmployee", "Relationship", "ProjectContract",
        "MaritalStatus", "VisaApplicationFamilyMembersText",
        "_legacy_Relationship", "_legacy_ProjectContract",
        "_legacy_MaritalStatusStatus", "_legacy_MaritalStatusText",
    ];

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        string lookupTranslationsPath,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationsPath);
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows}) * FROM ({ExtractSql}) AS q"
            : ExtractSql;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014PersonRawRow>();
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

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014PersonRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        parsed = new Visa2014PersonRawRow(
            LegacyOid: legacyOid,
            FirstName: row.GetValueOrDefault("FirstName"),
            LastName: row.GetValueOrDefault("LastName"),
            MiddleName: row.GetValueOrDefault("MiddleName"),
            BirthDate: DateTime.TryParse(row.GetValueOrDefault("BirthDate"), out var dob) ? dob : null,
            BirthPlace: row.GetValueOrDefault("BirthPlace"),
            LegacyBirthCountry: row.GetValueOrDefault("LegacyBirthCountry"),
            ForeignAddress: row.GetValueOrDefault("ForeignAddress"),
            LegacyForeignAddressCountry: row.GetValueOrDefault("LegacyForeignAddressCountry"),
            LegacyGender: row.GetValueOrDefault("LegacyGender"),
            IsEmployee: row.GetValueOrDefault("IsEmployee") == "1",
            IsFamilyMember: row.GetValueOrDefault("IsFamilyMember") == "1",
            LegacyEmployeeOid: Guid.TryParse(row.GetValueOrDefault("LegacyEmployeeOid"), out var emp) ? emp : null,
            LegacyRelationship: row.GetValueOrDefault("LegacyRelationship"),
            LegacyProjectContract: row.GetValueOrDefault("LegacyProjectContract"),
            LegacyMaritalStatusStatus: row.GetValueOrDefault("LegacyMaritalStatusStatus"),
            LegacyMaritalStatusText: row.GetValueOrDefault("LegacyMaritalStatusText"),
            ActivePerson: row.GetValueOrDefault("ActivePerson") == "1",
            HasPhoto: row.GetValueOrDefault("HasPhoto") == "1",
            PhotoByteLength: int.TryParse(row.GetValueOrDefault("PhotoByteLength"), out var len) ? len : 0,
            PhotoSha256: row.GetValueOrDefault("PhotoSha256"),
            RawPersonalNumber: row.GetValueOrDefault("RawPersonalNumber"),
            LegacyNationality: row.GetValueOrDefault("LegacyNationality"));
        return true;
    }

    internal static TransformBatchResult TransformRows(
        IReadOnlyList<Visa2014PersonRawRow> rawRows,
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
        ApplyPersonalNumberDedupe(working, dedupeSummary);

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

        return new TransformBatchResult(importRows, dedupeMergedCount);
    }

    internal sealed record TransformBatchResult(List<Dictionary<string, object?>> ImportRows, int DedupeMergedCount);

    private sealed class WorkingRow(Visa2014PersonRawRow Raw)
    {
        public Visa2014PersonRawRow Raw { get; } = Raw;
        public string ImportAction { get; set; } = "import";
        public string? DedupeGroupId { get; set; }
    }

    private static void ApplyPersonalNumberDedupe(List<WorkingRow> rows, List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new { Row = r, Norm = NormalizePersonalNumber(r.Raw.RawPersonalNumber) })
            .Where(x => !IsSentinelPersonalNumber(x.Norm))
            .GroupBy(x => x.Norm.ToUpperInvariant(), StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var canonical = members
                .OrderByDescending(x => CompletenessScore(x.Row.Raw))
                .ThenBy(x => x.Row.Raw.LegacyOid)
                .First();

            var groupId = $"PN:{group.Key}";
            foreach (var member in members)
            {
                member.Row.DedupeGroupId = groupId;
                if (!ReferenceEquals(member.Row, canonical.Row))
                    member.Row.ImportAction = "duplicate_merged";
            }

            dedupeSummary.Add(new Dictionary<string, object?>
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "PersonalNumber",
                ["normalizedValue"] = group.Key,
                ["memberCount"] = members.Count,
                ["canonical_legacyRowId"] = canonical.Row.Raw.LegacyOid,
                ["canonicalRule"] = "most_complete; tieBreak Oid",
            });
        }
    }

    private static int CompletenessScore(Visa2014PersonRawRow row)
    {
        int score = 0;
        if (!string.IsNullOrWhiteSpace(row.FirstName)) score++;
        if (!string.IsNullOrWhiteSpace(row.LastName)) score++;
        if (!string.IsNullOrWhiteSpace(row.MiddleName)) score++;
        if (row.BirthDate.HasValue) score++;
        if (!string.IsNullOrWhiteSpace(row.BirthPlace)) score++;
        if (!string.IsNullOrWhiteSpace(row.ForeignAddress)) score++;
        if (row.HasPhoto) score++;
        return score;
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
            ["_legacyTable"] = "Person",
            ["_dedupeGroupId"] = working.DedupeGroupId ?? "",
            ["_importAction"] = working.ImportAction,
            ["_hasPhoto"] = raw.HasPhoto,
            ["_photoByteLength"] = raw.PhotoByteLength,
            ["_photoSha256"] = raw.PhotoSha256 ?? "",
        };

        if (string.IsNullOrWhiteSpace(raw.FirstName) ||
            string.IsNullOrWhiteSpace(raw.LastName) ||
            !raw.BirthDate.HasValue)
        {
            skipReason = "required_null:FirstName|LastName|DateOfBirth";
            row["FirstName"] = raw.FirstName;
            row["LastName"] = raw.LastName;
            row["DateOfBirth"] = raw.BirthDate;
            return row;
        }

        row["FirstName"] = raw.FirstName.Trim();
        row["LastName"] = raw.LastName.Trim();
        row["MiddleName"] = string.IsNullOrWhiteSpace(raw.MiddleName) ? null : raw.MiddleName.Trim();
        row["DateOfBirth"] = raw.BirthDate;
        row["BirthPlace"] = string.IsNullOrWhiteSpace(raw.BirthPlace) ? null : raw.BirthPlace.Trim();
        row["ForeignAddress"] = string.IsNullOrWhiteSpace(raw.ForeignAddress) ? null : raw.ForeignAddress.Trim();
        row["PersonalNumber"] = NormalizePersonalNumber(raw.RawPersonalNumber);
        row["Email"] = "";
        row["IsEmployee"] = raw.IsEmployee;
        row["PersonRole"] = raw.IsEmployee ? "Employee" : "FamilyMember";
        row["IsArchived"] = !raw.ActivePerson;
        row["SponsoringEmployee"] = raw.LegacyEmployeeOid?.ToString();

        TrySetLookup(row, catalogs, "Country", raw.LegacyBirthCountry, "CountryOfBirth", unmapped, ref skipReason);
        TrySetLookup(row, catalogs, "Country", raw.LegacyForeignAddressCountry, "ForeignAddressCountry", unmapped, ref skipReason);
        TrySetLookup(row, catalogs, "Country", raw.LegacyNationality, "Nationality", unmapped, ref skipReason);
        TrySetLookup(row, catalogs, "Gender", raw.LegacyGender, "Gender", unmapped, ref skipReason);
        TrySetLookup(row, catalogs, "MaritalStatus", raw.LegacyMaritalStatusStatus, "MaritalStatus", unmapped, ref skipReason);
        TrySetLookup(row, catalogs, "Relationship", raw.LegacyRelationship, "Relationship", unmapped, ref skipReason);
        TrySetLookup(row, catalogs, "ProjectContract", raw.LegacyProjectContract, "ProjectContract", unmapped, ref skipReason);

        row["_legacy_Relationship"] = raw.LegacyRelationship;
        row["_legacy_ProjectContract"] = raw.LegacyProjectContract;
        row["_legacy_MaritalStatusStatus"] = raw.LegacyMaritalStatusStatus;
        row["_legacy_MaritalStatusText"] = raw.LegacyMaritalStatusText;

        row["VisaApplicationFamilyMembersText"] =
            string.IsNullOrWhiteSpace(raw.LegacyMaritalStatusText) ? null : raw.LegacyMaritalStatusText.Trim();

        return row;
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

    internal static string NormalizePersonalNumber(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "0";
        var trimmed = raw.Trim();
        return trimmed is "-" or "." ? "0" : trimmed;
    }

    private static bool IsSentinelPersonalNumber(string normalized) =>
        string.IsNullOrWhiteSpace(normalized) ||
        string.Equals(normalized, "0", StringComparison.Ordinal);

    internal static List<string> InferColumns(
        IReadOnlyList<Dictionary<string, object?>> rows,
        IReadOnlyList<string> preferredOrder)
    {
        var set = new HashSet<string>(preferredOrder, StringComparer.Ordinal);
        foreach (var row in rows)
        {
            foreach (var key in row.Keys)
                set.Add(key);
        }

        var ordered = preferredOrder.Where(set.Contains).ToList();
        ordered.AddRange(set.Except(ordered, StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal));
        return ordered;
    }
}
