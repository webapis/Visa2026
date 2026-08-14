namespace Visa2026.Module.Services.UserReports;

using Visa2026.Module.BusinessObjects;

public enum UserReportPlaceholderScope
{
    Header = 0,
    Row = 1,
    Both = 2,
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

    public string BuildWordToken(UserReportPlaceholderScope usageScope)
    {
        if (IsImage)
            return $"{{{{IMAGE:{ShortCode}}}}}";

        return usageScope == UserReportPlaceholderScope.Row
            ? $"{{{{.{ShortCode}}}}}"
            : $"{{{{ds.{ShortCode}}}}}";
    }

    public string BuildExcelToken(UserReportPlaceholderScope usageScope) =>
        usageScope == UserReportPlaceholderScope.Row
            ? $"{{{{.{ShortCode}}}}}"
            : $"{{{{ds.{ShortCode}}}}}";
}

public sealed class UserReportPlaceholderManualQuery
{
    public UserReportBoType? RootBoType { get; init; }

    public UserReportPlaceholderScope? Scope { get; init; }

    public string? Search { get; init; }
}