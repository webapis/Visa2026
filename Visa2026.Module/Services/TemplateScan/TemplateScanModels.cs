#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

public enum ScanSourceKind
{
    Image,
    Pdf,
    /// <summary>Yellow-marked Word (.docx) — OpenXML highlight/shading, no vision.</summary>
    Word,
    /// <summary>Yellow-marked Excel (.xlsx) — yellow cell fills, no vision.</summary>
    Excel,
}

public enum ScanSuitabilityVerdict
{
    Pass,
    Warn,
    Fail,
}

public enum ScanSuitabilityIssueCode
{
    FileTooLarge,
    TooManyPages,
    ResolutionTooLow,
    SkewExcessive,
    TextConfidenceLow,
    NoTextDetected,
    UnsupportedFormat,
    NoYellowHighlights,
    YellowHighlightsUnmapped,
}

public enum ScanKind
{
    BlankForm,
    FilledSample,
}

public sealed class ScanNormalizeRequest
{
    public required byte[] Content { get; init; }

    public required string FileName { get; init; }

    /// <summary>1-based page numbers to analyze; null = all (capped by options).</summary>
    public IReadOnlyList<int>? SelectedPages { get; init; }
}

public sealed class ScanNormalizedInput
{
    public required ScanSourceKind SourceKind { get; init; }

    public required IReadOnlyList<ScanPageImage> Pages { get; init; }

    public required long OriginalByteLength { get; init; }

    public required string FileName { get; init; }

    /// <summary>Original .docx/.xlsx bytes when <see cref="SourceKind"/> is Word or Excel.</summary>
    public byte[]? OfficePackageBytes { get; init; }

    public bool IsOfficeSource =>
        SourceKind is ScanSourceKind.Word or ScanSourceKind.Excel;
}

public sealed class ScanPageImage
{
    public required int PageIndex { get; init; }

    /// <summary>PNG bytes for vision calls. PDF pages may use a placeholder raster until S2.</summary>
    public required byte[] PngBytes { get; init; }

    public required int WidthPx { get; init; }

    public required int HeightPx { get; init; }
}

public sealed class ScanOcrLine
{
    public required int PageIndex { get; init; }

    public required string Text { get; init; }

    public double Confidence { get; init; } = 1.0;
}

public sealed class ScanOcrRequest
{
    public required ScanNormalizedInput Input { get; init; }

    public required byte[] OriginalContent { get; init; }
}

public sealed class ScanOcrResult
{
    public required IReadOnlyList<ScanOcrLine> Lines { get; init; }

    /// <summary>Aggregate 0..1 confidence for suitability.</summary>
    public required double TextConfidence { get; init; }
}

public sealed class ScanSuitabilityRequest
{
    public required ScanNormalizedInput Input { get; init; }

    public required ScanOcrResult Ocr { get; init; }
}

public sealed class ScanSuitabilityIssue
{
    public required ScanSuitabilityIssueCode Code { get; init; }

    public required string Message { get; init; }

    public int? PageIndex { get; init; }
}

public sealed class ScanSuitabilityReport
{
    public required ScanSuitabilityVerdict Verdict { get; init; }

    public required double TextConfidence { get; init; }

    public required IReadOnlyList<ScanSuitabilityIssue> Issues { get; init; }

    public bool CanContinue => Verdict != ScanSuitabilityVerdict.Fail;
}

public sealed class ScanAuthoringPlaybook
{
    public required string Markdown { get; init; }

    public required string Fingerprint { get; init; }

    public required string VersionLabel { get; init; }
}
