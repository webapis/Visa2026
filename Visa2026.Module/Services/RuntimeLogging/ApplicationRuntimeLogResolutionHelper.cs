using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationRuntimeLogResolutionHelper
{
    public static void ApplyStatus(
        ApplicationRuntimeLog row,
        ApplicationRuntimeLogResolutionStatus status,
        DateTime utcNow,
        string? resolvedBy,
        string? resolutionNotes,
        string? fixCommitHash,
        string? agentRunId)
    {
        row.ResolutionStatus = status;

        if (status is ApplicationRuntimeLogResolutionStatus.Acknowledged
            or ApplicationRuntimeLogResolutionStatus.InProgress
            or ApplicationRuntimeLogResolutionStatus.Fixed
            or ApplicationRuntimeLogResolutionStatus.Ignored)
        {
            row.AcknowledgedAtUtc ??= utcNow;
        }

        if (!string.IsNullOrWhiteSpace(resolvedBy))
            row.ResolvedBy = ApplicationRuntimeLogTextHelper.Truncate(resolvedBy, 128) ?? string.Empty;

        if (resolutionNotes != null)
            row.ResolutionNotes = ApplicationRuntimeLogTextHelper.Truncate(resolutionNotes, 4000) ?? string.Empty;

        if (fixCommitHash != null)
            row.FixCommitHash = ApplicationRuntimeLogTextHelper.Truncate(fixCommitHash, 64) ?? string.Empty;

        if (agentRunId != null)
            row.AgentRunId = ApplicationRuntimeLogTextHelper.Truncate(agentRunId, 128) ?? string.Empty;

        if (status is ApplicationRuntimeLogResolutionStatus.Fixed or ApplicationRuntimeLogResolutionStatus.Ignored)
            row.ResolvedAtUtc = utcNow;
    }

    public static ApplicationRuntimeLogResolutionSummary ToSummary(ApplicationRuntimeLog row) =>
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
            AcknowledgedAtUtc = row.AcknowledgedAtUtc,
            ResolvedAtUtc = row.ResolvedAtUtc,
            ResolvedBy = row.ResolvedBy,
            ResolutionNotes = row.ResolutionNotes,
            FixCommitHash = row.FixCommitHash,
            AgentRunId = row.AgentRunId
        };
}
