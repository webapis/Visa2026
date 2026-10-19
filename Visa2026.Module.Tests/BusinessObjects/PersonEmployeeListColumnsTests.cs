using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class PersonEmployeeListColumnsTests
{
    private static readonly DateTime Today = new(2026, 10, 17);

    [Fact]
    public void Position_columns_use_open_history_or_latest_start()
    {
        var person = new Person();
        person.PositionHistory.Add(new EmployeePositionHistory
        {
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2024, 6, 1),
            Position = new Position { Name = "Old visa", NameTm = "Old visa" },
            ActualPosition = new ActualPosition { Name = "Old company" },
            Department = new Department { Name = "Old dept", NameTm = "Old dept" },
        });
        person.PositionHistory.Add(new EmployeePositionHistory
        {
            StartDate = new DateTime(2025, 3, 1),
            Position = new Position { Name = "Welder", NameTm = "Welder" },
            ActualPosition = new ActualPosition { Name = "Site welder" },
            Department = new Department { Name = "Construction", NameTm = "Construction" },
        });

        Assert.Equal("Welder", person.EmployeeListPositionName);
        Assert.Equal("Site welder", person.EmployeeListActualPositionName);
        Assert.Equal("Construction", person.EmployeeListDepartmentName);
    }

    [Fact]
    public void Position_columns_fall_back_to_latest_start_when_every_row_is_closed()
    {
        var person = new Person();
        person.PositionHistory.Add(new EmployeePositionHistory
        {
            StartDate = new DateTime(2018, 1, 1),
            EndDate = new DateTime(2019, 1, 1),
            Position = new Position { Name = "First", NameTm = "First" },
            ActualPosition = new ActualPosition { Name = "First actual" },
        });
        person.PositionHistory.Add(new EmployeePositionHistory
        {
            StartDate = new DateTime(2021, 5, 1),
            EndDate = new DateTime(2022, 5, 1),
            Position = new Position { Name = "Last", NameTm = "Last" },
            ActualPosition = new ActualPosition { Name = "Last actual" },
            Department = new Department { Name = "Ops", NameTm = "Ops" },
        });

        Assert.Equal("Last", person.EmployeeListPositionName);
        Assert.Equal("Last actual", person.EmployeeListActualPositionName);
        Assert.Equal("Ops", person.EmployeeListDepartmentName);
    }

    [Fact]
    public void Active_visa_numbers_are_in_force_today_comma_separated_latest_first()
    {
        var person = new Person();
        var passport = new Passport();
        person.Passports.Add(passport);
        passport.Visas.Add(new Visa
        {
            VisaNumber = "V-OLD",
            StartDate = Today.AddYears(-2),
            IssueDate = Today.AddYears(-2),
            ExpirationDate = Today.AddDays(-1),
        });
        passport.Visas.Add(new Visa
        {
            VisaNumber = "V-SOON",
            StartDate = Today.AddDays(1),
            IssueDate = Today,
            ExpirationDate = Today.AddYears(1),
        });
        passport.Visas.Add(new Visa
        {
            VisaNumber = "V-A",
            StartDate = Today.AddMonths(-6),
            IssueDate = Today.AddMonths(-6),
            ExpirationDate = Today.AddMonths(6),
        });
        passport.Visas.Add(new Visa
        {
            VisaNumber = "V-B",
            StartDate = Today.AddMonths(-1),
            IssueDate = Today.AddMonths(-1),
            ExpirationDate = null,
        });
        passport.Visas.Add(CancelledVisa("V-X"));

        Assert.Equal("V-B, V-A", PersonCurrentItems.FormatActiveVisaNumbers(person, Today));
    }

    [Fact]
    public void Active_work_permit_numbers_are_in_force_today_comma_separated_latest_first()
    {
        var person = new Person();
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            WorkPermitNumber = "WP-OLD",
            StartDate = Today.AddYears(-2),
            ExpirationDate = Today.AddDays(-1),
        });
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            WorkPermitNumber = "WP-SOON",
            StartDate = Today.AddDays(1),
            ExpirationDate = Today.AddYears(1),
        });
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            WorkPermitNumber = "WP-A",
            StartDate = Today.AddMonths(-8),
            ExpirationDate = Today.AddMonths(4),
        });
        person.WorkPermitItems.Add(new WorkPermitItem
        {
            WorkPermitNumber = "WP-B",
            StartDate = Today.AddMonths(-2),
            ExpirationDate = Today,
        });
        person.WorkPermitItems.Add(CancelledWorkPermit("WP-X"));

        Assert.Equal("WP-B, WP-A", PersonCurrentItems.FormatActiveWorkPermitNumbers(person, Today));
    }

    [Fact]
    public void Residence_address_uses_report_city_and_street_of_the_current_address()
    {
        var person = new Person();
        person.AddressesOfResidence.Add(new AddressOfResidence
        {
            Type = ResidenceType.PrivateHouse,
            FullAddress = "expired street",
            ExpirationDate = DateTime.Today.AddDays(-10),
            Region = new Region { NameTm = "Balkan welaýaty" },
            City = new City { NameTm = "old city" },
        });
        person.AddressesOfResidence.Add(new AddressOfResidence
        {
            Type = ResidenceType.PrivateHouse,
            FullAddress = "Şatlyk Gurbandurdy yolunun 57 km",
            ExpirationDate = DateTime.Today.AddYears(1),
            Region = new Region { NameTm = "Balkan welaýaty" },
            City = new City { NameTm = "Turkmenbashy etraby" },
        });

        Assert.Equal(
            "Balkan welaýatynyň, Turkmenbashy etraby, Şatlyk Gurbandurdy yolunun 57 km",
            person.EmployeeListResidenceAddress);
    }

    [Fact]
    public void Education_columns_stack_place_and_specialty_latest_graduation_first()
    {
        var person = new Person();
        person.Educations.Add(new Education
        {
            ID = Guid.NewGuid(),
            GraduationYear = "2010",
            EducationLevel = new EducationLevel { Name = "Place level 2010", NameTm = "Place level 2010" },
            EducationInstitution = new EducationInstitution { Name = "Iskenderun Demir Polat", NameTm = "Iskenderun Demir Polat" },
            EducationCountry = new Country { Name = "Türkiýe", NameTm = "Türkiýe" },
            Specialty = new Specialty { Name = "Jemgyýetçilik ylymlary", NameTm = "Jemgyýetçilik ylymlary" },
        });
        person.Educations.Add(new Education
        {
            ID = Guid.NewGuid(),
            GraduationYear = "2013",
            EducationLevel = new EducationLevel { Name = "Place level 2013", NameTm = "Place level 2013" },
            EducationInstitution = new EducationInstitution { Name = "Aşgabat uniwersiteti", NameTm = "Aşgabat uniwersiteti" },
            EducationCountry = new Country { Name = "Türkmenistan", NameTm = "Türkmenistan" },
            Specialty = new Specialty { Name = "Montaj", NameTm = "Montaj" },
        });
        person.Educations.Add(new Education { ID = Guid.NewGuid(), GraduationYear = "2001" });

        var places = PersonCurrentItems.ListEducationPlaces(person);
        Assert.Equal(
            new[] { "Place level 2013", "Aşgabat uniwersiteti", "Türkmenistan" },
            places[0]);
        Assert.Equal(
            new[] { "Place level 2010", "Iskenderun Demir Polat", "Türkiýe" },
            places[1]);

        var professions = PersonCurrentItems.ListEducationProfessions(person);
        Assert.Equal(new[] { "Montaj" }, professions[0]);
        Assert.Equal(new[] { "Jemgyýetçilik ylymlary" }, professions[1]);
        Assert.Equal("Montaj | Jemgyýetçilik ylymlary", person.EmployeeListEducationProfession);
        Assert.DoesNotContain("2001", person.EmployeeListEducationPlace);
    }

    private static Visa CancelledVisa(string number)
    {
        var visa = new Visa
        {
            VisaNumber = number,
            StartDate = Today.AddMonths(-3),
            IssueDate = Today.AddMonths(-3),
            ExpirationDate = Today.AddYears(1),
        };
        visa.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            ApplicationProfile = new ApplicationProfile
            {
                ActionFamily = ApplicationProfileActionFamily.Cancellation,
            },
        });
        return visa;
    }

    private static WorkPermitItem CancelledWorkPermit(string number)
    {
        var item = new WorkPermitItem
        {
            WorkPermitNumber = number,
            StartDate = Today.AddMonths(-3),
            ExpirationDate = Today.AddYears(1),
        };
        item.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
            ApplicationProfile = new ApplicationProfile
            {
                ActionFamily = ApplicationProfileActionFamily.Cancellation,
            },
        });
        return item;
    }
}
