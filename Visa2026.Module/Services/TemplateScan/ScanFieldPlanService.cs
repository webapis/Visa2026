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
    private readonly IScanOfficeYellowExtractor _officeYellow;
    private readonly TemplateAiScanOptions _options;

    public ScanFieldPlanService(
        ITemplateScanAiProvider provider,
        IScanFieldPlanMerger merger,
        IScanOfficeYellowExtractor officeYellow,
        Microsoft.Extensions.Options.IOptions<TemplateAiScanOptions> options)
    {
        _provider = provider;
        _merger = merger;
        _officeYellow = officeYellow;
        _options = options?.Value ?? new TemplateAiScanOptions();
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

        // Provider retained for clarification chat only; field plan is OpenXML yellow (no vision).
        _ = _provider;
        _ = cancellationToken;
        _ = _options;

        var yellows = _officeYellow.Extract(officeBytes, request.Ingest.Input.SourceKind);
        var proposal = ScanOfficeFieldPlanBuilder.Build(yellows, request.PlaceholderSet);

        return _merger.Merge(new ScanFieldPlanMergeRequest
        {
            Proposal = proposal,
            PlaceholderSet = request.PlaceholderSet,
            ScanKind = request.ScanKind,
            ValueHints = request.ValueHints,
        });
    }
}