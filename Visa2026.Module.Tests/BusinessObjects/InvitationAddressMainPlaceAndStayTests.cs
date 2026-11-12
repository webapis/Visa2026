using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// MainPlace / StayAddress edges for invitation letter and application-form field 35.
/// Wrong type branch empties the place and drops catalog text from PDF/letter merge.
/// </summary>
public class InvitationAddressMainPlaceAndStayTests
{
    [Fact]
    public void MainPlace_reads_lodging_hotel_hospital_other_and_private_house()
    {
        Assert.Equal(
            "Sarahs UYJ",
            InvitationAddressText.MainPlace(new InvitationAddress
            {
                Type = ResidenceType.Lodging,
                Lodging = new Lodging { FullAddress = "Sarahs UYJ" },
            }));
        Assert.Equal(
            "Ýyldyz",
            InvitationAddressText.MainPlace(new InvitationAddress
            {
                Type = ResidenceType.Hotel,
                Hotel = new Hotel { Name = " Ýyldyz " },
            }));
        Assert.Equal(
            "Şypahana",
            InvitationAddressText.MainPlace(new InvitationAddress
            {
                Type = ResidenceType.Hospital,
                Hospital = new Hospital { Name = "Şypahana" },
            }));
        Assert.Equal(
            "Başga ýer 3",
            InvitationAddressText.MainPlace(new InvitationAddress
            {
                Type = ResidenceType.Other,
                OtherSite = new OtherSite { FullAddress = "Başga ýer 3" },
            }));
        Assert.Equal(
            "Köçe 4",
            InvitationAddressText.MainPlace(new InvitationAddress
            {
                Type = ResidenceType.PrivateHouse,
                PrivateHouseAddress = " Köçe 4 ",
            }));
    }

    [Fact]
    public void MainPlace_empty_when_type_unset_or_site_missing()
    {
        Assert.Equal(string.Empty, InvitationAddressText.MainPlace(new InvitationAddress()));
        Assert.Equal(
            string.Empty,
            InvitationAddressText.MainPlace(new InvitationAddress
            {
                Type = ResidenceType.Lodging,
                Lodging = null,
            }));
        Assert.Equal(
            string.Empty,
            InvitationAddressText.MainPlace(new InvitationAddress
            {
                Type = ResidenceType.Hotel,
                Hotel = new Hotel { Name = "   " },
            }));
    }

    [Fact]
    public void StayAddress_null_is_empty() =>
        Assert.Equal(string.Empty, InvitationAddressText.StayAddress(null));

    [Fact]
    public void StayAddress_falls_back_to_other_places_when_main_place_missing()
    {
        var address = new InvitationAddress
        {
            Type = ResidenceType.Hotel,
            AlternativeAddress = new AlternativeAddressesForInvitation
            {
                AddressLine = " ýa-da Balkan ",
            },
        };

        Assert.Equal("ýa-da Balkan", InvitationAddressText.StayAddress(address));
    }

    [Fact]
    public void StayAddress_omits_other_places_when_only_main_place_is_set()
    {
        var address = new InvitationAddress
        {
            Type = ResidenceType.Lodging,
            Lodging = new Lodging { FullAddress = "Sarahs UYJ" },
        };

        Assert.Equal("Sarahs UYJ", InvitationAddressText.StayAddress(address));
    }
}
