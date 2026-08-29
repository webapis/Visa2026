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
    public async Task BuildAsync_NoneProvider_ReturnsEmptyWithoutYellowVision()
    {
        var (_, _, ingest, fieldPlan) = ScanTestServiceFactory.Create();
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = ScanTestImageFactory.CreatePdf(1),
            FileName = "form.pdf",
        });

        var ingestWithOcr = new ScanIngestResult
        {
            Input = ingested.Input,
            Ocr = new ScanOcrResult
            {
                Lines = [new ScanOcrLine { PageIndex = 0, Text = "Full application number", Confidence = 0.9 }],
                TextConfidence = 0.9,
            },
            Suitability = ingested.Suitability,
            Playbook = ingested.Playbook,
        };

        var plan = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingestWithOcr,
            PlaceholderSet = set,
        });

        Assert.Equal("None", plan.Source);
        Assert.Empty(plan.Fields);
    }

    [Fact]
    public async Task BuildAsync_ProviderThrow_DoesNotInventOcrFields()
    {
        var (_, _, ingest, _) = ScanTestServiceFactory.Create();
        var fieldPlan = new ScanFieldPlanService(
            new ThrowingScanAiProvider(),
            new ScanFieldPlanMerger(),
            Options.Create(new TemplateAiScanOptions()));

        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var ingested = ingest.Ingest(new ScanNormalizeRequest
        {
            Content = ScanTestImageFactory.CreatePdf(1),
            FileName = "form.pdf",
        });

        var ingestWithOcr = new ScanIngestResult
        {
            Input = ingested.Input,
            Ocr = new ScanOcrResult
            {
                Lines = [new ScanOcrLine { PageIndex = 0, Text = "Full application number", Confidence = 0.9 }],
                TextConfidence = 0.9,
            },
            Suitability = ingested.Suitability,
            Playbook = ingested.Playbook,
        };

        var plan = await fieldPlan.BuildAsync(new ScanFieldPlanBuildRequest
        {
            Ingest = ingestWithOcr,
            PlaceholderSet = set,
        });

        Assert.Equal("none", plan.Source);
        Assert.Equal(0, plan.YellowHighlightCount);
        Assert.Empty(plan.Fields);
    }

    private sealed class ThrowingScanAiProvider : ITemplateScanAiProvider
    {
        public string Key => "throw";
        public bool IsEnabled => true;

        public Task<ScanFieldPlanProposal> ProposeFieldPlanAsync(
            ScanFieldPlanRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Azure OpenAI HTTP 404: DeploymentNotFound");

        public Task<ScanClarificationResult> ClarifyAsync(
            ScanClarificationRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ScanDocxLayoutProposal> ProposeDocxLayoutAsync(
            ScanDocxLayoutRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}