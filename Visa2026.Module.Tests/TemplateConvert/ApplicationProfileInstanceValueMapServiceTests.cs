using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class ApplicationProfileInstanceValueMapServiceTests
{
    private readonly IApplicationProfilePlaceholderSetService _setService =
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService());

    private readonly IApplicationProfileInstanceValueMapService _service = new ApplicationProfileInstanceValueMapService();

    private static ApplicationProfile FullProfile() =>
        new()
        {
            RequirePersonPassport = true,
            RequirePersonVisa = true,
            RequirePersonEducation = true,
            RequirePersonAddressOfResidence = true,
            RequirePersonPosition = true,
            RequirePersonSalary = true,
            RequirePersonMedical = true,
            RequirePersonInvitationItem = true,
            RequirePersonWorkPermitItem = true,
            RequirePersonBorderZoneItem = true,
            RequirePersonRejectionItem = true,
            RequirePersonTravelHistory = true,
        };

    private static ApplicationRosterMergeLine Line(
        string firstName = "Dowletmyrat",
        string lastName = "Amanov",
        string passportNumber = "U36556957",
        string? birthPlace = null,
        string? foreignAddress = null,
        string? salary = null) =>
        new()
        {
            Person = new Person
            {
                FirstName = firstName,
                LastName = lastName,
                BirthPlace = birthPlace,
                ForeignAddress = foreignAddress,
            },
            CurrentPassport = new Passport { PassportNumber = passportNumber },
            CurrentSalary = salary == null ? null : new EmployeeSalary { Amount = salary },
        };

    private ApplicationProfileInstanceValueMap Build(
        IReadOnlyList<ApplicationRosterMergeLine>? rows = null,
        ApplicationProfile? profile = null,
        ApplicationProfileTemplateDataScope dataScope = ApplicationProfileTemplateDataScope.Both,
        ApplicationProfileInstance? instance = null)
    {
        profile ??= FullProfile();
        var set = _setService.GetSet(new ApplicationProfilePlaceholderSetQuery
        {
            Profile = profile,
            DataScope = dataScope,
        });

        return _service.Build(new ApplicationProfileInstanceValueMapRequest
        {
            Instance = instance ?? new ApplicationProfileInstance(),
            PlaceholderSet = set,
            DataScope = dataScope,
            Rows = rows ?? [Line()],
        });
    }

    private static ValueCandidate? Candidate(ApplicationProfileInstanceValueMap map, string shortCode, int? rowIndex = 0) =>
        map.Candidates.FirstOrDefault(c =>
            string.Equals(c.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase) && c.RowIndex == rowIndex);

    [Fact]
    public void Row_values_are_keyed_by_short_code()
    {
        var map = Build();

        var row = Assert.Single(map.Rows);
        Assert.Equal("Dowletmyrat Amanov", row["PFN"]);
        Assert.Equal("U36556957", row["PPN"]);
    }

    [Fact]
    public void Row_candidates_carry_the_row_token_and_index()
    {
        var map = Build([Line(), Line(firstName: "Aýnabat", lastName: "Meredowa", passportNumber: "U11111111")]);

        var second = Candidate(map, "PFN", rowIndex: 1);
        Assert.NotNull(second);
        Assert.Equal("{{.PFN}}", second!.Token);
        Assert.Equal("Aýnabat Meredowa", second.RawValue);
        Assert.Equal(ValueKind.PersonName, second.Kind);
    }

    [Fact]
    public void Person_name_candidates_expose_both_word_orders()
    {
        var candidate = Candidate(Build(), "PFN");

        Assert.NotNull(candidate);
        Assert.Contains("dowletmyrat amanov", candidate!.MatchKeys);
        Assert.Contains("amanov dowletmyrat", candidate.MatchKeys);
    }

    [Fact]
    public void Passport_candidates_are_identifiers_with_separators_stripped()
    {
        var candidate = Candidate(Build([Line(passportNumber: "U 3655-6957")]), "PPN");

        Assert.NotNull(candidate);
        Assert.Equal(ValueKind.Identifier, candidate!.Kind);
        Assert.Contains("u36556957", candidate.MatchKeys);
    }

    [Fact]
    public void Header_values_use_the_header_token_form()
    {
        var instance = new ApplicationProfileInstance { ApplicationDate = new DateTime(2026, 1, 20) };

        var map = Build(instance: instance);

        Assert.Equal("20.01.2026", map.Header["ADAT"]);
        Assert.Equal("{{ds.ADAT}}", Candidate(map, "ADAT", rowIndex: null)!.Token);
    }

    [Fact]
    public void Header_only_scope_produces_no_rows()
    {
        var map = Build(dataScope: ApplicationProfileTemplateDataScope.ApplicationHeader);

        Assert.Empty(map.Rows);
        Assert.DoesNotContain("PFN", map.Header.Keys);
    }

    [Fact]
    public void People_only_scope_produces_no_header()
    {
        var map = Build(dataScope: ApplicationProfileTemplateDataScope.PeopleM2M);

        Assert.Empty(map.Header);
        Assert.Single(map.Rows);
    }

    /// <summary>The map is built from the E1 allowed set, so a disabled pack cannot appear at all.</summary>
    [Fact]
    public void Tokens_the_profile_disallows_never_appear()
    {
        var profile = FullProfile();
        profile.RequirePersonPassport = false;

        var map = Build(profile: profile);

        Assert.DoesNotContain("PPN", map.Rows[0].Keys);
        Assert.Null(Candidate(map, "PPN"));
        Assert.Contains("PFN", map.Rows[0].Keys);
    }

    [Fact]
    public void Values_shorter_than_the_minimum_are_rejected()
    {
        var map = Build([Line(firstName: "Ai", lastName: string.Empty)]);

        Assert.Null(Candidate(map, "PFN"));
        Assert.Contains(map.Rejected, r => r.ShortCode == "PFN" && r.Reason == ValueRejectionReason.TooShort);
    }

    [Fact]
    public void Bare_small_numbers_are_rejected()
    {
        var map = Build([Line(salary: "12")]);

        Assert.Null(Candidate(map, "CSAL"));
        Assert.Contains(map.Rejected, r => r.ShortCode == "CSAL" && r.Reason == ValueRejectionReason.SmallNumber);
    }

    [Fact]
    public void A_salary_long_enough_to_attribute_is_kept()
    {
        var candidate = Candidate(Build([Line(salary: "5000")]), "CSAL");

        Assert.NotNull(candidate);
        Assert.Equal(ValueKind.Number, candidate!.Kind);
    }

    /// <summary>
    /// Two tokens holding the same literal cannot be told apart, so both are dropped and recorded
    /// rather than highlighted as a coin flip.
    /// </summary>
    [Fact]
    public void A_literal_shared_by_two_tokens_is_rejected_as_ambiguous()
    {
        var map = Build([Line(birthPlace: "Gaziantep sahiri", foreignAddress: "Gaziantep sahiri")]);

        Assert.Null(Candidate(map, "PBPL"));
        Assert.Null(Candidate(map, "PFAD"));
        Assert.Contains(map.Rejected, r => r.ShortCode == "PBPL" && r.Reason == ValueRejectionReason.Ambiguous);
        Assert.Contains(map.Rejected, r => r.ShortCode == "PFAD" && r.Reason == ValueRejectionReason.Ambiguous);
    }

    [Fact]
    public void Distinct_literals_survive_the_ambiguity_pass()
    {
        var map = Build([Line(birthPlace: "Gaziantep sahiri", foreignAddress: "Emek mahallesi")]);

        Assert.NotNull(Candidate(map, "PBPL"));
        Assert.NotNull(Candidate(map, "PFN"));
    }

    /// <summary>
    /// `Person_ForeignAddressWithCountry` prefixes a country code, so with no country it returns
    /// exactly `Person_ForeignAddress`. Neither token can then be attributed, and both drop out.
    /// </summary>
    [Fact]
    public void A_composed_token_that_collapses_onto_its_source_is_ambiguous()
    {
        var map = Build([Line(foreignAddress: "Emek mahallesi")]);

        Assert.Null(Candidate(map, "PFAD"));
        Assert.Null(Candidate(map, "PFWC"));
        Assert.Contains(map.Rejected, r => r.ShortCode == "PFWC" && r.Reason == ValueRejectionReason.Ambiguous);
    }

    /// <summary>The same token repeating across people is normal, not ambiguity.</summary>
    [Fact]
    public void The_same_value_in_two_rows_is_not_ambiguous()
    {
        var map = Build([Line(salary: "5000"), Line(firstName: "Aýnabat", lastName: "Meredowa", salary: "5000")]);

        Assert.NotNull(Candidate(map, "CSAL", rowIndex: 0));
        Assert.NotNull(Candidate(map, "CSAL", rowIndex: 1));
        Assert.DoesNotContain(map.Rejected, r => r.Reason == ValueRejectionReason.Ambiguous);
    }

    [Fact]
    public void No_template_is_required_and_photos_are_skipped()
    {
        var map = Build();

        Assert.DoesNotContain("PPH", map.Rows[0].Keys);
        Assert.DoesNotContain(map.Candidates, c => c.ShortCode == "PPH");
    }

    [Fact]
    public void Empty_values_produce_neither_candidates_nor_rejections()
    {
        var map = Build([Line(birthPlace: null)]);

        Assert.Null(Candidate(map, "PBPL"));
        Assert.DoesNotContain(map.Rejected, r => r.ShortCode == "PBPL");
    }
}
