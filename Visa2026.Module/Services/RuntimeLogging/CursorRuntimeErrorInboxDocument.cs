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

    /// <summary>IIS slot or source label when pulled from a remote host (e.g. Production).</summary>
    public string? SourceSlot { get; init; }

    /// <summary>SQL database name when pulled from a remote host.</summary>
    public string? SourceDatabase { get; init; }

    public static CursorRuntimeErrorInboxDocument FromPersisted(
        Guid id,
        ApplicationRuntimeLogEntry entry,
        string? sourceSlot = null,
        string? sourceDatabase = null) =>
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
            SentryEventId = entry.SentryEventId,
            SourceSlot = sourceSlot,
            SourceDatabase = sourceDatabase
        };

    public static CursorRuntimeErrorInboxDocument FromRow(
        ApplicationRuntimeLog row,
        string? sourceSlot = null,
        string? sourceDatabase = null) =>
        new()
        {
            Id = row.ID,
            OccurredAtUtc = row.OccurredAtUtc,
            Severity = row.Severity,
            ResolutionStatus = row.ResolutionStatus,
            ErrorCode = row.ErrorCode,
            Category = row.Category,
            Message = row.Message,
            ExceptionType = row.ExceptionType,
            StackTrace = row.StackTrace,
            UserName = row.UserName,
            CorrelationId = row.CorrelationId,
            RequestPath = row.RequestPath,
            MachineName = row.MachineName,
            DeploymentEnvironment = row.DeploymentEnvironment,
            ApplicationVersion = row.ApplicationVersion,
            RelatedBatchId = row.RelatedBatchId,
            SentryEventId = row.SentryEventId,
            SourceSlot = sourceSlot,
            SourceDatabase = sourceDatabase
        };
}
