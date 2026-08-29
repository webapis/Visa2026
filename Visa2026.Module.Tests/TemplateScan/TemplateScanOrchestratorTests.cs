#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanDraftDocxBuilderTests
{
    [Fact]
    public void Build_emits_docx_with_profile_tokens()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
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
                        PageIndex = 0,
                        LabelText = "Application number",
                        ProposedToken = "{{ds.AFNUM}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

        var layout = DeterministicScanDocxLayoutPlanner.Build(new ScanDocxLayoutRequest
        {
            FieldPlan = plan,
            Playbook = new ScanAuthoringPlaybookService().GetPlaybook(),
        });

        var result = new ScanDraftDocxBuilder().Build(new ScanDraftDocxRequest
        {
            Layout = layout,
            FieldPlan = plan,
        });

        Assert.NotEmpty(result.Content);
        Assert.Contains("{{ds.AFNUM}}", result.EmittedTokens, StringComparer.Ordinal);

        using var stream = new MemoryStream(result.Content);
        var extracted = new UserReportPlaceholderExtractor().ExtractPlaceholdersAsync(stream).GetAwaiter().GetResult();
        Assert.Contains(extracted, t => t.Contains("AFNUM", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public void Build_preserves_paragraph_structure_with_embedded_tokens()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

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
                        FieldId = "f1",
                        PageIndex = 0,
                        LabelText = "Application number",
                        ProposedToken = "{{ds.AFNUM}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

        var layout = new ScanDocxLayoutProposal
        {
            Blocks =
            [
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "No {{ds.AFNUM}}" },
                new ScanDocxBlock { Kind = "paragraph", Align = "right", Text = "Türkmenistanyň Döwlet migrasiýa gullugyna" },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "Adaty tertipde!" },
                new ScanDocxBlock { Kind = "paragraph", Align = "left", Text = "Hatymyzyň goşundysynda görkezilen {{ds.AFNUM}} boýunça." },
            ],
            Rationale = "test",
        };

        var result = new ScanDraftDocxBuilder().Build(new ScanDraftDocxRequest
        {
            Layout = layout,
            FieldPlan = plan,
        });

        Assert.Contains("{{ds.AFNUM}}", result.EmittedTokens, StringComparer.Ordinal);
        using var stream = new MemoryStream(result.Content);
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(stream, false);
        var texts = doc.MainDocumentPart!.Document.Body!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
            .Select(t => t.Text)
            .ToList();
        Assert.Contains(texts, t => t.Contains("Adaty tertipde!", StringComparison.Ordinal));
        Assert.Contains(texts, t => t.Contains("migrasiýa", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(texts, t => t.StartsWith("Application number:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_twoColumn_and_styles_match_letter_layout()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

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
                        FieldId = "f1",
                        PageIndex = 0,
                        LabelText = "№ 4/-434",
                        ProposedToken = "{{ds.AFNUM}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

        var layout = new ScanDocxLayoutProposal
        {
            Blocks =
            [
                new ScanDocxBlock
                {
                    Kind = "twoColumn",
                    Text = "№ {{ds.AFNUM}}\n{{ds.ADAT}}",
                    RightText = "Türkmenistanyň Döwlet\nmigrasiýa gullugyna",
                    Align = "left",
                    RightAlign = "right",
                },
                new ScanDocxBlock
                {
                    Kind = "paragraph",
                    Align = "left",
                    Style = "italic",
                    Text = "{{ds.Urgency_NameTm}}",
                },
                new ScanDocxBlock
                {
                    Kind = "paragraph",
                    Align = "justify",
                    Text = "Body with {{ds.TPCNT}} people.",
                },
                new ScanDocxBlock
                {
                    Kind = "twoColumn",
                    Text = "Türkmenistandaky şahamçasynyň\nmüdiri",
                    RightText = "Mehmet ÇIRAK",
                    Align = "left",
                    RightAlign = "right",
                    Style = "bold",
                    RightStyle = "bold",
                },
            ],
            Rationale = "test-layout",
        };

        var result = new ScanDraftDocxBuilder().Build(new ScanDraftDocxRequest
        {
            Layout = layout,
            FieldPlan = plan,
        });

        using var stream = new MemoryStream(result.Content);
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        Assert.NotEmpty(body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>());

        var justifications = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Justification>()
            .Select(j => j.Val?.Value)
            .ToList();
        Assert.Contains(justifications, j => j == DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Both);
        Assert.Contains(justifications, j => j == DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Right);

        Assert.NotEmpty(body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Italic>());
        Assert.NotEmpty(body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Bold>());
        Assert.Contains(result.EmittedTokens, t => t.Contains("AFNUM", StringComparison.Ordinal));
    }
}

public class TemplateScanOrchestratorTests
{
    [Fact]
    public async Task GenerateAsync_produces_validated_draft()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
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
                        PageIndex = 0,
                        LabelText = "Application number",
                        ProposedToken = "{{ds.AFNUM}}",
                        Confidence = ScanFieldConfidence.High,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

        var orchestrator = CreateOrchestrator();
        var outcome = await orchestrator.GenerateAsync(new TemplateScanAnalysis
        {
            NormalizedInput = new ScanNormalizedInput
            {
                SourceKind = ScanSourceKind.Image,
                Pages = Array.Empty<ScanPageImage>(),
                OriginalByteLength = 0,
                FileName = "scan.png",
            },
            Suitability = new ScanSuitabilityReport
            {
                Verdict = ScanSuitabilityVerdict.Pass,
                TextConfidence = 0.9,
                Issues = Array.Empty<ScanSuitabilityIssue>(),
            },
            FieldPlan = plan,
            PlaceholderSet = set,
            Playbook = new ScanAuthoringPlaybookService().GetPlaybook(),
            TemplateName = "Scan template",
            DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
        });

        Assert.NotEmpty(outcome.Content);
        Assert.True(outcome.Outline.IsReadable);
        Assert.False(outcome.HasErrors);
        Assert.Contains(outcome.EmittedTokens, t => t.Contains("AFNUM", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DI_registers_orchestrator()
    {
        using var sp = BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<ITemplateScanOrchestrator>());
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUserReportPlaceholderExtractor, UserReportPlaceholderExtractor>();
        services.AddSingleton<IExcelTemplatePlaceholderExtractor, ExcelTemplatePlaceholderExtractor>();
        services.AddSingleton<IUserReportValidationService, PermissiveValidator>();
        services.AddSingleton<IExcelReportValidationService, PermissiveValidator>();
        services.AddTemplateConvert();
        services.AddTemplateScan();
        services.Configure<TemplateAiScanOptions>(static o => o.Provider = "None");
        return services.BuildServiceProvider();
    }

    private static ITemplateScanOrchestrator CreateOrchestrator()
    {
        var validation = new EphemeralTemplateValidationService(
            new UserReportPlaceholderExtractor(),
            new ExcelTemplatePlaceholderExtractor(),
            new PermissiveValidator(),
            new PermissiveValidator());

        return new TemplateScanOrchestrator(
            new ScanDocxLayoutService(new NoneTemplateScanAiProvider()),
            new ScanDraftDocxBuilder(),
            validation,
            new TemplateDocumentOutlineReader());
    }

    private sealed class PermissiveValidator : IUserReportValidationService, IExcelReportValidationService
    {
        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
            IList<string> placeholders,
            UserReportBoType boType) =>
            Task.FromResult<IList<PlaceholderValidationResult>>(Array.Empty<PlaceholderValidationResult>());

        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
            IList<string> placeholders,
            UserReportBoType boType,
            ExcelMergeMode mergeMode) =>
            Task.FromResult<IList<PlaceholderValidationResult>>(Array.Empty<PlaceholderValidationResult>());
    }
}
