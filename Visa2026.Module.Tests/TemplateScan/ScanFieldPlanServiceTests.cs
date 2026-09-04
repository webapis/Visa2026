#nullable enable

using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanFieldPlanServiceTests
{
    [Fact]
    public async Task BuildAsync_YellowWord_MapsWithoutVision()
    {
        var (_, _, ingest, fieldPlan) = ScanTestServiceFactory.Create();
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434", "Adaty tertipde!"),
            FileName = "letter.docx",
        });

        var plan = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingested,
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
        });

        Assert.Equal("office-yellow", plan.Source);
        Assert.Contains(plan.Fields, f => f.ProposedToken != null
            && TemplateTokenSyntax.TryGetShortCode(f.ProposedToken, out var c)
            && c.Equals("AFNUM", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildAsync_NonOffice_Throws()
    {
        var fieldPlan = new ScanFieldPlanService(
            new ScanFieldPlanMerger(),
            new ScanOfficeYellowExtractor(),
            new ScanAmbiguousYellowRefinementService(
                new NoneTemplateScanAiProvider(),
                Options.Create(new TemplateAiScanOptions())));

        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var ingest = new ScanIngestResult
        {
            Input = new ScanNormalizedInput
            {
                SourceKind = ScanSourceKind.Image,
                Pages = Array.Empty<ScanPageImage>(),
                OriginalByteLength = 10,
                FileName = "x.png",
            },
            Ocr = new ScanOcrResult { Lines = Array.Empty<ScanOcrLine>(), TextConfidence = 0 },
            Suitability = new ScanSuitabilityReport
            {
                Verdict = ScanSuitabilityVerdict.Pass,
                TextConfidence = 1,
                Issues = Array.Empty<ScanSuitabilityIssue>(),
            },
            Playbook = new ScanAuthoringPlaybook
            {
                Markdown = "x",
                Fingerprint = "x",
                VersionLabel = "x",
            },
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingest,
            PlaceholderSet = set,
        }));
    }
}