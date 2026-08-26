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

        if (verbose)
            Console.WriteLine(
                $"INF Visa IssuingApplicationProfileInstance index: {map.Count} visa(s) " +
                $"(from {piaLinks.Count} PIA link(s), {piaToApplication.Count} PIA→Application row(s))");

        return map;
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
