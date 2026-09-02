#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanFieldPlanOfficerOverrideTests
{
    [Fact]
    public void ApplyToken_swaps_placeholder_and_keeps_span()
    {
        var set = HeaderSet();
        var span = new DocumentRegion.WordSpan("body/2", 0, 8);
        var plan = Plan(set, "{{ds.ADAT}}", span);

        var next = ScanFieldPlanOfficerOverride.ApplyToken(plan, "f1", "ACRDT");

        Assert.NotSame(plan, next);
        var field = Assert.Single(next.Fields);
        Assert.Equal("{{ds.ACRDT}}", field.ProposedToken);
        Assert.Equal(ScanFieldConfidence.High, field.Confidence);
        Assert.Same(span, field.SourceRegion);
        Assert.Equal("officer", next.Source);
    }

    [Fact]
    public void ApplyToken_unknown_code_is_noop()
    {
        var set = HeaderSet();
        var plan = Plan(set, "{{ds.ADAT}}", null);

        var next = ScanFieldPlanOfficerOverride.ApplyToken(plan, "f1", "NOTACODE");

        Assert.Same(plan, next);
    }

    [Fact]
    public void ApplyToken_empty_code_unmaps()
    {
        var set = HeaderSet();
        var plan = Plan(set, "{{ds.ADAT}}", new DocumentRegion.WordSpan("body/0", 0, 1));

        var next = ScanFieldPlanOfficerOverride.ApplyToken(plan, "f1", string.Empty);

        var field = Assert.Single(next.Fields);
        Assert.Null(field.ProposedToken);
        Assert.Equal(ScanFieldConfidence.Low, field.Confidence);
        Assert.NotNull(field.SourceRegion);
    }

    [Fact]
    public void FormatChatContext_includes_mark_label_and_current_token()
    {
        var set = HeaderSet();
        var plan = Plan(set, "{{ds.ADAT}}", new DocumentRegion.WordSpan("body/0", 0, 10));
        var fieldId = Assert.Single(plan.Fields).FieldId;

        var text = ScanFieldPlanOfficerOverride.FormatChatContext(plan, fieldId, 2);

        Assert.Contains("#2", text, StringComparison.Ordinal);
        Assert.Contains("02.02.2009", text, StringComparison.Ordinal);
        Assert.Contains("ADAT", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatOfficerHint_is_short_and_mentions_the_mark()
    {
        var set = HeaderSet();
        var plan = Plan(set, "{{ds.ADAT}}", new DocumentRegion.WordSpan("body/0", 0, 10));
        var fieldId = Assert.Single(plan.Fields).FieldId;

        var text = ScanFieldPlanOfficerOverride.FormatOfficerHint(plan, fieldId, 2);

        Assert.Contains("Mark #2", text, StringComparison.Ordinal);
        Assert.Contains("02.02.2009", text, StringComparison.Ordinal);
        Assert.Contains("ADAT", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Remap only this mark", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyPartCodes_swaps_one_compound_part_and_keeps_siblings()
    {
        var set = HeaderSet();
        var plan = Plan(
            set,
            "{{ds.CHPN}}, {{ds.CHPA}}, {{ds.ADAT}}",
            new DocumentRegion.WordSpan("body/4", 0, 40),
            "A0123456, Ankara, 28.05.2024");
        var ordered = ScanReviewFieldOrder.Order(plan.Fields);
        Assert.Equal(3, ordered.Count);
        var datePart = ordered[2];
        Assert.Equal("ADAT", TemplateTokenSyntax.GetShortCodes(datePart.ProposedToken).Single());

        var next = ScanFieldPlanOfficerOverride.ApplyPartCodes(plan, datePart.DisplayId, ["CHPD"]);

        var field = Assert.Single(next.Fields);
        Assert.Equal(["CHPN", "CHPA", "CHPD"], TemplateTokenSyntax.GetShortCodes(field.ProposedToken));
    }

    [Fact]
    public void ApplyTokens_joins_two_header_codes_with_comma_from_label()
    {
        var set = HeaderSet();
        var span = new DocumentRegion.WordSpan("body/3", 0, 40);
        var plan = Plan(set, "{{ds.ADAT}}", span, "02.02.2009, 20.01.2026");
        var fieldId = Assert.Single(plan.Fields).FieldId;

        var next = ScanFieldPlanOfficerOverride.ApplyTokens(plan, fieldId, ["ACRDT", "ADAT"]);

        var field = Assert.Single(next.Fields);
        Assert.Equal("{{ds.ACRDT}}, {{ds.ADAT}}", field.ProposedToken);
        Assert.Same(span, field.SourceRegion);
        Assert.Equal(["ACRDT", "ADAT"], TemplateTokenSyntax.GetShortCodes(field.ProposedToken));
    }

    [Fact]
    public void GetShortCodes_reads_compound_token()
    {
        var codes = TemplateTokenSyntax.GetShortCodes("{{ds.RPPN}}, {{ds.RPPA}} {{ds.RPPH}}");
        Assert.Equal(["RPPN", "RPPA", "RPPH"], codes);
    }

    [Fact]
    public void RemoveReviewRow_drops_a_simple_field()
    {
        var set = HeaderSet();
        var plan = Plan(set, "{{ds.ADAT}}", null);
        var fieldId = Assert.Single(plan.Fields).FieldId;

        var next = ScanFieldPlanOfficerOverride.RemoveReviewRow(plan, fieldId);

        Assert.Empty(next.Fields);
        Assert.Equal("officer", next.Source);
    }

    [Fact]
    public void RemoveReviewRow_hides_compound_part_and_keeps_remaining_token()
    {
        var set = HeaderSet();
        var plan = Plan(set, "{{ds.ADAT}}", null, "02.02.2009, 20.01.2026");
        var ordered = ScanReviewFieldOrder.Order(plan.Fields);
        Assert.Equal(2, ordered.Count);
        var second = ordered[1];

        var next = ScanFieldPlanOfficerOverride.RemoveReviewRow(plan, second.DisplayId);

        var field = Assert.Single(next.Fields);
        Assert.Contains(2, field.HiddenPartIndexes);
        Assert.Equal("{{ds.ADAT}}", field.ProposedToken);
        var shown = ScanReviewFieldOrder.Order(next.Fields);
        Assert.Single(shown);
        Assert.DoesNotContain(shown, m => string.Equals(m.DisplayId, second.DisplayId, StringComparison.Ordinal));
    }

    [Fact]
    public void RemoveReviewRow_last_visible_part_drops_the_mark()
    {
        var set = HeaderSet();
        var plan = Plan(set, "{{ds.ADAT}}", null, "02.02.2009, 20.01.2026");
        var ordered = ScanReviewFieldOrder.Order(plan.Fields);
        var hidden = ScanFieldPlanOfficerOverride.RemoveReviewRow(plan, ordered[1].DisplayId);
        var next = ScanFieldPlanOfficerOverride.RemoveReviewRow(hidden, ScanReviewFieldOrder.Order(hidden.Fields)[0].DisplayId);

        Assert.Empty(next.Fields);
    }

    [Fact]
    public void ApplyToken_row_only_code_on_header_yellow_writes_row_token()
    {
        var set = BothSet();
        var plan = Plan(set, "{{ds.ADAT}}", new DocumentRegion.WordSpan("body/1", 0, 8));

        var next = ScanFieldPlanOfficerOverride.ApplyToken(plan, "f1", "PVFM");

        var field = Assert.Single(next.Fields);
        Assert.Equal("{{.PVFM}}", field.ProposedToken);
    }

    [Fact]
    public void ApplyTokens_row_codes_on_header_yellow_stay_row_shaped()
    {
        var set = BothSet();
        var plan = Plan(set, "{{ds.ADAT}}", new DocumentRegion.WordSpan("body/1", 0, 40), "18.01.1977, Turkiye, Gaziantep");
        var fieldId = Assert.Single(plan.Fields).FieldId;

        var next = ScanFieldPlanOfficerOverride.ApplyTokens(plan, fieldId, ["PDBT", "PCBT", "PBPL"]);

        Assert.Equal("{{.PDBT}}, {{.PCBT}}, {{.PBPL}}", Assert.Single(next.Fields).ProposedToken);
    }

    [Fact]
    public void Merge_rewrites_header_shaped_row_tokens()
    {
        var field = Assert.Single(Plan(BothSet(), "{{ds.PVFM}}", null, "family block").Fields);
        Assert.Equal("{{.PVFM}}", field.ProposedToken);
    }

    [Fact]
    public void RemoveReviewRow_drops_a_gap()
    {
        var set = HeaderSet();
        var plan = Plan(set, "{{ds.ADAT}}", null);
        var withGap = new ScanFieldPlan
        {
            PlaceholderSet = plan.PlaceholderSet,
            ScanKind = plan.ScanKind,
            Fields = plan.Fields,
            StaticRegions = plan.StaticRegions,
            Gaps = [new ScanGap("g1", "unmapped yellow", null)],
            PendingQuestions = plan.PendingQuestions,
            Source = plan.Source,
            YellowHighlightCount = plan.YellowHighlightCount,
        };

        var next = ScanFieldPlanOfficerOverride.RemoveReviewRow(withGap, "g1");

        Assert.Single(next.Fields);
        Assert.Empty(next.Gaps);
    }

    private static ApplicationProfilePlaceholderSet HeaderSet() =>
        Set(ApplicationProfileTemplateDataScope.ApplicationHeader);

    private static ApplicationProfilePlaceholderSet BothSet() =>
        Set(ApplicationProfileTemplateDataScope.Both);

    private static ApplicationProfilePlaceholderSet Set(ApplicationProfileTemplateDataScope dataScope)
    {
        var catalog = new UserReportPlaceholderCatalogService();
        return new ApplicationProfilePlaceholderSetService(catalog).GetSet(new ApplicationProfilePlaceholderSetQuery
        {
            Profile = new ApplicationProfile(),
            DataScope = dataScope,
            TemplateKind = ApplicationProfileTemplateKind.Word,
        });
    }

    private static ScanFieldPlan Plan(
        ApplicationProfilePlaceholderSet set,
        string token,
        DocumentRegion? region,
        string labelText = "02.02.2009") =>
        new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = new ScanFieldPlanProposal
            {
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "f1",
                        PageIndex = 0,
                        Box = ScanBoundingBox.FullPage,
                        LabelText = labelText,
                        ProposedToken = token,
                        Confidence = ScanFieldConfidence.Medium,
                        Scope = ScanFieldScope.Header,
                        SourceRegion = region,
                    }
                ],
            },
        });
}