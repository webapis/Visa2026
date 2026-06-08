namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class NullApplicationRuntimeLogCursorInboxWriter : IApplicationRuntimeLogCursorInboxWriter
{
    public Task TryWriteAsync(Guid id, ApplicationRuntimeLogEntry entry, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
