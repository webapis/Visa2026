using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogResolutionSummary
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

    public DateTime? AcknowledgedAtUtc { get; init; }

    public DateTime? ResolvedAtUtc { get; init; }

    public string? ResolvedBy { get; init; }

    public string? ResolutionNotes { get; init; }

    public string? FixCommitHash { get; init; }

    public string? AgentRunId { get; init; }
}
