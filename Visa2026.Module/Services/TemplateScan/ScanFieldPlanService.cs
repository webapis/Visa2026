#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanFieldPlanService
{
    Task<ScanFieldPlan> BuildAsync(ScanFieldPlanBuildRequest request, CancellationToken cancellationToken = default);
}

public sealed class ScanFieldPlanService : IScanFieldPlanService
{
    private readonly IScanFieldPlanMerger _merger;
    private readonly IScanOfficeYellowExtractor _officeYellow;
    private readonly IScanAmbiguousYellowRefinementService _refinement;

    public ScanFieldPlanService(
        IScanFieldPlanMerger merger,
        IScanOfficeYellowExtractor officeYellow,
        IScanAmbiguousYellowRefinementService refinement)
    {
        _merger = merger;
        _officeYellow = officeYellow;
        _refinement = refinement;
    }

    public async Task<ScanFieldPlan> BuildAsync(ScanFieldPlanBuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Ingest.Input.IsOfficeSource
            || request.Ingest.Input.OfficePackageBytes is not { Length: > 0 } officeBytes)
        {
            throw new InvalidOperationException(
                "Create from yellow marks accepts only Word (.docx) or Excel (.xlsx) with yellow highlights.");
        }

        var yellows = _officeYellow.Extract(officeBytes, request.Ingest.Input.SourceKind);
        ScanFieldPlanProposal proposal;
        if (yellows.Count > 0)
        {
            proposal = ScanOfficeFieldPlanBuilder.Build(
                yellows,
                request.PlaceholderSet,
                officeBytes,
                request.Ingest.Input.SourceKind,
                request.ValueCandidates);

            proposal = await _refinement.RefineAsync(proposal, request, cancellationToken).ConfigureAwait(false);
            proposal = ScanRepresentativeNameGuard.RewriteProposal(
                proposal,
                request.PlaceholderSet,
                request.ValueCandidates);
        }
        else
        {
            var tokenSpans = ScanOfficeLibraryTokenExtractor.Extract(
                officeBytes,
                request.Ingest.Input.SourceKind,
                request.PlaceholderSet);
            proposal = tokenSpans.Count > 0
                ? ScanOfficeFieldPlanBuilder.BuildFromLibraryTokens(tokenSpans, request.PlaceholderSet)
                : ScanOfficeFieldPlanBuilder.Build(
                    yellows,
                    request.PlaceholderSet,
                    officeBytes,
                    request.Ingest.Input.SourceKind,
                    request.ValueCandidates);
        }

        return _merger.Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = request.PlaceholderSet,
            ScanKind = request.ScanKind,
            ValueHints = request.ValueHints,
        });
    }
}