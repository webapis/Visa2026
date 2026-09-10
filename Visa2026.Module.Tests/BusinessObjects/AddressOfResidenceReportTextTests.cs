using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class AddressOfResidenceReportTextTests
{
    [Fact]
    public void CityAndStreet_includes_welaýat_region_then_city_then_street()
    {
        var address = new AddressOfResidence
        {
            Type = ResidenceType.PrivateHouse,
            FullAddress = "Şatlyk Gurbandurdy yolunun 57 km. Yerleşyan Çalyk Enerji UYJ",
            Region = new Region { NameTm = "Balkan welaýaty" },
            City = new City { NameTm = "Turkmenbashy etraby" },
        };

        Assert.Equal(
            "Balkan welaýatynyň, Turkmenbashy etraby, Şatlyk Gurbandurdy yolunun 57 km. Yerleşyan Çalyk Enerji UYJ",
            AddressOfResidenceReportText.CityAndStreet(address));
    }

    [Fact]
    public void ToWelayatGenitive_turns_welaýaty_into_welaýatynyň()
    {
        Assert.Equal("Mary welaýatynyň", AddressOfResidenceReportText.ToWelayatGenitive("Mary welaýaty"));
        Assert.Equal("Ahal welaýatynyň", AddressOfResidenceReportText.ToWelayatGenitive("Ahal welaýatynyň"));
        Assert.Equal("Aşgabat şäheri", AddressOfResidenceReportText.ToWelayatGenitive("Aşgabat şäheri"));
    }

    [Fact]
    public void CityAndStreet_omits_Aşgabat_şäheri_region_when_same_as_city()
    {
        var address = new AddressOfResidence
        {
            Type = ResidenceType.PrivateHouse,
            FullAddress = "I.Gandyýew köçesi jaý-12",
            Region = new Region { NameTm = AddressOfResidenceReportText.AsgabatSaheri },
            City = new City { NameTm = "Aşgabat şäheri" },
        };

        Assert.Equal(
            "Aşgabat şäheri, I.Gandyýew köçesi jaý-12",
            AddressOfResidenceReportText.CityAndStreet(address));
    }

    [Fact]
    public void CityAndStreet_does_not_repeat_city_already_in_street()
    {
        var address = new AddressOfResidence
        {
            Type = ResidenceType.PrivateHouse,
            FullAddress = "Aşgabat şäheriniň 1958-nji (Andalyp) köçesi jaý-86",
            Region = new Region { NameTm = AddressOfResidenceReportText.AsgabatSaheri },
            City = new City { NameTm = "Aşgabat şäheriniň" },
        };

        Assert.Equal(
            "Aşgabat şäheriniň 1958-nji (Andalyp) köçesi jaý-86",
            AddressOfResidenceReportText.CityAndStreet(address));
    }

    [Fact]
    public void CityAndStreet_street_only_when_city_and_region_missing()
    {
        var address = new AddressOfResidence
        {
            Type = ResidenceType.PrivateHouse,
            FullAddress = "jaý-12",
        };

        Assert.Equal("jaý-12", AddressOfResidenceReportText.CityAndStreet(address));
    }
}