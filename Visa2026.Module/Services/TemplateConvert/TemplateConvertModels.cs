#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>Document formats the template converter can rewrite.</summary>
public enum TemplateSourceFormat
{
    Docx,
    Xlsx,
}

/// <summary>Word package part that owns an addressed paragraph.</summary>
public enum WordPart
{
    Body,
    Header,
    Footer,
}

/// <summary>
/// Addresses a span of a document that may be replaced by a placeholder token.
/// Word spans are paragraph-relative offsets over the concatenated <c>w:t</c> text of that paragraph,
/// because a visible phrase is routinely split across several runs.
/// </summary>
public abstract record DocumentRegion
{
    public sealed record WordSpan(string ParagraphAddress, int Start, int Length) : DocumentRegion;

    /// <summary>
    /// An inline / anchored picture in a body paragraph. <paramref name="DrawingIndex"/> is the
    /// ordinal among photo-sized drawings in that paragraph; <paramref name="TextInsertOffset"/>
    /// is the concatenated <c>w:t</c> offset where the token is inserted after the drawing is removed.
    /// </summary>
    public sealed record WordDrawing(string ParagraphAddress, int DrawingIndex, int TextInsertOffset) : DocumentRegion;

    public sealed record ExcelCell(string SheetName, string CellReference) : DocumentRegion;
}

/// <summary>One approved span → token replacement.</summary>
public sealed record TokenSubstitution(DocumentRegion Region, string Token);

/// <summary>
/// Repeating section boundary. <paramref name="CollectionToken"/> is a bare collection path such as
/// <c>ds.rows</c>; the writer emits <c>{{#ds.rows}}</c> and <c>{{/ds.rows}}</c>.
/// </summary>
public sealed record LoopMarker(DocumentRegion Start, DocumentRegion End, string CollectionToken);

public sealed class TemplateTokenWriteRequest
{
    public required byte[] SourceContent { get; init; }

    public required TemplateSourceFormat Format { get; init; }

    public IReadOnlyList<TokenSubstitution> Substitutions { get; init; } = Array.Empty<TokenSubstitution>();

    public IReadOnlyList<LoopMarker> Loops { get; init; } = Array.Empty<LoopMarker>();
}

/// <summary>A requested edit the writer refused to apply. Never a silent drop.</summary>
public sealed record TemplateWriteSkip(DocumentRegion Region, string Token, string Reason);

/// <summary>
/// Result of a write. <see cref="AppliedSubstitutions"/> and <see cref="AppliedLoops"/> are what the
/// diff gate must be given — passing the requested edits instead would flag skipped ones as violations.
/// </summary>
public sealed record TokenWriteResult(
    byte[] Content,
    IReadOnlyList<TokenSubstitution> AppliedSubstitutions,
    IReadOnlyList<LoopMarker> AppliedLoops,
    IReadOnlyList<TemplateWriteSkip> Skipped);

public sealed class TemplateDiffGateRequest
{
    public required byte[] OriginalContent { get; init; }

    public required byte[] ConvertedContent { get; init; }

    public required TemplateSourceFormat Format { get; init; }

    public IReadOnlyList<TokenSubstitution> Substitutions { get; init; } = Array.Empty<TokenSubstitution>();

    public IReadOnlyList<LoopMarker> Loops { get; init; } = Array.Empty<LoopMarker>();
}

public sealed record DiffGateResult(bool Passed, IReadOnlyList<string> Violations)
{
    public static DiffGateResult Pass() => new(true, Array.Empty<string>());

    public static DiffGateResult Fail(IReadOnlyList<string> violations) => new(false, violations);
}

public enum ResidualProbeKind
{
    Text,
    Identifier,
}

/// <summary>A filled-sample value that must not survive into the saved template.</summary>
public sealed record ResidualValueProbe(string Value, string Label, ResidualProbeKind Kind = ResidualProbeKind.Text);

public sealed record ResidualValueHit(string Label, string Value, string LocationHint);

public sealed record ResidualValueScanResult(bool IsClean, IReadOnlyList<ResidualValueHit> Hits)
{
    public static ResidualValueScanResult Clean() => new(true, Array.Empty<ResidualValueHit>());
}
