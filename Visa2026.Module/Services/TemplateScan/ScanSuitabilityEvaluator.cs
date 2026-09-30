#nullable enable

using Microsoft.Extensions.Options;
using Visa2026.Module.Localization;

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
                Message = VisaUiMessages.Format(
                    "TemplateScan.Issue.FileTooLarge",
                    options.HardMaxUploadBytes / (1024 * 1024)),
            });
        }
        else if (request.Input.OriginalByteLength > options.MaxUploadBytes)
        {
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.FileTooLarge,
                Message = VisaUiMessages.Get("TemplateScan.Issue.FileLarge"),
            });
        }

        var isOffice = request.Input.IsOfficeSource;
        if (!isOffice)
        {
            foreach (var page in request.Input.Pages)
            {
                var minDimension = Math.Min(page.WidthPx, page.HeightPx);
                if (minDimension < suitability.MinPageDimensionPx)
                {
                    issues.Add(new ScanSuitabilityIssue
                    {
                        Code = ScanSuitabilityIssueCode.ResolutionTooLow,
                        Message = VisaUiMessages.Format(
                            "TemplateScan.Issue.Resolution",
                            page.PageIndex + 1,
                            page.WidthPx,
                            page.HeightPx),
                        PageIndex = page.PageIndex,
                    });
                }
            }
        }

        var textConfidence = request.Ocr.TextConfidence;
        var imageDefersTextToVision =
            request.Input.SourceKind == ScanSourceKind.Image && request.Ocr.Lines.Count == 0;

        if (request.Ocr.Lines.Count == 0)
        {
            // Raster uploads have no local OCR by design (ScanOcrExtractor); Azure vision reads the PNG in S2.
            // Office yellow path still needs extractable text (empty Word/Excel fails here).
            if (!imageDefersTextToVision)
            {
                issues.Add(new ScanSuitabilityIssue
                {
                    Code = ScanSuitabilityIssueCode.NoTextDetected,
                    Message = isOffice
                        ? VisaUiMessages.Get("TemplateScan.Issue.NoOfficeText")
                        : VisaUiMessages.Get("TemplateScan.Issue.NoPdfText"),
                });
            }
        }
        else if (textConfidence < suitability.FailBelowTextConfidence)
        {
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.TextConfidenceLow,
                Message = VisaUiMessages.Get("TemplateScan.Issue.ConfidenceLow"),
            });
        }
        else if (textConfidence < suitability.WarnBelowTextConfidence)
        {
            issues.Add(new ScanSuitabilityIssue
            {
                Code = ScanSuitabilityIssueCode.TextConfidenceLow,
                Message = VisaUiMessages.Get("TemplateScan.Issue.ConfidenceModerate"),
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
