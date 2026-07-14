using Microsoft.Data.SqlClient;
using Visa2026.Module.DatabaseUpdate;
using ModuleResidenceType = Visa2026.Module.BusinessObjects.ResidenceType;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Prevents inserting a second active AddressOfResidence for the same person + site key
/// (matches repair SQL: Person + Type + City + FullAddress, plus site FK fallbacks for sync payloads).
/// </summary>
internal sealed class Visa2014AddressOfResidenceSiteDuplicateGuard
{
    private static readonly Guid EmptyGuid = Guid.Empty;

    private const string LoadSql = """
        SELECT CAST(PersonID AS varchar(36)) AS PersonId,
               Type,
               CAST(ISNULL(CityID, '00000000-0000-0000-0000-000000000000') AS varchar(36)) AS CityId,
               ISNULL(FullAddress, '') AS FullAddress,
               CAST(ISNULL(LodgingID, '00000000-0000-0000-0000-000000000000') AS varchar(36)) AS LodgingId,
               CAST(ISNULL(HotelID, '00000000-0000-0000-0000-000000000000') AS varchar(36)) AS HotelId,
               CAST(ISNULL(HospitalID, '00000000-0000-0000-0000-000000000000') AS varchar(36)) AS HospitalId,
               CAST(ISNULL(OtherSiteID, '00000000-0000-0000-0000-000000000000') AS varchar(36)) AS OtherSiteId,
               CAST(ID AS varchar(36)) AS AddressId
        FROM dbo.AddressesOfResidence
        WHERE (GCRecord IS NULL OR GCRecord = 0)
          AND PersonID IS NOT NULL
        """;

    private readonly Dictionary<SiteKey, Guid> _canonicalBySiteKey = new();

    public int LoadedRowCount { get; private set; }

    public static async Task<Visa2014AddressOfResidenceSiteDuplicateGuard> LoadFromSqlAsync(
        string targetConnectionString,
        bool verbose,
        CancellationToken cancellationToken = default)
    {
        var guard = new Visa2014AddressOfResidenceSiteDuplicateGuard();
        if (string.IsNullOrWhiteSpace(targetConnectionString))
            return guard;

        if (DatabaseProviderDetector.IsPostgreSql(targetConnectionString))
        {
            if (verbose)
                Console.WriteLine("WRN AddressOfResidence site duplicate guard skipped (PostgreSQL — SqlClient/T-SQL map).");
            return guard;
        }

        await using var connection = new SqlConnection(targetConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(LoadSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!Guid.TryParse(reader.GetString(0), out var personId))
                continue;
            if (!Guid.TryParse(reader.GetString(2), out var cityId))
                continue;
            if (!Guid.TryParse(reader.GetString(8), out var addressId))
                continue;

            var type = reader.GetInt32(1);
            var fullAddress = reader.GetString(3);
            guard.RegisterRow(
                personId,
                type,
                cityId,
                fullAddress,
                ParseOptionalGuid(reader.GetString(4)),
                ParseOptionalGuid(reader.GetString(5)),
                ParseOptionalGuid(reader.GetString(6)),
                ParseOptionalGuid(reader.GetString(7)),
                addressId);
        }

        if (verbose)
            Console.WriteLine($"INF AddressOfResidence duplicate guard: {guard.LoadedRowCount} active row(s)");

        return guard;
    }

    public Guid? TryResolveFromPayload(IReadOnlyDictionary<string, object?> payload)
    {
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Person", out var personId))
            return null;
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "City", out var cityId))
            return null;
        if (!TryParseResidenceType(payload.GetValueOrDefault("Type"), out var type))
            return null;

        if (Visa2014SyncPayloadFkHelper.TryGetPayloadString(payload, "FullAddress", out var fullAddress))
        {
            var byAddress = TryGetCanonical(personId, MapTypeToSql(type), cityId, fullAddress);
            if (byAddress.HasValue)
                return byAddress;
        }

        return type switch
        {
            ModuleResidenceType.Lodging when Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Lodging", out var lodgingId)
                => TryGetCanonicalBySiteFk(personId, type, cityId, SiteKind.Lodging, lodgingId),
            ModuleResidenceType.Hotel when Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Hotel", out var hotelId)
                => TryGetCanonicalBySiteFk(personId, type, cityId, SiteKind.Hotel, hotelId),
            ModuleResidenceType.Hospital when Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Hospital", out var hospitalId)
                => TryGetCanonicalBySiteFk(personId, type, cityId, SiteKind.Hospital, hospitalId),
            ModuleResidenceType.Other when Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "OtherSite", out var otherSiteId)
                => TryGetCanonicalBySiteFk(personId, type, cityId, SiteKind.OtherSite, otherSiteId),
            _ => null,
        };
    }

    public void RegisterFromPayload(IReadOnlyDictionary<string, object?> payload, Guid addressId)
    {
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Person", out var personId))
            return;
        if (!Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "City", out var cityId))
            return;
        if (!TryParseResidenceType(payload.GetValueOrDefault("Type"), out var type))
            return;

        payload.TryGetValue("FullAddress", out var fullAddressRaw);
        var fullAddress = fullAddressRaw as string ?? "";

        Guid? lodgingId = Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Lodging", out var l) ? l : null;
        Guid? hotelId = Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Hotel", out var h) ? h : null;
        Guid? hospitalId = Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "Hospital", out var hs) ? hs : null;
        Guid? otherSiteId = Visa2014SyncPayloadFkHelper.TryGetPayloadFkId(payload, "OtherSite", out var o) ? o : null;

        RegisterRow(personId, MapTypeToSql(type), cityId, fullAddress, lodgingId, hotelId, hospitalId, otherSiteId, addressId);
    }

    private void RegisterRow(
        Guid personId,
        int type,
        Guid cityId,
        string fullAddress,
        Guid? lodgingId,
        Guid? hotelId,
        Guid? hospitalId,
        Guid? otherSiteId,
        Guid addressId)
    {
        LoadedRowCount++;
        RegisterSiteKey(personId, type, cityId, fullAddress, addressId);

        if (lodgingId is { } li && li != EmptyGuid)
            RegisterSiteFkKey(personId, type, cityId, SiteKind.Lodging, li, addressId);
        if (hotelId is { } hi && hi != EmptyGuid)
            RegisterSiteFkKey(personId, type, cityId, SiteKind.Hotel, hi, addressId);
        if (hospitalId is { } hosi && hosi != EmptyGuid)
            RegisterSiteFkKey(personId, type, cityId, SiteKind.Hospital, hosi, addressId);
        if (otherSiteId is { } osi && osi != EmptyGuid)
            RegisterSiteFkKey(personId, type, cityId, SiteKind.OtherSite, osi, addressId);
    }

    private void RegisterSiteKey(Guid personId, int type, Guid cityId, string fullAddress, Guid addressId)
    {
        var key = new SiteKey(personId, type, cityId, fullAddress.Trim());
        if (!_canonicalBySiteKey.TryGetValue(key, out var existing) || addressId.CompareTo(existing) < 0)
            _canonicalBySiteKey[key] = addressId;
    }

    private void RegisterSiteFkKey(Guid personId, int type, Guid cityId, SiteKind siteKind, Guid siteId, Guid addressId)
    {
        var key = new SiteKey(personId, type, cityId, siteKind, siteId);
        if (!_canonicalBySiteKey.TryGetValue(key, out var existing) || addressId.CompareTo(existing) < 0)
            _canonicalBySiteKey[key] = addressId;
    }

    private Guid? TryGetCanonical(Guid personId, int type, Guid cityId, string fullAddress) =>
        _canonicalBySiteKey.TryGetValue(new SiteKey(personId, type, cityId, fullAddress.Trim()), out var id) ? id : null;

    private Guid? TryGetCanonicalBySiteFk(Guid personId, ModuleResidenceType type, Guid cityId, SiteKind siteKind, Guid siteId) =>
        _canonicalBySiteKey.TryGetValue(new SiteKey(personId, MapTypeToSql(type), cityId, siteKind, siteId), out var id) ? id : null;

    private static Guid? ParseOptionalGuid(string text) =>
        Guid.TryParse(text, out var parsed) && parsed != EmptyGuid ? parsed : null;

    private static bool TryParseResidenceType(object? raw, out ModuleResidenceType type)
    {
        type = default;
        if (raw is ModuleResidenceType enumValue)
        {
            type = enumValue;
            return true;
        }

        return raw is string text && Enum.TryParse(text, ignoreCase: true, out type);
    }

    private static int MapTypeToSql(ModuleResidenceType type) => type switch
    {
        ModuleResidenceType.Lodging => 0,
        ModuleResidenceType.Hotel => 1,
        ModuleResidenceType.PrivateHouse => 2,
        ModuleResidenceType.Hospital => 3,
        ModuleResidenceType.Other => 4,
        _ => -1,
    };

    private enum SiteKind
    {
        Lodging,
        Hotel,
        Hospital,
        OtherSite,
    }

    private readonly record struct SiteKey(
        Guid PersonId,
        int Type,
        Guid CityId,
        string FullAddress)
    {
        public SiteKey(Guid personId, int type, Guid cityId, SiteKind siteKind, Guid siteId)
            : this(personId, type, cityId, BuildSiteFkToken(siteKind, siteId))
        {
        }

        private static string BuildSiteFkToken(SiteKind siteKind, Guid siteId) =>
            $"{siteKind}:{siteId:D}";
    }
}
