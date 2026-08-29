#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanYellowHighlightTokenResolverTests
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
    public void Resolve_HeaderNumberAndDate_MapsAfnumAndAdat()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "№ 4/-434 28.04.2026 ý.",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.AFNUM}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.ADAT}}");
    }

    [Fact]
    public void Resolve_CountAndVisaPhrase_MapsCountPeriodCategory()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "18 (on sekiz) 6 (alty) aý köp gezeklik",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCNT}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCTX}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.VPER}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.VCAT}}");
    }

    [Fact]
    public void Merge_UnmappedCompoundYellow_FillsLibraryTokens()
    {
        var set = Set();
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = new ScanFieldPlanProposal
            {
                YellowHighlightCount = 2,
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "a",
                        PageIndex = 0,
                        LabelText = "№ 4/-434 28.04.2026 ý.",
                        ProposedToken = null,
                        Confidence = ScanFieldConfidence.Low,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "b",
                        PageIndex = 0,
                        LabelText = "18 (on sekiz) 6 (alty) aý köp gezeklik",
                        ProposedToken = null,
                        Confidence = ScanFieldConfidence.Low,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "c",
                        PageIndex = 0,
                        LabelText = "Adaty tertipde!",
                        ProposedToken = "{{ds.Urgency_NameTm}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

        Assert.True(plan.HasMappedFields);
        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.AFNUM}}");
        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.ADAT}}");
        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.TPCNT}}");
        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.VPER}}");
        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.VCAT}}");
        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.Urgency_NameTm}}");
        Assert.Empty(plan.Gaps);
    }

    [Fact]
    public void Merge_DuplicateCompoundAfterSplit_DropsGapAndAddsAdat()
    {
        var set = Set();
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = new ScanFieldPlanProposal
            {
                YellowHighlightCount = 3,
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "1",
                        PageIndex = 0,
                        LabelText = "18",
                        ProposedToken = "{{ds.TPCNT}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "2",
                        PageIndex = 0,
                        LabelText = "on sekiz",
                        ProposedToken = "{{ds.TPCTX}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "3",
                        PageIndex = 0,
                        LabelText = "6 (alty) aý",
                        ProposedToken = "{{ds.VPER}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "4",
                        PageIndex = 0,
                        LabelText = "köp gezeklik",
                        ProposedToken = "{{ds.VCAT}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "5",
                        PageIndex = 0,
                        LabelText = "Adaty tertipde!",
                        ProposedToken = "{{ds.Urgency_NameTm}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "6",
                        PageIndex = 0,
                        LabelText = "№ 4/-434 28.04.2026 ý.",
                        ProposedToken = "{{ds.AFNUM}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "7",
                        PageIndex = 0,
                        LabelText = "18 (on sekiz) 6 (alty) aý köp gezeklik",
                        ProposedToken = null,
                        Confidence = ScanFieldConfidence.Low,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.AFNUM}}" && f.LabelText.Contains("4/-434", StringComparison.Ordinal));
        Assert.Contains(plan.Fields, f => f.ProposedToken == "{{ds.ADAT}}");
        Assert.Empty(plan.Gaps);
    }
}
