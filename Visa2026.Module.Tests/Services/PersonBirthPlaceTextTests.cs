#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PersonBirthPlaceTextTests
{
    [Fact]
    public void City_only_keeps_kahramanmaras()
    {
        var person = PersonWith("Kahramanmaras", "Turkiye");
        Assert.Equal("Kahramanmaras", PersonBirthPlaceText.CityOnly(person));
        Assert.Equal("Kahramanmaras", new ApplicationRosterMergeLine { Person = person }.Person_BirthPlace);
    }

    [Fact]
    public void Combined_country_slash_city_returns_city()
    {
        var person = PersonWith("Turkiye/Gaziantep", "Turkiye");
        Assert.Equal("Gaziantep", PersonBirthPlaceText.CityOnly(person));
    }

    [Fact]
    public void Slash_city_does_not_need_country_lookup()
    {
        var person = new Person { BirthPlace = "Turkiye/Gaziantep" };
        Assert.Equal("Gaziantep", PersonBirthPlaceText.CityOnly(person));
    }

    [Fact]
    public void Comma_country_city_returns_city()
    {
        var person = PersonWith("Turkiye, Kahramanmaras", "Turkiye");
        Assert.Equal("Kahramanmaras", PersonBirthPlaceText.CityOnly(person));
    }

    [Fact]
    public void Diacritic_country_prefix_still_returns_city()
    {
        var person = new Person
        {
            BirthPlace = "T\u00FCrkiye/Gaziantep",
            CountryOfBirth = new Country
            {
                NameTm = "T\u00FCrki\u00FDe",
                Name = "Turkey",
                Code = "TUR",
            },
        };

        Assert.Equal("Gaziantep", PersonBirthPlaceText.CityOnly(person));
    }

    [Fact]
    public void Birth_place_that_is_only_the_country_is_empty()
    {
        var person = PersonWith("Turkiye", "Turkiye");
        Assert.Equal(string.Empty, PersonBirthPlaceText.CityOnly(person));
        Assert.Equal(string.Empty, PersonBirthPlaceText.CityOnly(new Person { BirthPlace = "Turkey" }));
    }

    [Fact]
    public void Empty_birth_place_does_not_fall_back_to_country()
    {
        var person = PersonWith(null, "Turkiye");
        Assert.Equal(string.Empty, PersonBirthPlaceText.CityOnly(person));
        Assert.Equal("Turkiye", new ApplicationRosterMergeLine { Person = person }.Person_CountryOfBirthTm);
    }

    [Fact]
    public void Cancel_visa_sanaw_row_prints_city_on_PBPL()
    {
        var person = PersonWith("Turkiye/Kahramanmaras", "Turkiye");
        var row = UserReportMergeDataHelper.BuildWizaYatyrylmakSanawRowDictionary(
            new ApplicationRosterMergeLine { Person = person },
            1);

        Assert.Equal("Kahramanmaras", Assert.IsType<string>(row["Person_BirthPlace"]));
        Assert.Equal("Kahramanmaras", Assert.IsType<string>(row["PBPL"]));
        Assert.Equal("Turkiye", Assert.IsType<string>(row["Person_CountryOfBirthTm"]));
        Assert.Equal("Turkiye", Assert.IsType<string>(row["PCBT"]));
    }

    private static Person PersonWith(string? birthPlace, string countryTm) =>
        new()
        {
            BirthPlace = birthPlace,
            CountryOfBirth = new Country { NameTm = countryTm, Name = countryTm, Code = "TUR" },
        };
}