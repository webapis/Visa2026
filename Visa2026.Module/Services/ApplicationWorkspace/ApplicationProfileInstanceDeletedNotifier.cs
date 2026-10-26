using System;

#nullable enable

namespace Visa2026.Module.Services.ApplicationWorkspace;

public interface IApplicationProfileInstanceDeletedNotifier
{
    event Action<Guid>? Deleted;

    void NotifyDeleted(Guid instanceId);
}

public sealed class ApplicationProfileInstanceDeletedNotifier : IApplicationProfileInstanceDeletedNotifier
{
    public event Action<Guid>? Deleted;

    public void NotifyDeleted(Guid instanceId) => Deleted?.Invoke(instanceId);
}
