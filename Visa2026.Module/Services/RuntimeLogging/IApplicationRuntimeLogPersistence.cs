namespace Visa2026.Module.Services.RuntimeLogging;

public interface IApplicationRuntimeLogPersistence
{
    Task<Guid?> PersistAsync(ApplicationRuntimeLogEntry entry, CancellationToken cancellationToken);
}
