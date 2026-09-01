#nullable enable

using System.Reflection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services.UserReports;

public class UserReportPlaceholderRelatedBoTests
{
    [Fact]
    public void Catalog_assigns_a_known_related_bo_to_every_entry()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var unknown = catalog.GetEntries()
            .Where(e => e.RelatedBo == UserReportPlaceholderRelatedBo.Unknown)
            .Select(e => e.ShortCode)
            .ToList();

        Assert.Empty(unknown);
    }

    [Theory]
    [InlineData("PPTP", "Passport_TypeTm")]
    [InlineData("PPAT", "Passport_Authority")]
    [InlineData("PPCC", "Passport_CountryCode")]
    [InlineData("PPCT", "Passport_CountryTm")]
    public void Passport_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonPassport, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Passport, entry.RelatedBo);
    }

    [Theory]
    [InlineData("PMNM", "Person_MiddleName")]
    [InlineData("PMST", "Person_MaritalStatusTm")]
    [InlineData("PNTM", "Person_NationalityTm")]
    [InlineData("PCBT", "Person_CountryOfBirthTm")]
    [InlineData("PSEF", "Person_SponsoringEmployeeFullName")]
    [InlineData("PSEP", "Person_SponsoringEmployeePositionTm")]
    [InlineData("PVFM", "Person_VisaApplicationFamilyMembersText")]
    public void Person_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.Core, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Person, entry.RelatedBo);
        Assert.Equal(UserReportPlaceholderScope.Row, entry.Scope);
        Assert.Equal("{{." + shortCode + "}}", entry.BuildWordToken(UserReportPlaceholderScope.Header));
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void Previous_workplaces_token_is_catalogued_on_person()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, "PWTM", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("Person_PreviousWorkplacesInTurkmenistan", entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.Core, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Person, entry.RelatedBo);
        Assert.Contains(UserReportBoType.ApplicationItem, entry.RootBoTypes);
        Assert.Contains(UserReportBoType.ApplicationProfileInstance, entry.RootBoTypes);

        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            "Person_PreviousWorkplacesInTurkmenistan", flags));
    }

    [Theory]
    [InlineData("EGLV", "Education_LevelTm")]
    [InlineData("EGIN", "Education_InstitutionName")]
    [InlineData("EGCC", "Education_CountryCode")]
    [InlineData("EGYR", "Education_GraduationYear")]
    [InlineData("EGSP", "Education_SpecialtyTm")]
    [InlineData("EGIY", "Education_LevelAndInstitutionTm")]
    public void Education_tokens_are_catalogued(string shortCode, string canonical)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var entry = catalog.GetEntries().Single(e =>
            string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(canonical, entry.CanonicalPath);
        Assert.Equal(UserReportPlaceholderPack.PersonEducation, entry.Pack);
        Assert.Equal(UserReportPlaceholderRelatedBo.Education, entry.RelatedBo);
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty(
            canonical, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    [Fact]
    public void Grouped_manual_puts_education_codes_together()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var groups = catalog.GetGroupedEntries();
        var education = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.Education);
        var codes = education.Entries.Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("EGLV", codes);
        Assert.Contains("EGIN", codes);
        Assert.Contains("EGCC", codes);
        Assert.Contains("EGYR", codes);
        Assert.Contains("EGSP", codes);
        Assert.Contains("EGIY", codes);
        Assert.DoesNotContain(education.Entries, e => e.RelatedBo != UserReportPlaceholderRelatedBo.Education);
    }

    [Fact]
    public void Passport_type_property_exists_on_roster_merge_line()
    {
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty("Passport_TypeTm", flags));
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty("Passport_Authority", flags));
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty("Passport_CountryCode", flags));
        Assert.NotNull(typeof(ApplicationRosterMergeLine).GetProperty("Passport_CountryTm", flags));
    }

    [Fact]
    public void Grouped_manual_puts_passport_codes_together()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var groups = catalog.GetGroupedEntries();
        var passport = groups.Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.Passport);
        var codes = passport.Entries.Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("PPN", codes);
        Assert.Contains("PPTP", codes);
        Assert.Contains("PPAT", codes);
        Assert.Contains("PPCC", codes);
        Assert.Contains("PPCT", codes);
        Assert.DoesNotContain(passport.Entries, e => e.RelatedBo != UserReportPlaceholderRelatedBo.Passport);
    }

    [Fact]
    public void Related_bo_filter_returns_only_that_group()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        var groups = catalog.GetGroupedEntries(new UserReportPlaceholderManualQuery
        {
            RelatedBo = UserReportPlaceholderRelatedBo.AuthorizedRepresentative,
        });

        Assert.Single(groups);
        Assert.Equal(UserReportPlaceholderRelatedBo.AuthorizedRepresentative, groups[0].RelatedBo);
        Assert.Contains(groups[0].Entries, e => e.ShortCode == "RPFN");
    }
}