#nullable enable

using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

public enum ScanFieldConfidence
{
    High,
    Medium,
    Low,
}

public enum ScanFieldScope
{
    Header,
    Row,
    LoopBoundary,
    Static,
}

public sealed record ScanBoundingBox(double Left, double Top, double Right, double Bottom)
{
    public static ScanBoundingBox FullPage => new(0, 0, 1, 1);

    public ScanBoundingBox Clamp()
    {
        static double C(double v) => Math.Clamp(v, 0, 1);
        var left = C(Math.Min(Left, Right));
        var right = C(Math.Max(Left, Right));
        var top = C(Math.Min(Top, Bottom));
        var bottom = C(Math.Max(Top, Bottom));
        return new ScanBoundingBox(left, top, right, bottom);
    }
}

public sealed record ScanValueHint(string Token, string MaskedValue, string? LabelText);

public sealed class ScanFieldPlanPagePayload
{
    public required int PageIndex { get; init; }

    public required byte[] PngBytes { get; init; }

    public required int WidthPx { get; init; }

    public required int HeightPx { get; init; }
}

public sealed class ScanFieldPlanRequest
{
    public required ScanKind ScanKind { get; init; }

    public required ScanAuthoringPlaybook Playbook { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public required IReadOnlyList<ScanFieldPlanPagePayload> Pages { get; init; }

    public required IReadOnlyList<ScanOcrLine> OcrLines { get; init; }

    public IReadOnlyList<ScanValueHint> ValueHints { get; init; } = Array.Empty<ScanValueHint>();

    public IReadOnlyList<ScanDetectedFieldDraft> DeterministicSeeds { get; init; } = Array.Empty<ScanDetectedFieldDraft>();
}

public sealed class ScanDetectedFieldDraft
{
    public required string FieldId { get; init; }

    public required ScanBoundingBox Box { get; init; }

    public required int PageIndex { get; init; }

    public required string LabelText { get; init; }

    public string? ProposedToken { get; init; }

    public ScanFieldConfidence Confidence { get; init; } = ScanFieldConfidence.Medium;

    public ScanFieldScope Scope { get; init; } = ScanFieldScope.Header;

    /// <summary>OpenXML address for Office yellow path (token writer).</summary>
    public DocumentRegion? SourceRegion { get; init; }

    public IReadOnlyList<ScanTokenAlternative> Alternatives { get; init; } = Array.Empty<ScanTokenAlternative>();

    /// <summary>Excel column header above the yellow cell (manual inference context).</summary>
    public string? ColumnHeader { get; init; }

    /// <summary>Word printed caption before the yellow span (wekil / ýolbaşçy / applicant).</summary>
    public string? NearbyLabel { get; init; }
}

public sealed class ScanStaticRegionDraft
{
    public required string RegionId { get; init; }

    public required int PageIndex { get; init; }

    public required ScanBoundingBox Box { get; init; }

    public required string TextPreview { get; init; }
}

public sealed record ScanGapDraft(string FieldId, string LabelText, string? SuggestedPropertyName);

public sealed record ScanClarificationPrompt(string Question, IReadOnlyList<string> SuggestedAnswers);

public sealed class ScanFieldPlanProposal
{
    public required IReadOnlyList<ScanDetectedFieldDraft> Fields { get; init; }

    public IReadOnlyList<ScanStaticRegionDraft> StaticRegions { get; init; } = Array.Empty<ScanStaticRegionDraft>();

    public IReadOnlyList<ScanGapDraft> Gaps { get; init; } = Array.Empty<ScanGapDraft>();

    public IReadOnlyList<ScanClarificationPrompt> PendingQuestions { get; init; } = Array.Empty<ScanClarificationPrompt>();

    public string? Rationale { get; init; }

    public string Source { get; init; } = "unknown";

    public int YellowHighlightCount { get; init; }
}

public sealed class ScanFieldPlan
{
    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public required ScanKind ScanKind { get; init; }

    public required IReadOnlyList<ScanDetectedField> Fields { get; init; }

    public required IReadOnlyList<ScanStaticRegion> StaticRegions { get; init; }

    public required IReadOnlyList<ScanGap> Gaps { get; init; }

    public required IReadOnlyList<ScanClarificationPrompt> PendingQuestions { get; init; }

    public string? Rationale { get; init; }

    public string Source { get; init; } = "unknown";

    public bool HasMappedFields => Fields.Any(static f => !string.IsNullOrWhiteSpace(f.ProposedToken));

    public int YellowHighlightCount { get; init; }
}

public sealed class ScanDetectedField
{
    public required string FieldId { get; init; }

    public required ScanBoundingBox Box { get; init; }

    public required int PageIndex { get; init; }

    public required string LabelText { get; init; }

    public string? ProposedToken { get; init; }

    public required ScanFieldConfidence Confidence { get; init; }

    public required ScanFieldScope Scope { get; init; }

    /// <summary>OpenXML address for Office yellow path (token writer).</summary>
    public DocumentRegion? SourceRegion { get; init; }

    public IReadOnlyList<ScanTokenAlternative> Alternatives { get; init; } = Array.Empty<ScanTokenAlternative>();

    /// <summary>Compound Review parts (1-based) the officer dismissed. Generate still uses the parent span.</summary>
    public IReadOnlyList<int> HiddenPartIndexes { get; init; } = Array.Empty<int>();
}

public sealed class ScanStaticRegion
{
    public required string RegionId { get; init; }

    public required int PageIndex { get; init; }

    public required ScanBoundingBox Box { get; init; }

    public required string TextPreview { get; init; }
}

public sealed record ScanGap(string FieldId, string LabelText, string? SuggestedPropertyName);

public sealed class ScanFieldPlanBuildRequest
{
    public required ScanIngestResult Ingest { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public ScanKind ScanKind { get; init; } = ScanKind.BlankForm;

    public IReadOnlyList<ScanValueHint> ValueHints { get; init; } = Array.Empty<ScanValueHint>();

    /// <summary>Case instance values for matching yellow cell text (same map as Convert).</summary>
    public IReadOnlyList<ValueCandidate> ValueCandidates { get; init; } = Array.Empty<ValueCandidate>();
}

public sealed class ScanFieldPlanMergeRequest
{
    public required ScanFieldPlanProposal Proposal { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public required ScanKind ScanKind { get; init; }

    public IReadOnlyList<ScanValueHint> ValueHints { get; init; } = Array.Empty<ScanValueHint>();
}

public sealed class ScanClarificationRequest
{
    public required string OfficerMessage { get; init; }

    public required ScanFieldPlan CurrentPlan { get; init; }

    public required ScanAuthoringPlaybook Playbook { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }
}

public sealed class ScanClarificationResult
{
    public required bool Accepted { get; init; }

    public required string ReplyText { get; init; }

    public required ScanFieldPlanProposal Plan { get; init; }
}

public enum ScanClarificationRejectReason
{
    OutOfScopeContentEdit,
    NotUnderstood,
    TokenNotInProfileSet,
    NoMappingChange,
}

public sealed class ScanClarificationTurnRequest
{
    public required string OfficerMessage { get; init; }

    public required ScanFieldPlan CurrentPlan { get; init; }

    public required ScanAuthoringPlaybook Playbook { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public ScanKind ScanKind { get; init; } = ScanKind.BlankForm;

    public IReadOnlyList<ScanValueHint> ValueHints { get; init; } = Array.Empty<ScanValueHint>();
}

public sealed class ScanClarificationTurnResult
{
    public required bool Accepted { get; init; }

    public required string ReplyText { get; init; }

    public ScanClarificationRejectReason? RejectReason { get; init; }

    public required ScanFieldPlan Plan { get; init; }
}

public sealed class ScanDocxLayoutRequest
{
    public required ScanFieldPlan FieldPlan { get; init; }

    public required ScanAuthoringPlaybook Playbook { get; init; }

    /// <summary>Page PNGs for vision layout reconstruction (same pages used for field detection).</summary>
    public IReadOnlyList<ScanPageImage> Pages { get; init; } = Array.Empty<ScanPageImage>();

    public IReadOnlyList<ScanOcrLine> OcrLines { get; init; } = Array.Empty<ScanOcrLine>();

    /// <summary>Masked value hints from the case (filled sample) — replace values with tokens in OCR fallback.</summary>
    public IReadOnlyList<ScanValueHint> ValueHints { get; init; } = Array.Empty<ScanValueHint>();
}

public sealed class ScanDocxLayoutProposal
{
    public required IReadOnlyList<ScanDocxBlock> Blocks { get; init; }

    public string? Rationale { get; init; }
}

public sealed class ScanDocxBlock
{
    /// <summary>
    /// <c>paragraph</c> / <c>static</c> — full line; <c>Text</c> may embed <c>{{ds.*}}</c>.
    /// <c>twoColumn</c> — borderless two-cell row; <c>Text</c>=left, <c>RightText</c>=right (use <c>\n</c> for multi-line cells).
    /// <c>field</c> — label + token (legacy flat list).
    /// <c>loopOpen</c> / <c>loopClose</c> — roster markers.
    /// <c>blank</c> — empty paragraph.
    /// </summary>
    public required string Kind { get; init; }

    public string? Text { get; init; }

    public string? Token { get; init; }

    /// <summary>Optional Word alignment: left, right, center, justify.</summary>
    public string? Align { get; init; }

    /// <summary>Right cell text for <c>twoColumn</c> blocks.</summary>
    public string? RightText { get; init; }

    /// <summary>Right cell alignment for <c>twoColumn</c> (defaults to right).</summary>
    public string? RightAlign { get; init; }

    /// <summary>Run style: normal, italic, bold, boldItalic.</summary>
    public string? Style { get; init; }

    /// <summary>Right cell run style for <c>twoColumn</c>.</summary>
    public string? RightStyle { get; init; }
}
