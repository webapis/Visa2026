#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class ApplicationProfileInstanceBusinessTripPlaceholderTests
{
    [Fact]
    public void Letter_tokens_use_from_and_to_region_city()
    {
        var fromRegion = new Region { NameTm = "Mary welaýaty" };
        var fromCity = new City { NameTm = "Mary etraby", Region = fromRegion };
        var toRegion = new Region { NameTm = "Ahal welaýaty" };
        var toCity = new City { NameTm = "Akbugdaý etraby", Region = toRegion };
        var application = new ApplicationProfileInstance
        {
            FromRegion = fromRegion,
            FromCity = fromCity,
            ToRegion = toRegion,
            ToCity = toCity,
            BusinessTripStartDate = new DateTime(2026, 2, 12),
            BusinessTripEndDate = new DateTime(2026, 2, 13),
            Purpose = "Aşgabat şäherinde ýerleşýän Türkiye respublikasynyň ilçihanasyna resmi işleri ýerine ýetirmek.",
        };

        Assert.Equal("12.02.2026", application.BusinessTripStartDateText);
        Assert.Equal("13.02.2026", application.BusinessTripEndDateText);
        Assert.Equal(2, application.BusinessTripDurationDays);
        Assert.Equal("iki", application.BusinessTripDurationDaysText);
        Assert.Equal("Mary welaýatynyň", application.FromRegionName_Genitive);
        Assert.Equal("Mary etrabyndan", application.FromCityName_Ablative);
        Assert.Equal("Ahal welaýatynyň", application.ToRegionName_Genitive);
        Assert.Equal("Akbugdaý etrabyna", application.ToCityName_Dative);

        var data = UserReportMergeDataHelper.BuildApplicationHeaderDictionary(application);
        Assert.Equal("12.02.2026", data["BTSD"]);
        Assert.Equal("13.02.2026", data["BTED"]);
        Assert.Equal(2, data["BTDCNT"]);
        Assert.Equal("iki", data["BTDCTX"]);
        Assert.Equal("Mary welaýatynyň", data["BTFRG"]);
        Assert.Equal("Mary etrabyndan", data["BTFCT"]);
        Assert.Equal("Ahal welaýatynyň", data["BTTRG"]);
        Assert.Equal("Akbugdaý etrabyna", data["BTTCT"]);
        Assert.Equal(application.Purpose, data["BTPRP"]);
    }

    [Fact]
    public void From_region_genitive_uses_city_region_name_when_from_region_nav_missing()
    {
        var fromCity = new City { NameTm = "Mary etraby", RegionName = "Mary welaýaty" };
        var application = new ApplicationProfileInstance
        {
            FromCity = fromCity,
        };

        Assert.Equal("Mary welaýatynyň", application.FromRegionName_Genitive);

        var data = UserReportMergeDataHelper.BuildApplicationHeaderDictionary(application);
        Assert.Equal("Mary welaýatynyň", data["BTFRG"]);
    }

    [Fact]
    public void Destination_tokens_prefer_to_region_city_over_obsolete_region_city()
    {
#pragma warning disable CS0618
        var application = new ApplicationProfileInstance
        {
            ToRegion = new Region { NameTm = "Ahal welaýaty" },
            ToCity = new City { NameTm = "Akbugdaý etraby" },
            Region = new Region { NameTm = "Lebap welaýaty" },
            City = new City { NameTm = "Türkmenabat şäheri" },
        };
#pragma warning restore CS0618

        Assert.Equal("Ahal welaýatynyň", application.ToRegionName_Genitive);
        Assert.Equal("Akbugdaý etrabyna", application.ToCityName_Dative);
    }

    [Fact]
    public void Destination_tokens_fall_back_to_obsolete_region_city_when_to_null()
    {
#pragma warning disable CS0618
        var application = new ApplicationProfileInstance
        {
            Region = new Region { NameTm = "Ahal welaýaty" },
            City = new City { NameTm = "Akbugdaý etraby" },
        };
#pragma warning restore CS0618

        Assert.Equal("Ahal welaýatynyň", application.ToRegionName_Genitive);
        Assert.Equal("Akbugdaý etrabyna", application.ToCityName_Dative);
    }

    [Fact]
    public void ShowFromTo_geo_is_true_for_business_trip_when_require_flags_set()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.BusinessTrip,
            RequireFromRegion = true,
            RequireFromCity = true,
            RequireToRegion = true,
            RequireToCity = true,
        };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.True(ApplicationProfileConfigurationResolver.UsesFromToRegionCity(application));
        Assert.True(ApplicationProfileConfigurationResolver.ShowFromRegion(application));
        Assert.True(ApplicationProfileConfigurationResolver.ShowFromCity(application));
        Assert.True(ApplicationProfileConfigurationResolver.ShowToRegion(application));
        Assert.True(ApplicationProfileConfigurationResolver.ShowToCity(application));
        Assert.False(ApplicationProfileConfigurationResolver.ShowRegion(application));
        Assert.False(ApplicationProfileConfigurationResolver.ShowCity(application));
    }

    [Fact]
    public void UsesFromToRegionCity_true_for_internal_check_in_out_codes()
    {
        Assert.True(ApplicationProfileConfigurationResolver.UsesFromToRegionCity(
            new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Registration, Code = "check_in_internal" }));
        Assert.True(ApplicationProfileConfigurationResolver.UsesFromToRegionCity(
            new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Registration, Code = "check_out_internal" }));
        Assert.False(ApplicationProfileConfigurationResolver.UsesFromToRegionCity(
            new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Registration, Code = "check_in_abroad" }));
    }

    [Fact]
    public void Sanaw_row_includes_destination_address_and_visa_number_type()
    {
        var application = new ApplicationProfileInstance
        {
            BusinessTripPrivateHouseAddress = "Ahal wel, Akbugdaý etr, Çalyk Enerji UYJ.",
            BusinessTripAddressType = ResidenceType.PrivateHouse,
        };
        var line = new ApplicationRosterMergeLine { ApplicationProfileInstance = application };
        var row = UserReportMergeDataHelper.BuildExcelItemListRowDictionary(line, 1);

        Assert.Equal("Ahal wel, Akbugdaý etr, Çalyk Enerji UYJ.", row["BTAD"]);
        Assert.True(row.ContainsKey("VNAT"));
    }

    [Fact]
    public void BTAD_joins_to_region_city_with_lodging_street()
    {
        var toRegion = new Region { NameTm = "Balkan welaýaty" };
        var toCity = new City { NameTm = "Türkmenbaşı etraby", Region = toRegion };
        var lodging = new Lodging
        {
            FullAddress = "T-başy-Garabogaz awtomobil ýol-v 6-7-nji km.günbatar tarapynda ýerleşýän Çalyk Enerji UYJ",
        };
        var application = new ApplicationProfileInstance
        {
            ToRegion = toRegion,
            ToCity = toCity,
            BusinessTripAddressType = ResidenceType.Lodging,
            BusinessTripLodging = lodging,
        };

        var expected = "Balkan welaýatynyň, Türkmenbaşı etraby, T-başy-Garabogaz awtomobil ýol-v 6-7-nji km.günbatar tarapynda ýerleşýän Çalyk Enerji UYJ";
        Assert.Equal(expected, BusinessTripDestinationHelper.FormatFullAddress(application));

        var line = new ApplicationRosterMergeLine { ApplicationProfileInstance = application };
        var row = UserReportMergeDataHelper.BuildExcelItemListRowDictionary(line, 1);
        Assert.Equal(expected, row["BTAD"]);
    }
}
