using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileTenantCatalogTravelHistoryTests
{
    [Fact]
    public void Calik_catalog_requires_travel_history_only_on_registration()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.True(
                Enum.TryParse(row.ActionFamily, ignoreCase: true, out ApplicationProfileActionFamily family),
                row.Code);
            if (family == ApplicationProfileActionFamily.Registration)
                Assert.True(row.RequirePersonTravelHistory, row.Code);
            else
                Assert.False(row.RequirePersonTravelHistory, row.Code);
        }

        Assert.Contains(rows, r => r.Code == "check_in_from_abroad" && r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "extend_visa_wp" && !r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "get_invitation" && !r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "cancel_visa" && !r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "cancel_visa_ext" && !r.RequirePersonTravelHistory);
    }

    [Fact]
    public void Mapper_turns_on_travel_history_only_for_registration()
    {
        var issuance = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(issuance, new ApplicationType());
        Assert.Equal(ApplicationProfileActionFamily.Issuance, issuance.ActionFamily);
        Assert.False(issuance.RequirePersonTravelHistory);

        var registration = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(
            registration,
            new ApplicationType { ShowRegistrations = true, Name = "App_Reg_Check_In" });
        Assert.Equal(ApplicationProfileActionFamily.Registration, registration.ActionFamily);
        Assert.True(registration.RequirePersonTravelHistory);

        var trip = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(trip, new ApplicationType { ShowBusinessTrips = true });
        Assert.Equal(ApplicationProfileActionFamily.BusinessTrip, trip.ActionFamily);
        Assert.False(trip.RequirePersonTravelHistory);
    }
}