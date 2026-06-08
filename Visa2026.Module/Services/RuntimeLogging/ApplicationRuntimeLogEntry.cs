using System;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogEntry
{
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    public ApplicationRuntimeLogSeverity Severity { get; init; }

    public string? Category { get; init; }

    public string? Message { get; init; }

    public string? ExceptionType { get; init; }

    public string? StackTrace { get; init; }

    public string? ErrorCode { get; init; }

    public string? UserName { get; init; }

    public string? CorrelationId { get; init; }

    public string? RequestPath { get; init; }

    public string? MachineName { get; init; }

    public ApplicationRuntimeLogDeploymentEnvironment DeploymentEnvironment { get; init; }

    public string? ApplicationVersion { get; init; }

    public Guid? RelatedBatchId { get; init; }

    /// <summary>Sentry event id when bridged to Sentry (Phase 4).</summary>
    public string? SentryEventId { get; init; }

    public static ApplicationRuntimeLogSeverity MapSeverity(LogLevel level) => level switch
    {
        LogLevel.Critical => ApplicationRuntimeLogSeverity.Critical,
        LogLevel.Error => ApplicationRuntimeLogSeverity.Error,
        LogLevel.Warning => ApplicationRuntimeLogSeverity.Warning,
        _ => ApplicationRuntimeLogSeverity.Error
    };
}
