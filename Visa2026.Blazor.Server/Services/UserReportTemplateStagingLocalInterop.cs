namespace Visa2026.Blazor.Server.Services;

/// <summary>JS interop result from <c>visaTemplateStagingLocal.downloadTemplate</c>.</summary>
public sealed class UserReportTemplateStagingDownloadJsResult
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public string? FileName { get; init; }
}

/// <summary>JS interop payload from <c>visaTemplateStagingLocal.syncFromFilePicker</c>.</summary>
public sealed class UserReportTemplateStagingLocalCollectJsResult
{
    public int ImportedCount { get; init; }

    public int SkippedUnchangedCount { get; init; }

    public int SkippedNotFoundCount { get; init; }

    public int FailedCount { get; init; }

    public bool Cancelled { get; init; }

    public List<UserReportTemplateStagingLocalUploadJsItem> Uploads { get; init; } = new();
}

public sealed class UserReportTemplateStagingLocalUploadJsItem
{
    public Guid TemplateId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public string? FileName { get; init; }

    public string? DisplayName { get; init; }
}
