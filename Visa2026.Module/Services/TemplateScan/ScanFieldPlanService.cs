#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanFieldPlanService
{
    Task<ScanFieldPlan> BuildAsync(ScanFieldPlanBuildRequest request, CancellationToken cancellationToken = default);
}

public sealed class ScanFieldPlanService : IScanFieldPlanService
{
    private readonly ITemplateScanAiProvider _provider;
    private readonly IScanFieldPlanMerger _merger;
    private readonly TemplateAiScanOptions _options;

    public ScanFieldPlanService(
        ITemplateScanAiProvider provider,
        IScanFieldPlanMerger merger,
        Microsoft.Extensions.Options.IOptions<TemplateAiScanOptions> options)
    {
        _provider = provider;
        _merger = merger;
        _options = options?.Value ?? new TemplateAiScanOptions();
    }

    public async Task<ScanFieldPlan> BuildAsync(ScanFieldPlanBuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fieldRequest = ScanFieldPlanRequestBuilder.Build(
            request,
            _options.RedactIdentifiersInExtract);

        ScanFieldPlanProposal proposal;
        try
        {
            proposal = await _provider.ProposeFieldPlanAsync(fieldRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            and not OutOfMemoryException
            and not StackOverflowException)
        {
            // Yellow-only rule requires vision; do not invent OCR catalog matches.
            proposal = new ScanFieldPlanProposal
            {
                Fields = Array.Empty<ScanDetectedFieldDraft>(),
                Gaps = Array.Empty<ScanGapDraft>(),
                YellowHighlightCount = 0,
                Rationale = "AI field plan failed (" + ex.Message + "). Yellow-highlight detection requires vision.",
                Source = "none",
            };
        }

        var plan = _merger.Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = request.PlaceholderSet,
            ScanKind = request.ScanKind,
            ValueHints = request.ValueHints,
        });

        return ScanFieldBoxLocalizer.Apply(plan, request.Ingest.Input.Pages);
    }
}