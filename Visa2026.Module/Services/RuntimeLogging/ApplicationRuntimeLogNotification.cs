using System;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogNotification
{
    public Guid Id { get; init; }

    public DateTime OccurredAtUtc { get; init; }

    public ApplicationRuntimeLogSeverity Severity { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }

    public string? CorrelationId { get; init; }

    public string? Category { get; init; }

    public static ApplicationRuntimeLogNotification FromPersisted(Guid id, ApplicationRuntimeLogEntry entry) =>
        new()
        {
            Id = id,
            OccurredAtUtc = entry.OccurredAtUtc,
            Severity = entry.Severity,
            ErrorCode = entry.ErrorCode,
            Message = entry.Message,
            CorrelationId = entry.CorrelationId,
            Category = entry.Category
        };
}
