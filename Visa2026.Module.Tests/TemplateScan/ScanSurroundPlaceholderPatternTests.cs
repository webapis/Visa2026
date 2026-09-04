#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanSurroundPlaceholderPatternTests
{
    private static ApplicationProfilePlaceholderSet Set() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Theory]
    [InlineData("Aynabat Meredowa", "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)", "PFN")]
    [InlineData("Gurban Annayew", "Ise cagrylan adam:", "PFN")]
    [InlineData("03.04.1991", "(ady, familiyasy, atasynyn ady, doglan senesi)", "PDBT")]
    [InlineData("U37109249", "pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)", "PPN")]
    [InlineData("19.02.2024", "pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)", "PPED")]
    [InlineData("19.02.2034", "yolbascy pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)", "CHPE")]
    public void Immediate_surround_ranks_the_matching_placeholder(
        string yellow,
        string nearby,
        string expectedCode)
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            yellow,
            nearby,
            null,
            Set(),
            UserReportPlaceholderScope.Header);

        Assert.NotEmpty(ranked);
        Assert.Equal(expectedCode, ranked[0].ShortCode);
        Assert.True(ranked[0].ScorePercent >= ScanSurroundPlaceholderPattern.NearbyMinScore);
    }

    [Fact]
    public void Surround_pattern_does_not_map_a_hired_person_name_to_company_address()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Meret Hydyrow",
            "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)",
            null,
            Set(),
            UserReportPlaceholderScope.Row);

        Assert.Equal("PFN", ranked[0].ShortCode);
        Assert.DoesNotContain(ranked.Take(2), a => a.ShortCode.Equals("ACADR", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rank_on_name_and_dob_comma_line_prefers_person_not_company_address()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Aynabat Meredowa, 03.04.1991",
            "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)",
            null,
            Set(),
            UserReportPlaceholderScope.Row);

        Assert.DoesNotContain(
            ranked.Take(3),
            a => a.ShortCode.Equals("ACADR", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            ranked.Take(3),
            a => a.ShortCode.Equals("PFN", StringComparison.OrdinalIgnoreCase)
                || a.ShortCode.Equals("PDBT", StringComparison.OrdinalIgnoreCase));
    }
}