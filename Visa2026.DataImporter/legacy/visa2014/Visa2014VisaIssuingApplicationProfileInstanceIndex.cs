namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Path B: legacy Visa.Oid → legacy Application.Oid for <see cref="Visa2026.Module.BusinessObjects.Visa.IssuingApplicationProfileInstance"/>.
/// Uses the same PIA resolution as the retired IssuingApplicationItem index, then maps PIA → Application.
/// </summary>
internal static class Visa2014VisaIssuingApplicationProfileInstanceIndex
{
    internal const string PiaToApplicationSql = """
        SELECT
            CAST(pia.Oid AS varchar(36)) AS PiaOid,
            CAST(pia.Application AS varchar(36)) AS ApplicationOid
        FROM dbo.PersonInApplication pia
        WHERE pia.GCRecord IS NULL
          AND pia.Application IS NOT NULL
        """;

    public static IReadOnlyDictionary<Guid, Guid> Load(
        string connectionString,
        bool verbose)
    {
        var piaToApplication = LoadPiaToApplication(connectionString, verbose);
        var piaLinks = Visa2014VisaIssuingApplicationItemIndex.Load(connectionString, verbose);

        var map = new Dictionary<Guid, Guid>();
        foreach (var link in piaLinks.Values)
        {
            if (!piaToApplication.TryGetValue(link.LegacyApplicationItemOid, out var legacyApplicationOid))
                continue;

            map[link.LegacyVisaOid] = legacyApplicationOid;
        }

        var asNumbers = Visa2014VisaIssuingApplicationItemIndex.LoadAsNumbers(connectionString, verbose);
        var applicationByProcessNumber = LoadUniqueApplicationProcessNumbers(connectionString, verbose);
        var asOverlay = OverlayAsNumberMatches(map, asNumbers, applicationByProcessNumber);

        if (verbose)
            Console.WriteLine(
                $"INF Visa IssuingApplicationProfileInstance index: {map.Count} visa(s) " +
                $"(from {piaLinks.Count} PIA link(s), {piaToApplication.Count} PIA→Application row(s), " +
                $"ASNumber overlay {asOverlay})");

        return map;
    }

    /// <summary>
    /// Visa.ASNumber (Işlenen belgisi) → Application.ProcessNumber is the issued-by
    /// match for passport-change and other types whose Visa.ProcessNumber stays on
    /// the original invitation PIA. Unique ProcessNumber only; overlay wins.
    /// </summary>
    internal static int OverlayAsNumberMatches(
        Dictionary<Guid, Guid> visaToApplication,
        IReadOnlyDictionary<Guid, string> visaAsNumbers,
        IReadOnlyDictionary<string, Guid> applicationByProcessNumber)
    {
        var overlaid = 0;
        foreach (var (visaOid, asNumber) in visaAsNumbers)
        {
            if (string.IsNullOrWhiteSpace(asNumber))
                continue;
            if (!applicationByProcessNumber.TryGetValue(asNumber.Trim(), out var applicationOid))
                continue;
            if (visaToApplication.TryGetValue(visaOid, out var existing) && existing == applicationOid)
                continue;

            visaToApplication[visaOid] = applicationOid;
            overlaid++;
        }

        return overlaid;
    }

    internal static Dictionary<string, Guid> LoadUniqueApplicationProcessNumbers(
        string connectionString,
        bool verbose)
    {
        const string sql = """
            SELECT
                CAST(a.Oid AS varchar(36)) AS ApplicationOid,
                LTRIM(RTRIM(a.ProcessNumber)) AS ProcessNumber
            FROM dbo.Application a
            WHERE a.GCRecord IS NULL
              AND a.ProcessNumber IS NOT NULL
              AND LTRIM(RTRIM(a.ProcessNumber)) <> ''
            """;

        var counts = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, sql, verbose))
        {
            if (!TryGuid(row, "ApplicationOid", out var applicationOid))
                continue;
            var processNumber = row.GetValueOrDefault("ProcessNumber")?.Trim();
            if (string.IsNullOrWhiteSpace(processNumber))
                continue;
            if (!counts.TryGetValue(processNumber, out var list))
            {
                list = [];
                counts[processNumber] = list;
            }

            if (!list.Contains(applicationOid))
                list.Add(applicationOid);
        }

        var unique = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var (processNumber, list) in counts)
        {
            if (list.Count == 1)
                unique[processNumber] = list[0];
        }

        if (verbose)
            Console.WriteLine(
                $"INF Application.ProcessNumber unique keys: {unique.Count} " +
                $"(skipped {counts.Count - unique.Count} duplicate ProcessNumber group(s))");

        return unique;
    }

    private static Dictionary<Guid, Guid> LoadPiaToApplication(string connectionString, bool verbose)
    {
        var map = new Dictionary<Guid, Guid>();
        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, PiaToApplicationSql, verbose))
        {
            if (!TryGuid(row, "PiaOid", out var piaOid) || !TryGuid(row, "ApplicationOid", out var applicationOid))
                continue;

            map[piaOid] = applicationOid;
        }

        if (verbose)
            Console.WriteLine($"INF PersonInApplication → Application rows: {map.Count}");

        return map;
    }

    private static bool TryGuid(IReadOnlyDictionary<string, string?> row, string key, out Guid value)
    {
        value = default;
        return row.TryGetValue(key, out var text) && Guid.TryParse(text?.Trim(), out value);
    }
}
