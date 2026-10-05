using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PdfMappingHelperTests
{
    [Fact]
    public void FinalizeMappings_AssignsCitizenshipAndBirthCountryToPrintedLabels()
    {
        var mappings = PdfMappingHelper.FinalizeMappings(Array.Empty<PdfFormMappingDefinition>());

        var citizenship = mappings.Single(m => m.PdfFieldKey == "topmostSubform[0].Page1[0]._06[0]");
        var birthCountry = mappings.Single(m => m.PdfFieldKey == "topmostSubform[0].Page1[0]._07[0]");

        Assert.Equal("Person.Nationality.Code", citizenship.PropertyPath);
        Assert.Equal("Person.CountryOfBirth.Code", birthCountry.PropertyPath);

        var lastVisa = mappings.Single(m => m.PdfFieldKey == "topmostSubform[0].Page2[0]._30[0]");
        Assert.Equal("Pdf_LastIssuedVisaSummary", lastVisa.PropertyPath);
    }

    [Fact]
    public void LastIssuedVisaSummary_JoinsDegreeTypeNumberAndValidity()
    {
        var text = PdfLastIssuedVisaText.Format(new Visa
        {
            VisaNumber = "A1",
            IssueDate = new DateTime(2024, 1, 1),
            StartDate = new DateTime(2024, 1, 1),
            ExpirationDate = new DateTime(2024, 6, 1),
            VisaType = new VisaType { NameTm = "BS1" },
            VisaCategory = new VisaCategory { NameTm = "iki gezeklik" },
        });

        Assert.Equal("BS1, iki gezeklik, A1, 01.01.2024-01.06.2024", text);
    }

    [Fact]
    public void MapApplicationData_FillsPersonNameFromMergeLine()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { PdfForm_Code = 3 },
        };
        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = application,
            Person = new Person { FirstName = "Gabriel", LastName = "Silva" },
        };
        var data = new Dictionary<string, object>();
        var mappings = new List<PdfFormMappingDefinition>
        {
            new()
            {
                PdfFieldKey = "_03",
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "Person.FirstName",
                Description = "First Name",
            },
            new()
            {
                PdfFieldKey = "_01",
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "Person.LastName",
                Description = "Last Name",
            },
        };

        PdfMappingHelper.MapApplicationData(data, application, item, objectSpace: null, logger: null, mappings);

        Assert.Equal("GABRIEL", data["_03"]);
        Assert.Equal("SILVA", data["_01"]);
    }

    [Fact]
    public void MapApplicationData_RewritesApplicationRootToProfileInstance()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { PdfForm_Code = 3 },
        };
        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = application,
            Person = new Person { FirstName = "Gabriel" },
        };
        var data = new Dictionary<string, object>();
        var mappings = new List<PdfFormMappingDefinition>
        {
            new()
            {
                PdfFieldKey = "L01",
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "Application.ApplicationType.PdfForm_Code",
                Description = "Visa operation type",
            },
        };

        PdfMappingHelper.MapApplicationData(data, application, item, objectSpace: null, logger: null, mappings);

        Assert.Equal(3, data["L01"]);
    }

    [Fact]
    public void MapApplicationData_FillsLinkedPassportNumber()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { PdfForm_Code = 3 },
        };
        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = application,
            Person = new Person { FirstName = "Gabriel" },
            CurrentPassport = new Passport { PassportNumber = "S0047600" },
        };
        var data = new Dictionary<string, object>();
        var mappings = new List<PdfFormMappingDefinition>
        {
            new()
            {
                PdfFieldKey = "_11",
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "CurrentPassport.PassportNumber",
                Description = "Passport Number",
            },
        };

        PdfMappingHelper.MapApplicationData(data, application, item, objectSpace: null, logger: null, mappings);

        Assert.Equal("S0047600", data["_11"]);
    }

    [Fact]
    public void FinalizeMappings_AddsPersonAndPassportWhenDatabaseEmpty()
    {
        var mappings = PdfMappingHelper.FinalizeMappings(Array.Empty<PdfFormMappingDefinition>());
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { PdfForm_Code = 2 },
        };
        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = application,
            Person = new Person { FirstName = "Gabriel", LastName = "Silva" },
            CurrentPassport = new Passport { PassportNumber = "S0047600" },
        };
        var data = new Dictionary<string, object>();

        PdfMappingHelper.MapApplicationData(data, application, item, objectSpace: null, logger: null, mappings);

        Assert.Equal("SILVA", data["topmostSubform[0].Page1[0]._01[0]"]);
        Assert.Equal("GABRIEL", data["topmostSubform[0].Page1[0]._03[0]"]);
        Assert.Equal("S0047600", data["topmostSubform[0].Page1[0]._11[0]"]);
        Assert.Equal(2, data["topmostSubform[0].Page1[0].L01[0]"]);
    }

    [Fact]
    public void FinalizeMappings_OverwritesStalePersonPathOnSameFieldKey()
    {
        var stale = new List<PdfFormMappingDefinition>
        {
            new()
            {
                PdfFieldKey = "topmostSubform[0].Page1[0]._01[0]",
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "ApplicationItem.Person.LastName",
                Description = "stale",
            },
        };

        var mappings = PdfMappingHelper.FinalizeMappings(stale);
        var lastName = mappings.Single(m => m.PdfFieldKey == "topmostSubform[0].Page1[0]._01[0]");

        Assert.Equal("Person.LastName", lastName.PropertyPath);
        Assert.Equal(PdfMappingMode.Property, lastName.MappingMode);
    }

    [Fact]
    public void TryGetValue_MatchesFullPathToShortXfaName()
    {
        var data = new Dictionary<string, object>
        {
            ["topmostSubform[0].Page1[0]._03[0]"] = "GABRIEL",
        };

        Assert.True(PdfXfaFieldValueLookup.TryGetValue(data, "_03", out var value));
        Assert.Equal("GABRIEL", value);
        Assert.True(PdfXfaFieldValueLookup.TryGetValue(data, "_03[0]", out value));
        Assert.Equal("GABRIEL", value);
        Assert.Equal("_03", PdfXfaFieldValueLookup.LocalName("topmostSubform[0].Page1[0]._03[0]"));
    }

    [Fact]
    public void MapApplicationData_UsesInvitationStayWhenTheProfileShowsInvitationAddress()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { ShowCurrentAddressOfResidence = true },
            ApplicationProfile = new ApplicationProfile { RequireInvitationAddress = true },
            InvitationAddress = new InvitationAddress
            {
                Region = new Region { PdfForm_Code = "AH" },
                City = new City { PdfForm_Code = "AKB" },
                Type = ResidenceType.Hotel,
                Hotel = new Hotel { Name = "Ýyldyz myhmanhanasy" },
                AlternativeAddress = new AlternativeAddressesForInvitation
                {
                    AddressLine = "ýa-da Balkan welaýatynyň",
                },
            },
        };
        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = application,
            CurrentAddressOfResidence = new AddressOfResidence
            {
                Region = new Region { PdfForm_Code = "LB" },
                City = new City { PdfForm_Code = "LB" },
                FullAddress = "Residence street",
            },
        };
        var data = new Dictionary<string, object>();
        var mappings = new List<PdfFormMappingDefinition>
        {
            new()
            {
                PdfFieldKey = PdfMappingHelper.InvitationStayRegionKey,
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "CurrentAddressOfResidence.Region.PdfForm_Code",
            },
            new()
            {
                PdfFieldKey = PdfMappingHelper.InvitationStayCityKey,
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "CurrentAddressOfResidence.City.PdfForm_Code",
            },
            new()
            {
                PdfFieldKey = PdfMappingHelper.InvitationStayAddressKey,
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "CurrentAddressOfResidence.FullAddress",
            },
        };

        PdfMappingHelper.MapApplicationData(data, application, item, objectSpace: null, logger: null, mappings);

        Assert.Equal("AH", data[PdfMappingHelper.InvitationStayRegionKey]);
        Assert.Equal("AKB", data[PdfMappingHelper.InvitationStayCityKey]);
        Assert.Equal("YYLDYZ MYHMANHANASY, YA-DA BALKAN WELAYATYNYNG", data[PdfMappingHelper.InvitationStayAddressKey]);
    }

    [Fact]
    public void MapApplicationData_KeepsAddressOfResidenceWhenInvitationAddressIsOff()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { ShowCurrentAddressOfResidence = true },
            ApplicationProfile = new ApplicationProfile { RequireInvitationAddress = false },
            InvitationAddress = new InvitationAddress
            {
                Region = new Region { PdfForm_Code = "AH" },
            },
        };
        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = application,
            CurrentAddressOfResidence = new AddressOfResidence
            {
                Region = new Region { PdfForm_Code = "LB" },
            },
        };
        var data = new Dictionary<string, object>();
        var mappings = new List<PdfFormMappingDefinition>
        {
            new()
            {
                PdfFieldKey = PdfMappingHelper.InvitationStayRegionKey,
                MappingMode = PdfMappingMode.Property,
                PropertyPath = "CurrentAddressOfResidence.Region.PdfForm_Code",
            },
        };

        PdfMappingHelper.MapApplicationData(data, application, item, objectSpace: null, logger: null, mappings);

        Assert.Equal("LB", data[PdfMappingHelper.InvitationStayRegionKey]);
    }
}