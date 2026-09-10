#nullable enable

using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationRosterMergeLineTravelHistoryPlaceholderTests
{
    [Fact]
    public void TravelHistory_placeholders_read_linked_row_not_registration_line()
    {
        var history = new ExternalArrival
        {
            TravelDate = new DateTime(2026, 7, 20),
            MovementType = MovementType.Entry,
            TravelType = TravelType.External,
            CheckPoint = new CheckPoint { NameTm = "Aşgabat şäher howa menzilindäki MGP" },
            Country = new Country { NameTm = "Türkiýe" },
            Notes = "Linked row",
        };
        var line = new ApplicationRosterMergeLine
        {
            TravelDate = new DateTime(2026, 1, 1),
            CheckPoint = new CheckPoint { NameTm = "Registration line checkpoint" },
            CurrentTravelHistory = history,
        };

        Assert.Equal("01.01.2026", line.Travel_DateText);
        Assert.Equal("Registration line checkpoint", line.Travel_CheckPointTm);
        Assert.Equal("Entry", line.TravelHistory_Kind);
        Assert.Equal("20.07.2026", line.TravelHistory_DateText);
        Assert.Equal("Aşgabat şäher howa menzilindäki MGP", line.TravelHistory_CheckPointTm);
        Assert.Equal("Aşgabat şäher howa menzilindäki MGP", line.TravelHistory_PlaceTm);
        Assert.Equal("External", line.TravelHistory_TravelType);
        Assert.Equal("Türkiýe", line.TravelHistory_CountryTm);
        Assert.Equal("Linked row", line.TravelHistory_Notes);
    }

    [Fact]
    public void TravelHistory_place_falls_back_to_city_for_internal_travel()
    {
        var history = new InternalArrival
        {
            TravelDate = new DateTime(2026, 3, 5),
            MovementType = MovementType.Entry,
            TravelType = TravelType.Internal,
            Region = new Region { NameTm = "Balkan welaýaty" },
            City = new City { NameTm = "Türkmenbaşy etraby" },
        };
        var line = new ApplicationRosterMergeLine { CurrentTravelHistory = history };

        Assert.Equal("Entry", line.TravelHistory_Kind);
        Assert.Equal("05.03.2026", line.TravelHistory_DateText);
        Assert.Equal(string.Empty, line.TravelHistory_CheckPointTm);
        Assert.Equal("Türkmenbaşy etraby", line.TravelHistory_PlaceTm);
        Assert.Equal("Balkan welaýaty", line.TravelHistory_RegionTm);
        Assert.Equal("Türkmenbaşy etraby", line.TravelHistory_CityTm);
    }

    [Fact]
    public void Travel_placeholders_fall_back_to_linked_travel_history_when_registration_line_empty()
    {
        var history = new ExternalArrival
        {
            TravelDate = new DateTime(2025, 8, 11),
            MovementType = MovementType.Entry,
            CheckPoint = new CheckPoint { NameTm = "Aşgabat şäher howa menzili MGSP" },
        };
        var line = new ApplicationRosterMergeLine { CurrentTravelHistory = history };

        Assert.Equal("11.08.2025", line.Travel_DateText);
        Assert.Equal("Aşgabat şäher howa menzili MGSP", line.Travel_CheckPointTm);
        Assert.Equal("11.08.2025", line.TravelHistory_DateText);
        Assert.Equal("Aşgabat şäher howa menzili MGSP", line.TravelHistory_CheckPointTm);
    }

    [Fact]
    public void Travel_checkpoint_falls_back_to_travel_history_city_for_internal_entry()
    {
        var history = new InternalArrival
        {
            TravelDate = new DateTime(2026, 3, 5),
            MovementType = MovementType.Entry,
            TravelType = TravelType.Internal,
            City = new City { NameTm = "Türkmenbaşy etraby" },
        };
        var line = new ApplicationRosterMergeLine { CurrentTravelHistory = history };

        Assert.Equal("05.03.2026", line.Travel_DateText);
        Assert.Equal("Türkmenbaşy etraby", line.Travel_CheckPointTm);
    }

    [Fact]
    public void Application_visa_category_falls_back_to_linked_visa_when_instance_has_none()
    {
        var line = new ApplicationRosterMergeLine
        {
            ApplicationProfileInstance = new ApplicationProfileInstance(),
            CurrentVisa = new Visa
            {
                VisaCategory = new VisaCategory { NameTm = "köp gezeklik" },
            },
        };

        Assert.Equal("köp gezeklik", line.Application_VisaCategory_NameTm);
        Assert.Equal("köp gezeklik", line.VisaCategory_NameTm);
    }

    [Fact]
    public void Application_visa_category_falls_back_to_invitation_when_visa_missing()
    {
        var line = new ApplicationRosterMergeLine
        {
            CurrentInvitationItem = new InvitationItem
            {
                Invitation = new Invitation
                {
                    VisaCategory = new VisaCategory { NameTm = "iki gezeklik" },
                },
            },
        };

        Assert.Equal("iki gezeklik", line.Application_VisaCategory_NameTm);
    }

    [Fact]
    public void Application_visa_category_prefers_instance_over_linked_visa()
    {
        var line = new ApplicationRosterMergeLine
        {
            ApplicationProfileInstance = new ApplicationProfileInstance
            {
                VisaCategory = new VisaCategory { NameTm = "bir gezeklik" },
            },
            CurrentVisa = new Visa
            {
                VisaCategory = new VisaCategory { NameTm = "köp gezeklik" },
            },
        };

        Assert.Equal("bir gezeklik", line.Application_VisaCategory_NameTm);
    }

    [Fact]
    public void Catalog_includes_travel_history_short_codes()
    {
        var catalog = new Visa2026.Module.Services.UserReports.UserReportPlaceholderCatalogService();
        var codes = catalog.GetEntries()
            .Where(e => e.RelatedBo == Visa2026.Module.Services.UserReports.UserReportPlaceholderRelatedBo.TravelHistory)
            .Select(e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("THKD", codes);
        Assert.Contains("THDT", codes);
        Assert.Contains("THCP", codes);
        Assert.Contains("THPL", codes);
        Assert.Contains("THTP", codes);
        Assert.Contains("THCN", codes);
        Assert.Contains("THRG", codes);
        Assert.Contains("THCT", codes);
        Assert.Contains("THNT", codes);
    }
}