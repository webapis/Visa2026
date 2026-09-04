#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanIngestService
{
    ScanIngestResult Ingest(ScanNormalizeRequest request);
}

public sealed class ScanIngestResult
{
    public required ScanNormalizedInput Input { get; init; }

    public required ScanOcrResult Ocr { get; init; }

    public required ScanSuitabilityReport Suitability { get; init; }

    public required ScanAuthoringPlaybook Playbook { get; init; }
}

public sealed class ScanIngestService : IScanIngestService
{
    private readonly IScanInputNormalizer _normalizer;
    private readonly IScanOcrExtractor _ocr;
    private readonly IScanSuitabilityEvaluator _suitability;
    private readonly IScanAuthoringPlaybookService _playbook;

    public ScanIngestService(
        IScanInputNormalizer normalizer,
        IScanOcrExtractor ocr,
        IScanSuitabilityEvaluator suitability,
        IScanAuthoringPlaybookService playbook)
    {
        _normalizer = normalizer;
        _ocr = ocr;
        _suitability = suitability;
        _playbook = playbook;
    }

    public ScanIngestResult Ingest(ScanNormalizeRequest request)
    {
        var input = _normalizer.Normalize(request);
        var ocr = _ocr.Extract(new ScanOcrRequest
        {
            Input = input,
            OriginalContent = request.Content,
        });
        var suitability = _suitability.Evaluate(new ScanSuitabilityRequest
        {
            Input = input,
            Ocr = ocr,
        });

        return new ScanIngestResult
        {
            Input = input,
            Ocr = ocr,
            Suitability = suitability,
            Playbook = _playbook.GetPlaybook(),
        };
    }
}
