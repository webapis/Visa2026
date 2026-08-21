using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

/// <summary>
/// End to end over the deterministic chain the convert dialog calls: placeholder set → value map →
/// candidate check → writer → diff gate → residual scan → validation.
/// </summary>
public class TemplateConvertOrchestratorTests
{
    private const string CaseNumber = "TRM-2026-120";
    private static readonly DateTime CaseDate = new(2026, 1, 20);

    private readonly ITemplateConvertOrchestrator _orchestrator = new TemplateConvertOrchestrator(
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()),
        new ApplicationProfileInstanceValueMapService(),
        new TemplateCandidateAnalyzer(Options.Create(new TemplateSuitabilityOptions())),
        new TemplateDocumentOutlineReader(),
        new TemplateTokenWriter(),
        new TemplateConversionDiffGate(),
        new TemplateResidualValueScanner(),
        new EphemeralTemplateValidationService(
            new UserReportPlaceholderExtractor(),
            new ExcelTemplatePlaceholderExtractor(),
            new PermissiveValidator(),
            new PermissiveValidator()));

    private static ApplicationProfile Profile() => new() { Name = "Work permit extension" };

    private static ApplicationProfileInstance Instance() =>
        new() { FullApplicationNumber = CaseNumber, ApplicationDate = CaseDate };

    private TemplateConvertAnalysis Analyze(byte[] content, string fileName = "letter.docx") =>
        _orchestrator.Analyze(new TemplateConvertAnalyzeRequest
        {
            Profile = Profile(),
            Instance = Instance(),
            Content = content,
            FileName = fileName,
            DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
        });

    private static byte[] Letter(params string[] paragraphs) =>
        TemplateConvertFixtures.CreateWordDocument(paragraphs.Select(p => new[] { p }).ToArray());

    [Theory]
    [InlineData("letter.docx", true)]
    [InlineData("roster.XLSX", true)]
    [InlineData("legacy.doc", false)]
    [InlineData("scan.pdf", false)]
    public void Only_the_new_office_formats_are_accepted(string fileName, bool expected) =>
        Assert.Equal(expected, _orchestrator.TryResolveFormat(fileName, out _));

    [Fact]
    public void Case_literals_in_the_upload_are_reported_as_matches()
    {
        var analysis = Analyze(Letter($"Arza {CaseNumber} senesi 20.01.2026."));

        var matched = analysis.Candidate.Highlights
            .Where(h => h.Kind == HighlightKind.Match)
            .Select(h => h.ShortCode)
            .ToList();

        Assert.Contains("AFNUM", matched);
        Assert.Contains("ADAT", matched);
        Assert.Equal(2, analysis.ConvertibleHighlights.Count);
    }

    [Fact]
    public async Task Matched_literals_become_tokens_and_pass_the_diff_gate()
    {
        var content = Letter($"Arza {CaseNumber} senesi 20.01.2026.");
        var analysis = Analyze(content);

        var outcome = await _orchestrator.ConvertAsync(analysis, content);

        Assert.Equal(2, outcome.Applied.Count);
        Assert.True(outcome.Diff.Passed);
        Assert.True(outcome.Residual.IsClean);
        Assert.False(outcome.Validation.HasHardFailure);
        Assert.Empty(outcome.Errors);

        var text = string.Concat(outcome.Outline.Paragraphs.Select(p => p.Text));
        Assert.Contains("{{ds.AFNUM}}", text);
        Assert.Contains("{{ds.ADAT}}", text);
        Assert.DoesNotContain(CaseNumber, text);
    }

    [Fact]
    public async Task Surrounding_wording_is_left_alone()
    {
        var content = Letter($"Hormatly ministr, arza {CaseNumber} boýunça.");
        var analysis = Analyze(content);

        var outcome = await _orchestrator.ConvertAsync(analysis, content);

        var text = string.Concat(outcome.Outline.Paragraphs.Select(p => p.Text));
        Assert.Equal("Hormatly ministr, arza {{ds.AFNUM}} boýunça.", text);
    }

    [Fact]
    public void A_document_without_case_values_cannot_be_converted()
    {
        var analysis = Analyze(Letter("Hormatly ministr, hoşniýetli salamymyzy iberýäris."));

        Assert.Equal(SuitabilityLevel.Fail, analysis.Candidate.Level);
        Assert.False(analysis.CanConvert);
    }

    [Fact]
    public void An_unreadable_upload_is_reported_instead_of_thrown()
    {
        var analysis = Analyze([1, 2, 3, 4]);

        Assert.False(analysis.Outline.IsReadable);
        Assert.Equal(SuitabilityLevel.Fail, analysis.Candidate.Level);
    }

    /// <summary>Property resolution is covered by the E6 tests; here it must never be the reason a case fails.</summary>
    private sealed class PermissiveValidator : IUserReportValidationService, IExcelReportValidationService
    {
        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
            IList<string> placeholders,
            UserReportBoType boType) =>
            Task.FromResult(Build(placeholders));

        public Task<IList<PlaceholderValidationResult>> ValidatePlaceholdersAsync(
            IList<string> placeholders,
            UserReportBoType boType,
            ExcelMergeMode mergeMode) =>
            Task.FromResult(Build(placeholders));

        private static IList<PlaceholderValidationResult> Build(IList<string> placeholders) =>
            placeholders
                .Select(p => new PlaceholderValidationResult { PlaceholderKey = p, IsValid = true })
                .Cast<PlaceholderValidationResult>()
                .ToList();
    }
}
