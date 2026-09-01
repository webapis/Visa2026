#nullable enable

using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

public sealed class ScanAmbiguousYellowRefinementRequest
{
    public required ScanAuthoringPlaybook Playbook { get; init; }

    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public required ScanSourceKind SourceKind { get; init; }

    public required IReadOnlyList<ScanAmbiguousYellowMark> Marks { get; init; }
}

public sealed class ScanAmbiguousYellowMark
{
    public required string FieldId { get; init; }

    public required string YellowText { get; init; }

    public string? ColumnHeader { get; init; }

    /// <summary>Word paragraph (or Excel header + cell) with the yellow span marked <c>&lt;&lt;&lt;…&gt;&gt;&gt;</c>.</summary>
    public string? SurroundingSnippet { get; init; }

    /// <summary>Printed label immediately before the yellow span, or the Excel column header.</summary>
    public string? PrintedLabel { get; init; }

    public string? SheetName { get; init; }

    /// <summary>Excel header-row cells around the yellow column, e.g. <c>A: Familiýasy | B: Ady</c>.</summary>
    public string? HeaderRow { get; init; }

    public ScanFieldScope Scope { get; init; } = ScanFieldScope.Row;

    public string? LocalProposedToken { get; init; }

    public IReadOnlyList<ScanTokenAlternative> LocalCandidates { get; init; } = Array.Empty<ScanTokenAlternative>();
}

public sealed class ScanAmbiguousYellowRefinementResult
{
    public required IReadOnlyList<ScanAmbiguousYellowMarkResult> Marks { get; init; }

    public string? Rationale { get; init; }

    public string Source { get; init; } = "none";
}

public sealed class ScanAmbiguousYellowMarkResult
{
    public required string FieldId { get; init; }

    public string? ProposedToken { get; init; }

    public ScanFieldConfidence Confidence { get; init; } = ScanFieldConfidence.Medium;

    public IReadOnlyList<ScanTokenAlternative> Candidates { get; init; } = Array.Empty<ScanTokenAlternative>();
}
