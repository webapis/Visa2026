namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014ApplicationItemRawRow(
    Guid LegacyOid,
    Guid LegacyApplicationOid,
    Guid? LegacyEmployeeOid,
    Guid? LegacyFamilyMemberOid,
    Guid? LegacyPassportOid,
    Guid? LegacyPreviousPassportOid,
    Guid? LegacyVisaOid,
    Guid? LegacyNextVisaOid,
    Guid? LegacyWorkPermitOid,
    Guid? LegacyInvitationItemOid,
    Guid? LegacyPositionOid,
    Guid? LegacyAddressOfResidenceOid,
    Guid? LegacyDirectAddressOid,
    DateTime? RegistrationDate,
    string? RegistrationNumber,
    DateTime? TravelDate,
    int? TiTravelType,
    string? CheckPointMgCode,
    string? CheckPointLabel,
    string? PurposeOfTravelLabel,
    string? BusinessTripAddressText,
    string? BusinessTripCityMgCode,
    string? BusinessTripCityName,
    bool Cancelled,
    bool Rejected,
    bool IsComplete,
    bool ForEmployee,
    bool ForFamilyMember,
    int? EmployeeSubtypeId,
    int? FamilySubtypeId,
    bool HasInvitationWpFk,
    int? InvitationAndWorkPermitRequired,
    bool HasWizaWpFk,
    int? WizaAndWorkPermitRequired,
    int? ChangeInformation,
    bool HasBorderZoneFk,
    bool BzDasoguz,
    bool BzTagtabazar,
    bool BzSerhetabat,
    bool BzYoloten,
    bool BzFarap,
    bool BzGarabogaz,
    bool BzSarahs,
    bool BzEtrek);

internal static class Visa2014ApplicationItemTransform
{
    private const string SeherEtrap = "\u015E\u00E4herEtrap";
    private const string CommaSeparatedNoneValue = "\u00DDok";
    private const string WorkPermitLocationAuditNote = "pending_work_permit_location_audit";

    private static readonly (string BitKey, Func<Visa2014ApplicationItemRawRow, bool> Getter)[] BorderZoneBitOrder =
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

    internal static readonly string[] ApplicationItemMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_dedupeGroupId", "_importAction",
        "_parentApplicationType", "_personLegacyOid",
        "Application", "ApplicationType", "Person",
        "CurrentPassport", "PreviousPassport", "CurrentVisa", "NextVisa",
        "CurrentWorkPermitItem", "CurrentInvitationItem", "CurrentPositionHistory", "CurrentAddressOfResidence",
        "CurrentEducation", "CurrentSalary",
        "RegistrationDate", "TravelDate", "TravelType", "MovementType",
        "CheckPoint", "PurposeOfTravel",
        "BusinessTripAddress", "BusinessTripCity",
        "BorderZoneLocation", "WorkPermittedLocations",
        "IsCancelled", "RejectionIssued", "VisaIssued",
        "_legacy_ApplicationTypeComposite", "_legacy_RegistrationNumber",
        "_audit_WorkPermittedLocations",
        "_legacy_CheckPointMgCode", "_legacy_CheckPointLabel", "_legacy_PurposeOfTravelLabel",
    ];

    internal static string ExtractSql => $"""
        SELECT
            CAST(pia.Oid AS varchar(36)) AS Oid,
            CAST(pia.Application AS varchar(36)) AS ApplicationOid,
            CAST(pia.Employee AS varchar(36)) AS EmployeeOid,
            CAST(pia.FamilyMember AS varchar(36)) AS FamilyMemberOid,
            CAST(pia.Passport AS varchar(36)) AS PassportOid,
            CAST(pia.PreviousPassport AS varchar(36)) AS PreviousPassportOid,
            CAST(pia.Visa AS varchar(36)) AS VisaOid,
            CAST(nextVisa.NextVisaOid AS varchar(36)) AS NextVisaOid,
            CAST(pia.WorkPermit AS varchar(36)) AS WorkPermitOid,
            invMatch.InvitationItemOid,
            CAST(pia.Position AS varchar(36)) AS PositionOid,
            CAST(pia.AddressOfResidence AS varchar(36)) AS AddressOfResidenceOid,
            CAST(pia.Address AS varchar(36)) AS DirectAddressOid,
            CONVERT(varchar(10), pia.RegistrationDate, 23) AS RegistrationDate,
            pia.RegistrationNumber,
            CONVERT(varchar(10), ti.TravelDate, 23) AS TravelDate,
            ti.TravelType AS TiTravelType,
            ISNULL(CAST(cp_line.TitleOfCheckPoint AS varchar(10)), ISNULL(CAST(cp_ti.TitleOfCheckPoint AS varchar(10)), '')) AS CheckPointMgCode,
            ISNULL(cp_line.TitleOfCheckPointL, cp_ti.TitleOfCheckPointL) AS CheckPointLabel,
            pot.PurposeOfTravelL AS PurposeOfTravelLabel,
            aobt.AddressOnTrip AS BusinessTripAddressText,
            ISNULL(CAST(se.mgCode AS varchar(10)), '') AS BusinessTripCityMgCode,
            se.[{SeherEtrap}L] AS BusinessTripCityName,
            CASE WHEN ISNULL(pia.Cancelled, 0) = 1 THEN '1' ELSE '0' END AS Cancelled,
            CASE WHEN ISNULL(pia.Rejected, 0) = 1 THEN '1' ELSE '0' END AS Rejected,
            CASE WHEN ISNULL(pia.IsComplete, 0) = 1 THEN '1' ELSE '0' END AS IsComplete,
            CASE WHEN ISNULL(a.ForEmployee, 0) = 1 THEN '1' ELSE '0' END AS ForEmployee,
            CASE WHEN ISNULL(a.ForFamilyMember, 0) = 1 THEN '1' ELSE '0' END AS ForFamilyMember,
            ate.TypeOfApplicationForEmployee AS EmployeeSubtypeId,
            atfm.TypeOfApplicationForFamilyMember AS FamilySubtypeId,
            CASE WHEN a.IsInvitationWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasInvitationWpFk,
            iwp.InvitationAndWorkPermitRequired,
            CASE WHEN a.IsWizaWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasWizaWpFk,
            wwp.WizaAndWorkPermitRequired,
            a.ChangeInformation,
            CASE WHEN a.BorderZoneForVisa IS NULL THEN '0' ELSE '1' END AS HasBorderZoneFk,
            CASE WHEN ISNULL(bz.[Daşoguz], 0) = 1 THEN '1' ELSE '0' END AS BzDasoguz,
            CASE WHEN ISNULL(bz.Tagtabazar, 0) = 1 THEN '1' ELSE '0' END AS BzTagtabazar,
            CASE WHEN ISNULL(bz.Serhetabat, 0) = 1 THEN '1' ELSE '0' END AS BzSerhetabat,
            CASE WHEN ISNULL(bz.[Ýolöten], 0) = 1 THEN '1' ELSE '0' END AS BzYoloten,
            CASE WHEN ISNULL(bz.Farap, 0) = 1 THEN '1' ELSE '0' END AS BzFarap,
            CASE WHEN ISNULL(bz.Garabogaz, 0) = 1 THEN '1' ELSE '0' END AS BzGarabogaz,
            CASE WHEN ISNULL(bz.Sarahs, 0) = 1 THEN '1' ELSE '0' END AS BzSarahs,
            CASE WHEN ISNULL(bz.Etrek, 0) = 1 THEN '1' ELSE '0' END AS BzEtrek
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
        LEFT JOIN dbo.AddressOnBusinessTrip aobt ON aobt.Oid = pia.AddressOnBusinessTrip
        LEFT JOIN dbo.WorkPermit wp ON wp.Oid = pia.WorkPermit AND wp.GCRecord IS NULL
        OUTER APPLY (
            SELECT TOP 1 CAST(v.Oid AS varchar(36)) AS NextVisaOid
            FROM dbo.Visa v
            WHERE v.ProcessNumber = CAST(pia.Oid AS varchar(36)) AND v.GCRecord IS NULL
            ORDER BY v.Oid
        ) nextVisa
        LEFT JOIN dbo.[{SeherEtrap}] se ON se.Oid = a.BusinessTripDestination
        LEFT JOIN dbo.BorderZoneForVisa bz ON bz.Oid = a.BorderZoneForVisa AND bz.GCRecord IS NULL
        OUTER APPLY (
            SELECT TOP 1 CAST(pii.Oid AS varchar(36)) AS InvitationItemOid
            FROM dbo.PersonInInvitation pii
            INNER JOIN dbo.ApplicationResult ar ON ar.Oid = pii.Invitation AND ar.GCRecord IS NULL
            WHERE pii.GCRecord IS NULL
              AND ar.Application = pia.Application
              AND (
                  (pia.Employee IS NOT NULL AND pii.Employee = pia.Employee)
                  OR (pia.FamilyMember IS NOT NULL AND pii.FamilyMember = pia.FamilyMember))
            ORDER BY ar.IssuedDate DESC, pii.Oid
        ) invMatch
        WHERE pia.GCRecord IS NULL
        """;

    public static Visa2014PersonImportBatch PrepareImportBatch(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        int? maxRows,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var sql = maxRows is > 0
            ? $"SELECT TOP ({maxRows.Value}) * FROM ({ExtractSql}) AS q"
            : ExtractSql;

        var dictRows = Visa2014SqlCmdReader.Query(connectionString, sql, verbose);
        var rawRows = new List<Visa2014ApplicationItemRawRow>();
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

        var visibility = ApplicationTypeVisibilityCatalog.Load();
        var currentEducationByPerson = Visa2014PersonCurrentFieldInference.BuildCurrentEducationByPerson(
            connectionString,
            verbose);
        var currentWorkPermitByPerson = Visa2014PersonCurrentFieldInference.BuildCurrentWorkPermitByPerson(
            connectionString,
            verbose);
        var context = new ApplicationItemTransformContext(
            visibility,
            currentEducationByPerson,
            currentWorkPermitByPerson);

        return TransformRows(rawRows, catalogs, context, out var skipped, out var unmappedDistinct, out var dedupeSummary);
    }

    private sealed record ApplicationItemTransformContext(
        ApplicationTypeVisibilityCatalog Visibility,
        IReadOnlyDictionary<Guid, Guid> CurrentEducationByPerson,
        IReadOnlyDictionary<Guid, Guid> CurrentWorkPermitByPerson);

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014ApplicationItemRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        if (!row.TryGetValue("ApplicationOid", out var appText) ||
            !Guid.TryParse(appText?.Trim(), out var legacyApplicationOid))
            return false;

        parsed = new Visa2014ApplicationItemRawRow(
            LegacyOid: legacyOid,
            LegacyApplicationOid: legacyApplicationOid,
            LegacyEmployeeOid: TryParseNullableGuid(row.GetValueOrDefault("EmployeeOid")),
            LegacyFamilyMemberOid: TryParseNullableGuid(row.GetValueOrDefault("FamilyMemberOid")),
            LegacyPassportOid: TryParseNullableGuid(row.GetValueOrDefault("PassportOid")),
            LegacyPreviousPassportOid: TryParseNullableGuid(row.GetValueOrDefault("PreviousPassportOid")),
            LegacyVisaOid: TryParseNullableGuid(row.GetValueOrDefault("VisaOid")),
            LegacyNextVisaOid: TryParseNullableGuid(row.GetValueOrDefault("NextVisaOid")),
            LegacyWorkPermitOid: TryParseNullableGuid(row.GetValueOrDefault("WorkPermitOid")),
            LegacyInvitationItemOid: TryParseNullableGuid(row.GetValueOrDefault("InvitationItemOid")),
            LegacyPositionOid: TryParseNullableGuid(row.GetValueOrDefault("PositionOid")),
            LegacyAddressOfResidenceOid: TryParseNullableGuid(row.GetValueOrDefault("AddressOfResidenceOid")),
            LegacyDirectAddressOid: TryParseNullableGuid(row.GetValueOrDefault("DirectAddressOid")),
            RegistrationDate: TryParseDate(row.GetValueOrDefault("RegistrationDate")),
            RegistrationNumber: row.GetValueOrDefault("RegistrationNumber"),
            TravelDate: TryParseDate(row.GetValueOrDefault("TravelDate")),
            TiTravelType: ParseNullableInt(row.GetValueOrDefault("TiTravelType")),
            CheckPointMgCode: NullIfEmpty(row.GetValueOrDefault("CheckPointMgCode")),
            CheckPointLabel: row.GetValueOrDefault("CheckPointLabel"),
            PurposeOfTravelLabel: row.GetValueOrDefault("PurposeOfTravelLabel"),
            BusinessTripAddressText: row.GetValueOrDefault("BusinessTripAddressText"),
            BusinessTripCityMgCode: NullIfEmpty(row.GetValueOrDefault("BusinessTripCityMgCode")),
            BusinessTripCityName: row.GetValueOrDefault("BusinessTripCityName"),
            Cancelled: row.GetValueOrDefault("Cancelled") == "1",
            Rejected: row.GetValueOrDefault("Rejected") == "1",
            IsComplete: row.GetValueOrDefault("IsComplete") == "1",
            ForEmployee: row.GetValueOrDefault("ForEmployee") == "1",
            ForFamilyMember: row.GetValueOrDefault("ForFamilyMember") == "1",
            EmployeeSubtypeId: ParseNullableInt(row.GetValueOrDefault("EmployeeSubtypeId")),
            FamilySubtypeId: ParseNullableInt(row.GetValueOrDefault("FamilySubtypeId")),
            HasInvitationWpFk: row.GetValueOrDefault("HasInvitationWpFk") == "1",
            InvitationAndWorkPermitRequired: ParseNullableInt(row.GetValueOrDefault("InvitationAndWorkPermitRequired")),
            HasWizaWpFk: row.GetValueOrDefault("HasWizaWpFk") == "1",
            WizaAndWorkPermitRequired: ParseNullableInt(row.GetValueOrDefault("WizaAndWorkPermitRequired")),
            ChangeInformation: ParseNullableInt(row.GetValueOrDefault("ChangeInformation")),
            HasBorderZoneFk: row.GetValueOrDefault("HasBorderZoneFk") == "1",
            BzDasoguz: row.GetValueOrDefault("BzDasoguz") == "1",
            BzTagtabazar: row.GetValueOrDefault("BzTagtabazar") == "1",
            BzSerhetabat: row.GetValueOrDefault("BzSerhetabat") == "1",
            BzYoloten: row.GetValueOrDefault("BzYoloten") == "1",
            BzFarap: row.GetValueOrDefault("BzFarap") == "1",
            BzGarabogaz: row.GetValueOrDefault("BzGarabogaz") == "1",
            BzSarahs: row.GetValueOrDefault("BzSarahs") == "1",
            BzEtrek: row.GetValueOrDefault("BzEtrek") == "1");
        return true;
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014ApplicationItemRawRow> rawRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        ApplicationItemTransformContext context,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);
        var working = rawRows.Select(r => new WorkingRow(r)).ToList();
        ApplyApplicationPersonDedupe(working, dedupeSummary);

        var importRows = new List<Dictionary<string, object?>>();
        foreach (var row in working)
        {
            var export = BuildExportRow(row, catalogs, context, out var skipReason, out var rowUnmapped);
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
                return new Dictionary<string, object?>(StringComparer.Ordinal)
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
            DedupeSummary = dedupeSummary,
            LegacyRowCount = rawRows.Count,
            DedupeMergedCount = 0,
        };
    }

    private sealed class WorkingRow(Visa2014ApplicationItemRawRow Raw)
    {
        public Visa2014ApplicationItemRawRow Raw { get; } = Raw;
        public string ImportAction { get; set; } = "import";
        public string? DedupeGroupId { get; set; }
    }

    private static void ApplyApplicationPersonDedupe(
        List<WorkingRow> rows,
        List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new
            {
                Row = r,
                PersonOid = ResolvePersonOid(r.Raw),
            })
            .Where(x => x.PersonOid.HasValue)
            .GroupBy(x => (x.Row.Raw.LegacyApplicationOid, x.PersonOid!.Value))
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var canonical = members
                .OrderBy(x => x.Row.Raw.LegacyOid)
                .First();

            var groupId = $"APP:{group.Key.LegacyApplicationOid:D}:PERSON:{group.Key.Item2:D}";
            foreach (var member in members)
            {
                member.Row.DedupeGroupId = groupId;
                if (!ReferenceEquals(member.Row, canonical.Row))
                    member.Row.ImportAction = "dedupe_skip";
            }

            dedupeSummary.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "Application+Person",
                ["normalizedValue"] = $"{group.Key.LegacyApplicationOid:D}|{group.Key.Item2:D}",
                ["memberCount"] = members.Count,
                ["canonical_legacyRowId"] = canonical.Row.Raw.LegacyOid,
                ["canonicalRule"] = "lowest_legacy_oid",
            });
        }
    }

    private static Dictionary<string, object?> BuildExportRow(
        WorkingRow working,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        ApplicationItemTransformContext context,
        out string? skipReason,
        out List<string> unmapped)
    {
        skipReason = null;
        unmapped = [];
        var raw = working.Raw;

        var applicationTypeComposite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            raw.ForEmployee,
            raw.ForFamilyMember,
            raw.EmployeeSubtypeId,
            raw.FamilySubtypeId,
            raw.HasInvitationWpFk,
            raw.InvitationAndWorkPermitRequired,
            raw.HasWizaWpFk,
            raw.WizaAndWorkPermitRequired,
            raw.ChangeInformation);

        var personOid = ResolvePersonOid(raw);
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_legacyRowId"] = raw.LegacyOid,
            ["_legacyTable"] = "PersonInApplication",
            ["_dedupeGroupId"] = working.DedupeGroupId ?? "",
            ["_importAction"] = working.ImportAction == "dedupe_skip" ? "skip" : "import",
            ["_legacy_ApplicationTypeComposite"] = applicationTypeComposite,
            ["_legacy_RegistrationNumber"] = raw.RegistrationNumber,
            ["_legacy_CheckPointMgCode"] = raw.CheckPointMgCode,
            ["_legacy_CheckPointLabel"] = raw.CheckPointLabel,
            ["_legacy_PurposeOfTravelLabel"] = raw.PurposeOfTravelLabel,
            ["Application"] = raw.LegacyApplicationOid.ToString("D"),
        };

        if (working.ImportAction == "dedupe_skip")
        {
            skipReason = "dedupe_duplicate";
            row["_personLegacyOid"] = personOid?.ToString("D");
            return row;
        }

        if (Visa2014ApplicationTransform.IsSkippedApplicationTypeComposite(applicationTypeComposite))
        {
            skipReason = $"skip_row:parent_ApplicationType:{applicationTypeComposite}";
            row["_parentApplicationType"] = null;
            row["_personLegacyOid"] = personOid?.ToString("D");
            return row;
        }

        string? applicationTypeName = null;
        if (Visa2014LookupTranslator.TryTranslate(catalogs, "ApplicationType", applicationTypeComposite, out var appTypeTarget, out var appTypeReason) &&
            !string.IsNullOrWhiteSpace(appTypeTarget))
        {
            applicationTypeName = appTypeTarget;
            row["ApplicationType"] = applicationTypeName;
            row["_parentApplicationType"] = applicationTypeName;
        }
        else
        {
            if (appTypeReason != null)
                unmapped.Add(appTypeReason);
            skipReason = appTypeReason ?? $"unmapped_lookup:ApplicationType:{applicationTypeComposite}";
            row["ApplicationType"] = null;
            row["_parentApplicationType"] = null;
            return row;
        }

        if (!personOid.HasValue)
        {
            skipReason = "required_null:Person";
            row["_personLegacyOid"] = null;
            return row;
        }

        row["_personLegacyOid"] = personOid.Value.ToString("D");
        row["Person"] = personOid.Value.ToString("D");

        if (!raw.LegacyPassportOid.HasValue)
        {
            skipReason = "required_null:CurrentPassport";
            row["CurrentPassport"] = null;
            return row;
        }

        row["CurrentPassport"] = raw.LegacyPassportOid.Value.ToString("D");
        row["PreviousPassport"] = raw.LegacyPreviousPassportOid?.ToString("D");
        row["CurrentVisa"] = raw.LegacyVisaOid?.ToString("D");
        row["NextVisa"] = raw.LegacyNextVisaOid?.ToString("D");
        row["CurrentWorkPermitItem"] = raw.LegacyWorkPermitOid?.ToString("D");
        row["CurrentInvitationItem"] = raw.LegacyInvitationItemOid?.ToString("D");
        row["CurrentPositionHistory"] = raw.LegacyPositionOid?.ToString("D");
        row["CurrentAddressOfResidence"] =
            Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw)?.ToString("D");

        row["RegistrationDate"] = raw.RegistrationDate?.ToString("yyyy-MM-dd");
        row["TravelDate"] = raw.TravelDate?.ToString("yyyy-MM-dd");
        DeriveRegistrationTravelTypes(applicationTypeName, row);

        TrySetCheckPoint(row, catalogs, raw, unmapped, ref skipReason);
        TrySetPurposeOfTravel(row, catalogs, raw.PurposeOfTravelLabel, unmapped);

        row["BusinessTripAddress"] = string.IsNullOrWhiteSpace(raw.BusinessTripAddressText)
            ? null
            : raw.BusinessTripAddressText.Trim();
        TrySetBusinessTripCity(row, catalogs, raw.BusinessTripCityMgCode, raw.BusinessTripCityName, unmapped);

        row["BorderZoneLocation"] = BuildBorderZoneLocation(catalogs, raw);
        row["WorkPermittedLocations"] = null;
        row["_audit_WorkPermittedLocations"] = raw.LegacyWorkPermitOid.HasValue
            ? WorkPermitLocationAuditNote
            : null;

        Visa2014ApplicationItemCancelledFlagsMapper.ApplyLegacyCancelledFlags(
            row,
            applicationTypeName,
            context.Visibility,
            raw.Cancelled);
        row["RejectionIssued"] = raw.Rejected;
        row["VisaIssued"] = raw.IsComplete;

        Visa2014PersonCurrentFieldInference.TrySetApplicationItemPersonCurrentFields(
            raw,
            applicationTypeName,
            context.Visibility,
            context.CurrentEducationByPerson,
            context.CurrentWorkPermitByPerson,
            row);

        return row;
    }

    private static Guid? ResolvePersonOid(Visa2014ApplicationItemRawRow raw) =>
        raw.ForEmployee ? raw.LegacyEmployeeOid
        : raw.ForFamilyMember ? raw.LegacyFamilyMemberOid
        : null;

    private static void DeriveRegistrationTravelTypes(string? applicationTypeName, Dictionary<string, object?> row)
    {
        row["TravelType"] = null;
        row["MovementType"] = null;
        if (string.IsNullOrWhiteSpace(applicationTypeName))
            return;

        switch (applicationTypeName)
        {
            case "App_Reg_Check_In":
                row["TravelType"] = "External";
                row["MovementType"] = "Entry";
                break;
            case "App_Reg_Check_Out":
                row["TravelType"] = "External";
                row["MovementType"] = "Exit";
                break;
            case "App_Reg_Check_In_Internal":
                row["TravelType"] = "Internal";
                row["MovementType"] = "Entry";
                break;
            case "App_Reg_Check_Out_Internal":
                row["TravelType"] = "Internal";
                row["MovementType"] = "Exit";
                break;
        }
    }

    private static void TrySetCheckPoint(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Visa2014ApplicationItemRawRow raw,
        List<string> unmapped,
        ref string? skipReason)
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
            if (codeReason != null)
                unmapped.Add(codeReason);
            return;
        }

        if (!string.IsNullOrWhiteSpace(raw.CheckPointLabel) &&
            Visa2014LookupTranslator.TryTranslate(catalogs, "CheckPoint", raw.CheckPointLabel, out var byLabel, out labelReason) &&
            !string.IsNullOrWhiteSpace(byLabel))
        {
            row["CheckPoint"] = byLabel;
            if (labelReason != null)
                unmapped.Add(labelReason);
            return;
        }

        var reason = codeReason ?? labelReason;
        if (reason != null)
            unmapped.Add(reason);

        if (catalogs.TryGetValue("CheckPoint", out var catalog) &&
            string.Equals(catalog.UnmappedPolicy, "block_row", StringComparison.OrdinalIgnoreCase))
            skipReason ??= reason ?? $"unmapped_lookup:CheckPoint:{raw.CheckPointMgCode ?? raw.CheckPointLabel}";

        row["CheckPoint"] = null;
    }

    private static void TrySetPurposeOfTravel(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string? legacyLabel,
        List<string> unmapped)
    {
        if (string.IsNullOrWhiteSpace(legacyLabel))
        {
            row["PurposeOfTravel"] = null;
            return;
        }

        if (Visa2014LookupTranslator.TryTranslate(catalogs, "PurposeOfTravel", legacyLabel, out var target, out var reason) &&
            !string.IsNullOrWhiteSpace(target))
        {
            row["PurposeOfTravel"] = target;
            if (reason != null)
                unmapped.Add(reason);
            return;
        }

        if (reason != null)
            unmapped.Add(reason);

        if (catalogs.TryGetValue("PurposeOfTravel", out var catalog) &&
            string.Equals(catalog.UnmappedPolicy, "use_default", StringComparison.OrdinalIgnoreCase))
        {
            row["PurposeOfTravel"] = "Işlemek üçin";
            return;
        }

        row["PurposeOfTravel"] = legacyLabel.Trim();
    }

    private static void TrySetBusinessTripCity(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string? cityMgCode,
        string? cityName,
        List<string> unmapped)
    {
        if (string.IsNullOrWhiteSpace(cityMgCode) && string.IsNullOrWhiteSpace(cityName))
        {
            row["BusinessTripCity"] = null;
            return;
        }

        if (!string.IsNullOrWhiteSpace(cityMgCode) &&
            Visa2014LookupTranslator.TryTranslate(catalogs, "City", cityMgCode, out var pdfCode, out _))
        {
            var resolved = CityNameByPdfCode.GetValueOrDefault(pdfCode ?? cityMgCode);
            if (resolved != null)
            {
                row["BusinessTripCity"] = resolved;
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(cityName))
        {
            if (Visa2014LookupTranslator.TryTranslate(catalogs, "CityByName", cityName, out var byName, out var reason) &&
                !string.IsNullOrWhiteSpace(byName))
            {
                row["BusinessTripCity"] = byName;
                return;
            }

            if (reason != null)
                unmapped.Add(reason);

            row["BusinessTripCity"] = NormalizeLegacyCityName(cityName);
            return;
        }

        unmapped.Add($"unmapped_lookup:City:{cityMgCode}");
        row["BusinessTripCity"] = null;
    }

    private static string BuildBorderZoneLocation(
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Visa2014ApplicationItemRawRow raw)
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

    private static string NormalizeLegacyCityName(string legacyCity) =>
        legacyCity
            .Replace("Asgabat", "Aşgabat", StringComparison.OrdinalIgnoreCase)
            .Replace("Dasoguz", "Daşoguz", StringComparison.OrdinalIgnoreCase)
            .Replace("Yoloten", "Ýolöten", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private static Guid? TryParseNullableGuid(string? value) =>
        Guid.TryParse(value?.Trim(), out var parsed) ? parsed : null;

    private static DateTime? TryParseDate(string? value) =>
        DateTime.TryParse(value, out var parsed) ? parsed : null;

    private static int? ParseNullableInt(string? value) =>
        int.TryParse(value?.Trim(), out var parsed) ? parsed : null;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Dictionary<string, string> CityNameByPdfCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BN15"] = "Türkmenbaşy etraby",
        ["AH48"] = "Akbugdaý etraby",
        ["MR36"] = "Mary etraby",
        ["AS69"] = "Aşgabat şäheri",
        ["MR19"] = "Mary şäheri",
        ["LB18"] = "Türkmenabat şäheri",
        ["DZ56"] = "Daşoguz şäheri",
        ["BN63"] = "Balkanabat şäheri",
        ["BN9"] = "Gumdag şäheri",
        ["BN10"] = "Garabogaz şäheri",
        ["MR23"] = "Serhetabat etraby",
        ["MR11"] = "Ýolöten şäheri",
        ["MR2"] = "Serhetabat şäheri",
        ["AH41"] = "Kaka etraby",
        ["AS57"] = "Köpetdag etraby",
    };
}
