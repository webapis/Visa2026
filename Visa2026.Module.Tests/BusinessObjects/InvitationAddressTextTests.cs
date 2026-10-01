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
            AlternativeAddress = new AlternativeAddressesForInvitation
            {
                AddressLine = "ýa-da Balkan welaýatynyň, Türkmenbaşy şäheri, Şagadam köçesi 12",
            },
        };

        Assert.Equal(
            "Ahal welaýatynyň, Ak bugdaý, ýa-da Balkan welaýatynyň, Türkmenbaşy şäheri, Şagadam köçesi 12",
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
