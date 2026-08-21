using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Product spec §6.1 splits validation into a blocking tier and a tier the officer may acknowledge
/// and proceed past (E-D2 checkbox). <see cref="PlaceholderValidationResult"/> carries no severity,
/// so the tier lives here.
/// </summary>
public enum TemplateValidationSeverity
{
    Error,
    Warning,
}

/// <summary>Machine-readable companion to <see cref="TemplateValidationIssue.Message"/> for UI copy and tests.</summary>
public enum TemplateValidationIssueCode
{
    /// <summary>Bytes are not a readable Word/Excel package.</summary>
    UnreadableDocument,

    /// <summary>Readable, but no <c>{{token}}</c> survived the write — nothing to approve.</summary>
    NoTokensFound,

    /// <summary>Token is not in the profile's allowed set and not a known exclusion (L10).</summary>
    UnknownToken,

    /// <summary>Token exists but the profile does not collect that person pack, so it renders blank.</summary>
    PackDisabledToken,

    /// <summary>Image token in an Excel template — the Excel generator cannot inject images.</summary>
    UnsupportedImageToken,

    /// <summary>Row token in a header-only template (or the reverse) — nothing will ever bind it.</summary>
    OutOfDataScopeToken,

    /// <summary>A <c>{{#loop}}</c> without its <c>{{/loop}}</c>, or a close with no open.</summary>
    BrokenLoop,

    /// <summary>Token is in the allowed set but does not resolve on the merge root.</summary>
    UnresolvedOnBoType,
}

public sealed record TemplateValidationIssue(
    string Message,
    TemplateValidationSeverity Severity,
    TemplateValidationIssueCode Code,
    string? Token);

public sealed record TemplateValidationReport(
    IReadOnlyList<string> Tokens,
    IReadOnlyList<PlaceholderValidationResult> Results,
    IReadOnlyList<TemplateValidationIssue> Issues,
    bool HasHardFailure)
{
    /// <summary>Drives the E-D2 "I understand warnings" checkbox: shown only when there is something to acknowledge.</summary>
    public bool HasWarnings => Issues.Any(static i => i.Severity == TemplateValidationSeverity.Warning);
}
