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
}
