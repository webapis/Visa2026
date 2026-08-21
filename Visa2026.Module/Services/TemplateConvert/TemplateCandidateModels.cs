#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

public enum SuitabilityLevel
{
    Fail = 0,
    Warn = 1,
    Pass = 2,
}

public enum SuitabilityReasonCode
{
    Unreadable,
    NoExtractableText,
    NoInstanceMatches,
    TooFewHeaderMatches,
    HeaderMatchesBelowPass,
    RosterLoopDetected,
    StrongHeaderCoverage,
    AlreadyTokenized,
    GapsPresent,
}

public sealed record SuitabilityReason(SuitabilityReasonCode Code, string Message);

public enum HighlightKind
{
    /// <summary>Matches instance data and resolves to a token in the profile set — will be replaced.</summary>
    Match,

    /// <summary>Looks like variable data but has no token in the set. Feeds "Needs help"; never written as a token.</summary>
    Gap,
}

public sealed record HighlightRegion(
    DocumentRegion Region,
    HighlightKind Kind,
    string MatchedText,
    string? Token,
    string? ShortCode,
    int? RowIndex);

public sealed class TemplateCandidateRequest
{
    public required byte[] Content { get; init; }

    public required TemplateSourceFormat Format { get; init; }

    public required ApplicationProfileInstanceValueMap ValueMap { get; init; }
}

public sealed class TemplateCandidateReport
{
    public required SuitabilityLevel Level { get; init; }

    public required IReadOnlyList<SuitabilityReason> Reasons { get; init; }

    public required IReadOnlyList<HighlightRegion> Highlights { get; init; }

    /// <summary>Distinct header tokens matched. The primary suitability input (E-D6).</summary>
    public required int DistinctHeaderMatches { get; init; }

    public required int DistinctRowMatches { get; init; }

    public required int GapCount { get; init; }

    /// <summary>Row matches spread over two or more roster rows, so the table can carry a loop.</summary>
    public required bool RosterLoopDetected { get; init; }

    public bool CanConvert => Level != SuitabilityLevel.Fail;

    /// <summary>Warn requires the officer's "Continue with warnings" checkbox before Convert.</summary>
    public bool RequiresWarningAcknowledgement => Level == SuitabilityLevel.Warn;
}

/// <summary>
/// E-D6 thresholds. Bound to <c>TemplateAiConvert:Suitability</c> so a pilot can retune them without
/// a redeploy — deliberately not constants.
/// </summary>
public sealed class TemplateSuitabilityOptions
{
    public const string SectionName = "TemplateAiConvert:Suitability";

    /// <summary>Below this many distinct header matches, and with no roster loop, the upload fails.</summary>
    public int MinHeaderMatchesToProceed { get; set; } = 3;

    /// <summary>At or above this many distinct header matches the upload passes outright.</summary>
    public int MinHeaderMatchesForPass { get; set; } = 6;

    /// <summary>With a roster loop present, this many header matches is enough to pass.</summary>
    public int MinHeaderMatchesWithRosterLoop { get; set; } = 2;
}
