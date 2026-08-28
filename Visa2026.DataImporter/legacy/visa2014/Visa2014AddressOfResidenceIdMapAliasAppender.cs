namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014AddressOfResidenceIdMapAliasAppender
{
    internal static Task<int> AppendAsync(
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<Guid, Guid> applicationItemIdMap,
        Dictionary<Guid, Guid> addressIdMap,
        bool verbose)
    {
        int added = 0;
        added += Visa2014AddressOfResidenceSponsorCanonicalRegistration.RegisterFromExistingLegacyAor(
            legacyConnectionString, personIdMap, addressIdMap, verbose);

        var inferenceBatch = Visa2014PiaAddressInference.PrepareEmployeeInferredAddresses(
            legacyConnectionString, lookupTranslationPaths, verbose: false);

        foreach (var plan in inferenceBatch.Plans)
        {
            if (!personIdMap.TryGetValue(plan.LegacyPersonOid, out _))
                continue;

            Guid? targetId = null;
            if (addressIdMap.TryGetValue(plan.SyntheticLegacyOid, out var mappedSynthetic))
                targetId = mappedSynthetic;
            else
            {
                foreach (var alias in plan.LegacyAddressOidAliases)
                {
                    if (addressIdMap.TryGetValue(alias, out var mappedAlias))
                    {
                        targetId = mappedAlias;
                        break;
                    }
                }
            }

            if (!targetId.HasValue)
                continue;

            added += RegisterIfMissing(addressIdMap, plan.SyntheticLegacyOid, targetId.Value);
            foreach (var alias in plan.LegacyAddressOidAliases)
                added += RegisterIfMissing(addressIdMap, alias, targetId.Value);
        }

        if (applicationItemIdMap.Count > 0)
            added += AppendPiaLineAliases(
                legacyConnectionString,
                applicationItemIdMap,
                addressIdMap,
                verbose);

        return Task.FromResult(added);
    }

    private static int AppendPiaLineAliases(
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> applicationItemIdMap,
        Dictionary<Guid, Guid> addressIdMap,
        bool verbose)
    {
        var oidList = applicationItemIdMap.Keys.Distinct().ToList();
        if (oidList.Count == 0)
            return 0;

        var inClause = string.Join(",", oidList.Select(o => $"'{o:D}'"));
        var sql = $"""
            SELECT q.*
            FROM ({Visa2014ApplicationItemTransform.ExtractSql}) AS q
            WHERE q.Oid IN ({inClause})
            """;

        if (verbose)
            Console.WriteLine($"INF Appending PIA address aliases for {oidList.Count} ApplicationItem row(s)...");

        var dictRows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        int added = 0;

        foreach (var dict in dictRows)
        {
            if (!Visa2014ApplicationItemTransform.TryParseRawRow(dict, out var raw))
                continue;

            var legacyKey = Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw);
            if (!legacyKey.HasValue || addressIdMap.ContainsKey(legacyKey.Value))
                continue;

            if (raw.LegacyAddressOfResidenceOid.HasValue
                && addressIdMap.TryGetValue(raw.LegacyAddressOfResidenceOid.Value, out var fromAor))
            {
                added += RegisterIfMissing(addressIdMap, legacyKey.Value, fromAor);
                continue;
            }

            if (raw.LegacyDirectAddressOid.HasValue
                && addressIdMap.TryGetValue(raw.LegacyDirectAddressOid.Value, out var fromDirect))
            {
                added += RegisterIfMissing(addressIdMap, legacyKey.Value, fromDirect);
                continue;
            }

            if (raw.ForFamilyMember && raw.LegacyEmployeeOid.HasValue)
            {
                var synthetic = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(raw.LegacyEmployeeOid.Value);
                if (addressIdMap.TryGetValue(synthetic, out var fromSponsor))
                    added += RegisterIfMissing(addressIdMap, legacyKey.Value, fromSponsor);
            }
            else if (raw.LegacyEmployeeOid.HasValue)
            {
                var synthetic = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(raw.LegacyEmployeeOid.Value);
                if (addressIdMap.TryGetValue(synthetic, out var fromCanonical))
                    added += RegisterIfMissing(addressIdMap, legacyKey.Value, fromCanonical);
            }
        }

        return added;
    }

    internal static int RegisterIfMissing(IDictionary<Guid, Guid> addressIdMap, Guid legacyKey, Guid targetId)
    {
        if (addressIdMap.ContainsKey(legacyKey))
            return 0;

        addressIdMap[legacyKey] = targetId;
        return 1;
    }
}
