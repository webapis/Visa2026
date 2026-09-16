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
    public void Resolve_CancelVisaLetter_MapsPersonThenVisaCount()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "sanawdaky 1 (bir) sany daşary ýurt raýatynyň 1 (bir) sany wizasyny ýatyrmagyňyzy",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCNT}}" && d.LabelText == "1");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCTX}}" && d.LabelText == "bir");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CVCNT}}" && d.LabelText == "1");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CVCTX}}" && d.LabelText == "bir");
    }

    [Fact]
    public void Resolve_IsolatedCount_UsesNearbyVisaCancelPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "1 (bir)",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "sany wizasyny ýatyrmagyňyzy haýyş edýäris");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CVCNT}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CVCTX}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.TPCNT}}");
    }

    [Fact]
    public void Resolve_IsolatedCount_UsesNearbyPersonPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "1 (bir)",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "sany daşary ýurt raýatynyň");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCNT}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCTX}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CVCNT}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CWCNT}}");
    }

    [Fact]
    public void Resolve_CancelVisaAndWorkPermitLetter_MapsPersonVisaThenWorkPermitCount()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "sanawdaky 1 (bir) sany daşary ýurt raýatynyň 1 (bir) sany wizasyny we 1 (bir) sany iş rugsatnamasyny ýatyrmagyňyzy",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCNT}}" && d.LabelText == "1");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCTX}}" && d.LabelText == "bir");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CVCNT}}" && d.LabelText == "1");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CVCTX}}" && d.LabelText == "bir");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CWCNT}}" && d.LabelText == "1");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CWCTX}}" && d.LabelText == "bir");
    }

    [Fact]
    public void Resolve_IsolatedCount_UsesNearbyWorkPermitCancelPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "1 (bir)",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "sany iş rugsatnamasyny ýatyrmagyňyzy haýyş edýäris");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CWCNT}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CWCTX}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.TPCNT}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CVCNT}}");
    }

    [Fact]
    public void Resolve_IsolatedDigit_UsesNearbyWorkPermitCancelPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "3",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "(üç) sany iş rugsatnamasyny ýatyrmagyňyzy");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CWCNT}}" && d.LabelText == "3");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CWCTX}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.TPCNT}}");
    }

    [Fact]
    public void Resolve_IsolatedTurkmenWords_UsesNearbyWorkPermitCancelPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "üç",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "sany iş rugsatnamasyny ýatyrmagyňyzy");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CWCTX}}" && d.LabelText == "üç");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CWCNT}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.TPCTX}}");
    }

    [Fact]
    public void Resolve_CancelWorkPermitAndInvitationLetter_MapsPersonWpThenInvitationCount()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "sanawdaky 3 (üç) sany daşary ýurt raýatynyň 3 (üç) sany işlemek üçin rugsatnamasyny we 3 (üç) sany çakylygyny ýatyrmagyňyzy",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCNT}}" && d.LabelText == "3");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCTX}}" && d.LabelText == "üç");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CWCNT}}" && d.LabelText == "3");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CWCTX}}" && d.LabelText == "üç");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CICNT}}" && d.LabelText == "3");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CICTX}}" && d.LabelText == "üç");
    }

    [Fact]
    public void Resolve_IsolatedCount_UsesNearbyInvitationCancelPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "3 (üç)",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "sany çakylygyny ýatyrmagyňyzy haýyş edýäris");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CICNT}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CICTX}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.TPCNT}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CWCNT}}");
    }

    [Fact]
    public void Resolve_IsolatedDigit_UsesNearbyInvitationCancelPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "3",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "(üç) sany çakylygyny ýatyrmagyňyzy");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CICNT}}" && d.LabelText == "3");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CICTX}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CWCNT}}");
    }

    [Fact]
    public void Resolve_IsolatedTurkmenWords_UsesNearbyInvitationCancelPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "üç",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "sany çakylygyny ýatyrmagyňyzy");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.CICTX}}" && d.LabelText == "üç");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CICNT}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.CWCTX}}");
    }

    [Fact]
    public void Resolve_BusinessTripLetter_MapsPersonThenDurationCount()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "sanawdaky 1 (bir) sany daşary ýurt raýatynyň 12.02.2026-den 13.02.2026-ne çenli 2 (iki) gün möhlet",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCNT}}" && d.LabelText == "1");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.TPCTX}}" && d.LabelText == "bir");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTDCNT}}" && d.LabelText == "2");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTDCTX}}" && d.LabelText == "iki");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTSD}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTED}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.ADAT}}");
    }

    [Fact]
    public void Resolve_IsolatedCount_UsesNearbyDurationPhrase()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "2 (iki)",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "gün möhlet bilen");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTDCNT}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTDCTX}}");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.TPCNT}}");
    }

    [Fact]
    public void Resolve_IsolatedDurationDigit_AfterPersonCount_MapsBtdcnt()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TPCNT", "TPCTX" };
        var nearby =
            "sanawdaky 1 (bir) sany daşary ýurt raýatynyň 12.02.2026-den 13.02.2026-ne çenli (iki) gün möhlet bilen";
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "2",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: nearby);

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTDCNT}}" && d.LabelText == "2");
        Assert.DoesNotContain(drafts, d => d.ProposedToken == "{{ds.TPCNT}}");
    }

    [Fact]
    public void ClipImmediateCountContext_KeepsPersonPairOffLaterDuration()
    {
        var following =
            "(bir) sany daşary ýurt raýatynyň 12.02.2026-den 13.02.2026-ne çenli 2 (iki) gün möhlet bilen";
        var clipped = ScanOfficialLetterHints.ClipImmediateCountContext(following);
        Assert.Contains("raýat", clipped, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gün", clipped, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("iki", clipped, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClipImmediateCountContext_KeepsDurationPair()
    {
        var following = "(iki) gün möhlet bilen";
        var clipped = ScanOfficialLetterHints.ClipImmediateCountContext(following);
        Assert.Contains("gün", clipped, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("iki", clipped, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Merge_IsolatedDurationDigit_IsNotSwallowedAsPersonCount()
    {
        var set = Set();
        var nearby =
            "sanawdaky 1 (bir) sany daşary ýurt raýatynyň 12.02.2026-den 13.02.2026-ne çenli (iki) gün möhlet bilen";
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
                        FieldId = "person-1",
                        Box = ScanBoundingBox.FullPage,
                        PageIndex = 0,
                        LabelText = "1",
                        ProposedToken = "{{ds.TPCNT}}",
                        SourceRegion = new DocumentRegion.WordSpan("w:p[1]", 0, 1),
                    },
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "duration-2",
                        Box = ScanBoundingBox.FullPage,
                        PageIndex = 0,
                        LabelText = "2",
                        ProposedToken = null,
                        NearbyLabel = nearby,
                        SourceRegion = new DocumentRegion.WordSpan("w:p[1]", 40, 1),
                    },
                ],
            },
        });

        Assert.Contains(plan.Fields, f => f.LabelText == "1" && f.ProposedToken == "{{ds.TPCNT}}");
        Assert.Contains(plan.Fields, f => f.LabelText == "2" && f.ProposedToken == "{{ds.BTDCNT}}");
    }

    [Fact]
    public void Resolve_HeaderDateThenTripDates_KeepsAdatThenStartEnd()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var header = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "11.02.2026 ý.",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);
        Assert.Contains(header, d => d.ProposedToken == "{{ds.ADAT}}");

        var start = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "12.02.2026",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "-den");
        Assert.Contains(start, d => d.ProposedToken == "{{ds.BTSD}}");

        var end = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "13.02.2026",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "-ne çenli");
        Assert.Contains(end, d => d.ProposedToken == "{{ds.BTED}}");
    }

    [Fact]
    public void Resolve_FromAndToRegionCity_MapsFourTokens()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "Mary welaýatynyň Mary etrabyndan Ahal welaýatynyň Akbugdaý etrabyna",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used);

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTFRG}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTFCT}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTTRG}}");
        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTTCT}}");
    }

    [Fact]
    public void Resolve_PurposeParagraph_MapsBtprp()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var drafts = ScanYellowHighlightTokenResolver.ResolveFromYellowText(
            "Aşgabat şäherinde ýerleşýän Türkiye respublikasynyň ilçihanasyna resmi işleri ýerine ýetirmek.",
            ScanBoundingBox.FullPage,
            0,
            Set(),
            used,
            nearbyLabel: "Maksady:");

        Assert.Contains(drafts, d => d.ProposedToken == "{{ds.BTPRP}}");
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
