namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed record Visa2014ApplicationRawRow(
    Guid LegacyOid,
    string? ManualApplicationNumber,
    DateTime? ManualApplicationDate,
    bool AutoRegistration,
    bool ForEmployee,
    bool ForFamilyMember,
    int? EmployeeSubtypeId,
    int? FamilySubtypeId,
    bool HasInvitationWpFk,
    int? InvitationAndWorkPermitRequired,
    bool HasWizaWpFk,
    int? WizaAndWorkPermitRequired,
    int? ChangeInformation,
    int? ApplicationUrgency,
    string? UrgencyMgCode,
    string? PeriodOfVisaL,
    string? VisaPeriodMgCode,
    string? VisaPeriodCountMonth,
    string? CategoryOfVisaL,
    string? VisaCategoryMgCode,
    string? NumberOfContract,
    string? ToCityMgCode,
    string? ToCityName,
    DateTime? DateOfDeparture,
    int DurationOfStay,
    string? MovementPermitNameTm,
    bool HasBorderZoneFk,
    bool BzDasoguz,
    bool BzTagtabazar,
    bool BzSerhetabat,
    bool BzYoloten,
    bool BzFarap,
    bool BzGarabogaz,
    bool BzSarahs,
    bool BzEtrek,
    string? DepartmentForRegistrationCode,
    string? DepartmentForRegistrationName);

internal static class Visa2014ApplicationTransform
{
    // VISA2015 uses ŞäherEtrap (U+015E U+00E4), not ŞeherEtrap — same as AddressOfResidence wave.
    private const string SeherEtrap = "\u015E\u00E4herEtrap";
    private const string GosmacaIslemageRugsatYeri = "Go\u015fma\u00E7aI\u015Flem\u00E4geRugsat\u00FDeri";
    private const string MovementPermitNameColumn = "I\u015Flejek\u00DDerini\u0148Ady";

    private static readonly (string BitKey, Func<Visa2014ApplicationRawRow, bool> Getter)[] BorderZoneBitOrder =
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

    private static readonly HashSet<string> ApplicationTypeSkipComposites = new(StringComparer.Ordinal)
    {
        "E:44:na:na:na",
        "E:55:na:na:na",
    };

    internal static readonly HashSet<string> ShowProjectContractApplicationTypes = new(StringComparer.Ordinal)
    {
        "App_Border_Zone_Permission",
        "App_Inv",
        "App_Inv_FM",
        "App_Sevice_Passport",
        "App_Inv_According_to_WP",
        "App_Inv_And_WP",
        "App_Visa_Ext_According_to_WP",
        "App_Visa_Ext",
        "App_Exit_Visa",
        "App_Visa_and_WP_Ext",
        "App_WP_Ext",
        "App_Additional_WP_location",
    };

    private static readonly HashSet<string> ShowMigrationServiceApplicationTypes = new(StringComparer.Ordinal)
    {
        "App_Cancel_BZ",
        "App_Cancel_App",
        "App_Change_Inv",
        "App_Cancel_Inv",
        "App_Cancel_Inv_WP",
        "App_Reg_Check_In",
        "App_Reg_Check_In_Internal",
        "App_Reg_Info_Change_Passport",
        "App_Reg_Info_Change_Visa",
        "App_Reg_Info_Change_Address",
        "App_Reg_ext",
        "App_Reg_Check_Out",
        "App_Reg_Check_Out_Internal",
        "App_Business_Trip_Departure",
        "App_Business_Trip_Arrival",
        "App_Visa_Ext_According_to_WP",
        "App_Change_Visa_Category",
        "App_Change_Passport",
        "App_Visa_Ext_FM",
        "App_Visa_For_New_Born_FM",
        "App_Cancel_Visa_and_WP",
        "App_Cancell_WP",
    };

    internal static readonly string[] ApplicationMainColumnOrder =
    [
        "_legacyRowId", "_legacyTable", "_dedupeGroupId", "_importAction",
        "_legacyManualNumber", "_legacySubtypeId",
        "FullApplicationNumber", "ApplicationNumber", "AppNumberPrefix",
        "ApplicationDate", "Year", "Month", "IsManualEntry", "ApplicationType",
        "MigrationService", "Urgency", "VisaPeriod", "VisaCategory", "ProjectContract",
        "BorderZoneLocation", "MovementPermitLocation",
        "ToCity", "BusinessTripStartDate", "BusinessTripEndDate",
        "_legacy_DepartmentForRegistration", "_legacy_DepartmentForRegistrationName",
        "_legacy_ApplicationTypeComposite", "_legacy_UrgencyComposite",
        "_legacy_VisaPeriodComposite", "_legacy_VisaCategoryComposite",
        "_legacy_ToCityMgCode", "_legacy_ToCityName",
    ];

    internal static string ExtractSql => $"""
        SELECT
            CAST(a.Oid AS varchar(36)) AS Oid,
            r.ManualApplicationNumber,
            CONVERT(varchar(10), r.ManualApplicationDate, 23) AS ManualApplicationDate,
            CASE WHEN ISNULL(a.AutoRegistration, 0) = 1 THEN '1' ELSE '0' END AS AutoRegistration,
            CASE WHEN ISNULL(a.ForEmployee, 0) = 1 THEN '1' ELSE '0' END AS ForEmployee,
            CASE WHEN ISNULL(a.ForFamilyMember, 0) = 1 THEN '1' ELSE '0' END AS ForFamilyMember,
            ate.TypeOfApplicationForEmployeeID AS EmployeeSubtypeId,
            atfm.TypeOfApplicationForFamilyMemberID AS FamilySubtypeId,
            CASE WHEN a.IsInvitationWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasInvitationWpFk,
            iwp.InvitationAndWorkPermitRequired,
            CASE WHEN a.IsWizaWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasWizaWpFk,
            wwp.WizaAndWorkPermitRequired,
            a.ChangeInformation,
            u.ApplicationUrgency,
            ISNULL(CAST(u.mgCode AS varchar(10)), '') AS UrgencyMgCode,
            vp.PeriodOfVisaL,
            ISNULL(CAST(vp.mgCode AS varchar(10)), '') AS VisaPeriodMgCode,
            ISNULL(CAST(vp.CountMonth AS varchar(10)), '') AS VisaPeriodCountMonth,
            vc.CategoryOfVisaL,
            ISNULL(CAST(vc.mgCode AS varchar(10)), '') AS VisaCategoryMgCode,
            COALESCE(c_app.NumberOfContract, personContract.NumberOfContract) AS NumberOfContract,
            ISNULL(CAST(se.mgCode AS varchar(10)), '') AS ToCityMgCode,
            se.[{SeherEtrap}L] AS ToCityName,
            CONVERT(varchar(10), a.DateOfDeparture, 23) AS DateOfDeparture,
            ISNULL(CAST(a.DurationOfStay AS varchar(10)), '0') AS DurationOfStay,
            mp.[{MovementPermitNameColumn}] AS MovementPermitNameTm,
            CASE WHEN a.BorderZoneForVisa IS NULL THEN '0' ELSE '1' END AS HasBorderZoneFk,
            CASE WHEN ISNULL(bz.[Daşoguz], 0) = 1 THEN '1' ELSE '0' END AS BzDasoguz,
            CASE WHEN ISNULL(bz.Tagtabazar, 0) = 1 THEN '1' ELSE '0' END AS BzTagtabazar,
            CASE WHEN ISNULL(bz.Serhetabat, 0) = 1 THEN '1' ELSE '0' END AS BzSerhetabat,
            CASE WHEN ISNULL(bz.[Ýolöten], 0) = 1 THEN '1' ELSE '0' END AS BzYoloten,
            CASE WHEN ISNULL(bz.Farap, 0) = 1 THEN '1' ELSE '0' END AS BzFarap,
            CASE WHEN ISNULL(bz.Garabogaz, 0) = 1 THEN '1' ELSE '0' END AS BzGarabogaz,
            CASE WHEN ISNULL(bz.Sarahs, 0) = 1 THEN '1' ELSE '0' END AS BzSarahs,
            CASE WHEN ISNULL(bz.Etrek, 0) = 1 THEN '1' ELSE '0' END AS BzEtrek,
            d.TitleOfDepartmentForRegistration AS DepartmentForRegistrationCode,
            d.DepartmentForRegistrationL AS DepartmentForRegistrationName
        FROM dbo.Application a
        INNER JOIN dbo.IRegistration_Data r ON r.Oid = a.IRegistration_Data
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        LEFT JOIN dbo.IsInvitationWithWorkPermit iwp ON iwp.Oid = a.IsInvitationWithWorkPermit
        LEFT JOIN dbo.IsWizaWithWorkPermit wwp ON wwp.Oid = a.IsWizaWithWorkPermit
        LEFT JOIN dbo.Urgency u ON u.Oid = a.Urgency
        LEFT JOIN dbo.VisaPeriod vp ON vp.Oid = a.VisaPeriod
        LEFT JOIN dbo.VisaCategory vc ON vc.Oid = a.VisaCategory
        LEFT JOIN dbo.Contract c_app ON c_app.Oid = a.Contract
        OUTER APPLY (
            SELECT TOP 1 COALESCE(c_per.NumberOfContract, c_sponsor.NumberOfContract) AS NumberOfContract
            FROM dbo.PersonInApplication pia
            LEFT JOIN dbo.Person emp ON emp.Oid = pia.Employee AND emp.GCRecord IS NULL
            LEFT JOIN dbo.Person fm ON fm.Oid = pia.FamilyMember AND fm.GCRecord IS NULL
            LEFT JOIN dbo.Person fmSponsor ON fm.IsFamilyMember = 1 AND fm.Employee = fmSponsor.Oid AND fmSponsor.GCRecord IS NULL
            LEFT JOIN dbo.Contract c_per ON c_per.Oid = COALESCE(emp.Contract, fm.Contract)
            LEFT JOIN dbo.Contract c_sponsor ON c_sponsor.Oid = fmSponsor.Contract
            WHERE pia.Application = a.Oid AND pia.GCRecord IS NULL
            ORDER BY pia.Oid
        ) personContract
        LEFT JOIN dbo.[{SeherEtrap}] se ON se.Oid = a.BusinessTripDestination
        LEFT JOIN dbo.[{GosmacaIslemageRugsatYeri}] mp ON mp.Oid = a.[{GosmacaIslemageRugsatYeri}]
        LEFT JOIN dbo.BorderZoneForVisa bz ON bz.Oid = a.BorderZoneForVisa AND bz.GCRecord IS NULL
        LEFT JOIN dbo.DepartmentForRegistration d ON d.Oid = a.DepartmentForRegistration
        WHERE a.GCRecord IS NULL
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
        var rawRows = new List<Visa2014ApplicationRawRow>();
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

        return TransformRows(rawRows, catalogs, out var skipped, out var unmappedDistinct, out var dedupeSummary);
    }

    internal static bool TryParseRawRow(IReadOnlyDictionary<string, string?> row, out Visa2014ApplicationRawRow parsed)
    {
        parsed = null!;
        if (!row.TryGetValue("Oid", out var oidText) ||
            !Guid.TryParse(oidText?.Trim(), out var legacyOid))
            return false;

        DateTime? manualDate = DateTime.TryParse(row.GetValueOrDefault("ManualApplicationDate"), out var appDate)
            ? appDate
            : null;

        parsed = new Visa2014ApplicationRawRow(
            LegacyOid: legacyOid,
            ManualApplicationNumber: row.GetValueOrDefault("ManualApplicationNumber"),
            ManualApplicationDate: manualDate,
            AutoRegistration: row.GetValueOrDefault("AutoRegistration") == "1",
            ForEmployee: row.GetValueOrDefault("ForEmployee") == "1",
            ForFamilyMember: row.GetValueOrDefault("ForFamilyMember") == "1",
            EmployeeSubtypeId: ParseNullableInt(row.GetValueOrDefault("EmployeeSubtypeId")),
            FamilySubtypeId: ParseNullableInt(row.GetValueOrDefault("FamilySubtypeId")),
            HasInvitationWpFk: row.GetValueOrDefault("HasInvitationWpFk") == "1",
            InvitationAndWorkPermitRequired: ParseNullableInt(row.GetValueOrDefault("InvitationAndWorkPermitRequired")),
            HasWizaWpFk: row.GetValueOrDefault("HasWizaWpFk") == "1",
            WizaAndWorkPermitRequired: ParseNullableInt(row.GetValueOrDefault("WizaAndWorkPermitRequired")),
            ChangeInformation: ParseNullableInt(row.GetValueOrDefault("ChangeInformation")),
            ApplicationUrgency: ParseNullableInt(row.GetValueOrDefault("ApplicationUrgency")),
            UrgencyMgCode: NullIfEmpty(row.GetValueOrDefault("UrgencyMgCode")),
            PeriodOfVisaL: row.GetValueOrDefault("PeriodOfVisaL"),
            VisaPeriodMgCode: NullIfEmpty(row.GetValueOrDefault("VisaPeriodMgCode")),
            VisaPeriodCountMonth: NullIfEmpty(row.GetValueOrDefault("VisaPeriodCountMonth")),
            CategoryOfVisaL: row.GetValueOrDefault("CategoryOfVisaL"),
            VisaCategoryMgCode: NullIfEmpty(row.GetValueOrDefault("VisaCategoryMgCode")),
            NumberOfContract: row.GetValueOrDefault("NumberOfContract"),
            ToCityMgCode: NullIfEmpty(row.GetValueOrDefault("ToCityMgCode")),
            ToCityName: row.GetValueOrDefault("ToCityName"),
            DateOfDeparture: DateTime.TryParse(row.GetValueOrDefault("DateOfDeparture"), out var departure) ? departure : null,
            DurationOfStay: int.TryParse(row.GetValueOrDefault("DurationOfStay"), out var duration) ? duration : 0,
            MovementPermitNameTm: row.GetValueOrDefault("MovementPermitNameTm"),
            HasBorderZoneFk: row.GetValueOrDefault("HasBorderZoneFk") == "1",
            BzDasoguz: row.GetValueOrDefault("BzDasoguz") == "1",
            BzTagtabazar: row.GetValueOrDefault("BzTagtabazar") == "1",
            BzSerhetabat: row.GetValueOrDefault("BzSerhetabat") == "1",
            BzYoloten: row.GetValueOrDefault("BzYoloten") == "1",
            BzFarap: row.GetValueOrDefault("BzFarap") == "1",
            BzGarabogaz: row.GetValueOrDefault("BzGarabogaz") == "1",
            BzSarahs: row.GetValueOrDefault("BzSarahs") == "1",
            BzEtrek: row.GetValueOrDefault("BzEtrek") == "1",
            DepartmentForRegistrationCode: NullIfEmpty(row.GetValueOrDefault("DepartmentForRegistrationCode")),
            DepartmentForRegistrationName: NullIfEmpty(row.GetValueOrDefault("DepartmentForRegistrationName")));
        return true;
    }

    private static Visa2014PersonImportBatch TransformRows(
        IReadOnlyList<Visa2014ApplicationRawRow> rawRows,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out List<Dictionary<string, object?>> skipped,
        out List<Dictionary<string, object?>> unmappedDistinct,
        out List<Dictionary<string, object?>> dedupeSummary)
    {
        skipped = [];
        dedupeSummary = [];
        var unmappedSet = new HashSet<string>(StringComparer.Ordinal);
        var working = rawRows.Select(r => new WorkingRow(r)).ToList();
        ApplyManualNumberDedupe(working, dedupeSummary);

        var importRows = new List<Dictionary<string, object?>>();
        foreach (var row in working)
        {
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

    private sealed class WorkingRow(Visa2014ApplicationRawRow Raw)
    {
        public Visa2014ApplicationRawRow Raw { get; } = Raw;
        public string? DedupeGroupId { get; set; }
    }

    private static void ApplyManualNumberDedupe(List<WorkingRow> rows, List<Dictionary<string, object?>> dedupeSummary)
    {
        var groups = rows
            .Select(r => new { Row = r, Norm = NormalizeManualNumber(r.Raw.ManualApplicationNumber) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Norm))
            .GroupBy(x => x.Norm, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.ToList();
            var groupId = $"MAN:{group.Key}";
            foreach (var member in members)
                member.Row.DedupeGroupId = groupId;

            dedupeSummary.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["_dedupeGroupId"] = groupId,
                ["key"] = "ManualApplicationNumber",
                ["normalizedValue"] = group.Key,
                ["memberCount"] = members.Count,
                ["canonicalRule"] = "keep_all_import_with_oid_upsert",
            });
        }
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
            ["_legacyTable"] = "Application",
            ["_dedupeGroupId"] = working.DedupeGroupId ?? "",
            ["_importAction"] = "import",
            ["_legacyManualNumber"] = raw.ManualApplicationNumber,
        };

        if (string.IsNullOrWhiteSpace(raw.ManualApplicationNumber))
        {
            skipReason = "required_null:FullApplicationNumber";
            return row;
        }

        if (!raw.ManualApplicationDate.HasValue)
        {
            skipReason = "required_null:ApplicationDate";
            row["FullApplicationNumber"] = raw.ManualApplicationNumber.Trim();
            return row;
        }

        ParseManualApplicationNumber(raw.ManualApplicationNumber, out var fullNumber, out var prefix, out var appNumber);
        row["FullApplicationNumber"] = fullNumber;
        row["ApplicationNumber"] = appNumber;
        row["AppNumberPrefix"] = prefix;
        row["ApplicationDate"] = raw.ManualApplicationDate.Value.ToString("yyyy-MM-dd");
        row["Year"] = raw.ManualApplicationDate.Value.Year;
        row["Month"] = raw.ManualApplicationDate.Value.Month;
        row["IsManualEntry"] = true;

        var subtypeId = raw.ForEmployee ? raw.EmployeeSubtypeId : raw.ForFamilyMember ? raw.FamilySubtypeId : null;
        row["_legacySubtypeId"] = subtypeId;

        var applicationTypeComposite = BuildApplicationTypeComposite(raw);
        row["_legacy_ApplicationTypeComposite"] = applicationTypeComposite;
        if (!TrySetApplicationType(row, catalogs, applicationTypeComposite, unmapped, ref skipReason))
            return row;

        TrySetMigrationService(row, catalogs, raw, unmapped);

        var urgencyComposite = BuildComposite(
            raw.ApplicationUrgency?.ToString() ?? "",
            raw.UrgencyMgCode ?? "");
        row["_legacy_UrgencyComposite"] = urgencyComposite;
        TrySetUrgency(row, catalogs, urgencyComposite, unmapped);

        if (!string.IsNullOrWhiteSpace(raw.PeriodOfVisaL))
        {
            var visaPeriodComposite = $"{raw.PeriodOfVisaL.Trim()}:{raw.VisaPeriodMgCode ?? ""}:{raw.VisaPeriodCountMonth ?? ""}";
            row["_legacy_VisaPeriodComposite"] = visaPeriodComposite;
            TrySetVisaPeriod(row, catalogs, visaPeriodComposite, unmapped);
        }
        else
        {
            row["_legacy_VisaPeriodComposite"] = "";
            row["VisaPeriod"] = null;
        }

        if (!string.IsNullOrWhiteSpace(raw.CategoryOfVisaL))
        {
            var visaCategoryComposite = BuildComposite(raw.CategoryOfVisaL, raw.VisaCategoryMgCode);
            row["_legacy_VisaCategoryComposite"] = visaCategoryComposite;
            TrySetVisaCategory(row, catalogs, visaCategoryComposite, unmapped);
        }
        else
        {
            row["_legacy_VisaCategoryComposite"] = "";
            row["VisaCategory"] = null;
        }

        TrySetProjectContract(row, catalogs, raw.NumberOfContract, unmapped);
        row["BorderZoneLocation"] = BuildBorderZoneLocation(catalogs, raw);
        TrySetMovementPermitLocation(row, raw.MovementPermitNameTm, unmapped);

        row["_legacy_ToCityMgCode"] = raw.ToCityMgCode;
        row["_legacy_ToCityName"] = raw.ToCityName;
        TrySetToCity(row, catalogs, raw.ToCityMgCode, raw.ToCityName, unmapped);

        if (raw.DateOfDeparture.HasValue)
        {
            row["BusinessTripStartDate"] = raw.DateOfDeparture.Value.ToString("yyyy-MM-dd");
            if (raw.DurationOfStay > 0)
                row["BusinessTripEndDate"] = raw.DateOfDeparture.Value.AddDays(raw.DurationOfStay - 1).ToString("yyyy-MM-dd");
            else
                row["BusinessTripEndDate"] = null;
        }
        else
        {
            row["BusinessTripStartDate"] = null;
            row["BusinessTripEndDate"] = null;
        }

        return row;
    }

    internal static void ParseManualApplicationNumber(
        string? manual,
        out string fullNumber,
        out string? prefix,
        out string? applicationNumber)
    {
        fullNumber = string.IsNullOrWhiteSpace(manual) ? "" : manual.Trim();
        prefix = null;
        applicationNumber = null;

        if (string.IsNullOrWhiteSpace(fullNumber))
            return;

        var slash = fullNumber.IndexOf('/');
        if (slash < 0)
        {
            applicationNumber = fullNumber;
            return;
        }

        prefix = fullNumber[..slash].Trim();
        var suffix = fullNumber[(slash + 1)..].Trim();
        if (suffix.StartsWith('-'))
            suffix = suffix[1..];
        applicationNumber = suffix;
    }

    internal static string BuildApplicationTypeComposite(Visa2014ApplicationRawRow raw) =>
        BuildApplicationTypeComposite(
            raw.ForEmployee,
            raw.ForFamilyMember,
            raw.EmployeeSubtypeId,
            raw.FamilySubtypeId,
            raw.HasInvitationWpFk,
            raw.InvitationAndWorkPermitRequired,
            raw.HasWizaWpFk,
            raw.WizaAndWorkPermitRequired,
            raw.ChangeInformation);

    internal static string BuildApplicationTypeComposite(
        bool forEmployee,
        bool forFamilyMember,
        int? employeeSubtypeId,
        int? familySubtypeId,
        bool hasInvitationWpFk,
        int? invitationAndWorkPermitRequired,
        bool hasWizaWpFk,
        int? wizaAndWorkPermitRequired,
        int? changeInformation)
    {
        var category = forEmployee ? "E" : forFamilyMember ? "F" : "";
        var subtypeId = forEmployee
            ? employeeSubtypeId
            : forFamilyMember
                ? familySubtypeId
                : null;
        var subtypeText = subtypeId?.ToString() ?? "";

        var invWp = "na";
        if (subtypeId == 0 && hasInvitationWpFk && invitationAndWorkPermitRequired.HasValue)
            invWp = invitationAndWorkPermitRequired.Value.ToString();

        var wizaWp = "na";
        if (subtypeId is 7 or 8 && hasWizaWpFk && wizaAndWorkPermitRequired.HasValue)
            wizaWp = wizaAndWorkPermitRequired.Value.ToString();

        var changeInfo = "na";
        if (subtypeId == 5 && changeInformation is 1 or 2)
            changeInfo = changeInformation.Value.ToString();

        return $"{category}:{subtypeText}:{invWp}:{wizaWp}:{changeInfo}";
    }

    internal static bool IsSkippedApplicationTypeComposite(string composite) =>
        ApplicationTypeSkipComposites.Contains(composite);

    private static bool TrySetApplicationType(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string composite,
        List<string> unmapped,
        ref string? skipReason)
    {
        if (ApplicationTypeSkipComposites.Contains(composite))
        {
            skipReason = $"skip_row:ApplicationType:{composite}";
            row["ApplicationType"] = null;
            return false;
        }

        if (Visa2014LookupTranslator.TryTranslate(catalogs, "ApplicationType", composite, out var target, out var reason) &&
            !string.IsNullOrWhiteSpace(target))
        {
            row["ApplicationType"] = target;
            return true;
        }

        if (reason != null)
            unmapped.Add(reason);

        skipReason = reason ?? $"unmapped_lookup:ApplicationType:{composite}";
        row["ApplicationType"] = null;
        return false;
    }

    private static void TrySetMigrationService(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Visa2014ApplicationRawRow raw,
        List<string> unmapped)
    {
        row["_legacy_DepartmentForRegistration"] = raw.DepartmentForRegistrationCode;
        row["_legacy_DepartmentForRegistrationName"] = raw.DepartmentForRegistrationName;
        row["MigrationService"] = null;

        if (string.IsNullOrWhiteSpace(raw.DepartmentForRegistrationCode))
            return;

        var applicationType = row.GetValueOrDefault("ApplicationType") as string;
        if (string.IsNullOrWhiteSpace(applicationType) ||
            !ShowMigrationServiceApplicationTypes.Contains(applicationType))
            return;

        if (Visa2014LookupTranslator.TryTranslate(
                catalogs,
                "MigrationService",
                ResolveMigrationServiceLegacyKey(raw.DepartmentForRegistrationCode, raw.DepartmentForRegistrationName),
                out var target,
                out var reason) &&
            !string.IsNullOrWhiteSpace(target))
        {
            row["MigrationService"] = target;
            return;
        }

        if (reason != null)
            unmapped.Add(reason);
    }

    /// <summary>
    /// Legacy <c>TDMGLBA</c> is shared by Kerki and Atamyrat branch labels — disambiguate via <c>DepartmentForRegistrationL</c>.
    /// </summary>
    internal static string? ResolveMigrationServiceLegacyKey(string? legacyCode, string? legacyNameTm)
    {
        if (string.IsNullOrWhiteSpace(legacyCode))
            return legacyCode;

        var code = legacyCode.Trim();
        if (!code.Equals("TDMGLBA", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(legacyNameTm))
        {
            return code;
        }

        var normalized = Visa2014CatalogMatchHelper.NormalizeKey(legacyNameTm);
        if (normalized.Contains("kerki", StringComparison.Ordinal))
            return "TDMGLBA:Kerki";
        if (normalized.Contains("atamyrat", StringComparison.Ordinal))
            return "TDMGLBA:Atamyrat";

        return code;
    }

    private static void TrySetUrgency(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string composite,
        List<string> unmapped)
    {
        if (Visa2014LookupTranslator.TryTranslate(catalogs, "Urgency", composite, out var target, out var reason) &&
            !string.IsNullOrWhiteSpace(target))
        {
            row["Urgency"] = target;
            return;
        }

        if (reason != null)
            unmapped.Add(reason);

        row["Urgency"] = "NORM";
    }

    private static void TrySetVisaPeriod(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string composite,
        List<string> unmapped)
    {
        if (Visa2014LookupTranslator.TryTranslate(catalogs, "VisaPeriod", composite, out var target, out var reason) &&
            !string.IsNullOrWhiteSpace(target))
        {
            row["VisaPeriod"] = target;
            return;
        }

        if (reason != null)
            unmapped.Add(reason);

        row["VisaPeriod"] = "Month6";
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
            return;
        }

        if (reason != null)
            unmapped.Add(reason);

        row["VisaCategory"] = "Multiple";
    }

    private static void TrySetProjectContract(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string? legacyCode,
        List<string> unmapped)
    {
        row["ProjectContract"] = null;
        row["_legacy_NumberOfContract"] = legacyCode;

        var applicationType = row.GetValueOrDefault("ApplicationType") as string;
        if (string.IsNullOrWhiteSpace(applicationType)
            || !ShowProjectContractApplicationTypes.Contains(applicationType))
            return;

        if (string.IsNullOrWhiteSpace(legacyCode))
            return;

        if (Visa2014LookupTranslator.TryTranslate(catalogs, "ProjectContract", legacyCode.Trim(), out var target, out var reason))
        {
            row["ProjectContract"] = target;
            if (reason != null)
                unmapped.Add(reason);
            return;
        }

        if (reason != null)
            unmapped.Add(reason);

        row["ProjectContract"] = legacyCode.Trim();
    }

    private static void TrySetMovementPermitLocation(
        Dictionary<string, object?> row,
        string? legacyNameTm,
        List<string> unmapped)
    {
        if (string.IsNullOrWhiteSpace(legacyNameTm))
        {
            row["MovementPermitLocation"] = null;
            return;
        }

        row["MovementPermitLocation"] = legacyNameTm.Trim();
    }

    private static void TrySetToCity(
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        string? cityMgCode,
        string? cityName,
        List<string> unmapped)
    {
        if (string.IsNullOrWhiteSpace(cityMgCode) && string.IsNullOrWhiteSpace(cityName))
        {
            row["ToCity"] = null;
            return;
        }

        if (!string.IsNullOrWhiteSpace(cityMgCode) &&
            Visa2014LookupTranslator.TryTranslate(catalogs, "City", cityMgCode, out var pdfCode, out _))
        {
            var resolved = ResolveCityNameFromPdfCode(pdfCode ?? cityMgCode);
            if (resolved != null)
            {
                row["ToCity"] = resolved;
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(cityName))
        {
            if (Visa2014LookupTranslator.TryTranslate(catalogs, "CityByName", cityName, out var byName, out var reason) &&
                !string.IsNullOrWhiteSpace(byName))
            {
                row["ToCity"] = byName;
                return;
            }

            if (reason != null)
                unmapped.Add(reason);

            row["ToCity"] = NormalizeLegacyCityName(cityName);
            return;
        }

        unmapped.Add($"unmapped_lookup:City:{cityMgCode}");
        row["ToCity"] = null;
    }

    private static string? ResolveCityNameFromPdfCode(string pdfCode) =>
        CityNameByPdfCode.GetValueOrDefault(pdfCode);

    private static string NormalizeLegacyCityName(string legacyCity) =>
        legacyCity
            .Replace("Asgabat", "Aşgabat", StringComparison.OrdinalIgnoreCase)
            .Replace("Dasoguz", "Daşoguz", StringComparison.OrdinalIgnoreCase)
            .Replace("Yoloten", "Ýolöten", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private static string BuildBorderZoneLocation(
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        Visa2014ApplicationRawRow raw)
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

    private static string NormalizeManualNumber(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "" : raw.Trim();

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

    private const string CommaSeparatedNoneValue = "Ýok";
}
