using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class InvitationAddressTextTests
{
    [Fact]
    public void Format_joins_first_region_and_city_then_the_catalog_line()
    {
        var address = new InvitationAddress
        {
            Region = new Region { NameTm = "Ahal welaýaty" },
            City = new City { NameTm = "Ak bugdaý" },
            Type = ResidenceType.Hotel,
            Hotel = new Hotel { Name = "Ýyldyz myhmanhanasy" },
            AlternativeAddress = new AlternativeAddressesForInvitation
            {
                AddressLine = "ýa-da Balkan welaýatynyň, Türkmenbaşy şäheri, Şagadam köçesi 12",
            },
        };

        Assert.Equal(
            "Ahal welaýatynyň, Ak bugdaý, Ýyldyz myhmanhanasy, ýa-da Balkan welaýatynyň, Türkmenbaşy şäheri, Şagadam köçesi 12",
            InvitationAddressText.Format(address));
    }

    [Fact]
    public void Format_puts_other_places_after_a_private_house()
    {
        var address = new InvitationAddress
        {
            Region = new Region { NameTm = "Ahal welaýaty" },
            City = new City { NameTm = "Ak bugdaý" },
            Type = ResidenceType.PrivateHouse,
            PrivateHouseAddress = "Şagadam köçesi 4",
            AlternativeAddress = new AlternativeAddressesForInvitation
            {
                AddressLine = "ýa-da Balkan welaýatynyň, Türkmenbaşy şäheri",
            },
        };

        Assert.Equal(
            "Ahal welaýatynyň, Ak bugdaý, Şagadam köçesi 4, ýa-da Balkan welaýatynyň, Türkmenbaşy şäheri",
            InvitationAddressText.Format(address));
    }

    [Fact]
    public void Format_omits_the_catalog_line_when_there_are_no_further_places()
    {
        var address = new InvitationAddress
        {
            Region = new Region { NameTm = "Ahal welaýaty" },
            City = new City { NameTm = "Ak bugdaý" },
        };

        Assert.Equal("Ahal welaýatynyň, Ak bugdaý", InvitationAddressText.Format(address));
    }
}
