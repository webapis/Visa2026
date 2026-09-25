#nullable enable

using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// Shape detectors and ScoreSnippet preferences for yellow-marks token guessing.
/// Wrong matches send the wrong merge token into Review.
/// </summary>
public class ScanShapeTokenMatcherTests
{
    private static ApplicationProfilePlaceholderSet FullSet()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        return new ApplicationProfilePlaceholderSetService(catalog).GetSet(new ApplicationProfilePlaceholderSetQuery
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
                ActionFamily = ApplicationProfileActionFamily.Registration,
            },
            DataScope = ApplicationProfileTemplateDataScope.Both,
            TemplateKind = ApplicationProfileTemplateKind.Word,
        });
    }

    [Theory]
    [InlineData("orta", true)]
    [InlineData("yokary", true)]
    [InlineData("bachelor", true)]
    [InlineData("not-a-level", false)]
    [InlineData("", false)]
    public void LooksLikeEducationLevel_matches_catalog_words(string folded, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikeEducationLevel(folded));

    [Theory]
    [InlineData("Turkmenistan uniwersiteti", true)]
    [InlineData("Orta mekdep", true)]
    [InlineData("Some college", true)]
    [InlineData("plain text", false)]
    public void LooksLikeEducationInstitution_matches_institution_markers(string folded, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikeEducationInstitution(folded));

    [Theory]
    [InlineData("erkek", true)]
    [InlineData("Ayal", true)]
    [InlineData("male", true)]
    [InlineData("other", false)]
    public void LooksLikeGenderWord_folds_and_matches(string text, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikeGenderWord(text));

    [Theory]
    [InlineData("1.250,00 USD", true)]
    [InlineData("500 TMT", true)]
    [InlineData("not money", false)]
    public void LooksLikeMoneyAmount_requires_currency_suffix(string text, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikeMoneyAmount(text));

    [Theory]
    [InlineData("1.250,00", true)]
    [InlineData("12.50", true)]
    [InlineData("1250", false)]
    public void LooksLikeMoneyAmountOnly_requires_decimal_or_grouped_form(string text, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikeMoneyAmountOnly(text));

    [Theory]
    [InlineData("USD", true)]
    [InlineData("manat", true)]
    [InlineData("XYZ", false)]
    public void LooksLikeCurrencyCode_matches_known_codes(string text, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikeCurrencyCode(text));

    [Theory]
    [InlineData("Mudiri Ahmet Berdiyew", true)]
    [InlineData("mudiri short", false)]
    [InlineData("Ahmet Berdiyew", false)]
    public void LooksLikeTitledPersonName_requires_mudiri_prefix_and_full_name(string text, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikeTitledPersonName(text));

    [Theory]
    [InlineData("Ahmet Berdiyew", true)]
    [InlineData("Enes Can Uzun", true)]
    [InlineData("A B", false)]
    [InlineData("Asgabat s.", false)]
    [InlineData("SingleName", false)]
    public void LooksLikePersonFullName_requires_two_to_four_letter_words(string text, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikePersonFullName(text));

    [Theory]
    [InlineData("asgabat saher berkararlyk etrap gorkut ata kocesi", true)]
    [InlineData("asgabat jay 12", true)]
    [InlineData("ashgabat street", false)]
    [InlineData("plain street", false)]
    public void LooksLikeTmResidenceStreet_requires_asgabat_plus_local_markers(string folded, bool expected) =>
        Assert.Equal(expected, ScanShapeTokenMatcher.LooksLikeTmResidenceStreet(folded));

    [Fact]
    public void ScoreSnippet_empty_returns_no_candidates()
    {
        var set = FullSet();
        Assert.Empty(ScanShapeTokenMatcher.ScoreSnippet("   ", set, UserReportPlaceholderScope.Both));
    }

    [Fact]
    public void ScoreSnippet_passport_number_prefers_PPN()
    {
        var set = FullSet();
        var top = ScanShapeTokenMatcher.ScoreSnippet("U34537060", set, UserReportPlaceholderScope.Both)
            .FirstOrDefault();
        Assert.NotNull(top);
        Assert.Equal("PPN", top!.ShortCode);
        Assert.True(top.ScorePercent >= 90);
    }

    [Fact]
    public void ScoreSnippet_gender_word_prefers_PGND()
    {
        var set = FullSet();
        var top = ScanShapeTokenMatcher.ScoreSnippet("erkek", set, UserReportPlaceholderScope.Both)
            .FirstOrDefault();
        Assert.NotNull(top);
        Assert.Equal("PGND", top!.ShortCode);
    }

    [Fact]
    public void ScoreSnippet_currency_code_prefers_CCUR_over_money_amount()
    {
        var set = FullSet();
        var top = ScanShapeTokenMatcher.ScoreSnippet("USD", set, UserReportPlaceholderScope.Both)
            .FirstOrDefault();
        Assert.NotNull(top);
        Assert.Equal("CCUR", top!.ShortCode);
    }

    [Fact]
    public void ScoreSnippet_money_amount_prefers_CSAL()
    {
        var set = FullSet();
        var top = ScanShapeTokenMatcher.ScoreSnippet("1.500,00 TMT", set, UserReportPlaceholderScope.Both)
            .FirstOrDefault();
        Assert.NotNull(top);
        Assert.Equal("CSAL", top!.ShortCode);
    }

    [Fact]
    public void ScoreSnippet_education_level_boosts_EGLV()
    {
        var set = FullSet();
        var eglv = ScanShapeTokenMatcher.ScoreSnippet("orta", set, UserReportPlaceholderScope.Both)
            .FirstOrDefault(c => c.ShortCode == "EGLV");
        Assert.NotNull(eglv);
        Assert.True(eglv!.ScorePercent >= 80);
    }

    [Fact]
    public void ScoreSnippet_person_full_name_prefers_PFN_not_titled()
    {
        var set = FullSet();
        var top = ScanShapeTokenMatcher.ScoreSnippet("Ahmet Berdiyew", set, UserReportPlaceholderScope.Both)
            .FirstOrDefault();
        Assert.NotNull(top);
        Assert.Equal("PFN", top!.ShortCode);
    }

    [Fact]
    public void ScoreSnippet_titled_director_prefers_CHFN()
    {
        var set = FullSet();
        var top = ScanShapeTokenMatcher.ScoreSnippet("Mudiri Ahmet Berdiyew", set, UserReportPlaceholderScope.Both)
            .FirstOrDefault();
        Assert.NotNull(top);
        Assert.Equal("CHFN", top!.ShortCode);
    }

    [Fact]
    public void ScoreSnippet_prefer_short_codes_boosts_matching_candidate()
    {
        var set = FullSet();
        var withoutBoost = ScanShapeTokenMatcher.ScoreSnippet(
            "15.03.1990", set, UserReportPlaceholderScope.Both);
        var withBoost = ScanShapeTokenMatcher.ScoreSnippet(
            "15.03.1990", set, UserReportPlaceholderScope.Both, preferShortCodes: ["ADAT"]);

        var adAtPlain = withoutBoost.First(c => c.ShortCode == "ADAT").ScorePercent;
        var adAtBoosted = withBoost.First(c => c.ShortCode == "ADAT").ScorePercent;
        Assert.True(adAtBoosted > adAtPlain);
    }

    [Fact]
    public void ScoreSnippet_dedupes_by_short_code_keeping_highest_score()
    {
        var set = FullSet();
        var results = ScanShapeTokenMatcher.ScoreSnippet("U34537060", set, UserReportPlaceholderScope.Both);
        Assert.Equal(results.Count, results.Select(c => c.ShortCode).Distinct(System.StringComparer.OrdinalIgnoreCase).Count());
    }
}
