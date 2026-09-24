using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Site completeness, clear-other-sites, profile defaults, and profile-default FormatFullAddress
/// for Application Profile business-trip destinations (BTAD / form gates).
/// </summary>
public sealed class BusinessTripDestinationHelperTests
{
    [Fact]
    public void IsComplete_requires_site_for_typed_destination()
    {
        Assert.False(BusinessTripDestinationHelper.IsComplete(null));

        var lodgingOnly = new ApplicationProfileInstance
        {
            BusinessTripAddressType = ResidenceType.Lodging,
        };
        Assert.False(BusinessTripDestinationHelper.IsComplete(lodgingOnly));

        lodgingOnly.BusinessTripLodging = new Lodging { FullAddress = "jaý-1" };
        Assert.True(BusinessTripDestinationHelper.IsComplete(lodgingOnly));

        var privateHouse = new ApplicationProfileInstance
        {
            BusinessTripAddressType = ResidenceType.PrivateHouse,
            BusinessTripPrivateHouseAddress = "  ",
        };
        Assert.False(BusinessTripDestinationHelper.IsComplete(privateHouse));
        privateHouse.BusinessTripPrivateHouseAddress = "Ahal, etrap";
        Assert.True(BusinessTripDestinationHelper.IsComplete(privateHouse));
    }

    [Fact]
    public void ClearSitesExcept_keeps_only_selected_type()
    {
        var instance = new ApplicationProfileInstance
        {
            BusinessTripLodging = new Lodging { FullAddress = "L" },
            BusinessTripHotel = new Hotel { Name = "H" },
            BusinessTripHospital = new Hospital { Name = "Hp" },
            BusinessTripOtherSite = new OtherSite { FullAddress = "O" },
            BusinessTripPrivateHouseAddress = "PH",
        };

        BusinessTripDestinationHelper.ClearSitesExcept(instance, ResidenceType.Hotel);

        Assert.Null(instance.BusinessTripLodging);
        Assert.NotNull(instance.BusinessTripHotel);
        Assert.Null(instance.BusinessTripHospital);
        Assert.Null(instance.BusinessTripOtherSite);
        Assert.Null(instance.BusinessTripPrivateHouseAddress);
    }

    [Fact]
    public void FormatFullAddress_site_overload_uses_hotel_name_and_legacy_catalog_fallback()
    {
        Assert.Equal(
            "Grand Hotel",
            BusinessTripDestinationHelper.FormatFullAddress(
                ResidenceType.Hotel,
                lodging: null,
                hotel: new Hotel { Name = "Grand Hotel" },
                hospital: null,
                otherSite: null,
                privateHouseAddress: null));

#pragma warning disable CS0618
        Assert.Equal(
            "legacy catalog line",
            BusinessTripDestinationHelper.FormatFullAddress(
                type: null,
                lodging: null,
                hotel: null,
                hospital: null,
                otherSite: null,
                privateHouseAddress: null,
                legacyCatalogAddress: new BusinessTripAddress { FullAddress = "  legacy catalog line  " }));
#pragma warning restore CS0618
    }

    [Fact]
    public void ApplyDefaultsFromProfile_copies_typed_defaults_and_clears_other_sites()
    {
        var lodging = new Lodging { FullAddress = "profile lodging" };
        var profile = new ApplicationProfile
        {
            DefaultBusinessTripAddressType = ResidenceType.Lodging,
            DefaultBusinessTripLodging = lodging,
            DefaultBusinessTripPrivateHouseAddress = "  house from profile  ",
        };
        var instance = new ApplicationProfileInstance
        {
            BusinessTripHotel = new Hotel { Name = "existing hotel" },
            BusinessTripHospital = new Hospital { Name = "existing hospital" },
            BusinessTripPrivateHouseAddress = "existing house",
#pragma warning disable CS0618
            BusinessTripAddress = new BusinessTripAddress { FullAddress = "legacy" },
#pragma warning restore CS0618
        };

        BusinessTripDestinationHelper.ApplyDefaultsFromProfile(instance, profile);

        Assert.Equal(ResidenceType.Lodging, instance.BusinessTripAddressType);
        Assert.Same(lodging, instance.BusinessTripLodging);
        // ClearSitesExcept keeps Lodging; other sites are cleared then overwritten from profile defaults.
        Assert.Null(instance.BusinessTripHotel);
        Assert.Null(instance.BusinessTripHospital);
        Assert.Equal("house from profile", instance.BusinessTripPrivateHouseAddress);
#pragma warning disable CS0618
        Assert.Null(instance.BusinessTripAddress);
#pragma warning restore CS0618
    }
}
