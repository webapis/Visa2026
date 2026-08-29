#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class TemplateScanClarificationServiceTests
{
    private static ApplicationProfilePlaceholderSet HeaderSet()
    {
        var catalog = new UserReportPlaceholderCatalogService();
        return new ApplicationProfilePlaceholderSetService(catalog).GetSet(new ApplicationProfilePlaceholderSetQuery
        {
            Profile = new ApplicationProfile(),
            DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
            TemplateKind = ApplicationProfileTemplateKind.Word,
        });
    }

    private static ScanFieldPlan SamplePlan(ApplicationProfilePlaceholderSet set) =>
        new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
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
                        Confidence = ScanFieldConfidence.Medium,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Source = "test",
            },
        });

    private static ScanAuthoringPlaybook Playbook() =>
        new ScanAuthoringPlaybookService().GetPlaybook();

    private static ITemplateScanClarificationService Service(ITemplateScanAiProvider? provider = null) =>
        new TemplateScanClarificationService(
            provider ?? new NoneTemplateScanAiProvider(),
            new ScanFieldPlanMerger());

    [Theory]
    [InlineData("Translate this form to Russian")]
    [InlineData("Make the scan sharper and crop the margins")]
    [InlineData("Rewrite the ministry paragraph")]
    public void Classifier_marks_content_edits_as_out_of_scope(string message) =>
        Assert.Equal(
            TemplateScanChatIntent.OutOfScopeContentEdit,
            TemplateScanChatIntentClassifier.Classify(message));

    [Theory]
    [InlineData("Map the date line to application date")]
    [InlineData("Which token fits passport number?")]
    [InlineData("Remap the company field to ACNAM")]
    public void Classifier_marks_mapping_clarifications(string message) =>
        Assert.Equal(
            TemplateScanChatIntent.MappingClarification,
            TemplateScanChatIntentClassifier.Classify(message));

    [Fact]
    public async Task Out_of_scope_rewrite_is_rejected_without_provider_call()
    {
        var set = HeaderSet();
        var plan = SamplePlan(set);
        var stub = new ClarificationStubProvider(plan);

        var result = await Service(stub).ApplyAsync(new ScanClarificationTurnRequest
        {
            OfficerMessage = "Rewrite the greeting in English",
            CurrentPlan = plan,
            Playbook = Playbook(),
            PlaceholderSet = set,
        });

        Assert.False(result.Accepted);
        Assert.Equal(ScanClarificationRejectReason.OutOfScopeContentEdit, result.RejectReason);
        Assert.False(stub.WasCalled);
        Assert.Contains("placeholders", result.ReplyText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mapping_ask_against_None_provider_refuses_without_changing_plan()
    {
        var set = HeaderSet();
        var plan = SamplePlan(set);

        var result = await Service().ApplyAsync(new ScanClarificationTurnRequest
        {
            OfficerMessage = "Map the date to application date",
            CurrentPlan = plan,
            Playbook = Playbook(),
            PlaceholderSet = set,
        });

        Assert.False(result.Accepted);
        Assert.Equal(ScanClarificationRejectReason.NotUnderstood, result.RejectReason);
        Assert.Same(plan, result.Plan);
    }

    [Fact]
    public async Task Accepted_clarification_updates_plan_through_merger()
    {
        var set = HeaderSet();
        var plan = SamplePlan(set);
        var updatedProposal = new ScanFieldPlanProposal
        {
            Fields =
            [
                new ScanDetectedFieldDraft
                {
                    FieldId = "f1",
                    PageIndex = 0,
                    LabelText = "Application number",
                    ProposedToken = "{{ds.ADAT}}",
                    Confidence = ScanFieldConfidence.High,
                    Scope = ScanFieldScope.Header,
                    Box = ScanBoundingBox.FullPage,
                },
            ],
            Source = "stub",
        };
        var stub = new ClarificationStubProvider(plan, updatedProposal);

        var result = await Service(stub).ApplyAsync(new ScanClarificationTurnRequest
        {
            OfficerMessage = "Use application date token for the date line",
            CurrentPlan = plan,
            Playbook = Playbook(),
            PlaceholderSet = set,
        });

        Assert.True(result.Accepted);
        Assert.Null(result.RejectReason);
        Assert.Contains(result.Plan.Fields, f => f.ProposedToken == "{{ds.ADAT}}");
        Assert.True(stub.WasCalled);
    }

    [Fact]
    public void DI_registers_clarification_service()
    {
        var services = new ServiceCollection();
        services.AddTemplateScan();
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<ITemplateScanClarificationService>());
    }

    private sealed class ClarificationStubProvider : ITemplateScanAiProvider
    {
        private readonly ScanFieldPlan _baseline;
        private readonly ScanFieldPlanProposal _proposal;

        public ClarificationStubProvider(ScanFieldPlan baseline, ScanFieldPlanProposal? proposal = null)
        {
            _baseline = baseline;
            _proposal = proposal ?? ScanFieldPlanMapper.ToProposal(baseline, "Stub");
        }

        public string Key => "Stub";
        public bool IsEnabled => true;
        public bool WasCalled { get; private set; }

        public Task<ScanFieldPlanProposal> ProposeFieldPlanAsync(
            ScanFieldPlanRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_proposal);

        public Task<ScanClarificationResult> ClarifyAsync(
            ScanClarificationRequest request,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new ScanClarificationResult
            {
                Accepted = true,
                ReplyText = "Updated mapping.",
                Plan = _proposal,
            });
        }

        public Task<ScanDocxLayoutProposal> ProposeDocxLayoutAsync(
            ScanDocxLayoutRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ScanDocxLayoutProposal { Blocks = Array.Empty<ScanDocxBlock>() });
    }
}
