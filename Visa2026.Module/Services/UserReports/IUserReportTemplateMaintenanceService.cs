namespace Visa2026.Module.Services.UserReports;

public sealed class UserReportTemplateExtractResult
{
    public required bool Success { get; init; }

    public int PlaceholderCount { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed class UserReportTemplateValidateResult
{
    public required bool Success { get; init; }

    public int ValidCount { get; init; }

    public int InvalidCount { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed class UserReportTemplateExtractValidateResult
{
    public required UserReportTemplateExtractResult Extract { get; init; }

    public UserReportTemplateValidateResult? Validate { get; init; }
}

/// <summary>Extract and validate placeholders on <see cref="BusinessObjects.UserReportTemplate"/> (shared by DetailView and staging import).</summary>
public interface IUserReportTemplateMaintenanceService
{
    Task<UserReportTemplateExtractResult> ExtractPlaceholdersAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<UserReportTemplateValidateResult> ValidatePlaceholdersAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<UserReportTemplateExtractValidateResult> ExtractAndValidatePlaceholdersAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);
}
