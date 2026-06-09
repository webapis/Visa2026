namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogRemotePullResult
{
    public int QueriedCount { get; init; }

    public int WrittenCount { get; init; }

    public int SkippedCount { get; init; }

    public IReadOnlyList<Guid> WrittenIds { get; init; } = Array.Empty<Guid>();

    public DateTime? NewestOccurredAtUtc { get; init; }
}
