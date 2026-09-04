#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

public static class ScanFieldPlanRequestBuilder
{
    public static ScanFieldPlanRequest Build(
        ScanFieldPlanBuildRequest request,
        bool redactIdentifiers = true)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Ingest);
        ArgumentNullException.ThrowIfNull(request.PlaceholderSet);

        var ingest = request.Ingest;
        var pages = ingest.Input.Pages
            .Select(p => new ScanFieldPlanPagePayload
            {
                PageIndex = p.PageIndex,
                PngBytes = p.PngBytes,
                WidthPx = p.WidthPx,
                HeightPx = p.HeightPx,
            })
            .ToList();

        var hints = (request.ValueHints ?? Array.Empty<ScanValueHint>())
            .Select(h => new ScanValueHint(
                h.Token,
                TemplateMappingRequestBuilder.MaskPreview(h.MaskedValue, TemplateMappingRequestBuilder.InferKind(h.MaskedValue), redactIdentifiers),
                h.LabelText))
            .ToList();

        var seeds = DeterministicScanFieldPlanner.Build(new ScanFieldPlanRequest
        {
            ScanKind = request.ScanKind,
            Playbook = ingest.Playbook,
            PlaceholderSet = request.PlaceholderSet,
            Pages = pages,
            OcrLines = ingest.Ocr.Lines,
            ValueHints = hints,
        }).Fields;

        return new ScanFieldPlanRequest
        {
            ScanKind = request.ScanKind,
            Playbook = ingest.Playbook,
            PlaceholderSet = request.PlaceholderSet,
            Pages = pages,
            OcrLines = ingest.Ocr.Lines,
            ValueHints = hints,
            DeterministicSeeds = seeds,
        };
    }
}
