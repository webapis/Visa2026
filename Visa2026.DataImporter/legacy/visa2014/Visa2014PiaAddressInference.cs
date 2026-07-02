namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Infers Person AddressOfResidence rows from PersonInApplication when the person has no legacy children.
/// Employees: one canonical address per person from pia.Address (latest application).
/// Family members: ApplicationItem lines use the sponsor employee address (no duplicate on FM person).
/// </summary>
internal static class Visa2014PiaAddressInference
{
    private const string SeherEtrap = "\u015E\u00E4herEtrap";

    internal sealed record PiaAddressCandidateRow(
        Guid PiaOid,
        Guid LinePersonOid,
        Guid? SponsorEmployeeOid,
        bool IsFamilyLine,
        Guid? LegacyAddressOfResidenceOid,
        Guid? LegacyDirectAddressOid,
        DateTime? ApplicationDate,
        DateTime? RegistrationDate,
        string? DocumentType,
        string? RegionMgCode,
        string? RegionName,
        string? CityMgCode,
        string? CityName,
        string? AddressLine,
        DateTime? ExpirationDate);

    internal sealed record PiaInferredAddressPlan(
        Guid LegacyPersonOid,
        Guid SyntheticLegacyOid,
        Dictionary<string, object?> ImportRow,
        IReadOnlyList<Guid> LegacyAddressOidAliases);

    internal sealed record PiaInferredAddressBatch(
        IReadOnlyList<PiaInferredAddressPlan> Plans,
        IReadOnlyDictionary<Guid, Guid> SponsorCanonicalLegacyKeys,
        int SkippedUnmapped,
        IReadOnlyList<Dictionary<string, object?>> Skipped);

    public static Guid PersonCanonicalSyntheticLegacyOid(Guid personOid) =>
        DeterministicLegacyOid($"person-pia-canonical-aor:{personOid:D}");

    private static Guid DeterministicLegacyOid(string seed)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }

    public static Guid? ResolveApplicationItemCurrentAddressLegacyKey(Visa2014ApplicationItemRawRow raw)
    {
        if (raw.LegacyAddressOfResidenceOid.HasValue)
            return raw.LegacyAddressOfResidenceOid;

        if (raw.ForFamilyMember)
        {
            if (raw.LegacyEmployeeOid.HasValue &&
                (raw.LegacyDirectAddressOid.HasValue || raw.LegacyAddressOfResidenceOid == null))
                return PersonCanonicalSyntheticLegacyOid(raw.LegacyEmployeeOid.Value);

            return raw.LegacyDirectAddressOid;
        }

        if (raw.LegacyDirectAddressOid.HasValue)
            return raw.LegacyDirectAddressOid;

        if (raw.LegacyEmployeeOid.HasValue)
            return PersonCanonicalSyntheticLegacyOid(raw.LegacyEmployeeOid.Value);

        return null;
    }

    internal static PiaInferredAddressBatch PrepareEmployeeInferredAddresses(
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var candidates = LoadCandidates(legacyConnectionString, verbose);
        var skipped = new List<Dictionary<string, object?>>();
        var plans = new List<PiaInferredAddressPlan>();
        var sponsorKeys = new Dictionary<Guid, Guid>();
        int skippedUnmapped = 0;

        var employeeGroups = candidates
            .Where(c => !c.IsFamilyLine && c.LegacyDirectAddressOid.HasValue)
            .GroupBy(c => c.LinePersonOid);

        foreach (var group in employeeGroups)
        {
            var canonical = group
                .OrderByDescending(c => c.ApplicationDate ?? DateTime.MinValue)
                .ThenByDescending(c => c.RegistrationDate ?? DateTime.MinValue)
                .ThenByDescending(c => c.PiaOid)
                .First();

            var rawRow = new Visa2014AddressOfResidenceRawRow(
                LegacyOid: canonical.LegacyDirectAddressOid!.Value,
                LegacyPersonOid: canonical.LinePersonOid,
                DocumentType: canonical.DocumentType,
                RegionMgCode: canonical.RegionMgCode,
                RegionName: canonical.RegionName,
                CityMgCode: canonical.CityMgCode,
                CityName: canonical.CityName,
                AddressLine: canonical.AddressLine,
                ExpirationDate: canonical.ExpirationDate);

            var syntheticOid = PersonCanonicalSyntheticLegacyOid(canonical.LinePersonOid);
            if (!Visa2014AddressOfResidenceTransform.TryBuildImportRow(
                    rawRow, catalogs, syntheticOid, out var importRow, out var skipReason))
            {
                skippedUnmapped++;
                skipped.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["_legacyPersonOid"] = canonical.LinePersonOid.ToString(),
                    ["_legacyPiaOid"] = canonical.PiaOid.ToString(),
                    ["reason"] = skipReason ?? "unmapped",
                    ["_legacy_AddressLine"] = canonical.AddressLine,
                });
                continue;
            }

            var aliases = group
                .Select(c => c.LegacyDirectAddressOid!.Value)
                .Distinct()
                .ToList();

            plans.Add(new PiaInferredAddressPlan(
                canonical.LinePersonOid,
                syntheticOid,
                importRow!,
                aliases));

            sponsorKeys[canonical.LinePersonOid] = syntheticOid;
        }

        foreach (var sponsorOid in candidates
                     .Where(c => c.IsFamilyLine && c.SponsorEmployeeOid.HasValue)
                     .Select(c => c.SponsorEmployeeOid!.Value)
                     .Distinct())
        {
            if (!sponsorKeys.ContainsKey(sponsorOid))
                sponsorKeys[sponsorOid] = PersonCanonicalSyntheticLegacyOid(sponsorOid);
        }

        if (verbose)
        {
            Console.WriteLine(
                $"INF PIA address inference: {plans.Count} employee canonical plan(s), " +
                $"{sponsorKeys.Count} sponsor key(s), {skippedUnmapped} skipped (lookup gaps).");
        }

        return new PiaInferredAddressBatch(plans, sponsorKeys, skippedUnmapped, skipped);
    }

    private static List<PiaAddressCandidateRow> LoadCandidates(string legacyConnectionString, bool verbose)
    {
        var sql = $"""
            WITH persons_without_own_aor AS (
                SELECT p.Oid AS PersonOid
                FROM dbo.Person p
                WHERE p.GCRecord IS NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.AddressOfResidence aor
                      WHERE aor.Person = p.Oid AND aor.GCRecord IS NULL)
            )
            SELECT
                CAST(pia.Oid AS varchar(36)) AS PiaOid,
                CAST(COALESCE(pia.FamilyMember, pia.Employee) AS varchar(36)) AS LinePersonOid,
                CAST(pia.Employee AS varchar(36)) AS SponsorEmployeeOid,
                CASE WHEN pia.FamilyMember IS NOT NULL THEN '1' ELSE '0' END AS IsFamilyLine,
                CAST(pia.AddressOfResidence AS varchar(36)) AS AddressOfResidenceOid,
                CAST(pia.Address AS varchar(36)) AS DirectAddressOid,
                CONVERT(varchar(23), reg.ManualApplicationDate, 121) AS ApplicationDate,
                CONVERT(varchar(23), pia.RegistrationDate, 121) AS RegistrationDate,
                doa.TypeOfDocument,
                ISNULL(rgn.mgCode, '') AS RegionMgCode,
                rgn.NameOfRegion AS RegionName,
                ISNULL(se.mgCode, '') AS CityMgCode,
                se.[{SeherEtrap}L] AS CityName,
                addr.AddressLine,
                CONVERT(varchar(10), addr.ExpiringDateOfAddressDocument, 23) AS ExpirationDate
            FROM dbo.PersonInApplication pia
            INNER JOIN dbo.Application app ON app.Oid = pia.Application AND app.GCRecord IS NULL
            LEFT JOIN dbo.IRegistration_Data reg ON reg.Oid = app.IRegistration_Data
            LEFT JOIN dbo.Address addr ON addr.Oid = pia.Address AND addr.GCRecord IS NULL
            LEFT JOIN dbo.Region rgn ON addr.Region = rgn.Oid
            LEFT JOIN dbo.[{SeherEtrap}] se ON addr.[{SeherEtrap}] = se.Oid
            LEFT JOIN dbo.DocumentOfAddress doa ON addr.DocumentOfAddress = doa.Oid
            WHERE pia.GCRecord IS NULL
              AND (
                  (pia.FamilyMember IS NULL AND pia.Employee IN (SELECT PersonOid FROM persons_without_own_aor) AND pia.Address IS NOT NULL)
                  OR (pia.FamilyMember IS NOT NULL AND (pia.AddressOfResidence IS NOT NULL OR pia.Address IS NOT NULL))
              )
            """;

        var dictRows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose);
        var rows = new List<PiaAddressCandidateRow>();
        foreach (var dict in dictRows)
        {
            if (!Guid.TryParse(dict.GetValueOrDefault("PiaOid"), out var piaOid))
                continue;
            if (!Guid.TryParse(dict.GetValueOrDefault("LinePersonOid"), out var linePersonOid))
                continue;

            rows.Add(new PiaAddressCandidateRow(
                PiaOid: piaOid,
                LinePersonOid: linePersonOid,
                SponsorEmployeeOid: TryParseNullableGuid(dict.GetValueOrDefault("SponsorEmployeeOid")),
                IsFamilyLine: dict.GetValueOrDefault("IsFamilyLine") == "1",
                LegacyAddressOfResidenceOid: TryParseNullableGuid(dict.GetValueOrDefault("AddressOfResidenceOid")),
                LegacyDirectAddressOid: TryParseNullableGuid(dict.GetValueOrDefault("DirectAddressOid")),
                ApplicationDate: TryParseDate(dict.GetValueOrDefault("ApplicationDate")),
                RegistrationDate: TryParseDate(dict.GetValueOrDefault("RegistrationDate")),
                DocumentType: dict.GetValueOrDefault("TypeOfDocument"),
                RegionMgCode: NullIfEmpty(dict.GetValueOrDefault("RegionMgCode")),
                RegionName: dict.GetValueOrDefault("RegionName"),
                CityMgCode: NullIfEmpty(dict.GetValueOrDefault("CityMgCode")),
                CityName: dict.GetValueOrDefault("CityName"),
                AddressLine: dict.GetValueOrDefault("AddressLine"),
                ExpirationDate: TryParseDate(dict.GetValueOrDefault("ExpirationDate"))));
        }

        return rows;
    }

    internal static void RegisterPlanAliases(
        PiaInferredAddressPlan plan,
        Guid createdTargetId,
        IDictionary<Guid, Guid> addressIdMap)
    {
        addressIdMap[plan.SyntheticLegacyOid] = createdTargetId;
        foreach (var alias in plan.LegacyAddressOidAliases)
            addressIdMap[alias] = createdTargetId;
    }

    internal static void RegisterSponsorCanonicalAlias(
        Guid sponsorLegacyPersonOid,
        Guid sponsorTargetAddressId,
        IDictionary<Guid, Guid> addressIdMap)
    {
        addressIdMap[PersonCanonicalSyntheticLegacyOid(sponsorLegacyPersonOid)] = sponsorTargetAddressId;
    }

    private static Guid? TryParseNullableGuid(string? text) =>
        Guid.TryParse(text?.Trim(), out var value) ? value : null;

    private static DateTime? TryParseDate(string? text) =>
        DateTime.TryParse(text, out var value) ? value : null;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}