using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

/// <summary>E9: Preview chat against None + L8 intent gate (Q11 / Q12).</summary>
public class TemplateConvertChatServiceTests
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

    private static (byte[] Original, byte[] Draft, TemplateMappingPlan Plan, IReadOnlyList<DocumentExtractRegion> Regions, TemplateConvertAnalysis Analysis)
        ConvertedLetter()
    {
        var orchestrator = new TemplateConvertOrchestrator(
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

        var original = TemplateConvertFixtures.CreateWordDocument(
            new[] { "Arza TRM-2026-120 senesi 20.01.2026." });

        var analysis = orchestrator.Analyze(new TemplateConvertAnalyzeRequest
        {
            Profile = new ApplicationProfile { Name = "Test" },
            Instance = new ApplicationProfileInstance
            {
                FullApplicationNumber = "TRM-2026-120",
                ApplicationDate = new DateTime(2026, 1, 20),
            },
            Content = original,
            FileName = "letter.docx",
            DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
        });

        var outcome = orchestrator.ConvertAsync(analysis, original).GetAwaiter().GetResult();
        var plan = new TemplateMappingPlan(
            outcome.Applied.ToList(),
            Array.Empty<LoopMarker>(),
            Array.Empty<MappingGap>(),
            Rationale: null);

        var regions = TemplateMappingRequestBuilder.FromCandidate(
            analysis.Format,
            analysis.PlaceholderSet,
            analysis.Candidate,
            redactIdentifiers: true).Regions;

        return (original, outcome.Content, plan, regions, analysis);
    }

    private static ITemplateConvertChatService Service(ITemplateConvertAiProvider? provider = null) =>
        new TemplateConvertChatService(
            provider ?? new NoneTemplateConvertAiProvider(),
            new TemplateMappingPlanSanitizer());

    [Theory]
    [InlineData("Make the letter more formal")]
    [InlineData("Rewrite the greeting in English")]
    [InlineData("Change the font to Arial")]
    [InlineData("Translate this to Russian")]
    [InlineData("Fix the logo and redesign the table")]
    public void Classifier_marks_content_edits_as_out_of_scope(string message) =>
        Assert.Equal(
            TemplateConvertChatIntent.OutOfScopeContentEdit,
            TemplateConvertChatIntentClassifier.Classify(message));

    [Theory]
    [InlineData("Remap the passport number to PPN")]
    [InlineData("Unmap that span")]
    [InlineData("Use {{ds.ACNAM}} for the company name")]
    public void Classifier_marks_mapping_asks(string message) =>
        Assert.Equal(
            TemplateConvertChatIntent.MappingAdjustment,
            TemplateConvertChatIntentClassifier.Classify(message));

    [Theory]
    [InlineData("Make it more formal")]
    [InlineData("Rewrite the greeting")]
    [InlineData("Change the layout and spacing")]
    public async Task Q11_rewrite_ask_is_rejected_with_OutOfScope_and_byte_identical_draft(string message)
    {
        var (original, draft, plan, regions, analysis) = ConvertedLetter();
        var before = draft.ToArray();

        var result = await Service().ApplyAsync(new TemplateConvertChatServiceRequest
        {
            Message = message,
            CurrentPlan = plan,
            Regions = regions,
            PlaceholderSet = analysis.PlaceholderSet,
            CurrentDraftContent = draft,
        });

        Assert.False(result.Accepted);
        Assert.Equal(ChatRejectReason.OutOfScopeContentEdit, result.RejectReason);
        Assert.Same(plan, result.Plan);
        Assert.Equal(before, draft);
        Assert.Contains("placeholders", result.ReplyText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Q12_accepted_mapping_turn_only_changes_substitutions_not_on_rewrite()
    {
        var (original, draft, plan, regions, analysis) = ConvertedLetter();
        Assert.NotEmpty(plan.Substitutions);

        var target = plan.Substitutions[0].Region;
        var stub = new MappingStubProvider(new TemplateMappingPlan(
            [new TokenSubstitution(target, "{{ds.ADAT}}")],
            Array.Empty<LoopMarker>(),
            Array.Empty<MappingGap>(),
            Rationale: "remapped"));

        var mapped = await Service(stub).ApplyAsync(new TemplateConvertChatServiceRequest
        {
            Message = "Remap this field to the application date token",
            CurrentPlan = plan,
            Regions = regions,
            PlaceholderSet = analysis.PlaceholderSet,
            CurrentDraftContent = draft,
        });

        Assert.True(mapped.Accepted);
        Assert.Null(mapped.RejectReason);
        Assert.Contains(mapped.Plan.Substitutions, s => s.Token.Contains("ADAT", StringComparison.OrdinalIgnoreCase));

        var orchestrator = new TemplateConvertOrchestrator(
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

        var newOutcome = await orchestrator.ApplyPlanAsync(analysis, original, mapped.Plan);
        Assert.False(draft.AsSpan().SequenceEqual(newOutcome.Content));
        Assert.True(newOutcome.Diff.Passed);

        var rewrite = await Service(stub).ApplyAsync(new TemplateConvertChatServiceRequest
        {
            Message = "Rewrite the greeting in English",
            CurrentPlan = mapped.Plan,
            Regions = regions,
            PlaceholderSet = analysis.PlaceholderSet,
            CurrentDraftContent = newOutcome.Content,
        });

        Assert.False(rewrite.Accepted);
        Assert.Equal(ChatRejectReason.OutOfScopeContentEdit, rewrite.RejectReason);
        Assert.Equal(mapped.Plan, rewrite.Plan);
        Assert.False(stub.RewriteReached);
    }

    [Fact]
    public async Task Mapping_ask_against_None_provider_refuses_without_changing_plan()
    {
        var (_, draft, plan, regions, analysis) = ConvertedLetter();

        var result = await Service().ApplyAsync(new TemplateConvertChatServiceRequest
        {
            Message = "Remap the passport number",
            CurrentPlan = plan,
            Regions = regions,
            PlaceholderSet = analysis.PlaceholderSet,
            CurrentDraftContent = draft,
        });

        Assert.False(result.Accepted);
        Assert.Equal(ChatRejectReason.NotUnderstood, result.RejectReason);
        Assert.Same(plan, result.Plan);
        Assert.Contains("turned off", result.ReplyText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DI_registers_the_chat_service()
    {
        var services = new ServiceCollection();
        services.AddTemplateConvert();
        services.Configure<TemplateAiConvertOptions>(o => o.Provider = "None");
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<ITemplateConvertChatService>());
    }

        private sealed class MappingStubProvider : ITemplateConvertAiProvider
    {
        private readonly TemplateMappingPlan _plan;

        public MappingStubProvider(TemplateMappingPlan plan) => _plan = plan;

        public string Key => "Stub";
        public bool IsEnabled => true;
        public bool RewriteReached { get; private set; }

        public Task<TemplateMappingPlan> ProposeMappingAsync(
            TemplateMappingRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_plan);

        public Task<TemplateChatTurnResult> ApplyChatAdjustmentAsync(
            TemplateChatTurnRequest request,
            CancellationToken cancellationToken = default)
        {
            if (TemplateConvertChatIntentClassifier.Classify(request.Message)
                == TemplateConvertChatIntent.OutOfScopeContentEdit)
            {
                RewriteReached = true;
            }

            return Task.FromResult(new TemplateChatTurnResult(
                true,
                "Remapped within the profile set.",
                _plan,
                null));
        }
    }

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
                .ToList();
    }
}