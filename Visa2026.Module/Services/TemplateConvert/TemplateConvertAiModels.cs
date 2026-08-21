using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// A document span the provider may assign a token to. <see cref="MaskedPreview"/> is the only
/// text it sees - never a raw instance value (E-D1 / L6).
/// </summary>
public sealed record DocumentExtractRegion(
    DocumentRegion Region,
    string MaskedPreview,
    ValueKind? Kind,
    int? RowIndex);

/// <summary>Local matcher decision already taken; the provider may keep, refine, or leave it alone.</summary>
public sealed record DeterministicMatch(
    DocumentRegion Region,
    string Token,
    string ShortCode);

/// <summary>Token the profile allows. Names only - no example values from the case.</summary>
public sealed record AllowedToken(
    string ShortCode,
    string DisplayName,
    UserReportPlaceholderScope Scope);

/// <summary>
/// L6 / L10 by construction: no <c>IObjectSpace</c>, no BO, no raw identifier values.
/// Asserted by Q7 reflection over this type's property graph.
/// </summary>
public sealed class TemplateMappingRequest
{
    public required TemplateSourceFormat Format { get; init; }

    public required IReadOnlyList<DocumentExtractRegion> Regions { get; init; }

    public required IReadOnlyList<AllowedToken> AllowedTokens { get; init; }

    public required string PlaceholderSetFingerprint { get; init; }

    public required IReadOnlyList<DeterministicMatch> PreMatched { get; init; }
}

public sealed record MappingGap(
    string LiteralPreview,
    string? SuggestedPropertyName,
    DocumentRegion Region);

public sealed record TemplateMappingPlan(
    IReadOnlyList<TokenSubstitution> Substitutions,
    IReadOnlyList<LoopMarker> Loops,
    IReadOnlyList<MappingGap> Gaps,
    string? Rationale)
{
    public static TemplateMappingPlan Empty(string? rationale = null) =>
        new(Array.Empty<TokenSubstitution>(), Array.Empty<LoopMarker>(), Array.Empty<MappingGap>(), rationale);

    public static TemplateMappingPlan FromDeterministic(
        IReadOnlyList<DeterministicMatch> matches,
        IReadOnlyList<MappingGap>? gaps = null,
        string? rationale = null) =>
        new(
            matches.Select(static m => new TokenSubstitution(m.Region, m.Token)).ToList(),
            Array.Empty<LoopMarker>(),
            gaps ?? Array.Empty<MappingGap>(),
            rationale);
}

public sealed class TemplateChatTurnRequest
{
    public required string Message { get; init; }

    public required TemplateMappingPlan CurrentPlan { get; init; }

    public required IReadOnlyList<DocumentExtractRegion> Regions { get; init; }

    public required IReadOnlyList<AllowedToken> AllowedTokens { get; init; }

    public required string PlaceholderSetFingerprint { get; init; }
}

public enum ChatRejectReason
{
    OutOfScopeContentEdit,
    TokenNotInProfileSet,
    AmbiguousRegion,
    NotUnderstood,
}

public sealed record TemplateChatTurnResult(
    bool Accepted,
    string ReplyText,
    TemplateMappingPlan? UpdatedPlan,
    ChatRejectReason? RejectReason);