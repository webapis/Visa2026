#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanFieldPlanMergerTests
{
    private static ApplicationProfilePlaceholderSet FullSet() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Fact]
    public void Merge_DropsInventedToken_AsGap()
    {
        var set = FullSet();
        var merger = new ScanFieldPlanMerger();
        var plan = merger.Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.BlankForm,
            Proposal = new ScanFieldPlanProposal
            {
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "f1",
                        Box = ScanBoundingBox.FullPage,
                        PageIndex = 0,
                        LabelText = "Mystery",
                        ProposedToken = "{{ds.NOT_A_REAL_TOKEN}}",
                        Confidence = ScanFieldConfidence.Medium,
                        Scope = ScanFieldScope.Header,
                    },
                ],
                Source = "test",
            },
        });

        Assert.Empty(plan.Fields);
        Assert.Contains(plan.Gaps, g => g.LabelText == "Mystery");
    }

    [Fact]
    public void Merge_AllowsKnownToken()
    {
        var set = FullSet();
        var merger = new ScanFieldPlanMerger();
        var plan = merger.Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.BlankForm,
            Proposal = new ScanFieldPlanProposal
            {
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "f1",
                        Box = ScanBoundingBox.FullPage,
                        PageIndex = 0,
                        LabelText = "Application number",
                        ProposedToken = "{{ds.AFNUM}}",
                        Confidence = ScanFieldConfidence.Medium,
                        Scope = ScanFieldScope.Header,
                    },
                ],
                Source = "test",
            },
        });

        Assert.Single(plan.Fields);
        Assert.Equal("{{ds.AFNUM}}", plan.Fields[0].ProposedToken);
    }
}
