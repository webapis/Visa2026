using Microsoft.Extensions.Options;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanSuitabilityEvaluatorTests
{
    private static ScanSuitabilityEvaluator CreateEvaluator() =>
        new(Options.Create(new TemplateAiScanOptions
        {
            MaxUploadBytes = 20_971_520,
            HardMaxUploadBytes = 52_428_800,
            Suitability = new ScanSuitabilityOptions
            {
                FailBelowTextConfidence = 0.40,
                WarnBelowTextConfidence = 0.70,
                MinPageDimensionPx = 600,
            },
        }));

    [Fact]
    public void Evaluate_LowResolutionImage_Fails()
    {
        var evaluator = CreateEvaluator();
        var input = new ScanNormalizedInput
        {
            SourceKind = ScanSourceKind.Image,
            OriginalByteLength = 1024,
            FileName = "small.png",
            Pages =
            [
                new ScanPageImage
                {
                    PageIndex = 0,
                    PngBytes = Array.Empty<byte>(),
                    WidthPx = 50,
                    HeightPx = 50,
                },
            ],
        };

        var report = evaluator.Evaluate(new ScanSuitabilityRequest
        {
            Input = input,
            Ocr = new ScanOcrResult { Lines = Array.Empty<ScanOcrLine>(), TextConfidence = 0 },
        });

        Assert.Equal(ScanSuitabilityVerdict.Fail, report.Verdict);
        Assert.Contains(report.Issues, i => i.Code == ScanSuitabilityIssueCode.ResolutionTooLow);
    }

    [Fact]
    public void Evaluate_ClearImageWithoutLocalOcr_Passes_ForVision()
    {
        var evaluator = CreateEvaluator();
        var input = new ScanNormalizedInput
        {
            SourceKind = ScanSourceKind.Image,
            OriginalByteLength = 150_000,
            FileName = "letter.png",
            Pages =
            [
                new ScanPageImage
                {
                    PageIndex = 0,
                    PngBytes = Array.Empty<byte>(),
                    WidthPx = 1200,
                    HeightPx = 1600,
                },
            ],
        };

        var report = evaluator.Evaluate(new ScanSuitabilityRequest
        {
            Input = input,
            Ocr = new ScanOcrResult { Lines = Array.Empty<ScanOcrLine>(), TextConfidence = 0 },
        });

        Assert.Equal(ScanSuitabilityVerdict.Pass, report.Verdict);
        Assert.DoesNotContain(report.Issues, i => i.Code == ScanSuitabilityIssueCode.NoTextDetected);
    }

    [Fact]
    public void Evaluate_PdfWithNoText_Fails()
    {
        var evaluator = CreateEvaluator();
        var input = new ScanNormalizedInput
        {
            SourceKind = ScanSourceKind.Pdf,
            OriginalByteLength = 2048,
            FileName = "blank.pdf",
            Pages =
            [
                new ScanPageImage
                {
                    PageIndex = 0,
                    PngBytes = Array.Empty<byte>(),
                    WidthPx = 800,
                    HeightPx = 1200,
                },
            ],
        };

        var report = evaluator.Evaluate(new ScanSuitabilityRequest
        {
            Input = input,
            Ocr = new ScanOcrResult { Lines = Array.Empty<ScanOcrLine>(), TextConfidence = 0 },
        });

        Assert.Equal(ScanSuitabilityVerdict.Fail, report.Verdict);
        Assert.Contains(report.Issues, i => i.Code == ScanSuitabilityIssueCode.NoTextDetected);
    }

    [Fact]
    public void Evaluate_ModerateTextConfidence_Warns()
    {
        var evaluator = CreateEvaluator();
        var input = new ScanNormalizedInput
        {
            SourceKind = ScanSourceKind.Pdf,
            OriginalByteLength = 1024,
            FileName = "form.pdf",
            Pages =
            [
                new ScanPageImage
                {
                    PageIndex = 0,
                    PngBytes = Array.Empty<byte>(),
                    WidthPx = 800,
                    HeightPx = 1200,
                },
            ],
        };

        var report = evaluator.Evaluate(new ScanSuitabilityRequest
        {
            Input = input,
            Ocr = new ScanOcrResult
            {
                Lines = [new ScanOcrLine { PageIndex = 0, Text = "Arza", Confidence = 0.5 }],
                TextConfidence = 0.55,
            },
        });

        Assert.Equal(ScanSuitabilityVerdict.Warn, report.Verdict);
    }

    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(400, 8, 1.0)]
    [InlineData(100, 2, 0.25)]
    public void OcrConfidence_ComputesFromCharsAndLines(int chars, int lines, double expected)
    {
        var confidence = ScanOcrExtractor.ComputeConfidence(chars, lines);
        Assert.Equal(expected, confidence, 3);
    }
}
