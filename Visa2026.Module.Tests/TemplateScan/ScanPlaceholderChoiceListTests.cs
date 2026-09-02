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

    private static ApplicationProfilePlaceholderSet FullSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
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
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });
}