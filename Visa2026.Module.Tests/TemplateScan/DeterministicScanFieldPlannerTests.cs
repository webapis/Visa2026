#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class DeterministicScanFieldPlannerTests
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
            },
            DataScope = ApplicationProfileTemplateDataScope.Both,
            TemplateKind = ApplicationProfileTemplateKind.Word,
        });
    }

    private static ScanFieldPlanRequest SampleRequest(ApplicationProfilePlaceholderSet set, params ScanOcrLine[] lines) =>
        new()
        {
            ScanKind = ScanKind.BlankForm,
            Playbook = new ScanAuthoringPlaybook { Markdown = "test", Fingerprint = "abc", VersionLabel = "abc" },
            PlaceholderSet = set,
            Pages =
            [
                new ScanFieldPlanPagePayload
                {
                    PageIndex = 0,
                    PngBytes = ScanTestImageFactory.CreatePngWithDimensions(800, 1200),
                    WidthPx = 800,
                    HeightPx = 1200,
                },
            ],
            OcrLines = lines,
        };

    [Fact]
    public void Build_MatchesCatalogLabelToToken()
    {
        var set = FullSet();
        var request = SampleRequest(set, new ScanOcrLine { PageIndex = 0, Text = "Full application number", Confidence = 0.9 });

        var proposal = DeterministicScanFieldPlanner.Build(request);

        Assert.Contains(proposal.Fields, f => f.ProposedToken == "{{ds.AFNUM}}");
    }

    [Fact]
    public void Build_ValueLikeLineWithoutMatch_BecomesGap()
    {
        var set = FullSet();
        var request = SampleRequest(set, new ScanOcrLine { PageIndex = 0, Text = "28.04.2026", Confidence = 0.9 });

        var proposal = DeterministicScanFieldPlanner.Build(request);

        Assert.Empty(proposal.Fields);
        Assert.NotEmpty(proposal.Gaps);
    }

    [Fact]
    public void ScoreMatch_PrefersContainedLabel()
    {
        var score = DeterministicScanFieldPlanner.ScoreMatch(
            "full application number",
            "full application number");

        Assert.True(score >= 60);
    }
}
