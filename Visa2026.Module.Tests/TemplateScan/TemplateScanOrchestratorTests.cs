#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Tests.TemplateConvert;
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
    public async Task GenerateAsync_yellow_word_writes_tokens_into_copy()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var bytes = ScanOfficeYellowExtractorTests.CreateWordFixture("№ 4/-434");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        Assert.NotEmpty(yellows);

        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        Assert.True(plan.HasMappedFields);

        var orchestrator = CreateOrchestrator();
        var outcome = await orchestrator.GenerateAsync(new TemplateScanAnalysis
        {
            NormalizedInput = new ScanNormalizedInput
            {
                SourceKind = ScanSourceKind.Word,
                Pages = Array.Empty<ScanPageImage>(),
                OriginalByteLength = bytes.LongLength,
                FileName = "marked.docx",
                OfficePackageBytes = bytes,
            },
            Suitability = new ScanSuitabilityReport
            {
                Verdict = ScanSuitabilityVerdict.Pass,
                TextConfidence = 1.0,
                Issues = Array.Empty<ScanSuitabilityIssue>(),
            },
            FieldPlan = plan,
            PlaceholderSet = set,
            Playbook = new ScanAuthoringPlaybookService().GetPlaybook(),
            TemplateName = "Yellow marks template",
            DataScope = ApplicationProfileTemplateDataScope.Both,
        });

        Assert.NotEmpty(outcome.Content);
        Assert.Equal(ApplicationProfileTemplateKind.Word, outcome.TemplateKind);
        Assert.True(outcome.Outline.IsReadable);
        Assert.True(!outcome.HasErrors, string.Join(" | ", outcome.Errors));
        Assert.Contains(outcome.EmittedTokens, t => t.Contains("AFNUM", StringComparison.OrdinalIgnoreCase));

        using (var check = new MemoryStream(outcome.Content, writable: false))
        using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(check, false))
        {
            Assert.DoesNotContain(
                doc.MainDocumentPart!.Document.Body!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Highlight>(),
                h => h.Val?.Value == DocumentFormat.OpenXml.Wordprocessing.HighlightColorValues.Yellow);
        }
    }

    [Fact]
    public async Task GenerateAsync_yellow_excel_writes_rows_loop_marker()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile
                {
                    RequirePersonPassport = true,
                    RequirePersonEducation = true,
                    RequirePersonPosition = true,
                    RequirePersonAddressOfResidence = true,
                },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Excel,
            });

        using var ms = new MemoryStream();
        using (var wb = new ClosedXML.Excel.XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("B4").Value = "Familiýasy";
            ws.Cell("C4").Value = "Ady";
            void Yellow(int col, string value)
            {
                var cell = ws.Cell(5, col);
                cell.Value = value;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Yellow;
            }

            Yellow(2, "Erol");
            Yellow(3, "Hilmi");
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Excel);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        var orchestrator = CreateOrchestrator();
        var outcome = await orchestrator.GenerateAsync(new TemplateScanAnalysis
        {
            NormalizedInput = new ScanNormalizedInput
            {
                SourceKind = ScanSourceKind.Excel,
                Pages = Array.Empty<ScanPageImage>(),
                OriginalByteLength = bytes.LongLength,
                FileName = "sanaw.xlsx",
                OfficePackageBytes = bytes,
            },
            Suitability = new ScanSuitabilityReport
            {
                Verdict = ScanSuitabilityVerdict.Pass,
                TextConfidence = 1.0,
                Issues = Array.Empty<ScanSuitabilityIssue>(),
            },
            FieldPlan = plan,
            PlaceholderSet = set,
            Playbook = new ScanAuthoringPlaybookService().GetPlaybook(),
            TemplateName = "Sanaw scan",
            DataScope = ApplicationProfileTemplateDataScope.Both,
        });

        Assert.True(!outcome.HasErrors, string.Join(" | ", outcome.Errors));
        Assert.Equal("{{#ds.rows}}", TemplateConvertFixtures.GetCellText(outcome.Content, "Sanaw", "A5"));
        Assert.Equal("{{/ds.rows}}", TemplateConvertFixtures.GetCellText(outcome.Content, "Sanaw", "A6"));

        using (var verify = new MemoryStream(outcome.Content, writable: false))
        using (var wb = new ClosedXML.Excel.XLWorkbook(verify))
        {
            foreach (var cell in wb.Worksheet("Sanaw").CellsUsed(ClosedXML.Excel.XLCellsUsedOptions.All))
            {
                Assert.True(
                    cell.Style.Fill.PatternType is ClosedXML.Excel.XLFillPatternValues.None
                        or ClosedXML.Excel.XLFillPatternValues.Gray125,
                    $"Yellow fill left on {cell.Address} after Generate.");
            }
        }
    }

    [Fact]
    public void GenerateAsync_yellow_excel_builds_row_loop_from_field_plan()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Excel,
            });

        using var ms = new MemoryStream();
        using (var wb = new ClosedXML.Excel.XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sanaw");
            ws.Cell("B4").Value = "Familiýasy";
            ws.Cell("C4").Value = "Ady";
            ws.Cell("B5").Value = "Erol";
            ws.Cell("B5").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Yellow;
            ws.Cell("C5").Value = "Hilmi";
            ws.Cell("C5").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.Yellow;
            wb.SaveAs(ms);
        }

        var bytes = ms.ToArray();
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Excel);
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Excel);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        Assert.True(plan.Fields.Count >= 2, "Expected mapped row fields on the first sheet.");
        Assert.All(plan.Fields, f => Assert.IsType<DocumentRegion.ExcelCell>(f.SourceRegion));

        var subs = plan.Fields
            .Where(f => !string.IsNullOrWhiteSpace(f.ProposedToken) && f.SourceRegion != null)
            .Select(f => new TokenSubstitution(f.SourceRegion!, f.ProposedToken!.Trim()))
            .ToList();

        var loops = TemplateRosterLoopPlanner.PlanExcelLoopsFromSubstitutions(subs);
        Assert.NotEmpty(loops);
    }

    [Fact]
    public async Task GenerateAsync_recovers_yellow_spans_when_SourceRegion_missing()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var bytes = ScanOfficeYellowExtractorTests.CreateWordWithCaptionThenYellow(
            "we Kärhananyň wiza işleri boýunça ygtyýarly wekili:",
            "Nepesowa Tumar Aşyrowna");
        var yellows = new ScanOfficeYellowExtractor().Extract(bytes, ScanSourceKind.Word);
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, set, bytes, ScanSourceKind.Word);
        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.FilledSample,
            Proposal = proposal,
        });

        Assert.All(plan.Fields, f => Assert.NotNull(f.SourceRegion));

        var stripped = new ScanFieldPlan
        {
            PlaceholderSet = plan.PlaceholderSet,
            ScanKind = plan.ScanKind,
            Fields = plan.Fields.Select(f => new ScanDetectedField
            {
                FieldId = f.FieldId,
                Box = f.Box,
                PageIndex = f.PageIndex,
                LabelText = f.LabelText,
                ProposedToken = f.ProposedToken,
                Confidence = f.Confidence,
                Scope = f.Scope,
                SourceRegion = null,
                Alternatives = f.Alternatives,
            }).ToList(),
            StaticRegions = plan.StaticRegions,
            Gaps = plan.Gaps,
            PendingQuestions = plan.PendingQuestions,
            Rationale = plan.Rationale,
            Source = plan.Source,
            YellowHighlightCount = plan.YellowHighlightCount,
        };

        var outcome = await CreateOrchestrator().GenerateAsync(new TemplateScanAnalysis
        {
            NormalizedInput = new ScanNormalizedInput
            {
                SourceKind = ScanSourceKind.Word,
                Pages = Array.Empty<ScanPageImage>(),
                OriginalByteLength = bytes.LongLength,
                FileName = "wekil.docx",
                OfficePackageBytes = bytes,
            },
            Suitability = new ScanSuitabilityReport
            {
                Verdict = ScanSuitabilityVerdict.Pass,
                TextConfidence = 1.0,
                Issues = Array.Empty<ScanSuitabilityIssue>(),
            },
            FieldPlan = stripped,
            PlaceholderSet = set,
            Playbook = new ScanAuthoringPlaybookService().GetPlaybook(),
            TemplateName = "Wekil recover",
            DataScope = ApplicationProfileTemplateDataScope.Both,
        });

        Assert.True(!outcome.HasErrors, string.Join(" | ", outcome.Errors));
        Assert.Contains(outcome.EmittedTokens, t => t.Contains("RPFN", StringComparison.OrdinalIgnoreCase));
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
            new TemplateDocumentOutlineReader(),
            new TemplateTokenWriter(),
            new TemplateConversionDiffGate());
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
