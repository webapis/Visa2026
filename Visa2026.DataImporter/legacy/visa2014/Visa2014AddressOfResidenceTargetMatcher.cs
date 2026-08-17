using Microsoft.Data.SqlClient;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014AddressOfResidenceTargetMatcher
{
    internal static async Task<Guid?> TryMatchTargetIdAsync(
        SqlConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow)
    {
        var typeText = importRow.GetValueOrDefault("Type") as string;
        if (string.IsNullOrWhiteSpace(typeText))
            return null;

        return typeText switch
        {
            "PrivateHouse" => await MatchPrivateHouseAsync(conn, personId, importRow),
            "Lodging" => await MatchLodgingAsync(conn, personId, importRow),
            "Hotel" => await MatchHotelAsync(conn, personId, importRow),
            "Hospital" => await MatchHospitalAsync(conn, personId, importRow),
            "Other" => await MatchOtherSiteAsync(conn, personId, importRow),
            _ => null,
        };
    }

    internal static int MapResidenceTypeToSqlValue(string typeText) => typeText switch
    {
        "Lodging" => 0,
        "Hotel" => 1,
        "PrivateHouse" => 2,
        "Hospital" => 3,
        "Other" => 4,
        _ => -1,
    };

    private static async Task<Guid?> MatchPrivateHouseAsync(
        SqlConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow)
    {
        var fullAddress = importRow.GetValueOrDefault("FullAddress") as string;
        if (string.IsNullOrWhiteSpace(fullAddress))
            return null;

        var expirationText = importRow.GetValueOrDefault("ExpirationDate") as string;
        DateTime? expiration = DateTime.TryParse(expirationText, out var exp) ? exp.Date : null;

        return await ScalarGuidAsync(conn,
            """
            SELECT TOP 1 CAST(ID AS varchar(36))
            FROM AddressesOfResidence
            WHERE (GCRecord IS NULL OR GCRecord = 0)
              AND PersonID = @personId
              AND Type = @type
              AND FullAddress = @fullAddress
              AND (
                    (@expiration IS NULL AND ExpirationDate IS NULL)
                 OR (ExpirationDate IS NOT NULL AND CAST(ExpirationDate AS date) = @expiration)
              )
            ORDER BY ID
            """,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("PrivateHouse")),
            ("@fullAddress", fullAddress.Trim()),
            ("@expiration", expiration.HasValue ? expiration.Value : DBNull.Value));
    }

    private static async Task<Guid?> MatchLodgingAsync(
        SqlConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow)
    {
        var lodgingName = importRow.GetValueOrDefault("Lodging") as string;
        if (string.IsNullOrWhiteSpace(lodgingName))
            return null;

        return await ScalarGuidAsync(conn,
            """
            SELECT TOP 1 CAST(aor.ID AS varchar(36))
            FROM AddressesOfResidence aor
            INNER JOIN Lodgings l ON l.ID = aor.LodgingID
            WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0)
              AND aor.PersonID = @personId
              AND aor.Type = @type
              AND l.FullAddress = @lodgingName
            ORDER BY aor.ID
            """,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("Lodging")),
            ("@lodgingName", lodgingName.Trim()));
    }

    private static async Task<Guid?> MatchHotelAsync(
        SqlConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow)
    {
        var hotelName = importRow.GetValueOrDefault("Hotel") as string;
        if (string.IsNullOrWhiteSpace(hotelName))
            return null;

        return await ScalarGuidAsync(conn,
            """
            SELECT TOP 1 CAST(aor.ID AS varchar(36))
            FROM AddressesOfResidence aor
            INNER JOIN Hotels h ON h.ID = aor.HotelID
            WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0)
              AND aor.PersonID = @personId
              AND aor.Type = @type
              AND h.Name = @hotelName
            ORDER BY aor.ID
            """,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("Hotel")),
            ("@hotelName", hotelName.Trim()));
    }

    private static async Task<Guid?> MatchHospitalAsync(
        SqlConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow)
    {
        var hospitalName = importRow.GetValueOrDefault("Hospital") as string;
        if (string.IsNullOrWhiteSpace(hospitalName))
            return null;

        return await ScalarGuidAsync(conn,
            """
            SELECT TOP 1 CAST(aor.ID AS varchar(36))
            FROM AddressesOfResidence aor
            INNER JOIN Hospitals h ON h.ID = aor.HospitalID
            WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0)
              AND aor.PersonID = @personId
              AND aor.Type = @type
              AND h.Name = @hospitalName
            ORDER BY aor.ID
            """,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("Hospital")),
            ("@hospitalName", hospitalName.Trim()));
    }

    private static async Task<Guid?> MatchOtherSiteAsync(
        SqlConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow)
    {
        var otherSiteName = importRow.GetValueOrDefault("OtherSite") as string;
        if (string.IsNullOrWhiteSpace(otherSiteName))
            return null;

        return await ScalarGuidAsync(conn,
            """
            SELECT TOP 1 CAST(aor.ID AS varchar(36))
            FROM AddressesOfResidence aor
            INNER JOIN OtherSites o ON o.ID = aor.OtherSiteID
            WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0)
              AND aor.PersonID = @personId
              AND aor.Type = @type
              AND o.FullAddress = @otherSiteName
            ORDER BY aor.ID
            """,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("Other")),
            ("@otherSiteName", otherSiteName.Trim()));
    }

    private static async Task<Guid?> ScalarGuidAsync(
        SqlConnection conn,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);

        var result = await cmd.ExecuteScalarAsync();
        if (result is null or DBNull)
            return null;

        return Guid.TryParse(result.ToString(), out var parsed) ? parsed : null;
    }
}
