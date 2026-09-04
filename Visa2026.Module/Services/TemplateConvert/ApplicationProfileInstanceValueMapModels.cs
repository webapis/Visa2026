using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

public enum ValueKind
{
    Text,
    Date,
    Number,
    Identifier,
    PersonName,
}

public enum ValueRejectionReason
{
    /// <summary>Below <see cref="TemplateTextNormalizer.MinimumMatchLength"/> — would highlight noise.</summary>
    TooShort,

    /// <summary>A bare one- or two-digit number matches far too much text.</summary>
    SmallNumber,

    /// <summary>The same literal resolves to more than one token, so it cannot be attributed safely.</summary>
    Ambiguous,
}

/// <summary>
/// One instance value that a document literal may be matched against.
/// <paramref name="MatchKeys"/> holds every comparison form: re-rendered dates, swapped name order,
/// separator-stripped identifiers. Matching compares against all of them.
/// </summary>
public sealed record ValueCandidate(
    string ShortCode,
    string Token,
    string RawValue,
    string NormalizedValue,
    ValueKind Kind,
    int? RowIndex,
    IReadOnlyList<string> MatchKeys);

public sealed record RejectedValue(
    string ShortCode,
    string RawValue,
    ValueKind Kind,
    int? RowIndex,
    ValueRejectionReason Reason);

public sealed class ApplicationProfileInstanceValueMapRequest
{
    public required ApplicationProfileInstance Instance { get; init; }

    /// <summary>From E1. The map never contains a token the profile is not allowed to use.</summary>
    public required ApplicationProfilePlaceholderSet PlaceholderSet { get; init; }

    public ApplicationProfileTemplateDataScope DataScope { get; init; } = ApplicationProfileTemplateDataScope.Both;

    /// <summary>
    /// Roster lines to use. When null they are resolved through
    /// <c>UserReportMergeDataHelper.GetActiveApplicationItems</c>, which needs a persisted instance.
    /// </summary>
    public IReadOnlyList<ApplicationRosterMergeLine>? Rows { get; init; }

    /// <summary>
    /// When true, literals shared by multiple tokens (e.g. <c>TUR</c> on PNAT/PCBC/PFAC) stay in
    /// <see cref="ApplicationProfileInstanceValueMap.Candidates"/> for local disambiguation (yellow-mark scan).
    /// Convert keeps the default false so ambiguous literals are rejected.
    /// </summary>
    public bool RetainAmbiguousLiterals { get; init; }
}

public sealed class ApplicationProfileInstanceValueMap
{
    public required Guid ApplicationProfileInstanceId { get; init; }

    public required IReadOnlyDictionary<string, string?> Header { get; init; }

    public required IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows { get; init; }

    public required IReadOnlyList<ValueCandidate> Candidates { get; init; }

    /// <summary>Recorded separately so the suitability score can be honest about what was skipped.</summary>
    public required IReadOnlyList<RejectedValue> Rejected { get; init; }
}
