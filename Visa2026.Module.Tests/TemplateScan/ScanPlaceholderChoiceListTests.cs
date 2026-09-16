#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanPlaceholderChoiceListTests
{
    [Fact]
    public void CompanySignatory_search_keeps_CHPN_and_CHPL()
    {
        var allowed = FullSet().Allowed;
        var groups = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: ["PPED"], search: "CompanySignatory");
        var signatory = Assert.Single(groups);
        Assert.Equal(UserReportPlaceholderRelatedBo.CompanySignatory, signatory.RelatedBo);
        var codes = signatory.Entries.Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("ACFNM", codes);
        Assert.Contains("ACPOS", codes);
        Assert.Contains("CHFN", codes);
        Assert.Contains("CHPA", codes);
        Assert.Contains("CHPD", codes);
        Assert.Contains("CHPE", codes);
        Assert.Contains("CHPN", codes);
        Assert.Contains("CHPL", codes);
    }

    [Fact]
    public void Compound_part_list_does_not_hide_sibling_signatory_codes()
    {
        var allowed = FullSet().Allowed;
        var groups = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: ["PPED"]);
        var codes = groups
            .Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.CompanySignatory)
            .Entries
            .Select(e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("CHPN", codes);
        Assert.Contains("CHPL", codes);
        Assert.Contains("CHPD", codes);
        Assert.Contains("CHPE", codes);
        Assert.DoesNotContain("PPED", codes);
    }

    [Fact]
    public void Signatory_search_matches_group_display_name()
    {
        var allowed = FullSet().Allowed;
        var groups = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: Array.Empty<string>(), search: "signatory");
        Assert.Contains(groups, g => g.RelatedBo == UserReportPlaceholderRelatedBo.CompanySignatory);
        Assert.DoesNotContain(groups, g => g.RelatedBo == UserReportPlaceholderRelatedBo.Person);
    }

    [Theory]
    [InlineData("CINB")]
    [InlineData("Cancel invitation AS numbers")]
    [InlineData("CISB")]
    [InlineData("Cancel invitation issued dates")]
    [InlineData("CIEB")]
    [InlineData("Cancel invitation expiration dates")]
    [InlineData("Çakylygyň belgisi")]
    public void Cancel_invitation_sanaw_search_finds_block_codes(string search)
    {
        var allowed = FullSet().Allowed;
        var codes = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: Array.Empty<string>(), search)
            .SelectMany(static g => g.Entries)
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (search.Contains("CINB", StringComparison.OrdinalIgnoreCase)
            || search.Contains("AS", StringComparison.OrdinalIgnoreCase)
            || search.Contains("belgisi", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("CINB", codes);
        else if (search.Contains("CISB", StringComparison.OrdinalIgnoreCase)
            || search.Contains("issued", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("CISB", codes);
        else
            Assert.Contains("CIEB", codes);
    }

    [Theory]
    [InlineData("CICTX")]
    [InlineData("CICNT")]
    [InlineData("CancelInvCountText")]
    [InlineData("cancel invitation count")]
    [InlineData("çakylyk")]
    public void Cancel_invitation_count_search_finds_catalog_codes(string search)
    {
        var allowed = FullSet().Allowed;
        var groups = ScanPlaceholderChoiceList.RemainingGroups(
            allowed,
            hideShortCodes: Array.Empty<string>(),
            search: search);
        var codes = groups.SelectMany(g => g.Entries)
            .Select(e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (string.Equals(search, "CICTX", StringComparison.OrdinalIgnoreCase)
            || search.Contains("Text", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("CICTX", codes);
        else if (string.Equals(search, "CICNT", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("CICNT", codes);
        else
        {
            Assert.Contains("CICNT", codes);
            Assert.Contains("CICTX", codes);
        }
    }

    [Theory]
    [InlineData("From Region", "BTFRG")]
    [InlineData("From City", "BTFCT")]
    [InlineData("To Region", "BTTRG")]
    [InlineData("To City", "BTTCT")]
    [InlineData("FromRegion", "BTFRG")]
    [InlineData("FromCity", "BTFCT")]
    [InlineData("ToRegion", "BTTRG")]
    [InlineData("ToCity", "BTTCT")]
    [InlineData("Purpose", "BTPRP")]
    [InlineData("Maksady", "BTPRP")]
    public void From_to_region_city_search_finds_case_codes(string search, string expected)
    {
        var allowed = FullSet().Allowed;
        var codes = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: Array.Empty<string>(), search)
            .SelectMany(static g => g.Entries)
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(expected, codes);
    }

    [Theory]
    [InlineData("BTSD")]
    [InlineData("business trip")]
    [InlineData("iş sapary")]
    [InlineData("Maksady")]
    [InlineData("Iş saparynda boljak salgysy")]
    [InlineData("business trip adress")]
    [InlineData("BTAD")]
    [InlineData("BusinessTripAddress_FullAddress")]
    [InlineData("Address_FullAddress")]
    public void Business_trip_search_finds_catalog_codes(string search)
    {
        var allowed = FullSet().Allowed;
        var codes = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: Array.Empty<string>(), search)
            .SelectMany(static g => g.Entries)
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (string.Equals(search, "BTSD", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("BTSD", codes);
        else if (search.Contains("Maksady", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("BTPRP", codes);
        else if (search.Contains("boljak", StringComparison.OrdinalIgnoreCase)
            || search.Contains("adress", StringComparison.OrdinalIgnoreCase)
            || search.Contains("BTAD", StringComparison.OrdinalIgnoreCase)
            || search.Contains("FullAddress", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("BTAD", codes);
        else
        {
            Assert.Contains("BTSD", codes);
            Assert.Contains("BTDCNT", codes);
            Assert.Contains("BTPRP", codes);
        }
    }

    [Fact]
    public void Education_search_keeps_level_institution_and_specialty()
    {
        var allowed = FullSet().Allowed;
        var groups = ScanPlaceholderChoiceList.RemainingGroups(
            allowed,
            hideShortCodes: Array.Empty<string>(),
            search: "education");
        var education = Assert.Single(groups, g => g.RelatedBo == UserReportPlaceholderRelatedBo.Education);
        var codes = education.Entries.Select(e => e.ShortCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("EGLV", codes);
        Assert.Contains("EGIN", codes);
        Assert.Contains("EGSP", codes);
    }

    [Theory]
    [InlineData("speciality")]
    [InlineData("specialty")]
    [InlineData("institution")]
    [InlineData("education level")]
    [InlineData("PersonEducation")]
    [InlineData("EGLV")]
    public void Education_search_aliases_find_catalog_codes(string search)
    {
        var allowed = FullSet().Allowed;
        var codes = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: Array.Empty<string>(), search)
            .SelectMany(static g => g.Entries)
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.NotEmpty(codes);
        if (search.Contains("special", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("EGSP", codes);
        else if (search.Contains("institution", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("EGIN", codes);
        else if (search.Contains("level", StringComparison.OrdinalIgnoreCase) || search.Equals("EGLV", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("EGLV", codes);
        else
        {
            Assert.Contains("EGLV", codes);
            Assert.Contains("EGIN", codes);
            Assert.Contains("EGSP", codes);
        }
    }

    [Theory]
    [InlineData("birth place")]
    [InlineData("birthplace")]
    [InlineData("place of birth")]
    [InlineData("PBPL")]
    [InlineData("doglan yeri")]
    [InlineData("Doglan ýeri")]
    public void Birth_place_search_finds_PBPL(string search)
    {
        var allowed = FullSet().Allowed;
        var codes = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: Array.Empty<string>(), search)
            .SelectMany(static g => g.Entries)
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("PBPL", codes);
    }

    [Theory]
    [InlineData("WPLC")]
    [InlineData("CWLB")]
    [InlineData("Work Permitted Locations")]
    [InlineData("hereket")]
    public void Work_permitted_locations_search_finds_catalog_codes(string search)
    {
        var allowed = FullSet().Allowed;
        var codes = ScanPlaceholderChoiceList.RemainingGroups(allowed, hideShortCodes: Array.Empty<string>(), search)
            .SelectMany(static g => g.Entries)
            .Select(static e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (string.Equals(search, "CWLB", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("CWLB", codes);
        else if (string.Equals(search, "WPLC", StringComparison.OrdinalIgnoreCase))
            Assert.Contains("WPLC", codes);
        else
        {
            Assert.Contains("WPLC", codes);
            Assert.Contains("CWLB", codes);
        }
    }

    [Fact]
    public void Travel_history_search_matches_group_display_name()
    {
        var allowed = FullSet().Allowed;
        var groups = ScanPlaceholderChoiceList.RemainingGroups(
            allowed,
            hideShortCodes: Array.Empty<string>(),
            search: "travel history");
        Assert.Contains(groups, g => g.RelatedBo == UserReportPlaceholderRelatedBo.TravelHistory);
        var codes = groups
            .Single(g => g.RelatedBo == UserReportPlaceholderRelatedBo.TravelHistory)
            .Entries
            .Select(e => e.ShortCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("THKD", codes);
        Assert.Contains("THDT", codes);
        Assert.Contains("THCP", codes);
        Assert.Contains("THPL", codes);
    }

    private static ApplicationProfilePlaceholderSet FullSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
                {
                    ActionFamily = ApplicationProfileActionFamily.Registration,
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
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });
}