#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

public sealed class NoneTemplateScanAiProvider : ITemplateScanAiProvider
{
    public const string ProviderKey = "None";

    public string Key => ProviderKey;

    public bool IsEnabled => false;

    public Task<ScanFieldPlanProposal> ProposeFieldPlanAsync(
        ScanFieldPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        // Yellow-highlight authoring requires a vision provider.
        return Task.FromResult(new ScanFieldPlanProposal
        {
            Fields = Array.Empty<ScanDetectedFieldDraft>(),
            Gaps = Array.Empty<ScanGapDraft>(),
            YellowHighlightCount = 0,
            Rationale = "Vision AI is required to detect yellow highlights on the scan.",
            Source = ProviderKey,
        });
    }

    public Task<ScanClarificationResult> ClarifyAsync(
        ScanClarificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new ScanClarificationResult
        {
            Accepted = false,
            ReplyText = "AI assistance is turned off. Review detected fields on the list or enable a vision provider.",
            Plan = ScanFieldPlanMapper.ToProposal(request.CurrentPlan, ProviderKey),
        });
    }

    public Task<ScanDocxLayoutProposal> ProposeDocxLayoutAsync(
        ScanDocxLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(DeterministicScanDocxLayoutPlanner.Build(request));
    }
}
