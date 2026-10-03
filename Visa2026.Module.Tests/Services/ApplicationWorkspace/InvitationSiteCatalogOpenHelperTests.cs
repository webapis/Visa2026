using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services.ApplicationWorkspace;

/// <summary>
/// Invitation site create: wrong field key / city assign must not open the wrong BO or leave City unset.
/// </summary>
public class InvitationSiteCatalogOpenHelperTests
{
    [Theory]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationLodging, typeof(Lodging))]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationHotel, typeof(Hotel))]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationHospital, typeof(Hospital))]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationOtherSite, typeof(OtherSite))]
    public void ResolveType_maps_invitation_site_field_keys(string fieldKey, Type expected) =>
        Assert.Equal(expected, InvitationSiteCatalogOpenHelper.ResolveType(fieldKey));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("InvitationCity")]
    [InlineData("InvitationPrivateHouse")]
    [InlineData("InvitationAlternativeAddress")]
    [InlineData("BusinessTripLodging")]
    public void ResolveType_rejects_non_site_keys(string? fieldKey) =>
        Assert.Null(InvitationSiteCatalogOpenHelper.ResolveType(fieldKey));

    [Fact]
    public void TryOpenNew_returns_false_for_null_application_or_empty_city()
    {
        Assert.False(InvitationSiteCatalogOpenHelper.TryOpenNew(
            null!,
            ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationLodging,
            Guid.NewGuid()));
        Assert.False(InvitationSiteCatalogOpenHelper.TryOpenNew(
            null!,
            ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationLodging,
            Guid.Empty));
    }

    [Fact]
    public void InvitationSiteCity_Assign_sets_city_on_each_site_type()
    {
        var city = new City { NameTm = "Ak bugdaý" };

        var lodging = new Lodging();
        Assert.True(InvitationSiteCity.Assign(lodging, city));
        Assert.Same(city, lodging.City);

        var hotel = new Hotel();
        Assert.True(InvitationSiteCity.Assign(hotel, city));
        Assert.Same(city, hotel.City);

        var hospital = new Hospital();
        Assert.True(InvitationSiteCity.Assign(hospital, city));
        Assert.Same(city, hospital.City);

        var other = new OtherSite();
        Assert.True(InvitationSiteCity.Assign(other, city));
        Assert.Same(city, other.City);
    }

    [Fact]
    public void InvitationSiteCity_Assign_rejects_unknown_targets()
    {
        Assert.False(InvitationSiteCity.Assign(null, new City()));
        Assert.False(InvitationSiteCity.Assign(new Region(), new City()));
        Assert.False(InvitationSiteCity.Assign(new Person(), new City()));
    }

    [Theory]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationLodging, true)]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationHotel, true)]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationHospital, true)]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationOtherSite, true)]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationPrivateHouse, false)]
    [InlineData(ApplicationWorkspaceCaseHeaderFieldsHelper.InvitationCity, false)]
    [InlineData(null, false)]
    public void IsInvitationSiteCatalogField_matches_createable_sites(string? key, bool expected) =>
        Assert.Equal(expected, ApplicationWorkspaceCaseHeaderFieldsHelper.IsInvitationSiteCatalogField(key));
}
