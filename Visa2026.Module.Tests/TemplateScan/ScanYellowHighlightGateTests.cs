#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanYellowHighlightGateTests
{
    private static ScanFieldPlan Plan(bool mapped, int yellow, int gaps = 0)
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var fields = mapped
            ?
            [
                new ScanDetectedField
                {
                    FieldId = "1",
                    PageIndex = 0,
                    LabelText = "4/-434",
                    ProposedToken = "{{ds.AFNUM}}",
                    Confidence = ScanFieldConfidence.High,
                    Scope = ScanFieldScope.Header,
                    Box = ScanBoundingBox.FullPage,
                },
            ]
            : Array.Empty<ScanDetectedField>();

        var gapList = Enumerable.Range(0, gaps)
            .Select(i => new ScanGap(i.ToString(), "unmapped " + i, null))
            .ToList();

        return new ScanFieldPlan
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Fields = fields,
            StaticRegions = Array.Empty<ScanStaticRegion>(),
            Gaps = gapList,
            PendingQuestions = Array.Empty<ScanClarificationPrompt>(),
            YellowHighlightCount = yellow,
            Source = "test",
        };
    }

    private static ScanSuitabilityReport PriorPass() => new()
    {
        Verdict = ScanSuitabilityVerdict.Pass,
        TextConfidence = 0.9,
        Issues = Array.Empty<ScanSuitabilityIssue>(),
    };

    [Fact]
    public void Apply_NoYellow_Fails()
    {
        var report = ScanYellowHighlightGate.Apply(PriorPass(), yellowHighlightCount: 0, Plan(mapped: false, yellow: 0));
        Assert.Equal(ScanSuitabilityVerdict.Fail, report.Verdict);
        Assert.Contains(report.Issues, i => i.Code == ScanSuitabilityIssueCode.NoYellowHighlights);
    }

    [Fact]
    public void Apply_YellowButUnmapped_Fails()
    {
        var report = ScanYellowHighlightGate.Apply(PriorPass(), yellowHighlightCount: 3, Plan(mapped: false, yellow: 3));
        Assert.Equal(ScanSuitabilityVerdict.Fail, report.Verdict);
        Assert.Contains(report.Issues, i => i.Code == ScanSuitabilityIssueCode.YellowHighlightsUnmapped);
    }

    [Fact]
    public void Apply_YellowMapped_Passes()
    {
        var report = ScanYellowHighlightGate.Apply(PriorPass(), yellowHighlightCount: 2, Plan(mapped: true, yellow: 2));
        Assert.Equal(ScanSuitabilityVerdict.Pass, report.Verdict);
    }

    [Fact]
    public void Catalog_Contains_Urgency_NameTm()
    {
        var entries = new UserReportPlaceholderCatalogService().GetEntries();
        Assert.Contains(entries, e => string.Equals(e.ShortCode, "Urgency_NameTm", StringComparison.OrdinalIgnoreCase));
    }
}