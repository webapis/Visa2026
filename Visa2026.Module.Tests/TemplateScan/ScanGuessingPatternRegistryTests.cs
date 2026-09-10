#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanGuessingPatternRegistryTests
{
    private static ApplicationProfilePlaceholderSet Forma16Set() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
                {
                    RequirePersonPassport = true,
                    RequirePersonVisa = true,
                    RequirePersonPosition = true,
                    RequirePersonAddressOfResidence = true,
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    private static ApplicationProfilePlaceholderSet Set() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
                {
                    RequirePersonEducation = true,
                    RequirePersonPosition = true,
                    RequirePersonSalary = true,
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Fact]
    public void Detect_ministry_letter_marks_as_official_letter()
    {
        var kinds = ScanGuessingPatternRegistry.Detect(
            "Gyssagly tertipde!",
            null,
            null,
            ScanSourceKind.Word);
        Assert.Contains(kinds, k => k == ScanGuessingPatternKind.OfficialLetter);
    }

    [Fact]
    public void Detect_borcnama_caption_under_the_line()
    {
        var kinds = ScanGuessingPatternRegistry.Detect(
            "Hilmi Erol, 16.05.1980y.",
            "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)",
            null,
            ScanSourceKind.Word);
        Assert.Contains(kinds, k => k == ScanGuessingPatternKind.CaptionUnderLine);
    }

    [Fact]
    public void Detect_sahsy_left_field_label()
    {
        var kinds = ScanGuessingPatternRegistry.Detect(
            "TUR",
            "Rayatlygy",
            null,
            ScanSourceKind.Word);
        Assert.Contains(kinds, k => k == ScanGuessingPatternKind.LeftLabelForm);
        Assert.DoesNotContain(kinds, k => k == ScanGuessingPatternKind.CaptionUnderLine);
    }

    [Fact]
    public void Detect_excel_column_header()
    {
        var kinds = ScanGuessingPatternRegistry.Detect(
            "Erol",
            null,
            "Familiyasy",
            ScanSourceKind.Excel);
        Assert.Equal(ScanGuessingPatternKind.ExcelColumnHeader, Assert.Single(kinds));
    }

    [Fact]
    public void Detect_labor_contract_inline_director_title()
    {
        var kinds = ScanGuessingPatternRegistry.Detect(
            "Mudiri Mehmet Cirak",
            "tarapyndan",
            null,
            ScanSourceKind.Word);
        Assert.Contains(kinds, k => k == ScanGuessingPatternKind.InlineProse);
    }

    [Theory]
    [InlineData("Hilmi Erol", "Familiyasy, ady, atasynyn ady", "PFN")]
    [InlineData("TUR", "Rayatlygy", "PNAT")]
    [InlineData("11402573788", "Sahsy belgisi", "PPIN")]
    [InlineData("elektrik-elektronika inzenerciligi", "Hunari", "EGSP")]
    [InlineData("Taslamany dolandyryş mudiri", "Wezipesi", "POSN")]
    [InlineData("Norsel Kompaniyasy", "Turkmenistanda onki islan yerleri", "PWTM")]
    public void Left_label_form_ranks_sahsy_field_codes(string yellow, string nearby, string expected)
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            yellow, nearby, null, Set(), UserReportPlaceholderScope.Row);
        Assert.Equal(expected, ranked[0].ShortCode);
    }

    [Theory]
    [InlineData("Yetkin Didem", "1. Familiyasy, ady, atasynyn ady", "PFN")]
    [InlineData("TUR", "2. Rayatlygy", "PNAT")]
    [InlineData("18.01.1977", "3. Doglan senesi", "PDBT")]
    [InlineData("U36556957", "4. Pasportynyn belgisi", "PPN")]
    [InlineData("TUR", "5. Doglan yeri, yurdy", "PCBC")]
    [InlineData("Ayal", "6. Jynsy", "PGND")]
    [InlineData("Emek mahallesi gazi ali dusun caddesi", "7. Oy salgysy", "PFAC")]
    [InlineData("Turkmenistandaky sahamca mudirinin orunbasarynyn - gyzy", "8. Gelmeginin maksady", "RGEL")]
    [InlineData("Asgabat saherinin 1958-nji (Andalyp) kocesi jay-86", "9. Turkmenistanda bolyan yeri", "ADRS")]
    [InlineData("A1688318", "10. Wizanyn derejesi, gornusi we belgi", "VNUM")]
    [InlineData("Asgabat saher howa menzilindaki MGP", "11. Wizanyn berlen yeri (yurdy)", "VPLC")]
    [InlineData("20.01.2026", "12. Wizanyn berlen senesi we mohleti", "VISD")]
    [InlineData("20.01.2026", "13. Giren wagty", "TRDT")]
    [InlineData("Asgabat saher howa menzilindaki MGP", "14. Giren yeri", "TRCK")]
    [InlineData("Calik Enerji Sanayi we Tijaret", "15. Kabul edyan edara ya-da sahsyyet", "ACNAM")]
    public void Numbered_left_labels_rank_forma16_placeholders(string yellow, string nearby, string expected)
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            yellow, nearby, null, Forma16Set(), UserReportPlaceholderScope.Row);
        Assert.Equal(expected, ranked[0].ShortCode);
    }

    [Fact]
    public void Numbered_visa_row_is_left_label_form()
    {
        var kinds = ScanGuessingPatternRegistry.Detect(
            "20.01.2026-de",
            "12. Wizanyn berlen senesi we mohleti",
            null,
            ScanSourceKind.Word);
        Assert.Contains(kinds, k => k == ScanGuessingPatternKind.LeftLabelForm);
    }

    [Fact]
    public void StripLeadingItemNumber_drops_forma16_index()
    {
        Assert.Equal(
            "wizanyn berlen senesi we mohleti",
            ScanFormFieldLabelHints.StripLeadingItemNumber("12. wizanyn berlen senesi we mohleti"));
    }

    [Fact]
    public void Inline_director_title_maps_to_signatory_name()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Mudiri Mehmet Cirak",
            null,
            null,
            Set(),
            UserReportPlaceholderScope.Header);
        Assert.Equal("CHFN", ranked[0].ShortCode);
    }

    [Fact]
    public void Inline_employee_footer_maps_to_person_name()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Hilmi Erol",
            "Isgar",
            null,
            Set(),
            UserReportPlaceholderScope.Row);
        Assert.Equal("PFN", ranked[0].ShortCode);
    }

    [Fact]
    public void Inline_employer_footer_maps_to_signatory_name()
    {
        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "Mehmet Cirak",
            "Is beriji",
            null,
            Set(),
            UserReportPlaceholderScope.Header);
        Assert.Contains(ranked[0].ShortCode, new[] { "CHFN", "ACFNM" }, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Money_amount_maps_to_contract_salary_when_in_set()
    {
        var set = Set();
        if (!set.Contains("CSAL"))
            return;

        var ranked = ScanSurroundPlaceholderPattern.Rank(
            "1.667.00 USD",
            null,
            null,
            set,
            UserReportPlaceholderScope.Row);
        Assert.Equal("CSAL", ranked[0].ShortCode);
    }

    [Fact]
    public void Excel_profiles_match_purpose_and_inviting_party()
    {
        Assert.Equal("RGEL", ScanExcelColumnProfiles.Match("Gelmeginin maksady")!.ShortCodes[0]);
        Assert.Equal("ACNAM", ScanExcelColumnProfiles.Match("Cagyran Tarap")!.ShortCodes[0]);
    }

    [Fact]
    public void Official_letter_urgency_and_double_entry_map()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var urgency = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "Gyssagly tertipde!",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);
        Assert.Contains(urgency, d => d.ProposedToken == "{{ds.Urgency_NameTm}}");

        used.Clear();
        var category = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "iki gezeklik",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);
        Assert.Contains(category, d => d.ProposedToken == "{{ds.VCAT}}");
    }
}