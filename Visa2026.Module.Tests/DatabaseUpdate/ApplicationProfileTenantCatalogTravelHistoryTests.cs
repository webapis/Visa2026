using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileTenantCatalogTravelHistoryTests
{
    [Fact]
    public void Calik_catalog_requires_travel_history_except_business_trip()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.True(
                Enum.TryParse(row.ActionFamily, ignoreCase: true, out ApplicationProfileActionFamily family),
                row.Code);
            if (family == ApplicationProfileActionFamily.BusinessTrip)
                Assert.False(row.RequirePersonTravelHistory, row.Code);
            else
                Assert.True(row.RequirePersonTravelHistory, row.Code);
        }

        Assert.Contains(rows, r => r.Code == "check_in_from_abroad" && r.RequirePersonTravelHistory);
    }

    [Fact]
    public void Mapper_turns_on_travel_history_except_business_trip()
    {
        var issuance = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(issuance, new ApplicationType());
        Assert.Equal(ApplicationProfileActionFamily.Issuance, issuance.ActionFamily);
        Assert.True(issuance.RequirePersonTravelHistory);

        var trip = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(trip, new ApplicationType { ShowBusinessTrips = true });
        Assert.Equal(ApplicationProfileActionFamily.BusinessTrip, trip.ActionFamily);
        Assert.False(trip.RequirePersonTravelHistory);
    }
}