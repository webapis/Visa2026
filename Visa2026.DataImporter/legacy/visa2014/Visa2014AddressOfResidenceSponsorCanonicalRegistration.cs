namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014AddressOfResidenceSponsorCanonicalRegistration
{
    internal static int RegisterFromExistingLegacyAor(
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IDictionary<Guid, Guid> addressIdMap,
        bool verbose)
    {
        const string sql = """
            SELECT
                CAST(aor.Person AS varchar(36)) AS PersonOid,
                CAST(aor.Oid AS varchar(36)) AS AorOid,
                CONVERT(varchar(10), addr.ExpiringDateOfAddressDocument, 23) AS ExpirationDate
            FROM dbo.AddressOfResidence aor
            INNER JOIN dbo.Address addr ON addr.Oid = aor.Address AND addr.GCRecord IS NULL
            WHERE aor.GCRecord IS NULL
            """;

        var rows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        var bestPerPerson = new Dictionary<Guid, (Guid AorOid, DateTime? Expiration)>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("PersonOid"), out var personOid))
                continue;
            if (!Guid.TryParse(row.GetValueOrDefault("AorOid"), out var aorOid))
                continue;
            if (!personIdMap.ContainsKey(personOid))
                continue;

            DateTime? expiration = DateTime.TryParse(row.GetValueOrDefault("ExpirationDate"), out var exp) ? exp : null;
            if (!bestPerPerson.TryGetValue(personOid, out var current) ||
                CompareAddressRecency(expiration, aorOid, current.Expiration, current.AorOid) > 0)
            {
                bestPerPerson[personOid] = (aorOid, expiration);
            }
        }

        int registered = 0;
        foreach (var (personOid, best) in bestPerPerson)
        {
            if (!addressIdMap.TryGetValue(best.AorOid, out var targetId))
                continue;

            var synthetic = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(personOid);
            if (addressIdMap.ContainsKey(synthetic))
                continue;

            addressIdMap[synthetic] = targetId;
            registered++;
        }

        if (verbose && registered > 0)
            Console.WriteLine($"INF Registered {registered} sponsor canonical AddressOfResidence alias(es).");

        return registered;
    }

    /// <summary>
    /// Prefer open-ended (null expiration) addresses, then later expiration dates; OID breaks ties.
    /// </summary>
    internal static int CompareAddressRecency(DateTime? expA, Guid oidA, DateTime? expB, Guid oidB)
    {
        var rankA = expA?.Date ?? DateTime.MaxValue;
        var rankB = expB?.Date ?? DateTime.MaxValue;
        var cmp = rankA.CompareTo(rankB);
        return cmp != 0 ? cmp : oidA.CompareTo(oidB);
    }
}
