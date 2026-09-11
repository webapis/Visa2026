using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileTenantCatalogAddressPositionTests
{
    [Fact]
    public void Calik_catalog_requires_address_and_position_on_every_profile()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.True(row.RequirePersonAddressOfResidence, row.Code);
            Assert.True(row.RequirePersonPosition, row.Code);
        }
    }

    [Fact]
    public void Mapper_turns_on_address_and_position_for_all_families()
    {
        var issuance = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(issuance, new ApplicationType());
        Assert.True(issuance.RequirePersonAddressOfResidence);
        Assert.True(issuance.RequirePersonPosition);

        var trip = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(trip, new ApplicationType { ShowBusinessTrips = true });
        Assert.True(trip.RequirePersonAddressOfResidence);
        Assert.True(trip.RequirePersonPosition);
    }
}