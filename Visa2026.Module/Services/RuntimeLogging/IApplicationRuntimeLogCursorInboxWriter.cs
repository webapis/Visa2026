namespace Visa2026.Module.Services.RuntimeLogging;

public interface IApplicationRuntimeLogCursorInboxWriter
{
    Task TryWriteAsync(Guid id, ApplicationRuntimeLogEntry entry, CancellationToken cancellationToken = default);
}
