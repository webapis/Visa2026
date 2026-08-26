namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014VisaRawRow(
    Guid LegacyOid,
    string? VisaNumber,
    string? TypeOfVisaL,
    string? MgCode,
    string? CategoryOfVisaL,
    string? CategoryMgCode,
    string? IssuedPlaceOfVisaL,
    DateTime? IssueDate,
    DateTime? StartDate,
    DateTime? ExpirationDate,
    Guid LegacyPassportOid,
    string? AsNumber,
    Guid? LegacyPersonInApplicationProfileInstanceOid,
    bool IsFamilyMemberPerson,
    bool BzDasoguz,
    bool BzTagtabazar,
    bool BzSerhetabat,
    bool BzYoloten,
    bool BzFarap,
    bool BzGarabogaz,
    bool BzSarahs,
    bool BzEtrek,
    bool HasBorderZoneFk,
    bool HasVisaDocument,
    int VisaDocumentByteLength);

internal static class Visa2014VisaTransform
{
    private static readonly string[] SentinelVisaNumbers =
    [
        "AFV0000000",
        "JLV0000000",
    ];

    private static readonly HashSet<string> VisaIssuedPlaceSkipLabels = new(StringComparer.Ordinal)
    {
        "DELİ", "Pekin", "Serhetabat", "London", "Kazan", "Taşkent", "Mary H.M.", "BAKU",
    };

    private static readonly (string BitKey, Func<Visa2014VisaRawRow, bool> Getter)[] BorderZoneBitOrder =
    [
        ("Daşoguz", r => r.BzDasoguz),
        ("Tagtabazar", r => r.BzTagtabazar),
        ("Serhetabat", r => r.BzSerhetabat),
        ("Ýolöten", r => r.BzYoloten),
        ("Farap", r => r.BzFarap),
        ("Sarahs", r => r.BzSarahs),
        ("Garabogaz", r => r.BzGarabogaz),
        ("Etrek", r => r.BzEtrek),
    ];

    internal const string ExtractSql = """
        SELECT
            CAST(v.Oid AS varchar(36)) AS Oid,
            v.VisaNumber,
            d.TypeOfVisaL,
            ISNULL(CAST(d.mgCode AS varchar(10)), '') AS mgCode,
            vc.CategoryOfVisaL,
            ISNULL(CAST(vc.mgCode AS varchar(10)), '') AS CategoryMgCode,
            vip.IssuedPlaceOfVisaL,
            CONVERT(varchar(10), v.VisaIssuedDate, 23) AS VisaIssuedDate,
            CONVERT(varchar(10), v.VisaStartDate, 23) AS VisaStartDate,
            CONVERT(varchar(10), v.VisaEndDate, 23) AS VisaEndDate,
            CAST(v.Passport AS varchar(36)) AS LegacyPassportOid,
            v.ASNumber,
            CAST(v.ProcessNumber AS varchar(36)) AS LegacyPersonInApplicationProfileInstanceOid,
            CASE WHEN v.BorderZone IS NULL THEN '0' ELSE '1' END AS HasBorderZoneFk,
            CASE WHEN ISNULL(bz.[Daşoguz], 0) = 1 THEN '1' ELSE '0' END AS BzDasoguz,
            CASE WHEN ISNULL(bz.Tagtabazar, 0) = 1 THEN '1' ELSE '0' END AS BzTagtabazar,
            CASE WHEN ISNULL(bz.Serhetabat, 0) = 1 THEN '1' ELSE '0' END AS BzSerhetabat,
            CASE WHEN ISNULL(bz.[Ýolöten], 0) = 1 THEN '1' ELSE '0' END AS BzYoloten,
            CASE WHEN ISNULL(bz.Farap, 0) = 1 THEN '1' ELSE '0' END AS BzFarap,
            CASE WHEN ISNULL(bz.Garabogaz, 0) = 1 THEN '1' ELSE '0' END AS BzGarabogaz,
            CASE WHEN ISNULL(bz.Sarahs, 0) = 1 THEN '1' ELSE '0' END AS BzSarahs,
            CASE WHEN ISNULL(bz.Etrek, 0) = 1 THEN '1' ELSE '0' END AS BzEtrek,
            CASE WHEN ISNULL(person.IsFamilyMember, 0) = 1 AND ISNULL(person.IsEmployee, 0) = 0 THEN '1' ELSE '0' END AS IsFamilyMemberPerson,
            CASE WHEN v.[GöçürmeNusga] IS NOT NULL AND DATALENGTH(v.[GöçürmeNusga]) > 0 THEN '1' ELSE '0' END AS HasVisaDocument,
            ISNULL(DATALENGTH(v.[GöçürmeNusga]), 0) AS VisaDocumentByteLength
        FROM dbo.Visa v
        INNER JOIN dbo.Passport p ON v.Passport = p.Oid AND p.GCRecord IS NULL
        INNER JOIN dbo.Person person ON person.Oid = p.Person AND person.GCRecord IS NULL
        LEFT JOIN dbo.VisaType vt ON v.VisaType = vt.Oid
        LEFT JOIN dbo.IVisaType_Data d ON vt.IVisaType_Data = d.Oid
        LEFT JOIN dbo.VisaCategory vc ON v.VisaCategory = vc.Oid
        LEFT JOIN dbo.VisaIssuedPlace vip ON v.VisaIssuedPlace = vip.Oid
        LEFT JOIN dbo.BorderZoneForVisa bz ON v.BorderZone = bz.Oid AND bz.GCRecord IS NULL
        WHERE v.GCRecord IS NULL
        """;

    internal static readonly string[] VisaMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_dedupeGroupId", "_importAction",
        "_hasVisaDocument", "_visaDocumentByteLength",
        "VisaNumber", "VisaType", "VisaCategory", "VisaIssuedPlace",
        "IssueDate", "StartDate", "ExpirationDate", "BorderZoneLocation", "Passport",
        "Application", "ProcessNumber", "LegacyPersonInApplicationProfileInstanceOid",
        "ExtensionRequired", "IsCancelled", "IsChanged", "IsExtended", "ShowOptionalFields",
        "IssuingInvitationItem", "Notes",
        "_legacy_VisaTypeComposite", "_legacy_VisaTypePersonOverride", "_legacy_VisaCategoryComposite",
        "_legacy_IssuedPlaceOfVisaL", "_legacy_PassportOid", "_legacy_ASNumber", "_legacy_PersonInApplicationProfileInstanceOid",
        "_legacy_ApplicationProfileInstanceOid",
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
        var rawRows = new List<Visa2014VisaRawRow>();
        var parseSkipped = 0;
        foreach (var dict in dictRows)
        {
            if (TryParseRawRow(dict, out var parsed))
                rawRows.Add(parsed);
            else
                parseSkipped++;
        }

        if (verbose && parseSkipped > 0)
            Console.WriteLine($"  Skipped {parseSkipped} sqlcmd row(s) with invalid shape.");

        var cancellationIndex = Visa2014LegacyDocumentCancellationIndex.Load(
            connectionString,
            lookupTranslationPaths,
            verbose);

        var issuingApplicationByVisa = Visa2014VisaIssuingApplicationProfileInstanceIndex.Load(
            connectionString,
            verbose);

        var transformed = TransformRows(
            rawRows,
            catalogs,
            cancellationIndex,
            issuingApplicationByVisa,
            out var skipped,
            out var unmappedDistinct,
            out var dedupeSummary);
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

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014VisaRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        if (!row.TryGetValue("LegacyPassportOid", out var passportText) ||
            !Guid.TryParse(passportText?.Trim(), out var legacyPassportOid))
            return false;

        parsed = new Visa2014VisaRawRow(
            LegacyOid: legacyOid,
            VisaNumber: row.GetValueOrDefault("VisaNumber"),
            TypeOfVisaL: row.GetValueOrDefault("TypeOfVisaL"),
            MgCode: row.GetValueOrDefault("MgCode"),
            CategoryOfVisaL: row.GetValueOrDefault("CategoryOfVisaL"),
            CategoryMgCode: row.GetValueOrDefault("CategoryMgCode"),
            IssuedPlaceOfVisaL: row.GetValueOrDefault("IssuedPlaceOfVisaL"),
            IssueDate: DateTime.TryParse(row.GetValueOrDefault("VisaIssuedDate"), out var issued) ? issued : null,
            StartDate: DateTime.TryParse(row.GetValueOrDefault("VisaStartDate"), out var start) ? start : null,
            ExpirationDate: DateTime.TryParse(row.GetValueOrDefault("VisaEndDate"), out var expires) ? expires : null,
            LegacyPassportOid: legacyPassportOid,
            AsNumber: string.IsNullOrWhiteSpace(row.GetValueOrDefault("ASNumber"))
                ? null
                : row.GetValueOrDefault("ASNumber")!.Trim(),
            LegacyPersonInApplicationProfileInstanceOid: Guid.TryParse(
                    row.GetValueOrDefault("LegacyPersonInApplicationProfileInstanceOid")?.Trim(), out var piaOid)
                ? piaOid
                : null,
            IsFamilyMemberPerson: row.GetValueOrDefault("IsFamilyMemberPerson") == "1",
            BzDasoguz: row.GetValueOrDefault("BzDasoguz") == "1",
            BzTagtabazar: row.GetValueOrDefault("BzTagtabazar") == "1",
            BzSerhetabat: row.GetValueOrDefault("BzSerhetabat") == "1",
            BzYoloten: row.GetValueOrDefault("BzYoloten") == "1",
            BzFarap: row.GetValueOrDefault("BzFarap") == "1",
            BzGarabogaz: row.GetValueOrDefault("BzGarabogaz") == "1",
            BzSarahs: row.GetValueOrDefault("BzSarahs") == "1",
            BzEtrek: row.GetValueOrDefault("BzEtrek") == "1",
            HasBorderZoneFk: row.GetValueOrDefault("HasBorderZoneFk") == "1",
            HasVisaDocument: row.GetValueOrDefault("HasVisaDocument") == "1",
            VisaDocumentByteLength: int.TryParse(row.GetValueOrDefault("VisaDocumentByteLength"), out var len) ? len : 0);
        return true;
    }

    internal static Visa2014PersonTransform.TransformBatchResult TransformRows(
        IReadOnlyList<Visa2014VisaRawRow> rawRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Visa2014LegacyDocumentCancellationIndex cancellationIndex,
        IReadOnlyDictionary<Guid, Guid> issuingApplicationByVisa,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        unmappedDistinct = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);

        var working = rawRows.Select(r => new WorkingRow(r)).ToList();
        ApplyVisaNumberDedupe(working, dedupeSummary);

        var importRows = new List<Dictionary<string, object?>>();
        var dedupeMergedCount = 0;

        foreach (var row in working)
        {
            if (row.ImportAction == "duplicate_merged")
            {
                dedupeMergedCount++;
                continue;
            }

            var export = BuildExportRow(
                row,
                catalogs,
                cancellationIndex,
                issuingApplicationByVisa,
                out var skipReason,
                out var rowUnmapped);
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

    private sealed class WorkingRow(Visa2014VisaRawRow Raw)
    {
        public Visa2014VisaRawRow Raw { get; } = Raw;
        public string ImportAction { get; set; } = "import";
        public string? DedupeGroupId { get; set; }
    }

    private static void ApplyVisaNumberDedupe(List<WorkingRow> rows, List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new { Row = r, Norm = NormalizeVisaNumber(r.Raw.VisaNumber) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Norm) && !IsSentinelVisaNumber(x.Norm))
            .GroupBy(x => x.Norm.ToUpperInvariant(), StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var canonical = members
                .OrderByDescending(x => x.Row.Raw.ExpirationDate ?? DateTime.MinValue)
                .ThenBy(x => x.Row.Raw.LegacyOid)
                .First();

            var groupId = $"VNO:{group.Key}";
            foreach (var member in members)
            {
                member.Row.DedupeGroupId = groupId;
                if (!ReferenceEquals(member.Row, canonical.Row))
                    member.Row.ImportAction = "duplicate_merged";
            }

            dedupeSummary.Add(new Dictionary<string, object?>
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "VisaNumber",
                ["normalizedValue"] = group.Key,
                ["memberCount"] = members.Count,
                ["canonical_legacyRowId"] = canonical.Row.Raw.LegacyOid,
                ["canonicalRule"] = "most_recent_end_date; tieBreak Oid",
            });
        }
    }

    private static Dictionary<string, object?> BuildExportRow(
        WorkingRow working,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Visa2014LegacyDocumentCancellationIndex cancellationIndex,
        IReadOnlyDictionary<Guid, Guid> issuingApplicationByVisa,
        out string? skipReason,
        out List<string> unmapped)
    {
        skipReason = null;
        unmapped = [];
        var raw = working.Raw;

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "Visa",
            ["_dedupeGroupId"] = working.DedupeGroupId ?? "",
            ["_importAction"] = working.ImportAction,
            ["_hasVisaDocument"] = raw.HasVisaDocument,
            ["_visaDocumentByteLength"] = raw.VisaDocumentByteLength,
        };

        if (string.IsNullOrWhiteSpace(raw.VisaNumber) ||
            !raw.IssueDate.HasValue ||
            !raw.StartDate.HasValue ||
            !raw.ExpirationDate.HasValue)
        {
            skipReason = "required_null:VisaNumber|IssueDate|StartDate|ExpirationDate";
            row["VisaNumber"] = raw.VisaNumber;
            row["IssueDate"] = raw.IssueDate;
            row["StartDate"] = raw.StartDate;
            row["ExpirationDate"] = raw.ExpirationDate;
            return row;
        }

        if (raw.ExpirationDate <= raw.StartDate)
        {
            skipReason = "invalid_date_range:ExpirationDate<=StartDate";
            row["VisaNumber"] = raw.VisaNumber;
            row["ExpirationDate"] = raw.ExpirationDate;
            row["StartDate"] = raw.StartDate;
            return row;
        }

        if (string.IsNullOrWhiteSpace(raw.CategoryOfVisaL))
        {
            skipReason = "required_null:VisaCategory";
            row["VisaNumber"] = raw.VisaNumber;
            return row;
        }

        var visaNumber = NormalizeVisaNumber(raw.VisaNumber);
        if (IsSentinelVisaNumber(visaNumber))
            visaNumber = AppendLegacyOidTail(visaNumber, raw.LegacyOid);

        row["VisaNumber"] = visaNumber;
        row["IssueDate"] = raw.IssueDate;
        row["StartDate"] = raw.StartDate;
        row["ExpirationDate"] = raw.ExpirationDate;
        row["Passport"] = raw.LegacyPassportOid.ToString("D");
        row["ProcessNumber"] = raw.AsNumber;
        row["LegacyPersonInApplicationProfileInstanceOid"] = raw.LegacyPersonInApplicationProfileInstanceOid;
        row["_legacy_ASNumber"] = raw.AsNumber;
        row["_legacy_PersonInApplicationProfileInstanceOid"] = raw.LegacyPersonInApplicationProfileInstanceOid?.ToString("D");
        row["ExtensionRequired"] = true;
        row["IsCancelled"] = cancellationIndex.IsVisaCancelled(raw.LegacyOid);
        row["IsChanged"] = false;
        row["IsExtended"] = false;
        row["ShowOptionalFields"] = false;
        row["IssuingInvitationItem"] = null;
        row["Notes"] = null;

        if (issuingApplicationByVisa.TryGetValue(raw.LegacyOid, out var legacyApplicationOid))
        {
            row["Application"] = legacyApplicationOid.ToString("D");
            row["_legacy_ApplicationProfileInstanceOid"] = legacyApplicationOid.ToString("D");
        }

        var visaTypeComposite = BuildComposite(raw.TypeOfVisaL, raw.MgCode);
        row["_legacy_VisaTypeComposite"] = visaTypeComposite;
        TrySetVisaType(row, catalogs, visaTypeComposite, raw.IsFamilyMemberPerson, unmapped);

        var visaCategoryComposite = BuildComposite(raw.CategoryOfVisaL, raw.CategoryMgCode);
        row["_legacy_VisaCategoryComposite"] = visaCategoryComposite;
        TrySetVisaCategory(row, catalogs, visaCategoryComposite, unmapped);

        row["_legacy_IssuedPlaceOfVisaL"] = raw.IssuedPlaceOfVisaL;
        TrySetVisaIssuedPlace(row, catalogs, raw.IssuedPlaceOfVisaL, unmapped, ref skipReason);

        row["BorderZoneLocation"] = BuildBorderZoneLocation(catalogs, raw);
        row["_legacy_PassportOid"] = raw.LegacyPassportOid.ToString("D");

        return row;
    }

    private static void TrySetVisaType(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string composite,
        bool isFamilyMemberPerson,
        List<string> unmapped)
    {
        var key = ResolveVisaTypeLocalizationKey(
            isFamilyMemberPerson, composite, catalogs, out var reason, out var personOverride);
        row["VisaType"] = key;
        if (personOverride)
            row["_legacy_VisaTypePersonOverride"] = "family_member->FM";
        if (reason != null)
            unmapped.Add(reason);
    }

    /// <summary>
    /// Family-member persons always map to FM (legacy often stores WP:11 incorrectly).
    /// Otherwise TypeOfVisaL:mgCode via lookup-translations; unmapped default WP.
    /// </summary>
    internal static string ResolveVisaTypeLocalizationKey(
        bool isFamilyMemberPerson,
        string composite,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? unmappedReason,
        out bool personOverride)
    {
        unmappedReason = null;
        personOverride = false;
        if (isFamilyMemberPerson)
        {
            personOverride = true;
            return "FM";
        }

        if (Visa2014LookupTranslator.TryTranslate(catalogs, "VisaType", composite, out var target, out var reason) &&
            !string.IsNullOrWhiteSpace(target))
        {
            unmappedReason = reason;
            return target!;
        }

        unmappedReason = reason;
        return "WP";
    }

    private static void TrySetVisaCategory(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string composite,
        List<string> unmapped)
    {
        if (Visa2014LookupTranslator.TryTranslate(catalogs, "VisaCategory", composite, out var target, out var reason) &&
            !string.IsNullOrWhiteSpace(target))
        {
            row["VisaCategory"] = target;
            if (reason != null)
                unmapped.Add(reason);
            return;
        }

        if (reason != null)
            unmapped.Add(reason);

        row["VisaCategory"] = "Multiple";
    }

    private static void TrySetVisaIssuedPlace(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string? legacyLabel,
        List<string> unmapped,
        ref string? skipReason)
    {
        if (string.IsNullOrWhiteSpace(legacyLabel))
        {
            skipReason ??= "required_null:VisaIssuedPlace";
            row["VisaIssuedPlace"] = null;
            return;
        }

        var trimmed = legacyLabel.Trim();

        if (Visa2014LookupTranslator.TryTranslate(catalogs, "VisaIssuedPlace", trimmed, out var mapped, out var reason) &&
            !string.IsNullOrWhiteSpace(mapped))
        {
            row["VisaIssuedPlace"] = mapped;
            return;
        }

        if (IsVisaIssuedPlaceSkipLabel(trimmed))
        {
            if (reason != null)
                unmapped.Add(reason);
            skipReason ??= reason ?? $"unmapped_lookup:VisaIssuedPlace:{trimmed}";
            row["VisaIssuedPlace"] = null;
            return;
        }

        row["VisaIssuedPlace"] = trimmed;
    }

    private static bool IsVisaIssuedPlaceSkipLabel(string trimmed) =>
        VisaIssuedPlaceSkipLabels.Any(label =>
            Visa2014CatalogMatchHelper.KeysEqual(label, trimmed));

    private static string BuildBorderZoneLocation(
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Visa2014VisaRawRow raw)
    {
        if (!raw.HasBorderZoneFk)
            return CommaSeparatedNoneValue;

        catalogs.TryGetValue("BorderZoneName", out var catalog);
        var bitToTarget = catalog?.LegacyToTarget ?? new Dictionary<string, string>(StringComparer.Ordinal);

        var labels = new List<string>();
        foreach (var (bitKey, getter) in BorderZoneBitOrder)
        {
            if (!getter(raw))
                continue;

            if (TryResolveBitTarget(bitKey, bitToTarget, out var target))
                labels.Add(target);
        }

        return labels.Count == 0 ? CommaSeparatedNoneValue : string.Join(", ", labels);
    }

    private static bool TryResolveBitTarget(
        string bitKey,
        IReadOnlyDictionary<string, string> bitToTarget,
        out string target)
    {
        if (bitToTarget.TryGetValue(bitKey, out var exact) && !string.IsNullOrWhiteSpace(exact))
        {
            target = exact;
            return true;
        }

        foreach (var (legacy, mapped) in bitToTarget)
        {
            if (Visa2014CatalogMatchHelper.KeysEqual(legacy, bitKey))
            {
                target = mapped;
                return true;
            }
        }

        target = bitKey;
        return true;
    }

    private static string BuildComposite(string? leftPart, string? rightPart)
    {
        var left = string.IsNullOrWhiteSpace(leftPart) ? "" : leftPart.Trim();
        var right = string.IsNullOrWhiteSpace(rightPart) ? "" : rightPart.Trim();
        return $"{left}:{right}";
    }

    private static string NormalizeVisaNumber(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();

    private static bool IsSentinelVisaNumber(string normalized) =>
        SentinelVisaNumbers.Any(s =>
            string.Equals(s, normalized, StringComparison.OrdinalIgnoreCase));

    private static string AppendLegacyOidTail(string visaNumber, Guid legacyOid) =>
        visaNumber + legacyOid.ToString("N")[^8..];

    private const string CommaSeparatedNoneValue = "Ýok";
}
