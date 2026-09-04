#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

public interface ITemplateScanAiProvider
{
    string Key { get; }

    bool IsEnabled { get; }

    Task<ScanFieldPlanProposal> ProposeFieldPlanAsync(
        ScanFieldPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<ScanClarificationResult> ClarifyAsync(
        ScanClarificationRequest request,
        CancellationToken cancellationToken = default);

    Task<ScanDocxLayoutProposal> ProposeDocxLayoutAsync(
        ScanDocxLayoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines ambiguous yellow marks using the placeholder manual (sample data only — never case/DB values).
    /// </summary>
    Task<ScanAmbiguousYellowRefinementResult> RefineAmbiguousYellowMarksAsync(
        ScanAmbiguousYellowRefinementRequest request,
        CancellationToken cancellationToken = default);
}
