namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// App_Additional_WP_location header strategy: when Application.GoşmaçaIşlemägeRugsatÝeri is empty,
/// fill MovementPermitLocation from the majority PersonInApplication.WorkPermit.WorkPermitLocation
/// bit matrix (same catalog as WorkPermitItem.WorkPermittedLocations).
/// </summary>
internal static class Visa2014ApplicationWorkPermitLocationFallbackIndex
{
    internal const string AdditionalWpLocationApplicationTypeName = "App_Additional_WP_location";
    internal const int MovementPermitLocationMaxLength = 500;

    internal const string MajorityLocationSql = """
        WITH ranked AS (
            SELECT
                pia.Application AS ApplicationOid,
                wp.WorkPermitLocation AS LocationOid,
                COUNT(*) AS Cnt,
                MIN(pia.Oid) AS MinPiaOid
            FROM dbo.PersonInApplication pia
            INNER JOIN dbo.WorkPermit wp ON wp.Oid = pia.WorkPermit AND wp.GCRecord IS NULL
            WHERE pia.GCRecord IS NULL
              AND wp.WorkPermitLocation IS NOT NULL
            GROUP BY pia.Application, wp.WorkPermitLocation
        )
        SELECT
            CAST(ApplicationOid AS varchar(36)) AS ApplicationOid,
            CAST(LocationOid AS varchar(36)) AS LocationOid
        FROM (
            SELECT
                ApplicationOid,
                LocationOid,
                ROW_NUMBER() OVER (
                    PARTITION BY ApplicationOid
                    ORDER BY Cnt DESC, MinPiaOid ASC) AS rn
            FROM ranked
        ) picked
        WHERE rn = 1
        """;

    public static IReadOnlyDictionary<Guid, string> Load(
        string legacyConnectionString,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        bool verbose)
    {
        var pairRows = Visa2014SqlCmdReader.Query(legacyConnectionString, MajorityLocationSql, verbose: false);
        var locationByApp = new Dictionary<Guid, Guid>();
        foreach (var row in pairRows)
        {
            if (!Guid.TryParse(row.GetValueOrDefault("ApplicationOid"), out var applicationOid))
                continue;
            if (!Guid.TryParse(row.GetValueOrDefault("LocationOid"), out var locationOid))
                continue;
            locationByApp[applicationOid] = locationOid;
        }

        if (locationByApp.Count == 0)
            return new Dictionary<Guid, string>();

        var bitColumns = Visa2014WorkPermitLocationBitMatrix.LoadBitColumnNames(legacyConnectionString);
        var locationRows = Visa2014WorkPermitLocationBitMatrix.LoadLocationRows(
            legacyConnectionString,
            locationByApp.Values,
            verbose);

        var labels = new Dictionary<Guid, string>();
        foreach (var (applicationOid, locationOid) in locationByApp)
        {
            locationRows.TryGetValue(locationOid, out var locationRow);
            var text = Visa2014WorkPermitLocationBitMatrix.BuildWorkPermittedLocations(
                locationRow,
                bitColumns,
                catalogs,
                unmappedCollector: null);
            if (string.IsNullOrWhiteSpace(text))
                continue;
            labels[applicationOid] = TruncateLabels(text);
        }

        if (verbose)
            Console.WriteLine($"INF App_Additional_WP_location WP-location fallback: {labels.Count} application(s)");

        return labels;
    }

    public static void ApplyWhenEmpty(
        IEnumerable<Dictionary<string, object?>> importRows,
        IReadOnlyDictionary<Guid, string> labelsByApplicationOid)
    {
        if (labelsByApplicationOid.Count == 0)
            return;

        foreach (var row in importRows)
        {
            if (!IsAdditionalWpLocationRow(row))
                continue;
            if (row.GetValueOrDefault("MovementPermitLocation") is string existing
                && !string.IsNullOrWhiteSpace(existing))
                continue;
            if (row.GetValueOrDefault("_legacyRowId") is not Guid oid)
                continue;
            if (!labelsByApplicationOid.TryGetValue(oid, out var labels)
                || string.IsNullOrWhiteSpace(labels))
                continue;
            row["MovementPermitLocation"] = labels;
        }
    }

    internal static bool IsAdditionalWpLocationType(string? applicationTypeName) =>
        string.Equals(
            applicationTypeName,
            AdditionalWpLocationApplicationTypeName,
            StringComparison.OrdinalIgnoreCase);

    internal static Guid? PickMajorityLocationOid(
        IReadOnlyList<(Guid LocationOid, int Count, Guid MinPiaOid)> groups)
    {
        if (groups.Count == 0)
            return null;

        return groups
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.MinPiaOid)
            .Select(g => g.LocationOid)
            .First();
    }

    internal static string TruncateLabels(string labels)
    {
        var trimmed = labels.Trim();
        if (trimmed.Length <= MovementPermitLocationMaxLength)
            return trimmed;

        var cut = trimmed[..MovementPermitLocationMaxLength];
        var lastComma = cut.LastIndexOf(',');
        if (lastComma > 0)
            return cut[..lastComma].TrimEnd();
        return cut;
    }

    private static bool IsAdditionalWpLocationRow(Dictionary<string, object?> row) =>
        IsAdditionalWpLocationType(row.GetValueOrDefault("ApplicationType") as string);
}