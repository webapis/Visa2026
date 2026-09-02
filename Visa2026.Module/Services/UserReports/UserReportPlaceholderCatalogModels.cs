namespace Visa2026.Module.Services.UserReports;

using Visa2026.Module.BusinessObjects;

public enum UserReportPlaceholderScope
{
    Header = 0,
    Row = 1,
    Both = 2,
}

/// <summary>
/// Which record a placeholder needs before it can be filled. Declared explicitly per catalog entry
/// because the canonical path is not a reliable signal: <c>Contract_StartDateText</c> reads
/// <c>CurrentVisa.ExpirationDate</c>, and <c>Passport_PersonalNumber</c> falls back to
/// <c>Person.PersonalNumber</c>, so prefix matching would gate both wrongly.
/// </summary>
public enum UserReportPlaceholderPack
{
    /// <summary>Unrecognised <c>packKey</c>. Excluded from profile-scoped sets — never silently allowed.</summary>
    Unknown = 0,

    /// <summary>Application, company, signatory, and Person master data. Always available.</summary>
    Core = 1,

    PersonPassport = 2,
    PersonVisa = 3,
    PersonEducation = 4,
    PersonAddressOfResidence = 5,
    PersonPosition = 6,
    PersonSalary = 7,
    PersonMedical = 8,
    PersonInvitationItem = 9,
    PersonWorkPermitItem = 10,
    PersonBorderZoneItem = 11,
    PersonRejectionItem = 12,
    PersonTravelHistory = 13,
}

public sealed class UserReportPlaceholderCatalogFile
{
    public int Version { get; set; } = 1;

    public List<UserReportPlaceholderCatalogEntryDto> Entries { get; set; } = [];
}

public sealed class UserReportPlaceholderCatalogEntryDto
{
    public string ShortCode { get; set; } = string.Empty;

    public string CanonicalPath { get; set; } = string.Empty;

    /// <summary><c>Header</c>, <c>Row</c>, or <c>Both</c>.</summary>
    public List<string> Scopes { get; set; } = [];

    /// <summary><c>Application</c> and/or <c>ApplicationRosterMergeLine</c>.</summary>
    public List<string> RootBoTypes { get; set; } = [];

    public string ExampleValue { get; set; } = string.Empty;

    public Dictionary<string, string> Labels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsImage { get; set; }

    /// <summary>Name of a <see cref="UserReportPlaceholderPack"/> member. Required for profile-scoped sets.</summary>
    public string PackKey { get; set; } = string.Empty;

    /// <summary>Name of a <see cref="UserReportPlaceholderRelatedBo"/> member. Groups the officer manual and AI payload.</summary>
    public string RelatedBo { get; set; } = string.Empty;
}

public sealed class UserReportPlaceholderCatalogEntry
{
    public required string ShortCode { get; init; }

    public required string CanonicalPath { get; init; }

    public required UserReportPlaceholderScope Scope { get; init; }

    public required IReadOnlyList<UserReportBoType> RootBoTypes { get; init; }

    public required string ExampleValue { get; init; }

    public required string LabelEn { get; init; }

    public required string LabelTk { get; init; }

    public required string LabelRu { get; init; }

    public required string LabelTr { get; init; }

    public bool IsImage { get; init; }

    public UserReportPlaceholderPack Pack { get; init; } = UserReportPlaceholderPack.Unknown;

    public UserReportPlaceholderRelatedBo RelatedBo { get; init; } = UserReportPlaceholderRelatedBo.Unknown;

    public string GetLabel(string? cultureName)
    {
        var culture = (cultureName ?? string.Empty).Trim();
        if (culture.StartsWith("tk", StringComparison.OrdinalIgnoreCase))
            return LabelTk;
        if (culture.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
            return LabelRu;
        if (culture.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
            return LabelTr;
        return LabelEn;
    }

    /// <summary>
    /// Catalog scope wins when the entry is Header-only or Row-only. Word yellow on şahsy-style
    /// letters is classified Header, but Person tokens such as PVFM still belong on the roster line
    /// as <c>{{.PVFM}}</c> — not <c>{{ds.PVFM}}</c> on <see cref="ApplicationProfileInstance"/>.
    /// </summary>
    public UserReportPlaceholderScope EffectiveUsage(UserReportPlaceholderScope usageScope) =>
        Scope == UserReportPlaceholderScope.Both ? usageScope : Scope;

    public string BuildWordToken(UserReportPlaceholderScope usageScope)
    {
        if (IsImage)
        {
            var imageKey = string.IsNullOrWhiteSpace(CanonicalPath) ? ShortCode : CanonicalPath.Trim();
            return $"{{{{IMAGE:{imageKey}}}}}";
        }

        return EffectiveUsage(usageScope) == UserReportPlaceholderScope.Row
            ? $"{{{{.{ShortCode}}}}}"
            : $"{{{{ds.{ShortCode}}}}}";
    }

    public string BuildExcelToken(UserReportPlaceholderScope usageScope) =>
        BuildWordToken(usageScope);
}

public sealed class UserReportPlaceholderManualQuery
{
    public UserReportBoType? RootBoType { get; init; }

    public UserReportPlaceholderScope? Scope { get; init; }

    public UserReportPlaceholderRelatedBo? RelatedBo { get; init; }

    public string? Search { get; init; }
}

public sealed class UserReportPlaceholderCatalogGroup
{
    public required UserReportPlaceholderRelatedBo RelatedBo { get; init; }

    public required IReadOnlyList<UserReportPlaceholderCatalogEntry> Entries { get; init; }
}