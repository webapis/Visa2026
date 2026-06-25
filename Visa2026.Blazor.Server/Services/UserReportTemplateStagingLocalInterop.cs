namespace Visa2026.Blazor.Server.Services;

/// <summary>JS interop result from <c>visaTemplateStagingLocal.exportDocument</c>.</summary>
public sealed class UserReportTemplateStagingLocalExportJsResult
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public string? FolderName { get; init; }

    public string? FileName { get; init; }

    public bool Opened { get; init; }

    public bool NeedsPathHint { get; init; }
}

/// <summary>JS interop result from <c>visaTemplateStagingLocal.chooseFolder</c>.</summary>
public sealed class UserReportTemplateStagingChooseFolderJsResult
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public string? FolderName { get; init; }
}

/// <summary>JS interop payload from <c>visaTemplateStagingLocal.collectChangedUploads</c>.</summary>
public sealed class UserReportTemplateStagingLocalCollectJsResult
{
    public int ImportedCount { get; init; }

    public int SkippedUnchangedCount { get; init; }

    public int SkippedNotFoundCount { get; init; }

    public int FailedCount { get; init; }

    public List<UserReportTemplateStagingLocalUploadJsItem> Uploads { get; init; } = new();
}

public sealed class UserReportTemplateStagingLocalUploadJsItem
{
    public Guid TemplateId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public string? FileName { get; init; }

    public string? FileBase64 { get; init; }

    public string? ContentHash { get; init; }
}
