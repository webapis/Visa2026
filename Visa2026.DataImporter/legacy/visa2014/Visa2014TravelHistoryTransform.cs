using Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014TravelHistoryRawRow(
    Guid LegacyPiaOid,
    Guid? LegacyTravelInformationOid,
    Guid LegacyPersonOid,
    DateTime? TravelDate,
    DateTime? RegistrationDate,
    bool ForEmployee,
    bool ForFamilyMember,
    int? EmployeeSubtypeId,
    int? FamilySubtypeId,
    bool HasInvitationWpFk,
    int? InvitationAndWorkPermitRequired,
    bool HasWizaWpFk,
    int? WizaAndWorkPermitRequired,
    int? ChangeInformation,
    string? CheckPointMgCode,
    string? CheckPointLabel,
    string? PurposeOfTravelLabel);

internal static class Visa2014TravelHistoryTransform
{
    internal const string ExtractSql = """
        SELECT
            CAST(pia.Oid AS varchar(36)) AS Oid,
            CAST(ti.Oid AS varchar(36)) AS TravelInformationOid,
            CAST(CASE WHEN ISNULL(a.ForEmployee, 0) = 1 THEN pia.Employee ELSE pia.FamilyMember END AS varchar(36)) AS LegacyPersonOid,
            CONVERT(varchar(10), ti.TravelDate, 23) AS TravelDate,
            CONVERT(varchar(10), pia.RegistrationDate, 23) AS RegistrationDate,
            CASE WHEN ISNULL(a.ForEmployee, 0) = 1 THEN '1' ELSE '0' END AS ForEmployee,
            CASE WHEN ISNULL(a.ForFamilyMember, 0) = 1 THEN '1' ELSE '0' END AS ForFamilyMember,
            ate.TypeOfApplicationForEmployee AS EmployeeSubtypeId,
            atfm.TypeOfApplicationForFamilyMember AS FamilySubtypeId,
            CASE WHEN a.IsInvitationWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasInvitationWpFk,
            iwp.InvitationAndWorkPermitRequired,
            CASE WHEN a.IsWizaWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasWizaWpFk,
            wwp.WizaAndWorkPermitRequired,
            a.ChangeInformation,
            ISNULL(CAST(cp_line.TitleOfCheckPoint AS varchar(10)), ISNULL(CAST(cp_ti.TitleOfCheckPoint AS varchar(10)), '')) AS CheckPointMgCode,
            ISNULL(cp_line.TitleOfCheckPointL, cp_ti.TitleOfCheckPointL) AS CheckPointLabel,
            pot.PurposeOfTravelL AS PurposeOfTravelLabel
        FROM dbo.PersonInApplication pia
        INNER JOIN dbo.Application a ON a.Oid = pia.Application AND a.GCRecord IS NULL
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        LEFT JOIN dbo.IsInvitationWithWorkPermit iwp ON iwp.Oid = a.IsInvitationWithWorkPermit
        LEFT JOIN dbo.IsWizaWithWorkPermit wwp ON wwp.Oid = a.IsWizaWithWorkPermit
        LEFT JOIN dbo.TravelInformation ti ON ti.Oid = CASE
            WHEN ISNULL(a.ForEmployee, 0) = 1 THEN pia.EmployeeEntryDate
            ELSE pia.FamilyMemberEntryDate
        END
        LEFT JOIN dbo.[CheckPoint] cp_line ON cp_line.Oid = pia.[CheckPoint]
        LEFT JOIN dbo.[CheckPoint] cp_ti ON cp_ti.Oid = ti.[CheckPoint]
        LEFT JOIN dbo.PurposeOfTravel pot ON pot.Oid = pia.PurposeOfTrave
        WHERE pia.GCRecord IS NULL
          AND CASE WHEN ISNULL(a.ForEmployee, 0) = 1 THEN pia.Employee ELSE pia.FamilyMember END IS NOT NULL
        """;

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
        var rawRows = new List<Visa2014TravelHistoryRawRow>();
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

        return TransformRows(rawRows, catalogs, out _, out _, out _);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014TravelHistoryRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) || !Guid.TryParse(oidText?.Trim(), out var piaOid))
            return false;
        if (!row.TryGetValue("LegacyPersonOid", out var personText) ||
            !Guid.TryParse(personText?.Trim(), out var personOid))
            return false;

        parsed = new Visa2014TravelHistoryRawRow(
            LegacyPiaOid: piaOid,
            LegacyTravelInformationOid: TryParseGuid(row.GetValueOrDefault("TravelInformationOid")),
            LegacyPersonOid: personOid,
            TravelDate: TryParseDate(row.GetValueOrDefault("TravelDate")),
            RegistrationDate: TryParseDate(row.GetValueOrDefault("RegistrationDate")),
            ForEmployee: row.GetValueOrDefault("ForEmployee") == "1",
            ForFamilyMember: row.GetValueOrDefault("ForFamilyMember") == "1",
            EmployeeSubtypeId: TryParseInt(row.GetValueOrDefault("EmployeeSubtypeId")),
            FamilySubtypeId: TryParseInt(row.GetValueOrDefault("FamilySubtypeId")),
            HasInvitationWpFk: row.GetValueOrDefault("HasInvitationWpFk") == "1",
            InvitationAndWorkPermitRequired: TryParseInt(row.GetValueOrDefault("InvitationAndWorkPermitRequired")),
            HasWizaWpFk: row.GetValueOrDefault("HasWizaWpFk") == "1",
            WizaAndWorkPermitRequired: TryParseInt(row.GetValueOrDefault("WizaAndWorkPermitRequired")),
            ChangeInformation: TryParseInt(row.GetValueOrDefault("ChangeInformation")),
            CheckPointMgCode: NullIfEmpty(row.GetValueOrDefault("CheckPointMgCode")),
            CheckPointLabel: row.GetValueOrDefault("CheckPointLabel"),
            PurposeOfTravelLabel: row.GetValueOrDefault("PurposeOfTravelLabel"));
        return true;
    }

    internal static Type? ResolveConcreteType(string? travelType, string? movementType)
    {
        if (string.Equals(travelType, "External", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(movementType, "Entry", StringComparison.OrdinalIgnoreCase))
            return typeof(ExternalArrival);
        if (string.Equals(travelType, "External", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(movementType, "Exit", StringComparison.OrdinalIgnoreCase))
            return typeof(ExternalDeparture);
        if (string.Equals(travelType, "Internal", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(movementType, "Entry", StringComparison.OrdinalIgnoreCase))
            return typeof(InternalArrival);
        if (string.Equals(travelType, "Internal", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(movementType, "Exit", StringComparison.OrdinalIgnoreCase))
            return typeof(InternalDeparture);
        return null;
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014TravelHistoryRawRow> rawRows,
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
        var seen = new HashSet<string>(StringComparer.Ordinal);

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

            var dedupeKey = string.Join('|',
                row.GetValueOrDefault("_legacy_PersonOid"),
                row.GetValueOrDefault("TravelDate"),
                row.GetValueOrDefault("TravelType"),
                row.GetValueOrDefault("MovementType"),
                row.GetValueOrDefault("CheckPoint") ?? "");
            if (!seen.Add(dedupeKey))
            {
                row["_skipReason"] = "dedupe_duplicate";
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
            DedupeMergedCount = skipped.Count(r => Equals(r.GetValueOrDefault("_skipReason"), "dedupe_duplicate")),
        };
    }

    private static Dictionary<string, object?> BuildExportRow(
        Visa2014TravelHistoryRawRow raw,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out string? skipReason,
        out List<string> unmapped)
    {
        skipReason = null;
        unmapped = [];
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyPiaOid,
            ["_legacyTable"] = "PersonInApplication",
            ["_legacyTravelInformationOid"] = raw.LegacyTravelInformationOid?.ToString("D"),
            ["_legacy_PersonOid"] = raw.LegacyPersonOid.ToString("D"),
            ["Person"] = raw.LegacyPersonOid.ToString("D"),
            ["_importAction"] = "import",
        };

        var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            raw.ForEmployee,
            raw.ForFamilyMember,
            raw.EmployeeSubtypeId,
            raw.FamilySubtypeId,
            raw.HasInvitationWpFk,
            raw.InvitationAndWorkPermitRequired,
            raw.HasWizaWpFk,
            raw.WizaAndWorkPermitRequired,
            raw.ChangeInformation);
        row["_legacy_ApplicationTypeComposite"] = composite;

        if (Visa2014ApplicationTransform.IsSkippedApplicationTypeComposite(composite))
        {
            skipReason = $"skip_row:parent_ApplicationType:{composite}";
            return row;
        }

        if (!Visa2014LookupTranslator.TryTranslate(catalogs, "ApplicationType", composite, out var appTypeTarget, out var appTypeReason)
            || string.IsNullOrWhiteSpace(appTypeTarget))
        {
            if (appTypeReason != null)
                unmapped.Add(appTypeReason);
            skipReason = appTypeReason ?? $"unmapped_lookup:ApplicationType:{composite}";
            return row;
        }

        row["_parentApplicationType"] = appTypeTarget;
        Visa2014ApplicationItemTransform.DeriveRegistrationTravelTypes(appTypeTarget, row);
        if (ResolveConcreteType(row.GetValueOrDefault("TravelType") as string, row.GetValueOrDefault("MovementType") as string) == null)
        {
            skipReason = $"not_registration_travel:{appTypeTarget}";
            return row;
        }

        var travelDate = raw.TravelDate ?? raw.RegistrationDate;
        if (!travelDate.HasValue)
        {
            skipReason = "missing_travel_date";
            return row;
        }

        row["TravelDate"] = travelDate.Value.ToString("yyyy-MM-dd");
        TrySetCheckPoint(row, catalogs, raw, unmapped);
        if (!string.IsNullOrWhiteSpace(raw.PurposeOfTravelLabel))
            row["Notes"] = raw.PurposeOfTravelLabel.Trim();

        return row;
    }

    private static void TrySetCheckPoint(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Visa2014TravelHistoryRawRow raw,
        List<string> unmapped)
    {
        if (string.IsNullOrWhiteSpace(raw.CheckPointMgCode) && string.IsNullOrWhiteSpace(raw.CheckPointLabel))
        {
            row["CheckPoint"] = null;
            return;
        }

        string? codeReason = null;
        string? labelReason = null;
        if (!string.IsNullOrWhiteSpace(raw.CheckPointMgCode) &&
            Visa2014LookupTranslator.TryTranslate(catalogs, "CheckPoint", raw.CheckPointMgCode, out var byCode, out codeReason) &&
            !string.IsNullOrWhiteSpace(byCode))
        {
            row["CheckPoint"] = byCode;
            return;
        }

        if (!string.IsNullOrWhiteSpace(raw.CheckPointLabel) &&
            Visa2014LookupTranslator.TryTranslate(catalogs, "CheckPoint", raw.CheckPointLabel, out var byLabel, out labelReason) &&
            !string.IsNullOrWhiteSpace(byLabel))
        {
            row["CheckPoint"] = byLabel;
            return;
        }

        if (codeReason != null)
            unmapped.Add(codeReason);
        if (labelReason != null)
            unmapped.Add(labelReason);
        row["CheckPoint"] = null;
    }

    private static Guid? TryParseGuid(string? text) =>
        Guid.TryParse(text?.Trim(), out var g) ? g : null;

    private static int? TryParseInt(string? text) =>
        int.TryParse(text, out var n) ? n : null;

    private static DateTime? TryParseDate(string? text) =>
        DateTime.TryParse(text, out var d) ? d.Date : null;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
