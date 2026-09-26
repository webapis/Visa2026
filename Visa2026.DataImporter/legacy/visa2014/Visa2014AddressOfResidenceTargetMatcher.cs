using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014AddressOfResidenceTargetMatcher
{
    internal static async Task<Guid?> TryMatchTargetIdAsync(
        string connectionString,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            await using var conn = new NpgsqlConnection(
                DatabaseProviderDetector.StripEfCoreProvider(connectionString));
            await conn.OpenAsync(cancellationToken);
            return await TryMatchTargetIdAsync(conn, personId, importRow, postgres: true, cancellationToken);
        }

        await using var sqlConn = new SqlConnection(connectionString);
        await sqlConn.OpenAsync(cancellationToken);
        return await TryMatchTargetIdAsync(sqlConn, personId, importRow, postgres: false, cancellationToken);
    }

    /// <summary>SQL Server path used by id-map rebuild helpers.</summary>
    internal static Task<Guid?> TryMatchTargetIdAsync(
        SqlConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow) =>
        TryMatchTargetIdAsync(conn, personId, importRow, postgres: false, CancellationToken.None);

    private static async Task<Guid?> TryMatchTargetIdAsync(
        DbConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow,
        bool postgres,
        CancellationToken cancellationToken)
    {
        var typeText = importRow.GetValueOrDefault("Type") as string;
        if (string.IsNullOrWhiteSpace(typeText))
            return null;

        return typeText switch
        {
            "PrivateHouse" => await MatchPrivateHouseAsync(conn, personId, importRow, postgres, cancellationToken),
            "Lodging" => await MatchLodgingAsync(conn, personId, importRow, postgres, cancellationToken),
            "Hotel" => await MatchHotelAsync(conn, personId, importRow, postgres, cancellationToken),
            "Hospital" => await MatchHospitalAsync(conn, personId, importRow, postgres, cancellationToken),
            "Other" => await MatchOtherSiteAsync(conn, personId, importRow, postgres, cancellationToken),
            _ => null,
        };
    }

    private static int MapResidenceTypeToSqlValue(string typeText) => typeText switch
    {
        "Lodging" => 0,
        "Hotel" => 1,
        "PrivateHouse" => 2,
        "Hospital" => 3,
        "Other" => 4,
        _ => -1,
    };

    private static async Task<Guid?> MatchPrivateHouseAsync(
        DbConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow,
        bool postgres,
        CancellationToken cancellationToken)
    {
        var fullAddress = importRow.GetValueOrDefault("FullAddress") as string;
        if (string.IsNullOrWhiteSpace(fullAddress))
            return null;

        var expirationText = importRow.GetValueOrDefault("ExpirationDate") as string;
        DateTime? expiration = DateTime.TryParse(expirationText, out var exp) ? exp.Date : null;

        var sql = postgres
            ? """
              SELECT "ID"::text
              FROM "AddressesOfResidence"
              WHERE COALESCE("GCRecord", 0) = 0
                AND "PersonID" = @personId
                AND "Type" = @type
                AND "FullAddress" = @fullAddress
                AND (
                      (@expiration::date IS NULL AND "ExpirationDate" IS NULL)
                   OR ("ExpirationDate" IS NOT NULL AND CAST("ExpirationDate" AS date) = @expiration::date)
                )
              ORDER BY "ID"
              LIMIT 1
              """
            : """
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
              """;

        return await ScalarGuidAsync(conn, sql, cancellationToken,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("PrivateHouse")),
            ("@fullAddress", fullAddress.Trim()),
            ("@expiration", expiration.HasValue ? expiration.Value : DBNull.Value));
    }

    private static async Task<Guid?> MatchLodgingAsync(
        DbConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow,
        bool postgres,
        CancellationToken cancellationToken)
    {
        var lodgingName = importRow.GetValueOrDefault("Lodging") as string;
        if (string.IsNullOrWhiteSpace(lodgingName))
            return null;

        var sql = postgres
            ? """
              SELECT aor."ID"::text
              FROM "AddressesOfResidence" aor
              INNER JOIN "Lodgings" l ON l."ID" = aor."LodgingID"
              WHERE COALESCE(aor."GCRecord", 0) = 0
                AND aor."PersonID" = @personId
                AND aor."Type" = @type
                AND l."FullAddress" = @lodgingName
              ORDER BY aor."ID"
              LIMIT 1
              """
            : """
              SELECT TOP 1 CAST(aor.ID AS varchar(36))
              FROM AddressesOfResidence aor
              INNER JOIN Lodgings l ON l.ID = aor.LodgingID
              WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0)
                AND aor.PersonID = @personId
                AND aor.Type = @type
                AND l.FullAddress = @lodgingName
              ORDER BY aor.ID
              """;

        return await ScalarGuidAsync(conn, sql, cancellationToken,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("Lodging")),
            ("@lodgingName", lodgingName.Trim()));
    }

    private static async Task<Guid?> MatchHotelAsync(
        DbConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow,
        bool postgres,
        CancellationToken cancellationToken)
    {
        var hotelName = importRow.GetValueOrDefault("Hotel") as string;
        if (string.IsNullOrWhiteSpace(hotelName))
            return null;

        var sql = postgres
            ? """
              SELECT aor."ID"::text
              FROM "AddressesOfResidence" aor
              INNER JOIN "Hotels" h ON h."ID" = aor."HotelID"
              WHERE COALESCE(aor."GCRecord", 0) = 0
                AND aor."PersonID" = @personId
                AND aor."Type" = @type
                AND h."Name" = @hotelName
              ORDER BY aor."ID"
              LIMIT 1
              """
            : """
              SELECT TOP 1 CAST(aor.ID AS varchar(36))
              FROM AddressesOfResidence aor
              INNER JOIN Hotels h ON h.ID = aor.HotelID
              WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0)
                AND aor.PersonID = @personId
                AND aor.Type = @type
                AND h.Name = @hotelName
              ORDER BY aor.ID
              """;

        return await ScalarGuidAsync(conn, sql, cancellationToken,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("Hotel")),
            ("@hotelName", hotelName.Trim()));
    }

    private static async Task<Guid?> MatchHospitalAsync(
        DbConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow,
        bool postgres,
        CancellationToken cancellationToken)
    {
        var hospitalName = importRow.GetValueOrDefault("Hospital") as string;
        if (string.IsNullOrWhiteSpace(hospitalName))
            return null;

        var sql = postgres
            ? """
              SELECT aor."ID"::text
              FROM "AddressesOfResidence" aor
              INNER JOIN "Hospitals" h ON h."ID" = aor."HospitalID"
              WHERE COALESCE(aor."GCRecord", 0) = 0
                AND aor."PersonID" = @personId
                AND aor."Type" = @type
                AND h."Name" = @hospitalName
              ORDER BY aor."ID"
              LIMIT 1
              """
            : """
              SELECT TOP 1 CAST(aor.ID AS varchar(36))
              FROM AddressesOfResidence aor
              INNER JOIN Hospitals h ON h.ID = aor.HospitalID
              WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0)
                AND aor.PersonID = @personId
                AND aor.Type = @type
                AND h.Name = @hospitalName
              ORDER BY aor.ID
              """;

        return await ScalarGuidAsync(conn, sql, cancellationToken,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("Hospital")),
            ("@hospitalName", hospitalName.Trim()));
    }

    private static async Task<Guid?> MatchOtherSiteAsync(
        DbConnection conn,
        Guid personId,
        IReadOnlyDictionary<string, object?> importRow,
        bool postgres,
        CancellationToken cancellationToken)
    {
        var otherSiteName = importRow.GetValueOrDefault("OtherSite") as string;
        if (string.IsNullOrWhiteSpace(otherSiteName))
            return null;

        var sql = postgres
            ? """
              SELECT aor."ID"::text
              FROM "AddressesOfResidence" aor
              INNER JOIN "OtherSites" o ON o."ID" = aor."OtherSiteID"
              WHERE COALESCE(aor."GCRecord", 0) = 0
                AND aor."PersonID" = @personId
                AND aor."Type" = @type
                AND o."FullAddress" = @otherSiteName
              ORDER BY aor."ID"
              LIMIT 1
              """
            : """
              SELECT TOP 1 CAST(aor.ID AS varchar(36))
              FROM AddressesOfResidence aor
              INNER JOIN OtherSites o ON o.ID = aor.OtherSiteID
              WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0)
                AND aor.PersonID = @personId
                AND aor.Type = @type
                AND o.FullAddress = @otherSiteName
              ORDER BY aor.ID
              """;

        return await ScalarGuidAsync(conn, sql, cancellationToken,
            ("@personId", personId),
            ("@type", MapResidenceTypeToSqlValue("Other")),
            ("@otherSiteName", otherSiteName.Trim()));
    }

    private static async Task<Guid?> ScalarGuidAsync(
        DbConnection conn,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
            return null;

        return Guid.TryParse(result.ToString(), out var parsed) ? parsed : null;
    }
}