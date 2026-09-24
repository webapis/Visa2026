using Xunit;
using ModuleResidenceType = Visa2026.Module.BusinessObjects.ResidenceType;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014AddressOfResidenceSiteDuplicateGuardTests
{
    [Fact]
    public void TryResolveFromPayload_finds_canonical_row_by_lodging_site_fk()
    {
        var personId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var lodgingId = Guid.NewGuid();
        var keepId = Guid.NewGuid();

        var guard = new Visa2014AddressOfResidenceSiteDuplicateGuard();
        guard.RegisterFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.Lodging.ToString(),
            ["Lodging"] = new { ID = lodgingId },
        }, keepId);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.Lodging.ToString(),
            ["Lodging"] = new { ID = lodgingId },
        };

        Assert.Equal(keepId, guard.TryResolveFromPayload(payload));
    }

    [Fact]
    public void TryResolveFromPayload_finds_canonical_row_by_full_address()
    {
        var personId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var keepId = Guid.NewGuid();
        const string fullAddress = "1932 (A.Garlyyew) koc. 70/13 UYJ";

        var guard = new Visa2014AddressOfResidenceSiteDuplicateGuard();
        guard.RegisterFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.PrivateHouse.ToString(),
            ["FullAddress"] = fullAddress,
        }, keepId);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.PrivateHouse.ToString(),
            ["FullAddress"] = fullAddress,
        };

        Assert.Equal(keepId, guard.TryResolveFromPayload(payload));
    }

    [Fact]
    public void TryResolveFromPayload_returns_null_when_site_differs()
    {
        var personId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var keepId = Guid.NewGuid();

        var guard = new Visa2014AddressOfResidenceSiteDuplicateGuard();
        guard.RegisterFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.Lodging.ToString(),
            ["Lodging"] = new { ID = Guid.NewGuid() },
        }, keepId);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.Lodging.ToString(),
            ["Lodging"] = new { ID = Guid.NewGuid() },
        };

        Assert.Null(guard.TryResolveFromPayload(payload));
    }

    [Theory]
    [InlineData(nameof(ModuleResidenceType.Hotel), "Hotel")]
    [InlineData(nameof(ModuleResidenceType.Hospital), "Hospital")]
    [InlineData(nameof(ModuleResidenceType.Other), "OtherSite")]
    public void TryResolveFromPayload_finds_canonical_row_by_hotel_hospital_other_site_fk(
        string typeName,
        string sitePayloadKey)
    {
        var personId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var keepId = Guid.NewGuid();
        var type = Enum.Parse<ModuleResidenceType>(typeName);

        var guard = new Visa2014AddressOfResidenceSiteDuplicateGuard();
        guard.RegisterFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = type.ToString(),
            [sitePayloadKey] = new { ID = siteId },
        }, keepId);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = type.ToString(),
            [sitePayloadKey] = new { ID = siteId },
        };

        Assert.Equal(keepId, guard.TryResolveFromPayload(payload));
    }

    [Fact]
    public void TryResolveFromPayload_accepts_enum_Type_and_trims_FullAddress()
    {
        var personId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var keepId = Guid.NewGuid();
        const string fullAddress = "1932 (A.Garlyyew) koc. 70/13 UYJ";

        var guard = new Visa2014AddressOfResidenceSiteDuplicateGuard();
        guard.RegisterFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.PrivateHouse,
            ["FullAddress"] = $"  {fullAddress}  ",
        }, keepId);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.PrivateHouse,
            ["FullAddress"] = fullAddress,
        };

        Assert.Equal(keepId, guard.TryResolveFromPayload(payload));
    }

    [Fact]
    public void RegisterFromPayload_keeps_lowest_address_id_for_same_site_key()
    {
        var personId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var lodgingId = Guid.NewGuid();
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var guard = new Visa2014AddressOfResidenceSiteDuplicateGuard();
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.Lodging.ToString(),
            ["Lodging"] = new { ID = lodgingId },
        };

        guard.RegisterFromPayload(row, higherId);
        guard.RegisterFromPayload(row, lowerId);

        Assert.Equal(lowerId, guard.TryResolveFromPayload(row));
        Assert.Equal(2, guard.LoadedRowCount);
    }

    [Fact]
    public void TryResolveFromPayload_prefers_FullAddress_match_before_site_fk()
    {
        var personId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var lodgingId = Guid.NewGuid();
        var byAddressId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var bySiteId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        const string fullAddress = "same street line";

        var guard = new Visa2014AddressOfResidenceSiteDuplicateGuard();
        guard.RegisterFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.Lodging.ToString(),
            ["FullAddress"] = fullAddress,
        }, byAddressId);
        guard.RegisterFromPayload(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.Lodging.ToString(),
            ["Lodging"] = new { ID = lodgingId },
        }, bySiteId);

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = personId },
            ["City"] = new { ID = cityId },
            ["Type"] = ModuleResidenceType.Lodging.ToString(),
            ["FullAddress"] = fullAddress,
            ["Lodging"] = new { ID = lodgingId },
        };

        Assert.Equal(byAddressId, guard.TryResolveFromPayload(payload));
    }

    [Fact]
    public void TryResolveFromPayload_returns_null_when_Type_missing()
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Person"] = new { ID = Guid.NewGuid() },
            ["City"] = new { ID = Guid.NewGuid() },
            ["Lodging"] = new { ID = Guid.NewGuid() },
        };

        Assert.Null(new Visa2014AddressOfResidenceSiteDuplicateGuard().TryResolveFromPayload(payload));
    }
}
