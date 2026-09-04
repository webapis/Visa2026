#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanCompoundYellowTests
{
    private static ApplicationProfilePlaceholderSet Set() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Fact]
    public void Comma_in_yellow_is_a_combination_candidate()
    {
        Assert.True(ScanCompoundYellowParts.IsCommaCombination(
            "U37109249, T.C. ASKABAT BE, 19.02.2024"));
        Assert.False(ScanCompoundYellowParts.IsCommaCombination("Hilmi Erol 16.05.1980"));
        Assert.False(ScanCompoundYellowParts.IsCommaCombination("1,5"));
    }

    [Fact]
    public void Resolver_does_not_split_a_comma_highlight_into_independent_marks()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "U37109249, T.C. ASKABAT BE, 19.02.2024y.",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);

        Assert.Empty(drafts);
    }

    private static ApplicationProfilePlaceholderSet EducationSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile { RequirePersonEducation = true },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Fact]
    public void Label_bilimi_identifies_education_group()
    {
        Assert.Equal(
            UserReportPlaceholderRelatedBo.Education,
            ScanCompoundLabelGroup.Identify("Bilimi", null, EducationSet()));
    }

    [Fact]
    public void Binder_maps_education_comma_line_inside_label_group()
    {
        var bound = ScanCompoundYellowBinder.TryBind(
            "Yokary, TUR, Gundogar mediterian uniwersiteti",
            EducationSet(),
            UserReportPlaceholderScope.Row,
            "Bilimi");

        Assert.NotNull(bound);
        var codes = TemplateTokenSyntax.GetShortCodes(bound.Value.Token);
        Assert.Equal(["EGLV", "EGCC", "EGIN"], codes);
        Assert.DoesNotContain("PNAT", codes);
    }

    [Fact]
    public void Binder_maps_education_comma_line_with_turkmen_spelling()
    {
        var bound = ScanCompoundYellowBinder.TryBind(
            "Ýokary, TUR, Gündogar mediterıan uniwersiteti",
            EducationSet(),
            UserReportPlaceholderScope.Row,
            "Bilimi");

        Assert.NotNull(bound);
        Assert.Equal(
            ["EGLV", "EGCC", "EGIN"],
            TemplateTokenSyntax.GetShortCodes(bound.Value.Token));
    }

    [Fact]
    public void Caption_slots_come_from_the_parenthetical_under_the_line()
    {
        var slots = ScanFormCaptionHints.Slots(
            "pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)");
        Assert.Equal(3, slots.Count);
        Assert.Contains("mohleti", slots[2], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Binder_maps_passport_comma_line_using_form_caption()
    {
        var bound = ScanCompoundYellowBinder.TryBind(
            "U37109249, T.C. ASKABAT BE, 19.02.2024",
            Set(),
            UserReportPlaceholderScope.Row,
            "pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)");

        Assert.NotNull(bound);
        var codes = TemplateTokenSyntax.GetShortCodes(bound.Value.Token);
        Assert.Equal(["PPN", "PPAT", "PPED"], codes);
    }

    [Fact]
    public void Binder_maps_signatory_passport_comma_line_expiration_to_CHPE()
    {
        var bound = ScanCompoundYellowBinder.TryBind(
            "U57105240, T.G. ASGABAT SR, 19.02.2034",
            Set(),
            UserReportPlaceholderScope.Header,
            "yolbascy pasporty: (pasportyn seriyasy we belgisi, nirede we hacan berildi, mohleti)");

        Assert.NotNull(bound);
        var codes = TemplateTokenSyntax.GetShortCodes(bound.Value.Token);
        Assert.Equal(["CHPN", "CHPA", "CHPE"], codes);
        Assert.DoesNotContain("CHPD", codes);
        Assert.DoesNotContain("PPED", codes);
    }

    [Fact]
    public void Binder_maps_wekil_passport_comma_line_to_representative_codes()
    {
        var bound = ScanCompoundYellowBinder.TryBind(
            "I-AS 476479 Asgabat s., Berkararlyk etr. Hakimligi tarapyndan berlen, +993 65 56-13-49",
            Set(),
            UserReportPlaceholderScope.Header,
            "ygtyyarly wekili pasporty: (pasportyn seriyasy, belgisi, nirede we hacan berildi, telefonyn belgisi)");

        Assert.NotNull(bound);
        var codes = TemplateTokenSyntax.GetShortCodes(bound.Value.Token);
        Assert.Contains("RPPN", codes);
        Assert.Contains("RPPH", codes);
        Assert.DoesNotContain("PPN", codes);
    }

    [Fact]
    public void Binder_maps_company_registry_comma_line_from_caption()
    {
        var bound = ScanCompoundYellowBinder.TryBind(
            "No263407090, 02.02.2009y., Asgabat s., Bitarap Turkmenistan sayoly 538, +993 12 75-57-58",
            Set(),
            UserReportPlaceholderScope.Header,
            "karhana (hasaba alnan belgisi, senesi, yuridiki salgysy, telefon belgisi)");

        Assert.NotNull(bound);
        var codes = TemplateTokenSyntax.GetShortCodes(bound.Value.Token);
        Assert.Contains("ACRDT", codes);
        Assert.Contains("ACPHN", codes);
        Assert.Contains("ACADR", codes);
    }

    [Fact]
    public void Label_ise_cagrylan_adam_identifies_person_group()
    {
        Assert.Equal(
            UserReportPlaceholderRelatedBo.Person,
            ScanCompoundLabelGroup.Identify(
                "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)",
                null,
                Set()));
    }

    [Theory]
    [InlineData("Aynabat Meredowa, 03.04.1991")]
    [InlineData("Gurban Annayew, 12.01.1985y.")]
    public void Binder_maps_hired_person_name_and_dob_from_left_label_and_caption(string yellow)
    {
        var bound = ScanCompoundYellowBinder.TryBind(
            yellow,
            Set(),
            UserReportPlaceholderScope.Row,
            "Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)");

        Assert.NotNull(bound);
        var codes = TemplateTokenSyntax.GetShortCodes(bound.Value.Token);
        Assert.Equal(["PFN", "PDBT"], codes);
        Assert.DoesNotContain("ACADR", codes);
        Assert.DoesNotContain("ACRDT", codes);
        Assert.DoesNotContain("PFNM", codes);
    }

    [Fact]
    public void Binder_maps_hired_person_even_when_nearby_also_mentions_company()
    {
        var bound = ScanCompoundYellowBinder.TryBind(
            "Meret Hydyrow, 21.08.1977",
            Set(),
            UserReportPlaceholderScope.Row,
            "karhana (hasaba alnan belgisi, senesi, yuridiki salgysy) Ise cagrylan adam: (ady, familiyasy, atasynyn ady, doglan senesi)");

        Assert.NotNull(bound);
        Assert.Equal(
            ["PFN", "PDBT"],
            TemplateTokenSyntax.GetShortCodes(bound.Value.Token));
    }

    [Fact]
    public void Review_expands_comma_highlight_to_sub_rows_even_with_one_token()
    {
        var field = new ScanDetectedField
        {
            FieldId = "mark6",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "U37109249, T.C. ASKABAT BE, 19.02.2024",
            ProposedToken = "{{.PPN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = new DocumentRegion.WordSpan("body/6", 0, 40),
        };

        var ordered = ScanReviewFieldOrder.Order([field]);
        Assert.Equal(3, ordered.Count);
        Assert.Equal(["1.1", "1.2", "1.3"], ordered.Select(o => o.DisplayOrder).ToArray());
        Assert.Equal("U37109249", ordered[0].LabelText);
        Assert.Equal("T.C. ASKABAT BE", ordered[1].LabelText);
        Assert.Equal("19.02.2024", ordered[2].LabelText);
        Assert.Equal("mark6", ScanReviewFieldOrder.ParentFieldId(ordered[1].DisplayId));
    }

    [Fact]
    public void Review_hides_dismissed_compound_parts()
    {
        var field = new ScanDetectedField
        {
            FieldId = "mark6",
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "U37109249, T.C. ASKABAT BE, 19.02.2024",
            ProposedToken = "{{.PPN}}",
            Confidence = ScanFieldConfidence.High,
            Scope = ScanFieldScope.Row,
            SourceRegion = new DocumentRegion.WordSpan("body/6", 0, 40),
            HiddenPartIndexes = [2],
        };

        var ordered = ScanReviewFieldOrder.Order([field]);
        Assert.Equal(2, ordered.Count);
        Assert.Equal(["1.1", "1.3"], ordered.Select(o => o.DisplayOrder).ToArray());
        Assert.DoesNotContain(ordered, o => o.LabelText.Contains("ASKABAT", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyTokens_accepts_overlay_id()
    {
        var set = Set();
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = new ScanFieldPlanProposal
            {
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "mark6",
                        PageIndex = 0,
                        Box = ScanBoundingBox.FullPage,
                        LabelText = "U37109249, T.C. ASKABAT BE, 19.02.2024",
                        ProposedToken = "{{.PPN}}",
                        Confidence = ScanFieldConfidence.Medium,
                        Scope = ScanFieldScope.Row,
                    }
                ],
            },
        });

        var next = ScanFieldPlanOfficerOverride.ApplyTokens(plan, "mark6:2", ["PPN", "PPAT", "PPED"]);
        var field = Assert.Single(next.Fields);
        Assert.Equal(["PPN", "PPAT", "PPED"], TemplateTokenSyntax.GetShortCodes(field.ProposedToken));
    }
}