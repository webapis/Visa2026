namespace Visa2026.Module.Services.UserReports;

public enum UserReportTemplateStagingImportStatus
{
    Imported = 0,
    SkippedUnchanged = 1,
    SkippedNotFound = 2,
    Failed = 3,
}

public sealed class UserReportTemplateStagingExportResult
{
    public required Guid TemplateId { get; init; }

    public required string DisplayName { get; init; }

    public required string DocumentFileName { get; init; }

    public required string UncPath { get; init; }

    public string? OfficeOpenUrl { get; init; }
}

public sealed class UserReportTemplateStagingImportResult
{
    public required Guid TemplateId { get; init; }

    public required string DisplayName { get; init; }

    public required UserReportTemplateStagingImportStatus Status { get; init; }

    public string? ErrorMessage { get; init; }

    public bool ExtractValidateRan { get; init; }

    public int? InvalidPlaceholderCount { get; init; }
}

public sealed class UserReportTemplateStagingImportAllResult
{
    public required IReadOnlyList<UserReportTemplateStagingImportResult> Results { get; init; }

    public int ImportedCount => Results.Count(r => r.Status == UserReportTemplateStagingImportStatus.Imported);

    public int SkippedUnchangedCount => Results.Count(r => r.Status == UserReportTemplateStagingImportStatus.SkippedUnchanged);

    public int SkippedNotFoundCount => Results.Count(r => r.Status == UserReportTemplateStagingImportStatus.SkippedNotFound);

    public int FailedCount => Results.Count(r => r.Status == UserReportTemplateStagingImportStatus.Failed);
}
