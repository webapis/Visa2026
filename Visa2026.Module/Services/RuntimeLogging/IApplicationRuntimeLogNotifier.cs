namespace Visa2026.Module.Services.RuntimeLogging;

public interface IApplicationRuntimeLogNotifier
{
    Task NotifyAsync(ApplicationRuntimeLogNotification notification, CancellationToken cancellationToken = default);
}
