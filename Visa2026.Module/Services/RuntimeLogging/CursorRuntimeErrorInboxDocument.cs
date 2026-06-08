using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

/// <summary>JSON payload written to <c>.cursor/runtime-errors/inbox/</c> for Cursor agent triage.</summary>
public sealed class CursorRuntimeErrorInboxDocument
{
    public Guid Id { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public ApplicationRuntimeLogSeverity Severity { get; init; }

    public ApplicationRuntimeLogResolutionStatus ResolutionStatus { get; init; }

    public string? ErrorCode { get; init; }

    public string? Category { get; init; }

    public string? Message { get; init; }

    public string? ExceptionType { get; init; }

    public string? StackTrace { get; init; }

    public string? UserName { get; init; }

    public string? CorrelationId { get; init; }

    public string? RequestPath { get; init; }

    public string? MachineName { get; init; }

    public ApplicationRuntimeLogDeploymentEnvironment DeploymentEnvironment { get; init; }

    public string? ApplicationVersion { get; init; }

    public Guid? RelatedBatchId { get; init; }

    public string? SentryEventId { get; init; }

    public DateTime WrittenAtUtc { get; init; } = DateTime.UtcNow;

    public static CursorRuntimeErrorInboxDocument FromPersisted(
        Guid id,
        ApplicationRuntimeLogEntry entry) =>
        new()
        {
            Id = id,
            OccurredAtUtc = entry.OccurredAtUtc,
            Severity = entry.Severity,
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
            ErrorCode = entry.ErrorCode,
            Category = entry.Category,
            Message = entry.Message,
            ExceptionType = entry.ExceptionType,
            StackTrace = entry.StackTrace,
            UserName = entry.UserName,
            CorrelationId = entry.CorrelationId,
            RequestPath = entry.RequestPath,
            MachineName = entry.MachineName,
            DeploymentEnvironment = entry.DeploymentEnvironment,
            ApplicationVersion = entry.ApplicationVersion,
            RelatedBatchId = entry.RelatedBatchId,
            SentryEventId = entry.SentryEventId
        };
}
