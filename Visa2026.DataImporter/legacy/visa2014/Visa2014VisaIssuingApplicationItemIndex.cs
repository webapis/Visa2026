namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Resolves legacy Visa.Oid → PersonInApplication.Oid for Visa.IssuingApplicationItem.
/// Prefer ProcessNumber when that PIA is on an extension app (subtype 7);
/// else extension sibling; else other ProcessNumber (e.g. sticky invitation FK).
/// </summary>
internal sealed record Visa2014VisaIssuingApplicationItemLink(
    Guid LegacyVisaOid,
    Guid LegacyApplicationItemOid,
    string Source);

internal static class Visa2014VisaIssuingApplicationItemIndex
{
    internal const string ProcessNumberSql = """
        SELECT
            CAST(v.Oid AS varchar(36)) AS VisaOid,
            CAST(pia.Oid AS varchar(36)) AS PiaOid,
            CASE
                WHEN ate.TypeOfApplicationForEmployee = 7
                  OR atfm.TypeOfApplicationForFamilyMember = 7
                THEN '1' ELSE '0'
            END AS IsExtensionApp
        FROM dbo.Visa v
        INNER JOIN dbo.PersonInApplication pia ON pia.Oid = v.ProcessNumber AND pia.GCRecord IS NULL
        INNER JOIN dbo.Application a ON a.Oid = pia.Application AND a.GCRecord IS NULL
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        WHERE v.GCRecord IS NULL
          AND v.ProcessNumber IS NOT NULL
        """;

    internal const string VisaRowsSql = """
        SELECT
            CAST(v.Oid AS varchar(36)) AS VisaOid,
            CAST(v.Passport AS varchar(36)) AS PassportOid,
            CONVERT(varchar(10), v.VisaIssuedDate, 23) AS VisaIssuedDate
        FROM dbo.Visa v
        WHERE v.GCRecord IS NULL
          AND v.Passport IS NOT NULL
          AND v.VisaIssuedDate IS NOT NULL
          AND v.VisaIssuedDate >= '2000-01-01'
        """;

    internal const string ExtensionPiaSql = """
        SELECT
            CAST(pia.Oid AS varchar(36)) AS PiaOid,
            CAST(pia.Visa AS varchar(36)) AS PrevVisaOid,
            CONVERT(varchar(10), r.ManualApplicationDate, 23) AS ApplicationDate
        FROM dbo.PersonInApplication pia
        INNER JOIN dbo.Application a ON a.Oid = pia.Application AND a.GCRecord IS NULL
        INNER JOIN dbo.IRegistration_Data r ON r.Oid = a.IRegistration_Data
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        WHERE pia.GCRecord IS NULL
          AND pia.Visa IS NOT NULL
          AND (
              ate.TypeOfApplicationForEmployee = 7
              OR atfm.TypeOfApplicationForFamilyMember = 7
          )
        """;

    public static IReadOnlyDictionary<Guid, Guid> LoadProcessNumbers(
        string connectionString,
        bool verbose)
    {
        const string sql = """
            SELECT
                CAST(v.Oid AS varchar(36)) AS VisaOid,
                CAST(v.ProcessNumber AS varchar(36)) AS PiaOid
            FROM dbo.Visa v
            WHERE v.GCRecord IS NULL
              AND v.ProcessNumber IS NOT NULL
            """;

        var map = new Dictionary<Guid, Guid>();
        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, sql, verbose))
        {
            if (TryGuid(row, "VisaOid", out var visaOid) && TryGuid(row, "PiaOid", out var piaOid))
                map[visaOid] = piaOid;
        }

        if (verbose)
            Console.WriteLine($"INF Visa ProcessNumber (legacy PIA) rows: {map.Count}");
        return map;
    }

    public static IReadOnlyDictionary<Guid, string> LoadAsNumbers(
        string connectionString,
        bool verbose)
    {
        const string sql = """
            SELECT
                CAST(v.Oid AS varchar(36)) AS VisaOid,
                LTRIM(RTRIM(v.ASNumber)) AS ASNumber
            FROM dbo.Visa v
            WHERE v.GCRecord IS NULL
              AND v.ASNumber IS NOT NULL
              AND LTRIM(RTRIM(v.ASNumber)) <> ''
            """;

        var map = new Dictionary<Guid, string>();
        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, sql, verbose))
        {
            if (!TryGuid(row, "VisaOid", out var visaOid))
                continue;
            var asNumber = row.GetValueOrDefault("ASNumber")?.Trim();
            if (string.IsNullOrWhiteSpace(asNumber))
                continue;
            map[visaOid] = asNumber;
        }

        if (verbose)
            Console.WriteLine($"INF Visa ASNumber (Işlenen belgisi) rows: {map.Count}");
        return map;
    }

    public static IReadOnlyDictionary<Guid, Visa2014VisaIssuingApplicationItemLink> Load(
        string connectionString,
        bool verbose)
    {
        var processRows = Visa2014SqlCmdReader.Query(connectionString, ProcessNumberSql, verbose);
        var visaRows = Visa2014SqlCmdReader.Query(connectionString, VisaRowsSql, verbose);
        var extRows = Visa2014SqlCmdReader.Query(connectionString, ExtensionPiaSql, verbose);

        var processLinks = new List<(Guid VisaOid, Guid PiaOid, bool IsExtensionApp)>();
        foreach (var row in processRows)
        {
            if (TryGuid(row, "VisaOid", out var visaOid) && TryGuid(row, "PiaOid", out var piaOid))
                processLinks.Add((visaOid, piaOid, row.GetValueOrDefault("IsExtensionApp") == "1"));
        }

        var visas = new List<(Guid VisaOid, Guid PassportOid, DateTime IssuedDate)>();
        foreach (var row in visaRows)
        {
            if (!TryGuid(row, "VisaOid", out var visaOid) || !TryGuid(row, "PassportOid", out var passportOid))
                continue;
            if (!DateTime.TryParse(row.GetValueOrDefault("VisaIssuedDate"), out var issued))
                continue;
            visas.Add((visaOid, passportOid, issued));
        }

        var extensionPias = new List<(Guid PiaOid, Guid PrevVisaOid, DateTime? AppDate)>();
        foreach (var row in extRows)
        {
            if (!TryGuid(row, "PiaOid", out var piaOid) || !TryGuid(row, "PrevVisaOid", out var prevVisaOid))
                continue;
            DateTime? appDate = DateTime.TryParse(row.GetValueOrDefault("ApplicationDate"), out var d) ? d : null;
            extensionPias.Add((piaOid, prevVisaOid, appDate));
        }

        var map = Build(processLinks, visas, extensionPias);
        if (verbose)
        {
            var byProcess = map.Values.Count(v => v.Source == "processnumber");
            var bySibling = map.Values.Count(v => v.Source == "extension_sibling");
            var processExt = processLinks.Count(x => x.IsExtensionApp);
            var processOther = processLinks.Count(x => !x.IsExtensionApp);
            Console.WriteLine(
                $"INF Visa IssuingApplicationItem index: {map.Count} visa(s) " +
                $"(processnumber={byProcess}, extension_sibling={bySibling}; " +
                $"legacy ProcessNumber extension={processExt}, other={processOther})");
        }

        return map;
    }

    /// <summary>
    /// Merge: extension ProcessNumber first, then sibling, then remaining ProcessNumber.
    /// </summary>
    internal static Dictionary<Guid, Visa2014VisaIssuingApplicationItemLink> Build(
        IEnumerable<(Guid VisaOid, Guid PiaOid, bool IsExtensionApp)> processNumberLinks,
        IEnumerable<(Guid VisaOid, Guid PassportOid, DateTime IssuedDate)> visas,
        IEnumerable<(Guid PiaOid, Guid PrevVisaOid, DateTime? AppDate)> extensionPias)
    {
        var map = new Dictionary<Guid, Visa2014VisaIssuingApplicationItemLink>();
        var processList = processNumberLinks.ToList();

        foreach (var (visaOid, piaOid, isExtension) in processList)
        {
            if (!isExtension)
                continue;
            map[visaOid] = new Visa2014VisaIssuingApplicationItemLink(visaOid, piaOid, "processnumber");
        }

        var bestPiaByPrevVisa = new Dictionary<Guid, (Guid PiaOid, DateTime? AppDate)>();
        foreach (var group in extensionPias.GroupBy(x => x.PrevVisaOid))
        {
            var best = group
                .OrderByDescending(x => x.AppDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.PiaOid)
                .First();
            bestPiaByPrevVisa[group.Key] = (best.PiaOid, best.AppDate);
        }

        foreach (var passportGroup in visas.GroupBy(v => v.PassportOid))
        {
            var ordered = passportGroup
                .OrderBy(v => v.IssuedDate)
                .ThenBy(v => v.VisaOid)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var current = ordered[i];
                if (map.ContainsKey(current.VisaOid))
                    continue;

                (Guid VisaOid, Guid PassportOid, DateTime IssuedDate)? previous = null;
                for (var j = i - 1; j >= 0; j--)
                {
                    if (ordered[j].IssuedDate < current.IssuedDate)
                    {
                        previous = ordered[j];
                        break;
                    }
                }

                if (previous is null)
                    continue;

                if (!bestPiaByPrevVisa.TryGetValue(previous.Value.VisaOid, out var pia))
                    continue;

                map[current.VisaOid] = new Visa2014VisaIssuingApplicationItemLink(
                    current.VisaOid,
                    pia.PiaOid,
                    "extension_sibling");
            }
        }

        foreach (var (visaOid, piaOid, isExtension) in processList)
        {
            if (isExtension || map.ContainsKey(visaOid))
                continue;
            map[visaOid] = new Visa2014VisaIssuingApplicationItemLink(visaOid, piaOid, "processnumber");
        }

        return map;
    }

    private static bool TryGuid(IReadOnlyDictionary<string, string?> row, string key, out Guid value)
    {
        value = default;
        return row.TryGetValue(key, out var text) && Guid.TryParse(text?.Trim(), out value);
    }
}