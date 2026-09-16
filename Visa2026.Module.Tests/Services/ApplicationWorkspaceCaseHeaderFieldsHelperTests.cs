using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationProfileWizard;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceCaseHeaderFieldsHelperTests
{
    [Fact]
    public void Build_IncludesOnlyProfileUseFields()
    {
        var profile = new ApplicationProfile
        {
            RequireVisaType = true,
            RequireVisaPeriod = true,
            RequireProject = true,
        };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.Equal(5, fields.Count);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaPeriod);
        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Project);
        Assert.DoesNotContain(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Urgency);
    }

    [Fact]
    public void Build_IncludesBusinessTripAddressWhenRequired()
    {
        var profile = new ApplicationProfile { RequireBusinessTripAddress = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.Contains(fields, field => field.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BusinessTripAddressType);
        Assert.Equal(
            ApplicationWorkspaceCaseHeaderFieldKind.Lookup,
            Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BusinessTripAddressType).Kind);
    }

    [Fact]
    public void Build_BusinessTripAddressTypeLodging_IsNotEmptyFillState()
    {
        var profile = new ApplicationProfile { RequireBusinessTripAddress = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            BusinessTripAddressType = ResidenceType.Lodging,
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
        var typeField = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BusinessTripAddressType);

        Assert.True(typeField.SelectedId is Guid id && id != Guid.Empty);
        Assert.NotEqual(ApplicationWorkspaceCaseSummaryFillState.Empty, typeField.FillState);
    }

    [Fact]
    public void Build_IncludesPurposeWhenRequired()
    {
        var profile = new ApplicationProfile { RequirePurpose = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            Purpose = "Business trip reason",
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        var field = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Purpose);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Text, field.Kind);
        Assert.Equal("Business trip reason", field.DisplayValue);
    }

    [Fact]
    public void Build_IncludesBorderZoneAsCommaSeparatedMultiSelectWhenRequired()
    {
        var profile = new ApplicationProfile { RequireBorderZone = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            BorderZoneLocation = "Zone A, Zone B",
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        var field = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BorderZone);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.CommaSeparatedMultiSelect, field.Kind);
        Assert.Equal("Zone A, Zone B", field.Value);
    }

    [Fact]
    public void Build_IncludesWorkPermitLocationAsCommaSeparatedMultiSelectWhenRequired()
    {
        var profile = new ApplicationProfile { RequireWorkPermitLocation = true };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            MovementPermitLocation = "Ashgabat, Mary",
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        var field = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.WorkPermitLocation);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.CommaSeparatedMultiSelect, field.Kind);
        Assert.Equal("Ashgabat, Mary", field.Value);
    }

    [Fact]
    public void Build_AlwaysIncludesApplicationNumberAndDate()
    {
        var profile = new ApplicationProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            FullApplicationNumber = "8/-007",
            ApplicationDate = new DateTime(2024, 8, 25),
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);

        Assert.Equal(2, fields.Count);
        var number = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.ShortText, number.Kind);
        Assert.Equal("8/-007", number.DisplayValue);
        var date = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Date, date.Kind);
        Assert.Equal("25.08.2024", date.DisplayValue);
    }

    [Fact]
    public void IsIdentityField_NumberDateAndUrgency()
    {
        Assert.Equal(
            new[]
            {
                ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber,
                ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate,
                ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber,
                ApplicationWorkspaceCaseHeaderFieldsHelper.Urgency,
            },
            ApplicationWorkspaceCaseHeaderFieldsHelper.IdentityFieldKeys);
        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.IsIdentityField(
            ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber));
        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.IsIdentityField(
            ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber));
        Assert.True(ApplicationWorkspaceCaseHeaderFieldsHelper.IsIdentityField(
            ApplicationWorkspaceCaseHeaderFieldsHelper.Urgency));
        Assert.False(ApplicationWorkspaceCaseHeaderFieldsHelper.IsIdentityField(
            ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType));
        Assert.False(ApplicationWorkspaceCaseHeaderFieldsHelper.IsIdentityField(null));
    }

    [Fact]
    public void Build_ShowsEmptyDisplayAsDash()
    {
        var profile = new ApplicationProfile { RequireVisaType = true };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        var field = Assert.Single(
            ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null),
            item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType);

        Assert.Equal("—", field.DisplayValue);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Lookup, field.Kind);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Empty, field.FillState);
    }

    [Fact]
    public void Build_FillState_EmptyDash_DefaultMatch_OfficerOverride()
    {
        var defaultId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var profile = new ApplicationProfile
        {
            RequireVisaType = true,
            DefaultVisaTypeId = defaultId,
        };

        var empty = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance { ApplicationProfile = profile },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Empty,
            Assert.Single(empty, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType).FillState);

        var matched = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                VisaType = new VisaType { ID = defaultId, NameTm = "WV" },
            },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Default,
            Assert.Single(matched, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType).FillState);

        var officer = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                VisaType = new VisaType { ID = otherId, NameTm = "FM" },
            },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Officer,
            Assert.Single(officer, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType).FillState);
    }

    [Fact]
    public void Build_FillState_ApplicationNumberAndDateUseManualEntry()
    {
        var profile = new ApplicationProfile();
        var auto = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                FullApplicationNumber = "8/-013",
                ApplicationDate = new DateTime(2026, 8, 27),
            },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Default,
            Assert.Single(auto, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber).FillState);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Default,
            Assert.Single(auto, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate).FillState);

        var manual = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                FullApplicationNumber = "8/-013",
                ApplicationDate = new DateTime(2026, 8, 27),
                IsManualEntry = true,
            },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Officer,
            Assert.Single(manual, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber).FillState);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Officer,
            Assert.Single(manual, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate).FillState);
    }

    [Fact]
    public void Build_FillState_MultiSelectIgnoresOrderAndNoneValue()
    {
        var profile = new ApplicationProfile
        {
            RequireBorderZone = true,
            DefaultBorderZoneLocation = "Zone B, Zone A",
        };

        var none = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                BorderZoneLocation = CommaSeparatedSelectionHelper.NoneValue,
            },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Empty,
            Assert.Single(none, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BorderZone).FillState);

        var matched = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                BorderZoneLocation = "Zone A, Zone B",
            },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Default,
            Assert.Single(matched, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BorderZone).FillState);
    }

    [Fact]
    public void Build_FillState_BorderZoneYokIsDefaultValue()
    {
        var profile = new ApplicationProfile
        {
            RequireBorderZone = true,
            DefaultBorderZoneLocation = BorderZoneSelectionHelper.NoneValue,
        };

        var yok = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                BorderZoneLocation = BorderZoneSelectionHelper.NoneValue,
            },
            profile,
            null);
        var yokField = Assert.Single(yok, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BorderZone);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Default, yokField.FillState);
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, yokField.DisplayValue);

        var empty = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                BorderZoneLocation = null,
            },
            profile,
            null);
        var emptyField = Assert.Single(empty, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.BorderZone);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Default, emptyField.FillState);
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, emptyField.DisplayValue);
    }

    [Fact]
    public void Build_FillState_StartDateWithoutProfileDefaultIsOfficerWhenSet()
    {
        var profile = new ApplicationProfile { RequireStartDate = true };
        var empty = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance { ApplicationProfile = profile },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Empty,
            Assert.Single(empty, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.StartDate).FillState);

        var filled = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                BusinessTripStartDate = new DateTime(2026, 9, 1),
            },
            profile,
            null);
        Assert.Equal(
            ApplicationWorkspaceCaseSummaryFillState.Officer,
            Assert.Single(filled, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.StartDate).FillState);
    }

    [Fact]
    public void TrySetInstanceNumber_ParsesFullNumberAndMarksManualEntry()
    {
        var application = new ApplicationProfileInstance();

        var ok = ApplicationWorkspaceCaseHeaderFieldsHelper.TrySetInstanceNumber(application, "8/-007", out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("8/-007", application.FullApplicationNumber);
        Assert.Equal("8", application.AppNumberPrefix);
        Assert.Equal("007", application.ApplicationNumber);
        Assert.True(application.IsManualEntry);
    }

    [Fact]
    public void TrySetInstanceDate_SetsDateYearAndMonth()
    {
        var application = new ApplicationProfileInstance();

        var ok = ApplicationWorkspaceCaseHeaderFieldsHelper.TrySetInstanceDate(application, "2024-08-25", out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(new DateTime(2024, 8, 25), application.ApplicationDate);
        Assert.Equal(2024, application.Year);
        Assert.Equal(8, application.Month);
        Assert.True(application.IsManualEntry);
    }

    [Fact]
    public void Build_FillState_ProcessNumberEmptyIsBlueUntilSubmitted()
    {
        var profile = new ApplicationProfile { ProduceVisa = true };
        var before = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance { ApplicationProfile = profile },
            profile,
            null);
        var beforeField = Assert.Single(before, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Default, beforeField.FillState);
        Assert.Equal("—", beforeField.DisplayValue);

        var after = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            new ApplicationProfileInstance
            {
                ApplicationProfile = profile,
                ProcessNumber = "AS538188",
            },
            profile,
            null);
        var afterField = Assert.Single(after, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Officer, afterField.FillState);
        Assert.Equal("AS538188", afterField.DisplayValue);
    }

    [Fact]
    public void Build_EmptyFromGeo_IsDefaultFill_WhenManualEntryImport()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.BusinessTrip,
            Code = "business_trip_departure",
            RequireFromRegion = true,
            RequireFromCity = true,
            RequireToRegion = true,
            RequireToCity = true,
        };
        var toRegionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var toCityId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            IsManualEntry = true,
            FullApplicationNumber = "9/-4003",
            ApplicationDate = new DateTime(2014, 9, 26),
            ToRegion = new Region { ID = toRegionId, NameTm = "Balkan welaýaty" },
            ToCity = new City { ID = toCityId, NameTm = "Türkmenbaşy etraby" },
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
        var fromRegion = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.FromRegion);
        var fromCity = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.FromCity);

        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Default, fromRegion.FillState);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Default, fromCity.FillState);
        var missing = ApplicationWorkspaceCaseSummaryCompletenessGate.MissingRequiredFields(fields);
        Assert.DoesNotContain(missing, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.FromRegion);
        Assert.DoesNotContain(missing, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.FromCity);
    }

    [Fact]
    public void Build_EmptyFromGeo_IsEmptyFill_WhenOfficerCreated()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.BusinessTrip,
            Code = "business_trip_departure",
            RequireFromRegion = true,
            RequireFromCity = true,
        };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            IsManualEntry = false,
            FullApplicationNumber = "1/1",
            ApplicationDate = new DateTime(2026, 1, 15),
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
        var fromRegion = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.FromRegion);
        var fromCity = Assert.Single(fields, item => item.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.FromCity);

        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Empty, fromRegion.FillState);
        Assert.Equal(ApplicationWorkspaceCaseSummaryFillState.Empty, fromCity.FillState);
        var missing = ApplicationWorkspaceCaseSummaryCompletenessGate.MissingRequiredFields(fields);
        Assert.Contains(missing, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.FromRegion);
        Assert.Contains(missing, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.FromCity);
    }

    [Fact]
    public void CitiesForSelectedRegion_FiltersToCitiesInSelectedRegion()
    {
        var maryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ahalId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var maryCityId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var maryNameOnlyCityId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var cities = new[]
        {
            new ApplicationProfileWizardLookupItem
            {
                Id = maryCityId,
                DisplayName = "Mary etraby",
                RegionId = maryId,
            },
            new ApplicationProfileWizardLookupItem
            {
                Id = maryNameOnlyCityId,
                DisplayName = "Ýolöten etraby",
                RegionName = "Mary welaýaty",
            },
            new ApplicationProfileWizardLookupItem
            {
                Id = Guid.NewGuid(),
                DisplayName = "Gökdepe etraby",
                RegionId = ahalId,
            },
            new ApplicationProfileWizardLookupItem
            {
                Id = Guid.NewGuid(),
                DisplayName = "Akbugdaý etraby",
                RegionName = "Ahal welaýaty",
            },
        };
        var regions = new[]
        {
            new ApplicationProfileWizardLookupItem
            {
                Id = maryId,
                DisplayName = "Mary province",
                RegionName = "Mary welaýaty",
            },
            new ApplicationProfileWizardLookupItem
            {
                Id = ahalId,
                DisplayName = "Ahal province",
                RegionName = "Ahal welaýaty",
            },
        };

        var filtered = ApplicationWorkspaceCaseHeaderFieldsHelper.CitiesForSelectedRegion(
            cities,
            regions,
            maryId);

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, option => option.Id == maryCityId);
        Assert.Contains(filtered, option => option.Id == maryNameOnlyCityId);
        Assert.DoesNotContain(filtered, option => option.DisplayName == "Gökdepe etraby");

        var ahalFiltered = ApplicationWorkspaceCaseHeaderFieldsHelper.CitiesForSelectedRegion(
            cities,
            regions,
            ahalId);
        Assert.Equal(2, ahalFiltered.Count);
        Assert.Contains(ahalFiltered, option => option.DisplayName == "Gökdepe etraby");
        Assert.Contains(ahalFiltered, option => option.DisplayName == "Akbugdaý etraby");
    }

    [Fact]
    public void CitiesForSelectedRegion_WithoutRegion_ReturnsAllCities()
    {
        var cities = new[]
        {
            new ApplicationProfileWizardLookupItem { Id = Guid.NewGuid(), DisplayName = "Mary etraby" },
            new ApplicationProfileWizardLookupItem { Id = Guid.NewGuid(), DisplayName = "Gökdepe etraby" },
        };

        var filtered = ApplicationWorkspaceCaseHeaderFieldsHelper.CitiesForSelectedRegion(
            cities,
            Array.Empty<ApplicationProfileWizardLookupItem>(),
            regionId: null);

        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public void FilterSitesByCity_KeepsOnlyLodgingsForSelectedToCity()
    {
        var sarahsId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var kakaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var sites = new[]
        {
            new ApplicationWorkspaceCaseHeaderFieldsHelper.SiteCatalogOption(
                Guid.NewGuid(),
                "Döwletabatgazçykaryş müdirliginiň ýaşaýyş jaý toplumy",
                sarahsId,
                "Sarahs etraby"),
            new ApplicationWorkspaceCaseHeaderFieldsHelper.SiteCatalogOption(
                Guid.NewGuid(),
                "dokma toplumynyň UYJ",
                kakaId,
                "Kaka etraby"),
            new ApplicationWorkspaceCaseHeaderFieldsHelper.SiteCatalogOption(
                Guid.NewGuid(),
                "1932 (A.Garlyýew) köç. 70/1 UÝJ",
                null,
                "Aşgabat şäheri"),
        };

        var filtered = ApplicationWorkspaceCaseHeaderFieldsHelper.FilterSitesByCity(
            sites,
            sarahsId,
            "Sarahs etraby");

        var option = Assert.Single(filtered);
        Assert.Equal("Döwletabatgazçykaryş müdirliginiň ýaşaýyş jaý toplumy", option.DisplayName);
    }

    [Fact]
    public void FilterSitesByCity_MatchesCatalogCityNameWhenCityIdMissing()
    {
        var sarahsId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var sites = new[]
        {
            new ApplicationWorkspaceCaseHeaderFieldsHelper.SiteCatalogOption(
                Guid.NewGuid(),
                "Döwletabatgazçykaryş müdirliginiň ýaşaýyş jaý toplumy",
                null,
                "Sarahs etraby"),
            new ApplicationWorkspaceCaseHeaderFieldsHelper.SiteCatalogOption(
                Guid.NewGuid(),
                "dokma toplumynyň UYJ",
                null,
                "Kaka etraby"),
        };

        var filtered = ApplicationWorkspaceCaseHeaderFieldsHelper.FilterSitesByCity(
            sites,
            sarahsId,
            "Sarahs etraby");

        var option = Assert.Single(filtered);
        Assert.Contains("Döwletabatgazçykaryş", option.DisplayName);
    }
}