using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Carries the <see cref="ApplicationProfile"/> itself rather than its id: every caller (wizard,
/// convert modal) already holds the profile, and taking the BO keeps the service free of an
/// <c>IObjectSpace</c> dependency and unit-testable.
/// </summary>
public sealed class ApplicationProfilePlaceholderSetQuery
{
    public required ApplicationProfile Profile { get; init; }

    public required ApplicationProfileTemplateDataScope DataScope { get; init; }

    public ApplicationProfileTemplateKind TemplateKind { get; init; } = ApplicationProfileTemplateKind.Word;
}

public enum PlaceholderExclusionReason
{
    OutOfDataScope,
    PersonPackDisabled,
    StructuralUnsupportedForKind,
    UnknownPack,
}

public sealed record PlaceholderExclusion(string ShortCode, PlaceholderExclusionReason Reason);

public sealed class ApplicationProfilePlaceholderSet
{
    public required Guid ApplicationProfileId { get; init; }

    /// <summary>Echoed from the query so downstream services (validation, writer) need only the set.</summary>
    public required ApplicationProfileTemplateDataScope DataScope { get; init; }

    /// <inheritdoc cref="DataScope"/>
    public required ApplicationProfileTemplateKind TemplateKind { get; init; }

    public required IReadOnlyList<UserReportPlaceholderCatalogEntry> Allowed { get; init; }

    /// <summary>Returned, not swallowed — the officer gap explanation and the developer gap packet both need the reason.</summary>
    public required IReadOnlyList<PlaceholderExclusion> Excluded { get; init; }

    /// <summary>SHA-256 of the sorted allowed short codes. Audit trail and provider cache key.</summary>
    public required string Fingerprint { get; init; }

    /// <summary>Accepts a bare short code or a full token (<c>{{ds.PFN}}</c>, <c>{{.PFN}}</c>, <c>{{IMAGE:PPH}}</c>).</summary>
    public bool Contains(string token)
    {
        if (!TemplateTokenSyntax.TryGetShortCode(token, out var shortCode))
            return false;

        return Allowed.Any(e => string.Equals(e.ShortCode, shortCode, StringComparison.OrdinalIgnoreCase));
    }
}
