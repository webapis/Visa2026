using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationErrorReport
{
    public required string ErrorCode { get; init; }

    public required string Message { get; init; }

    public string? Category { get; init; }

    public ApplicationRuntimeLogSeverity Severity { get; init; } = ApplicationRuntimeLogSeverity.Error;

    public Exception? Exception { get; init; }

    public Guid? RelatedBatchId { get; init; }

    public string? RequestPath { get; init; }
}
