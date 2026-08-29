#nullable enable

using Microsoft.Extensions.Options;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanSuitabilityEvaluator
{
    ScanSuitabilityReport Evaluate(ScanSuitabilityRequest request);
}

public sealed class ScanSuitabilityEvaluator : IScanSuitabilityEvaluator
{
    private readonly IOptions<TemplateAiScanOptions> _featureOptions;

    public ScanSuitabilityEvaluator(IOptions<TemplateAiScanOptions> featureOptions)
    {
        _featureOptions = featureOptions;
    }

    public ScanSuitabilityReport Evaluate(ScanSuitabilityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = _featureOptions.Value;
        var suitability = options.Suitability;
        var issues = new List<ScanSuitabilityIssue>();

        var hardTooLarge = request.Input.OriginalByteLength > options.HardMaxUploadBytes;

        if (hardTooLarge)
        {
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.FileTooLarge,
                Message = $"The file exceeds the maximum size of {options.HardMaxUploadBytes / (1024 * 1024)} MB.",
            });
        }
        else if (request.Input.OriginalByteLength > options.MaxUploadBytes)
        {
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.FileTooLarge,
                Message = "The file is larger than the recommended upload size. Continue only if the scan is high quality.",
            });
        }

        foreach (var page in request.Input.Pages)
        {
            var minDimension = Math.Min(page.WidthPx, page.HeightPx);
            if (minDimension < suitability.MinPageDimensionPx)
            {
                issues.Add(new ScanSuitabilityIssue
                {
                    Code = ScanSuitabilityIssueCode.ResolutionTooLow,
                    Message = $"Page {page.PageIndex + 1} resolution ({page.WidthPx}×{page.HeightPx}) is too low for reliable field detection.",
                    PageIndex = page.PageIndex,
                });
            }
        }

        var textConfidence = request.Ocr.TextConfidence;
        var imageDefersTextToVision =
            request.Input.SourceKind == ScanSourceKind.Image && request.Ocr.Lines.Count == 0;

        if (request.Ocr.Lines.Count == 0)
        {
            // Raster uploads have no local OCR by design (ScanOcrExtractor); Azure vision reads the PNG in S2.
            if (!imageDefersTextToVision)
            {
                issues.Add(new ScanSuitabilityIssue
                {
                    Code = ScanSuitabilityIssueCode.NoTextDetected,
                    Message = "No extractable text was found in the PDF. Use a searchable PDF or upload a PNG/JPG scan.",
                });
            }
        }
        else if (textConfidence < suitability.FailBelowTextConfidence)
        {
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.TextConfidenceLow,
                Message = "Text recognition confidence is too low to propose placeholders reliably.",
            });
        }
        else if (textConfidence < suitability.WarnBelowTextConfidence)
        {
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.TextConfidenceLow,
                Message = "Text recognition confidence is moderate — review detected fields carefully.",
            });
        }

        var verdict = ResolveVerdict(
            issues,
            imageDefersTextToVision ? 1.0 : textConfidence,
            suitability,
            hardTooLarge);
        return new ScanSuitabilityReport
        {
            Verdict = verdict,
            TextConfidence = textConfidence,
            Issues = issues,
        };
    }

    internal static ScanSuitabilityVerdict ResolveVerdict(
        IReadOnlyList<ScanSuitabilityIssue> issues,
        double textConfidence,
        ScanSuitabilityOptions suitability,
        bool hardFileTooLarge)
    {
        if (hardFileTooLarge)
            return ScanSuitabilityVerdict.Fail;

        if (issues.Any(static i => i.Code == ScanSuitabilityIssueCode.ResolutionTooLow))
            return ScanSuitabilityVerdict.Fail;

        if (issues.Any(static i => i.Code == ScanSuitabilityIssueCode.NoTextDetected))
            return ScanSuitabilityVerdict.Fail;

        if (issues.Any(static i => i.Code == ScanSuitabilityIssueCode.NoYellowHighlights
            || i.Code == ScanSuitabilityIssueCode.YellowHighlightsUnmapped))
            return ScanSuitabilityVerdict.Fail;

        if (textConfidence < suitability.FailBelowTextConfidence)
            return ScanSuitabilityVerdict.Fail;

        if (issues.Any(static i => i.Code == ScanSuitabilityIssueCode.TextConfidenceLow)
            || issues.Any(static i => i.Code == ScanSuitabilityIssueCode.FileTooLarge))
            return ScanSuitabilityVerdict.Warn;

        if (textConfidence < suitability.WarnBelowTextConfidence)
            return ScanSuitabilityVerdict.Warn;

        return ScanSuitabilityVerdict.Pass;
    }
}
