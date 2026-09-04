#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanAmbiguousYellowRefinementServiceTests
{
    [Fact]
    public async Task RefineAsync_skips_when_ai_disabled()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Excel,
            });

        var proposal = new ScanFieldPlanProposal
        {
            Fields =
            [
                new ScanDetectedFieldDraft
                {
                    FieldId = "g1",
                    Box = ScanBoundingBox.FullPage,
                    PageIndex = 0,
                    LabelText = "Erol",
                    ProposedToken = null,
                    Confidence = ScanFieldConfidence.Low,
                    Scope = ScanFieldScope.Row,
                },
            ],
            Source = "excel-manual-inference",
        };

        var service = new ScanAmbiguousYellowRefinementService(
            new NoneTemplateScanAiProvider(),
            Microsoft.Extensions.Options.Options.Create(new TemplateAiScanOptions { Enabled = true }));

        var result = await service.RefineAsync(
            proposal,
            BuildRequest(set));

        Assert.Same(proposal, result);
    }

    private static ScanFieldPlanBuildRequest BuildRequest(ApplicationProfilePlaceholderSet set) =>
        new()
        {
            Ingest = new ScanIngestResult
            {
                Input = new ScanNormalizedInput
                {
                    SourceKind = ScanSourceKind.Excel,
                    Pages = Array.Empty<ScanPageImage>(),
                    OriginalByteLength = 1,
                    FileName = "test.xlsx",
                    OfficePackageBytes = [1],
                },
                Ocr = new ScanOcrResult { Lines = Array.Empty<ScanOcrLine>(), TextConfidence = 1 },
                Suitability = new ScanSuitabilityReport
                {
                    Verdict = ScanSuitabilityVerdict.Pass,
                    TextConfidence = 1,
                    Issues = Array.Empty<ScanSuitabilityIssue>(),
                },
                Playbook = new ScanAuthoringPlaybook
                {
                    Markdown = "test",
                    Fingerprint = "fp",
                    VersionLabel = "1",
                },
            },
            PlaceholderSet = set,
        };
}
