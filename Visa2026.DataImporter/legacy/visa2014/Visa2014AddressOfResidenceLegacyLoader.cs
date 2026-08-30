namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014AddressOfResidenceLegacyLoader
{
    internal static bool TryLoadLegacyAorRow(
        string legacyConnectionString,
        Guid legacyAorOid,
        out Visa2014AddressOfResidenceRawRow row)
    {
        row = null!;
        var sql = $"""
            SELECT *
            FROM ({Visa2014AddressOfResidenceTransform.ExtractSql}) AS q
            WHERE q.Oid = '{legacyAorOid:D}'
            """;

        var rows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        if (rows.Count == 0)
            return false;

        return Visa2014AddressOfResidenceTransform.TryParseRawRow(rows[0], out row);
    }

    internal static bool TryBuildImportRowForLegacyAddressKey(
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        Guid legacyAddressKey,
        Visa2014ApplicationItemRawRow raw,
        out Dictionary<string, object?>? importRow)
    {
        importRow = null;
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);

        if (raw.LegacyAddressOfResidenceOid == legacyAddressKey
            && TryLoadLegacyAorRow(legacyConnectionString, legacyAddressKey, out var aorRow)
            && Visa2014AddressOfResidenceTransform.TryBuildImportRow(
                aorRow, catalogs, legacyAddressKey, out importRow, out _))
        {
            return true;
        }

        if (raw.LegacyDirectAddressOid == legacyAddressKey)
        {
            return TryBuildImportRowFromPiaDirectAddress(
                legacyConnectionString,
                lookupTranslationPaths,
                legacyAddressKey,
                raw,
                catalogs,
                out importRow);
        }

        return false;
    }

    private static bool TryBuildImportRowFromPiaDirectAddress(
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        Guid legacyAddressOid,
        Visa2014ApplicationItemRawRow raw,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        out Dictionary<string, object?>? importRow)
    {
        importRow = null;
        var batch = Visa2014PiaAddressInference.PrepareEmployeeInferredAddresses(
            legacyConnectionString,
            lookupTranslationPaths,
            verbose: false);

        var personOid = ResolvePersonLegacyOid(raw);
        foreach (var plan in batch.Plans)
        {
            if (!plan.LegacyAddressOidAliases.Contains(legacyAddressOid)
                && plan.SyntheticLegacyOid != legacyAddressOid
                && (!personOid.HasValue || plan.LegacyPersonOid != personOid.Value))
            {
                continue;
            }

            importRow = new Dictionary<string, object?>(plan.ImportRow, StringComparer.Ordinal);
            importRow["_legacyRowId"] = legacyAddressOid.ToString("D");
            return true;
        }

        if (!personOid.HasValue)
            return false;

        if (!TryLoadLegacyDirectAddressRow(
                legacyConnectionString,
                legacyAddressOid,
                personOid.Value,
                out var directRow))
        {
            return false;
        }

        return Visa2014AddressOfResidenceTransform.TryBuildImportRow(
            directRow, catalogs, legacyAddressOid, out importRow, out _);
    }

    private static bool TryLoadLegacyDirectAddressRow(
        string legacyConnectionString,
        Guid legacyAddressOid,
        Guid legacyPersonOid,
        out Visa2014AddressOfResidenceRawRow row)
    {
        row = null!;
        const string seherEtrap = "\u015E\u00E4herEtrap";
        var sql = $"""
            SELECT
                CAST(addr.Oid AS varchar(36)) AS Oid,
                '{legacyPersonOid:D}' AS LegacyPersonOid,
                doa.TypeOfDocument,
                ISNULL(rgn.mgCode, '') AS RegionMgCode,
                rgn.NameOfRegion AS RegionName,
                ISNULL(se.mgCode, '') AS CityMgCode,
                se.[{seherEtrap}L] AS CityName,
                addr.AddressLine,
                CONVERT(varchar(10), addr.ExpiringDateOfAddressDocument, 23) AS ExpirationDate
            FROM dbo.Address addr
            LEFT JOIN dbo.Region rgn ON addr.Region = rgn.Oid
            LEFT JOIN dbo.[{seherEtrap}] se ON addr.[{seherEtrap}] = se.Oid
            LEFT JOIN dbo.DocumentOfAddress doa ON addr.DocumentOfAddress = doa.Oid
            WHERE addr.Oid = '{legacyAddressOid:D}'
              AND addr.GCRecord IS NULL
            """;

        var rows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        if (rows.Count == 0)
            return false;

        return Visa2014AddressOfResidenceTransform.TryParseRawRow(rows[0], out row);
    }

    /// <summary>
    /// Employee PIA → <see cref="Visa2014ApplicationItemRawRow.LegacyEmployeeOid"/>;
    /// family PIA → <see cref="Visa2014ApplicationItemRawRow.LegacyFamilyMemberOid"/>; else null.
    /// </summary>
    internal static Guid? ResolvePersonLegacyOid(Visa2014ApplicationItemRawRow raw) =>
        Visa2014ApplicationItemPersonOidResolver.Resolve(
            raw.ForEmployee,
            raw.ForFamilyMember,
            raw.LegacyEmployeeOid,
            raw.LegacyFamilyMemberOid);
}
