#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanDocxLayoutService
{
    Task<ScanDocxLayoutProposal> ProposeLayoutAsync(
        ScanDocxLayoutRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ScanDocxLayoutService : IScanDocxLayoutService
{
    private readonly ITemplateScanAiProvider _provider;

    public ScanDocxLayoutService(ITemplateScanAiProvider provider)
    {
        _provider = provider;
    }

    public async Task<ScanDocxLayoutProposal> ProposeLayoutAsync(
        ScanDocxLayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_provider.IsEnabled)
        {
            try
            {
                var proposal = await _provider.ProposeDocxLayoutAsync(request, cancellationToken).ConfigureAwait(false);
                if (proposal.Blocks.Count > 0)
                    return ScanLetterLayoutNormalizer.Apply(proposal, request.OcrLines);
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                and not OutOfMemoryException
                and not StackOverflowException)
            {
                // Convert/Scan parity: fall back to local layout when vision layout fails.
            }
        }

        return ScanLetterLayoutNormalizer.Apply(DeterministicScanDocxLayoutPlanner.Build(request), request.OcrLines);
    }
}