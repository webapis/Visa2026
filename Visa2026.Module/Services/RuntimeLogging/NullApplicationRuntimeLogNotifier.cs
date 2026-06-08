namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class NullApplicationRuntimeLogNotifier : IApplicationRuntimeLogNotifier
{
    public Task NotifyAsync(ApplicationRuntimeLogNotification notification, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
