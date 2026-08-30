namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Applies passport dedupe aliases onto an id-map (merged legacy Oid → same target as canonical).
/// </summary>
internal static class Visa2014PassportIdMapAliasApplier
{
    /// <summary>
    /// Maps merged (duplicate) legacy passport Oids onto the canonical row's target id.
    /// Skips aliases whose canonical key is missing, and never overwrites an existing merged key.
    /// </summary>
    internal static int ApplyDedupeAliases(
        Dictionary<string, string> idMap,
        IEnumerable<KeyValuePair<Guid, Guid>> dedupeAliases)
    {
        int addedFromDedupe = 0;
        foreach (var (mergedLegacyOid, canonicalLegacyOid) in dedupeAliases)
        {
            var canonicalKey = canonicalLegacyOid.ToString();
            if (!idMap.TryGetValue(canonicalKey, out var targetId))
                continue;

            var mergedKey = mergedLegacyOid.ToString();
            if (idMap.ContainsKey(mergedKey))
                continue;

            idMap[mergedKey] = targetId;
            addedFromDedupe++;
        }

        return addedFromDedupe;
    }
}
