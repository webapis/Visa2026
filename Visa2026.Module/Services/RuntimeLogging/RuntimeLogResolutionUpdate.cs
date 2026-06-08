using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class RuntimeLogResolutionUpdate
{
    public required Guid Id { get; init; }

    public required ApplicationRuntimeLogResolutionStatus Status { get; init; }

    public string? ResolvedBy { get; init; }

    public string? ResolutionNotes { get; init; }

    public string? FixCommitHash { get; init; }

    public string? AgentRunId { get; init; }
}
